using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;
using NexAlbum.Export.Pdf;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Kapak (Karar 03 Rev. 1) ve görüntü çıktısı (Karar 11 §4, §7.1). Kapak yaprağı (arka + sırt + ön) profildeki ölçülerle kurulur;
/// tuvalde iç sayfalar gibi düzenlenir (fotoğraf, metin, çerçeve, fon) ve PDF'te ayrı dosya olarak çıkar.
/// </summary>
public sealed partial class MainViewModel
{
    private PageItemViewModel? coverPage;

    public RelayCommand CreateCoverCommand { get; private set; } = null!;

    public RelayCommand RemoveCoverCommand { get; private set; } = null!;

    public RelayCommand ShowCoverCommand { get; private set; } = null!;

    public RelayCommand ExportImagesCommand { get; private set; } = null!;

    /// <summary>Kapak sayfasının görünüm modeli; kapak yoksa ya da profil kuramıyorsa null.</summary>
    public PageItemViewModel? CoverPage
    {
        get => coverPage;
        private set
        {
            if (SetProperty(ref coverPage, value))
            {
                OnPropertyChanged(nameof(HasCover));
                OnPropertyChanged(nameof(HasNoCover));
            }
        }
    }

    public bool HasCover => coverPage is not null;

    public bool HasNoCover => !HasCover;

    /// <summary>Tuvalde kapak mı düzenleniyor?</summary>
    public bool IsCoverSelected => SelectedPage?.IsCover == true;

    /// <summary>Tek sayfa tuvali görünsün mü (kitap görünümü kapalı ya da kapak seçili).</summary>
    public bool ShowSingleCanvas => !IsBookView || IsCoverSelected;

    public bool ShowBookCanvas => IsBookView && !IsCoverSelected;

    /// <summary>Sayfa şeridinin seçimi: kapak seçiliyken şeritte seçim yoktur; şeritten null gelirse kapak bırakılmaz.</summary>
    public PageItemViewModel? StripSelectedPage
    {
        get => IsCoverSelected ? null : SelectedPage;
        set
        {
            if (value is not null)
            {
                SelectedPage = value;
            }
        }
    }

    /// <summary>"Kapak: 616 × 300 mm · arka 300 + sırt 16 + ön 300" ya da kapak kurulamıyorsa nedeni.</summary>
    public string CoverInfo
    {
        get
        {
            if (project is null)
            {
                return string.Empty;
            }

            var geometry = CoverLayout.Geometry(project.Profile, out var issues);
            return geometry is null
                ? string.Join(' ', issues.Where(i => i.Severity == ValidationSeverity.Error).Select(i => i.Message))
                : "Kapak: " + CoverLayout.Describe(geometry);
        }
    }

    /// <summary>Profildeki ölçülerle kapak yaprağını kurar (varsa yeniden oluşturur; geri alınabilir).</summary>
    public void CreateCover()
    {
        if (project is null)
        {
            return;
        }

        var geometry = CoverLayout.Geometry(project.Profile, out var issues);
        if (geometry is null)
        {
            var reason = string.Join(' ', issues.Where(i => i.Severity == ValidationSeverity.Error).Select(i => i.Message));
            Status = "Kapak oluşturulamadı: " + reason;
            dialogs.Info("Kapak için profilde kapak tipi, ön kapak ölçüsü ve sırt genişliği gerekir. Bu değerleri baskı evinizden alın.\n\n" + reason);
            return;
        }

        var title = project.Cover?.Elements.OfType<TextElement>().FirstOrDefault()?.Content;
        var cover = CoverLayout.Create(project, geometry, clock.GetUtcNow(), title);
        UpdateProject((project with { Cover = cover }).Touch(clock.GetUtcNow()), project.Cover is null ? "Kapak oluştur" : "Kapağı yeniden oluştur");
        SelectedPage = CoverPage;
        Status = "Kapak hazır: " + CoverLayout.Describe(geometry) + ". Fotoğraf ve yazıyı iç sayfalar gibi değiştirebilirsiniz.";
    }

