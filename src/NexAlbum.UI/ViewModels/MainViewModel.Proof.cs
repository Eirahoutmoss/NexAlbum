using System.Globalization;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Export.Proof;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Çevrimdışı HTML prova (Karar 11 Rev. 2): sayfalar (ve kapak) tek bir HTML dosyasına gömülür; müşteri beğenir, not yazar, küçük bir
/// JSON dosyası indirir; tasarımcı onu içe aktarınca notlar sayfalara yazılır (geri alınabilir).
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Provadaki görüntülerin uzun kenarı (piksel).</summary>
    public const int ProofLongEdgePx = 1600;

    public RelayCommand ExportProofCommand { get; private set; } = null!;

    public RelayCommand ImportProofCommand { get; private set; } = null!;

    /// <summary>Seçili sayfanın notu (provadan gelen geri bildirim dahil); yoksa boş.</summary>
    public string SelectedPageNotes => SelectedPage?.Page.Notes ?? string.Empty;

    public bool HasSelectedPageNotes => SelectedPageNotes.Length > 0;

    public void ExportProof()
    {
        if (project is null || project.Pages.Count == 0)
        {
            return;
        }

        var path = dialogs.AskProofPath(project.Name + " - prova.html");
        if (path is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var current = project;
            var pages = new List<ProofPage>();
            if (CoverPage?.Cover is { } cover && current.Cover is { } coverPage)
            {
                pages.Add(ProofImage(current, new PageItemViewModel(coverPage, -1, current, cover) { ShowGuides = false }, coverPage.Id, "Kapak"));
            }

            for (var i = 0; i < current.Pages.Count; i++)
            {
                var page = new PageItemViewModel(current.Pages[i], i, current) { ShowGuides = false };
                pages.Add(ProofImage(current, page, current.Pages[i].Id, string.Format(CultureInfo.CurrentCulture, "Sayfa {0}", i + 1)));
            }

            var html = ProofDocument.Build(current.Name, current.Id, pages, clock.GetUtcNow());
            File.WriteAllText(path, html, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Status = string.Format(CultureInfo.CurrentCulture, "Prova hazır: {0} ({1} sayfa, {2:0.#} MB). Müşteriniz tarayıcıda açar; internet gerekmez.", Path.GetFileName(path), pages.Count, new FileInfo(path).Length / 1048576.0);
            dialogs.Reveal(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        {
            dialogs.Warn("Prova oluşturulamadı: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ImportProof()
    {
        if (project is null)
        {
            return;
        }

        var path = dialogs.AskProofFeedbackPath();
        if (path is null)
        {
            return;
        }

        try
        {
            var feedback = ProofFeedback.Parse(File.ReadAllText(path));
            var result = feedback.ApplyTo(project, clock.GetUtcNow());
            if (ReferenceEquals(result.Project, project))
            {
                Status = "Provada beğeni ya da not yok.";
                return;
            }

            UpdateProject(result.Project.Touch(clock.GetUtcNow()), "Prova geri bildirimi");
            Status = string.Format(CultureInfo.CurrentCulture, "Prova geri bildirimi işlendi: {0} beğeni, {1} not.", result.Liked, result.Notes)
                + (result.Unknown > 0 ? string.Format(CultureInfo.CurrentCulture, " {0} sayfa artık albümde yok.", result.Unknown) : string.Empty)
                + " Notlar sayfa panelinde.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or JsonException or InvalidOperationException)
        {
            dialogs.Warn("Geri bildirim okunamadı: " + ex.Message);
        }
    }

    private static ProofPage ProofImage(Core.Projects.AlbumProject project, PageItemViewModel page, string id, string label)
    {
        var longEdgeMm = Math.Max(page.PageArea.Width, page.PageArea.Height);
        var dpi = (int)Math.Clamp(Math.Round(ProofLongEdgePx / (longEdgeMm / 25.4)), 48, 200);
        var image = PageImageExporter.Render(page, project, dpi);
        return new ProofPage(id, label, PageImageExporter.EncodeJpeg(image, 85), image.PixelWidth, image.PixelHeight);
    }

    private void InitializeProof()
    {
        ExportProofCommand = new RelayCommand(ExportProof, () => HasProject && project!.Pages.Count > 0 && !IsBusy);
        ImportProofCommand = new RelayCommand(ImportProof, () => HasProject && !IsBusy);
    }

    private void RefreshProofCommands()
    {
        ExportProofCommand?.NotifyCanExecuteChanged();
        ImportProofCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedPageNotes));
        OnPropertyChanged(nameof(HasSelectedPageNotes));
    }
}
