using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Projects;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Uygulama kabuğu (Karar 12 Rev. 1, Karar 17): tema, başlangıç ekranındaki son projeler, galeri ızgarası ve süzgeçleri,
/// tuval yakınlaştırması ve komut paleti.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Başlangıç ekranında tutulan en fazla son proje sayısı.</summary>
    public const int MaxRecentProjects = 8;

    private AppTheme theme;
    private GalleryFilter galleryFilter;
    private int galleryColumns = 3;
    private bool isBookView;
    private PageItemViewModel? spreadLeft;
    private PageItemViewModel? spreadRight;
    private PageItemViewModel? facingDetail;

    /// <summary>Tema değişti; pencere yeni paleti uygular.</summary>
    public event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>Sunum modu istendi (F5); pencere tam ekran sunumu açar.</summary>
    public event EventHandler<PresentationViewModel>? PresentationRequested;

    public RelayCommand ToggleBookViewCommand { get; private set; } = null!;

    public RelayCommand PresentCommand { get; private set; } = null!;

    /// <summary>
    /// Kitap görünümü: tuvalde karşılıklı iki sayfa (1–2, 3–4…) ve ortada cilt gölgesi. Albüm çift sayfa halinde
    /// algılanır; fotoğrafçı ve çift sayfaları birlikte değerlendirir. Tercih bu bilgisayarda saklanır.
    /// </summary>
    public bool IsBookView
    {
        get => isBookView;
        set
        {
            if (SetProperty(ref isBookView, value))
            {
                SaveSettings(appSettings with { BookView = value });
                UpdateSpread();
                RefreshCommands();
                Status = value ? "Kitap görünümü: karşılıklı sayfalar." : "Tek sayfa görünümü.";
            }
        }
    }

    /// <summary>Kitap görünümünde soldaki (tek numaralı) sayfa.</summary>
    public PageItemViewModel? SpreadLeft
    {
        get => spreadLeft;
        private set => SetProperty(ref spreadLeft, value);
    }

    /// <summary>Kitap görünümünde sağdaki sayfa; albümün son sayfası tekse null.</summary>
    public PageItemViewModel? SpreadRight
    {
        get => spreadRight;
        private set => SetProperty(ref spreadRight, value);
    }

    public bool IsLeftSelected => isBookView && SelectedPage is not null && ReferenceEquals(SelectedPage, spreadLeft);

    public bool IsRightSelected => isBookView && SelectedPage is not null && ReferenceEquals(SelectedPage, spreadRight);

    /// <summary>Sunum modunu başlatır: seçili sayfanın çift sayfasından itibaren.</summary>
    public void Present()
    {
        if (project is null || project.Pages.Count == 0)
        {
            return;
        }

        PresentationRequested?.Invoke(this, new PresentationViewModel(project, SelectedPage?.Index ?? 0, spreads: true));
    }

    public CommandPaletteViewModel Palette { get; private set; } = null!;

    public CanvasZoomViewModel Zoom { get; } = new();

    public ObservableCollection<RecentProjectItem> RecentProjects { get; } = [];

    public ObservableCollection<GalleryRow> GalleryRows { get; } = [];

    public RelayCommand OpenPaletteCommand { get; private set; } = null!;

    public RelayCommand ToggleThemeCommand { get; private set; } = null!;

    public RelayCommand<RecentProjectItem> OpenRecentCommand { get; private set; } = null!;

    public RelayCommand<RecentProjectItem> ForgetRecentCommand { get; private set; } = null!;

    public AppTheme Theme => theme;

    public bool IsDarkTheme => theme == AppTheme.Gece;

    public string ThemeToggleToolTip => IsDarkTheme ? "Fildişi (açık) temaya geç" : "Gece (koyu) temaya geç";

    public bool HasRecentProjects => RecentProjects.Count > 0;

    public string ProjectDisplayName => project?.Name ?? string.Empty;

    public string PagesHeader => string.Format(CultureInfo.CurrentCulture, "SAYFALAR · {0}", Pages.Count);

    public bool HasPages => Pages.Count > 0;

    /// <summary>Tuvalin altındaki gezinme: "3 / 20"; kitap görünümünde "3–4 / 20".</summary>
    public string PageNavText => SelectedPage is null
        ? string.Empty
        : isBookView && spreadLeft is not null
            ? spreadRight is null
                ? string.Format(CultureInfo.CurrentCulture, "{0} / {1}", spreadLeft.Number, Pages.Count)
                : string.Format(CultureInfo.CurrentCulture, "{0}–{1} / {2}", spreadLeft.Number, spreadRight.Number, Pages.Count)
            : string.Format(CultureInfo.CurrentCulture, "{0} / {1}", SelectedPage.Number, Pages.Count);

    public GalleryFilter GalleryFilter
    {
        get => galleryFilter;
        set
        {
            if (SetProperty(ref galleryFilter, value))
            {
                if (value != GalleryFilter.Chapter && selectedChapter is not null)
                {
                    selectedChapter.IsSelected = false;
                    selectedChapter = null;
                    OnPropertyChanged(nameof(SelectedChapter));
                }

                RebuildGallery();
            }
        }
    }

    public bool IsFilterAll
    {
        get => galleryFilter == GalleryFilter.All;
        set => SetFilter(value, GalleryFilter.All);
    }

    public bool IsFilterUnused
    {
        get => galleryFilter == GalleryFilter.Unused;
        set => SetFilter(value, GalleryFilter.Unused);
    }

    public bool IsFilterUsed
    {
        get => galleryFilter == GalleryFilter.Used;
        set => SetFilter(value, GalleryFilter.Used);
    }

    public bool IsFilterEdited
    {
        get => galleryFilter == GalleryFilter.Edited;
        set => SetFilter(value, GalleryFilter.Edited);
    }

    public bool IsFilterFavorites
    {
        get => galleryFilter == GalleryFilter.Favorites;
        set => SetFilter(value, GalleryFilter.Favorites);
    }

    public string FilterFavoritesText => Assets.Count(a => a.IsFavorite).ToString(CultureInfo.CurrentCulture);

    public string FilterAllText => Count("Tümü", Assets.Count);

    public string FilterUnusedText => Count("Kullanılmamış", Assets.Count(a => !a.IsUsed));

    public string FilterUsedText => Count("Albümde", Assets.Count(a => a.IsUsed));

    public string FilterEditedText => Count("Rötuşlu", Assets.Count(a => a.IsEdited));

    /// <summary>Galeri ızgarasının sütun sayısı; pencere panel genişliğine göre verir.</summary>
    public int GalleryColumns
    {
        get => galleryColumns;
        set
        {
            if (SetProperty(ref galleryColumns, Math.Clamp(value, 1, 8)))
            {
                RebuildGallery();
            }
        }
    }

    public bool HasVisiblePhotos => GalleryRows.Count > 0;

    public string GalleryEmptyText => !HasPhotos
        ? "Henüz fotoğraf yok.\n«Fotoğraf» ya da «Klasör» ile ekleyin\nveya Gezgin'den buraya sürükleyin."
        : galleryFilter switch
        {
            GalleryFilter.Unused => "Tüm fotoğraflar albümde kullanılıyor.",
            GalleryFilter.Used => "Albümde henüz fotoğraf yok. «Oto Oluştur» ile başlayın.",
            GalleryFilter.Edited => "Rötuşlanmış fotoğraf yok. «Rötuş» ile toplu düzenleyebilirsiniz.",
            GalleryFilter.Chapter => "Bu bölümde fotoğraf yok.",
            GalleryFilter.Favorites => "Favori yok. Fotoğrafın yıldızına tıklayın ya da seçip F'ye basın;\nfavoriler otomatik yerleşimde önce yerleşir.",
            _ => string.Empty,
        };

    public void OpenPalette() => Palette.Open();

    /// <summary>Temayı değiştirir; <paramref name="save"/> ise ayar dosyasına yazar.</summary>
    public void SetTheme(AppTheme value, bool save)
    {
        if (save)
        {
            SaveSettings(appSettings with { Theme = ThemeService.ToSetting(value) });
        }

        if (theme == value)
        {
            return;
        }

        theme = value;
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(ThemeToggleToolTip));
        ThemeChanged?.Invoke(this, value);
        Status = ThemeService.DisplayName(value) + " teması etkin.";
    }

    /// <summary>Başlangıç ekranından son projeyi açar; dosya taşınmış/silinmişse listeden çıkarır.</summary>
    public void OpenRecent(RecentProjectItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!File.Exists(item.Path))
        {
            dialogs.Warn("Proje dosyası bulunamadı; taşınmış ya da silinmiş olabilir.\n\n" + item.Path + "\n\nListeden kaldırıldı.");
            ForgetRecent(item);
            return;
        }

        if (ConfirmDiscard())
        {
            Open(item.Path);
        }
    }

    public void ForgetRecent(RecentProjectItem? item)
    {
        if (item is null)
        {
            return;
        }

        var list = appSettings.RecentProjects.Where(r => !SamePath(r.Path, item.Path)).ToList();
        SaveRecent(list);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(Project) or nameof(Title))
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(ProjectDisplayName)));
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(PagesHeader)));
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasPages)));
        }
        else if (e.PropertyName is nameof(SelectedPageInfo) or nameof(SelectedPage))
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(PageNavText)));
        }
    }

    private static string Count(string label, int count) => string.Format(CultureInfo.CurrentCulture, "{0} · {1}", label, count);

    private static bool SamePath(string a, string b) => string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private void InitializeShell()
    {
        theme = ThemeService.Parse(appSettings.Theme);
        OpenPaletteCommand = new RelayCommand(OpenPalette);
        ToggleThemeCommand = new RelayCommand(() => SetTheme(IsDarkTheme ? AppTheme.Fildisi : AppTheme.Gece, save: true));
        OpenRecentCommand = new RelayCommand<RecentProjectItem>(OpenRecent, _ => !IsBusy);
        isBookView = appSettings.BookView;
        ToggleBookViewCommand = new RelayCommand(() => IsBookView = !IsBookView);
        PresentCommand = new RelayCommand(Present, () => HasProject && Pages.Count > 0 && !IsBusy);
        ForgetRecentCommand = new RelayCommand<RecentProjectItem>(ForgetRecent);
        LoadRecent();
        Palette = new CommandPaletteViewModel(BuildPaletteCommands());
    }

    private void RefreshShellCommands()
    {
        OpenRecentCommand?.NotifyCanExecuteChanged();
        PresentCommand?.NotifyCanExecuteChanged();
    }

    private void OnShellPageChanged(PageItemViewModel? page)
    {
        RestoreTextSelection(page);
        UpdateSpread();
        RefreshTemplates();
    }

    /// <summary>Seçili sayfaya göre çift sayfayı ve tuval ölçüsünü günceller; karşı sayfanın da ayrıntılı görüntüsü yüklenir.</summary>
    private void UpdateSpread()
    {
        var page = SelectedPage;
        PageItemViewModel? facing = null;
        if (isBookView && page is not null)
        {
            var left = page.Index - (page.Index % 2);
            SpreadLeft = Pages.ElementAtOrDefault(left);
            SpreadRight = Pages.ElementAtOrDefault(left + 1);
            facing = ReferenceEquals(page, SpreadLeft) ? SpreadRight : SpreadLeft;
            Zoom.SetPage(page.CanvasWidth * 2, page.CanvasHeight);
        }
        else
        {
            SpreadLeft = null;
            SpreadRight = null;
            Zoom.SetPage(page?.CanvasWidth ?? 0, page?.CanvasHeight ?? 0);
        }

        if (!ReferenceEquals(facingDetail, facing))
        {
            if (facingDetail is not null && !ReferenceEquals(facingDetail, page))
            {
                facingDetail.SetDetail(false);
            }

            facing?.SetDetail(true);
            facingDetail = facing;
        }

        OnPropertyChanged(nameof(IsLeftSelected));
        OnPropertyChanged(nameof(IsRightSelected));
        OnPropertyChanged(nameof(PageNavText));
    }

    private void SetFilter(bool on, GalleryFilter filter)
    {
        if (on)
        {
            GalleryFilter = filter;
        }
    }

    private bool Matches(AssetItemViewModel item) => galleryFilter switch
    {
        GalleryFilter.Unused => !item.IsUsed,
        GalleryFilter.Used => item.IsUsed,
        GalleryFilter.Edited => item.IsEdited,
        GalleryFilter.Favorites => item.IsFavorite,
        GalleryFilter.Chapter => InSelectedChapter(item),
        _ => true,
    };

    /// <summary>Galeri satırlarını süzgece ve sütun sayısına göre yeniden kurar; seçili fotoğraf (kimliğiyle) korunur.</summary>
    private void RebuildGallery()
    {
        var keep = SelectedAsset?.Asset.Id;
        var visible = Sorted(Assets.Where(Matches)).ToList();
        GalleryRows.Clear();
        for (var i = 0; i < visible.Count; i += galleryColumns)
        {
            GalleryRows.Add(new GalleryRow(visible.GetRange(i, Math.Min(galleryColumns, visible.Count - i))));
        }

        var match = keep is null ? null : Assets.FirstOrDefault(a => string.Equals(a.Asset.Id, keep, StringComparison.Ordinal));
        if (ReferenceEquals(match, SelectedAsset))
        {
            match?.IsSelected = true;
        }
        else
        {
            SelectedAsset = match;
        }

        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterUnused));
        OnPropertyChanged(nameof(IsFilterUsed));
        OnPropertyChanged(nameof(IsFilterEdited));
        OnPropertyChanged(nameof(IsFilterFavorites));
        OnPropertyChanged(nameof(FilterFavoritesText));
        OnPropertyChanged(nameof(FilterAllText));
        OnPropertyChanged(nameof(FilterUnusedText));
        OnPropertyChanged(nameof(FilterUsedText));
        OnPropertyChanged(nameof(FilterEditedText));
        OnPropertyChanged(nameof(HasVisiblePhotos));
        OnPropertyChanged(nameof(GalleryEmptyText));
    }

    private void RememberRecent(string path, string name)
    {
        var list = appSettings.RecentProjects
            .Where(r => !SamePath(r.Path, path))
            .Prepend(new RecentProject(Path.GetFullPath(path), name, clock.GetUtcNow()))
            .Take(MaxRecentProjects)
            .ToList();
        SaveRecent(list);
    }

    /// <summary>Son projeler listesi sessizce kaydedilir; yazılamazsa (salt okunur profil) liste yalnızca bu oturumda kalır.</summary>
    private void SaveRecent(IReadOnlyList<RecentProject> list)
    {
        appSettings = appSettings with { RecentProjects = list };
        try
        {
            settingsStore.Save(appSettings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Bilinçli olarak yutulur: son projeler listesi kritik değildir.
        }

        LoadRecent();
    }

    private void LoadRecent()
    {
        var now = clock.GetUtcNow();
        RecentProjects.Clear();
        foreach (var recent in appSettings.RecentProjects.Take(MaxRecentProjects))
        {
            RecentProjects.Add(RecentProjectItem.Create(recent, now));
        }

        OnPropertyChanged(nameof(HasRecentProjects));
    }

    private List<PaletteCommand> BuildPaletteCommands() =>
    [
        new("Yeni proje", "Proje", "Icon.New", "Ctrl+N", NewCommand, "oluştur başla albüm"),
        new("Proje aç", "Proje", "Icon.Open", "Ctrl+O", OpenCommand, "dosya albumproj"),
        new("Kaydet", "Proje", "Icon.Save", "Ctrl+S", SaveCommand),
        new("Farklı kaydet", "Proje", "Icon.SaveAs", "Ctrl+Shift+S", SaveAsCommand, "kopya"),
        new("Geri al", "Düzenleme", "Icon.Undo", "Ctrl+Z", UndoCommand, "vazgeç önceki"),
        new("Yinele", "Düzenleme", "Icon.Redo", "Ctrl+Y", RedoCommand, "tekrar"),
        new("Sayfa ekle", "Sayfa", "Icon.PagePlus", null, AddPageCommand, "yeni boş sayfa"),
        new("Sayfayı çoğalt", "Sayfa", "Icon.Restore", "Ctrl+D", DuplicatePageCommand, "kopyala"),
        new("Sayfayı sil", "Sayfa", "Icon.Trash", null, DeletePageCommand, "kaldır"),
        new("Sayfayı sola taşı", "Sayfa", "Icon.ChevronLeft", null, MovePageLeftCommand, "sırala öne"),
        new("Sayfayı sağa taşı", "Sayfa", "Icon.ChevronRight", null, MovePageRightCommand, "sırala arkaya"),
        new("Baskı profilini düzenle", "Proje", "Icon.Profile", null, EditProfileCommand, "taşma payı bleed güvenli alan dpi cilt"),
        new("Fotoğraf ekle", "Fotoğraf", "Icon.AddPhotos", null, ImportFilesCommand, "içe aktar dosya resim"),
        new("Klasörden fotoğraf ekle", "Fotoğraf", "Icon.AddFolder", null, ImportCommand, "içe aktar dizin"),
        new("Toplu rötuş", "Fotoğraf", "Icon.Retouch", null, BatchEditCommand, "photoshop action darktable rawtherapee düzenle"),
        new("Düzenleyicide aç", "Fotoğraf", "Icon.ExternalLink", null, OpenInEditorCommand, "photoshop rötuş"),
        new("Düzenlemeyi kaldır", "Fotoğraf", "Icon.Trash", null, RemoveEditCommand, "orijinal geri al"),
        new("Otomatik oluştur", "Albüm", "Icon.AutoLayout", "Ctrl+G", AutoLayoutCommand, "oto yerleşim diz"),
        new("Farklı sonuç dene", "Albüm", "Icon.Shuffle", null, ShuffleCommand, "karıştır yeniden"),
        new("Ön kontrol", "Albüm", "Icon.Preflight", null, PreflightCommand, "denetle hata uyarı dpi"),
        new("PDF çıktı al", "Albüm", "Icon.Export", "Ctrl+E", ExportPdfCommand, "dışa aktar baskı yazdır"),
        new("Sonraki sayfa", "Sayfa", "Icon.ChevronRight", "PgDn", NextPageCommand),
        new("Önceki sayfa", "Sayfa", "Icon.ChevronLeft", "PgUp", PreviousPageCommand),
        new("Sayfayı kilitle / kilidi aç", "Sayfa", "Icon.Lock", "Ctrl+L", ToggleLockCommand, "koru"),
        new("Fotoğrafı ortala", "Yuva", "Icon.Fit", null, CenterPhotoCommand, "sıfırla"),
        new("Kapla / tamamını göster", "Yuva", "Icon.Frame", null, ToggleFitCommand, "sığdır kırp"),
        new("Fotoğrafı yuvadan çıkar", "Yuva", "Icon.Close", null, ClearSlotCommand, "boşalt"),
        new("Boşlukları tüm sayfalara uygula", "Tasarım", "Icon.Grid", null, ApplySpacingCommand, "kenar aralık"),
        new("Fon görseli ekle", "Tasarım", "Icon.Image", null, ImportBackgroundsCommand, "arka plan doku kâğıt"),
        new("Metin ekle: Başlık", "Metin", "Icon.Type", null, new RelayCommand(() => AddText(TextPresets.Find("title"))), "yazı el yazısı script"),
        new("Metin ekle: İsimler", "Metin", "Icon.Type", null, new RelayCommand(() => AddText(TextPresets.Find("names"))), "yazı gelin damat çift"),
        new("Metin ekle: Tarih", "Metin", "Icon.Type", null, new RelayCommand(() => AddText(TextPresets.Find("date"))), "yazı gün"),
        new("Metin ekle: Alıntı", "Metin", "Icon.Type", null, new RelayCommand(() => AddText(TextPresets.Find("quote"))), "yazı söz şiir"),
        new("Seçili metni sil", "Metin", "Icon.Trash", "Del", DeleteTextCommand, "yazı kaldır"),
        new("Metni okunur yap (hale / gölge)", "Metin", "Icon.Type", null, MakeTextReadableCommand, "gölge hale kontur bant kabartma okunurluk etki"),
        new("Yuvadaki fotoğrafı konumlandır", "Düzenle", "Icon.Expand", null, TogglePositioningCommand, "kaydır kırp pan merkez odak yer"),
        new("Temayı değiştir (Gece / Fildişi)", "Görünüm", "Icon.Moon", null, ToggleThemeCommand, "koyu açık karanlık aydınlık"),
        new("Kitap görünümü (karşılıklı sayfalar)", "Görünüm", "Icon.Book", "Ctrl+B", ToggleBookViewCommand, "çift sayfa spread açık albüm"),
        new("Sunum modu", "Görünüm", "Icon.Present", "F5", PresentCommand, "tam ekran müşteri çift göster slayt"),
        new("Yakınlaştır", "Görünüm", "Icon.ZoomIn", "Ctrl++", Zoom.ZoomInCommand, "büyüt zoom"),
        new("Uzaklaştır", "Görünüm", "Icon.ZoomOut", "Ctrl+-", Zoom.ZoomOutCommand, "küçült zoom"),
        new("Ekrana sığdır", "Görünüm", "Icon.Fit", "Ctrl+F", Zoom.FitCommand, "zoom"),
        new("Gerçek boyut (%100)", "Görünüm", "Icon.Maximize", "Ctrl+0", Zoom.ActualSizeCommand, "zoom baskı"),
        new("Galeri: tüm fotoğraflar", "Galeri", "Icon.Gallery", null, new RelayCommand(() => GalleryFilter = GalleryFilter.All), "süzgeç filtre"),
        new("Galeri: kullanılmamışlar", "Galeri", "Icon.Gallery", null, new RelayCommand(() => GalleryFilter = GalleryFilter.Unused), "süzgeç filtre boşta"),
        new("Galeri: albümdekiler", "Galeri", "Icon.Gallery", null, new RelayCommand(() => GalleryFilter = GalleryFilter.Used), "süzgeç filtre kullanılan"),
        new("Galeri: rötuşlananlar", "Galeri", "Icon.Gallery", null, new RelayCommand(() => GalleryFilter = GalleryFilter.Edited), "süzgeç filtre düzenlenen"),
        new("Hikâye: bölümleri bul", "Hikâye", "Icon.Story", null, DetectStoryCommand, "düğün evre akış kına hazırlık gelin alma nikah dış çekim zaman exif"),
        new("Hikâye: bölümlere göre albüm oluştur", "Hikâye", "Icon.Story", null, BuildFromStoryCommand, "otomatik akış bölüm"),
        new("Hikâye: seçili fotoğraftan yeni bölüm", "Hikâye", "Icon.Split", null, SplitStoryHereCommand, "böl ayır başlat"),
        new("Galeri: favoriler", "Galeri", "Icon.Star", null, new RelayCommand(() => GalleryFilter = GalleryFilter.Favorites), "süzgeç filtre yıldız seçilen"),
        new("Favori yap / kaldır", "Galeri", "Icon.Star", "F", ToggleFavoriteCommand, "yıldız beğen seç öne çıkar"),
        new("Galeri: çekim zamanına göre sırala", "Galeri", "Icon.Gallery", null, new RelayCommand(() => SelectedGallerySort = GallerySortOptions[1]), "sırala tarih saat exif"),
        new("Galeri: dosya adına göre sırala", "Galeri", "Icon.Gallery", null, new RelayCommand(() => SelectedGallerySort = GallerySortOptions[2]), "sırala ad isim"),
        new("Ayarlar", "Uygulama", "Icon.Settings", null, EditSettingsCommand, "tema düzenleyici klasör"),
        new("Hakkında", "Uygulama", "Icon.Info", "F1", AboutCommand, "sürüm lisans geliştirici"),
    ];
}