    public void RemoveCover()
    {
        if (project?.Cover is null)
        {
            return;
        }

        UpdateProject((project with { Cover = null }).Touch(clock.GetUtcNow()), "Kapağı kaldır");
        SelectedPage ??= Pages.FirstOrDefault();
        Status = "Kapak kaldırıldı.";
    }

    /// <summary>Sayfaları (ve kapağı) JPG/PNG önizleme olarak kaydeder.</summary>
    public void ExportImages()
    {
        if (project is null)
        {
            return;
        }

        var path = dialogs.AskImagePath(project.Name + ".jpg");
        if (path is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = PageImageExporter.Export(project, CoverPage?.Cover, path, PageImageExporter.DefaultDpi, project.Settings.Export.JpegQuality);
            Status = string.Format(CultureInfo.CurrentCulture, "{0} görüntü kaydedildi ({1} DPI). {2}", result.Files.Count, PageImageExporter.DefaultDpi, PageImageExporter.PreviewNotice);
            if (result.Files.Count > 0)
            {
                dialogs.Reveal(result.Files[0]);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        {
            dialogs.Warn("Görüntüler kaydedilemedi: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Kapak varsa ve profil kurabiliyorsa PDF'in yanına "ad_kapak.pdf" yazılır.</summary>
    private static PdfExportResult? ExportCoverPdf(AlbumProject current, string albumPath, PdfExportSettings settings)
    {
        if (current.Cover is null || CoverLayout.Geometry(current.Profile, out _) is not { } geometry)
        {
            return null;
        }

        return PdfAlbumExporter.ExportCover(current, geometry, PdfAlbumExporter.CoverPath(albumPath), settings);
    }

    /// <summary>Ön kontrol: iç sayfalar + kapak.</summary>
    private ValidationResult CheckProject(AlbumProject current) =>
        ProjectPreflight.Check(current, new PreflightEnvironment { FontAvailable = FontCatalog.IsInstalled })
            .Merge(new ValidationResult(CoverLayout.Check(current)));

    /// <summary>Kapak görünüm modeli projeden yeniden kurulur (değişmediyse korunur).</summary>
    private void RebuildCover(AlbumProject value, bool reuse)
    {
        if (value.Cover is not { } cover || CoverLayout.Geometry(value.Profile, out _) is not { } geometry)
        {
            CoverPage = null;
            return;
        }

        if (reuse && coverPage is not null && ReferenceEquals(coverPage.Page, cover))
        {
            return;
        }

        CoverPage = new PageItemViewModel(cover, -1, value, geometry);
    }

    private void InitializeCover()
    {
        CreateCoverCommand = new RelayCommand(CreateCover, () => HasProject && !IsBusy);
        RemoveCoverCommand = new RelayCommand(RemoveCover, () => project?.Cover is not null && !IsBusy);
        ShowCoverCommand = new RelayCommand(() => SelectedPage = CoverPage, () => CoverPage is not null);
        ExportImagesCommand = new RelayCommand(ExportImages, () => HasProject && project!.Pages.Count > 0 && !IsBusy);
    }

    private void RefreshCoverCommands()
    {
        CreateCoverCommand?.NotifyCanExecuteChanged();
        RemoveCoverCommand?.NotifyCanExecuteChanged();
        ShowCoverCommand?.NotifyCanExecuteChanged();
        ExportImagesCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CoverInfo));
        OnCoverSelectionChanged();
    }

    private void OnCoverSelectionChanged()
    {
        OnPropertyChanged(nameof(IsCoverSelected));
        OnPropertyChanged(nameof(ShowSingleCanvas));
        OnPropertyChanged(nameof(ShowBookCanvas));
        OnPropertyChanged(nameof(StripSelectedPage));
    }
}
