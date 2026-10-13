using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Assets.Import;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Core.Validation;
using NexAlbum.Export.Pdf;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>Otomatik oluşturma modu seçeneği.</summary>
public sealed record LayoutModeOption(LayoutMode Mode, string Name);

/// <summary>
/// Ana pencerenin görünüm modeli: proje yaşam döngüsü, içe aktarma, otomatik yerleşim ve ön kontrol.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IDialogService dialogs;
    private readonly TimeProvider clock;
    private readonly AutoLayoutEngine engine = new();
    private AlbumProject? project;
    private string? filePath;
    private bool isDirty;
    private bool isBusy;
    private string status = "Hazır";
    private PageItemViewModel? selectedPage;
    private LayoutModeOption selectedMode;
    private ulong? seed;
    private ProjectLock? projectLock;

    /// <param name="dialogs">Pencereler ve dış eylemler.</param>
    /// <param name="clock">Saat (test için).</param>
    /// <param name="settings">Uygulama ayarları deposu; null ise bellek içi (testler). Uygulama <see cref="JsonAppSettingsStore"/> verir.</param>
    /// <param name="photoshop">Photoshop kapısı; null ise COM (Windows).</param>
    public MainViewModel(IDialogService dialogs, TimeProvider? clock = null, IAppSettingsStore? settings = null, Integrations.Photoshop.IPhotoshopHost? photoshop = null)
    {
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.clock = clock ?? TimeProvider.System;
        selectedMode = LayoutModes[1];

        NewCommand = new RelayCommand(NewProject, () => !IsBusy);
        OpenCommand = new RelayCommand(OpenProject, () => !IsBusy);
        SaveCommand = new RelayCommand(() => Save(saveAs: false), () => HasProject && !IsBusy);
        SaveAsCommand = new RelayCommand(() => Save(saveAs: true), () => HasProject && !IsBusy);
        ImportCommand = new AsyncRelayCommand(ImportAsync, () => HasProject && !IsBusy);
        AutoLayoutCommand = new AsyncRelayCommand(() => AutoLayoutAsync(newSeed: false), () => HasPhotos && !IsBusy);
        ShuffleCommand = new AsyncRelayCommand(() => AutoLayoutAsync(newSeed: true), () => HasPhotos && !IsBusy);
        PreflightCommand = new RelayCommand(RunPreflight, () => HasProject);
        EditProfileCommand = new RelayCommand(EditProfile, () => HasProject && !IsBusy);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, () => HasProject && Pages.Count > 0 && !IsBusy);
        ToggleLockCommand = new RelayCommand(ToggleLock, () => SelectedPage is not null);
        PreviousPageCommand = new RelayCommand(() => MovePage(-1), () => SelectedPage is { Index: > 0 });
        NextPageCommand = new RelayCommand(() => MovePage(1), () => SelectedPage is not null && (IsBookView ? SelectedPage.Index - (SelectedPage.Index % 2) + 2 : SelectedPage.Index + 1) < Pages.Count);
        AboutCommand = new RelayCommand(dialogs.ShowAbout);
        InitializeDesign();
        InitializeEditing();
        InitializeExternal(settings, photoshop);
        InitializePages();
        InitializeText();
        InitializeShell();
    }

    public IReadOnlyList<LayoutModeOption> LayoutModes { get; } =
    [
        new(LayoutMode.Quick, "Hızlı"),
        new(LayoutMode.Balanced, "Dengeli"),
        new(LayoutMode.Controlled, "Kontrollü"),
    ];

    public RelayCommand NewCommand { get; }

    public RelayCommand OpenCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand SaveAsCommand { get; }

    public AsyncRelayCommand ImportCommand { get; }

    public AsyncRelayCommand AutoLayoutCommand { get; }

    public AsyncRelayCommand ShuffleCommand { get; }

    public RelayCommand PreflightCommand { get; }

    public RelayCommand EditProfileCommand { get; }

    public AsyncRelayCommand ExportPdfCommand { get; }

    public RelayCommand ToggleLockCommand { get; }

    public RelayCommand PreviousPageCommand { get; }

    public RelayCommand NextPageCommand { get; }

    public RelayCommand AboutCommand { get; }

    public ObservableCollection<AssetItemViewModel> Assets { get; } = [];

    public ObservableCollection<PageItemViewModel> Pages { get; } = [];

    public ObservableCollection<IssueItemViewModel> Issues { get; } = [];

    public AlbumProject? Project => project;

    public string? FilePath => filePath;

    public bool HasProject => project is not null;

    public bool HasNoProject => project is null;

    public bool HasPhotos => project is not null && project.Photos.Count > 0;

    public bool IsDirty
    {
        get => isDirty;
        private set
        {
            if (SetProperty(ref isDirty, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(SaveState));
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RefreshCommands();
            }
        }
    }

    public string Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public string Title => project is null
        ? AppInfo.Name
        : $"{project.Name}{(IsDirty ? " ●" : string.Empty)} — {AppInfo.Name}";

    public string SaveState => project is null ? string.Empty : IsDirty ? "Kaydedilmedi ●" : "Kaydedildi";

    public string Version => "v" + AppInfo.Version;

    public LayoutModeOption SelectedMode
    {
        get => selectedMode;
        set => SetProperty(ref selectedMode, value ?? LayoutModes[1]);
    }

    public PageItemViewModel? SelectedPage
    {
        get => selectedPage;
        set
        {
            var previous = selectedPage;
            if (SetProperty(ref selectedPage, value))
            {
                previous?.SetDetail(false);
                value?.SetDetail(true);
                OnPropertyChanged(nameof(SelectedPageInfo));
                OnPropertyChanged(nameof(LockButtonText));
                OnDesignPageChanged(value);
                RestoreSlotSelection(value);
                OnShellPageChanged(value);
                RefreshCommands();
            }
        }
    }

    public string SelectedPageInfo => SelectedPage is null
        ? "Sayfa seçilmedi."
        : string.Format(
            CultureInfo.CurrentCulture,
            "{0} / {1} · {2} · {3} fotoğraf{4}",
            SelectedPage.Number,
            Pages.Count,
            TemplateName(SelectedPage.TemplateId),
            SelectedPage.Slots.Count(s => !s.IsEmpty),
            SelectedPage.IsLocked ? " · Kilitli" : string.Empty);

    public string LockButtonText => SelectedPage?.IsLocked == true ? "Kilidi aç" : "Kilitle";

    public string PhotoSummary => project is null
        ? string.Empty
        : string.Format(CultureInfo.CurrentCulture, "{0} fotoğraf · {1} kullanılmamış", project.Photos.Count, project.UnusedAssets().Count);

    /// <summary>Profil özeti (Karar 03 §10.4).</summary>
    public string ProfileSummary => project is null ? string.Empty : ProfileText.Summary(project.Profile, project.PageSize);

    public void NewProject()
    {
        if (!ConfirmDiscard())
        {
            return;
        }

        var choice = dialogs.AskNewProject();
        if (choice is null)
        {
            return;
        }

        ReleaseLock();
        SetProject(AlbumProject.Create(choice.Name, choice.Profile, clock.GetUtcNow(), AppInfo.Version), path: null, dirty: true);
        Status = "Yeni proje oluşturuldu. Fotoğraf ekleyerek başlayın.";
    }

    public void OpenProject()
    {
        if (!ConfirmDiscard())
        {
            return;
        }

        var path = dialogs.AskOpenProjectPath();
        if (path is not null)
        {
            Open(path);
        }
    }

    public void Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        LoadedProject loaded;
        try
        {
            loaded = ProjectPackageReader.Load(path);
        }
        catch (ProjectFormatException ex)
        {
            dialogs.Warn(ex.Message);
            return;
        }

        if (!AcquireLock(path))
        {
            return;
        }

        if (loaded.ChecksumMismatches.Count > 0
            && !dialogs.Confirm("Proje dosyasındaki bazı bölümler kaydedildikten sonra değişmiş görünüyor (bütünlük denetimi).\n\nYine de açılsın mı?"))
        {
            ReleaseLock();
            return;
        }

        var refreshed = AssetFiles.RefreshMissing(loaded.Project);
        SetProject(refreshed, path, dirty: loaded.Migrated || !ReferenceEquals(refreshed, loaded.Project));
        RememberRecent(path, refreshed.Name);
        var missing = AssetFiles.Missing(refreshed).Count;
        Status = missing > 0
            ? string.Format(CultureInfo.CurrentCulture, "Proje açıldı. {0} fotoğraf bulunamadı; ön kontrolde listelenir.", missing)
            : "Proje açıldı.";
        if (loaded.Migrated)
        {
            dialogs.Info("Proje eski bir sürümle kaydedilmişti ve güncellendi. Orijinal dosyanın yedeği alındı.");
        }
    }

    /// <summary>Kaydeder; başarılıysa true.</summary>
    public bool Save(bool saveAs)
    {
        if (project is null)
        {
            return false;
        }

        var path = filePath;
        if (saveAs || path is null)
        {
            path = dialogs.AskSaveProjectPath(project.Name + ProjectFormat.FileExtension);
            if (path is null)
            {
                return false;
            }
        }

        try
        {
            var saved = project.Touch(clock.GetUtcNow());
            ProjectPackageWriter.Save(path, saved, new SaveOptions
            {
                AppVersion = AppInfo.Version,
                Templates = UsedTemplates(saved),
                Clock = clock,
            });
            project = saved;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ProjectFormatException)
        {
            dialogs.Warn("Proje kaydedilemedi: " + ex.Message);
            return false;
        }

        if (!string.Equals(path, filePath, StringComparison.OrdinalIgnoreCase))
        {
            ReleaseLock();
            filePath = path;
            AcquireLock(path);
            OnPropertyChanged(nameof(FilePath));
        }

        savedProject = project;
        IsDirty = false;
        RememberRecent(path, project!.Name);
        Status = "Kaydedildi: " + Path.GetFileName(path);
        return true;
    }

    /// <summary>Pencere kapanırken: kaydedilmemiş değişiklik varsa sorar. Kapanabilirse true.</summary>
    public bool CanClose() => ConfirmDiscard();

    /// <summary>Klasörden (alt klasörleriyle) fotoğraf ekler.</summary>
    public async Task ImportAsync()
    {
        var folder = dialogs.AskPhotoFolder();
        if (project is null || folder is null)
        {
            return;
        }

        await RunImportAsync(c => new PhotoImporter().ImportFolder(folder, c, new ImportOptions { Recursive = true }), "fotoğraf").ConfigureAwait(true);
    }

    public async Task AutoLayoutAsync(bool newSeed)
    {
        var current = project;
        if (current is null)
        {
            return;
        }

        if (current.Pages.Any(p => !p.Locked)
            && !dialogs.Confirm("Kilitli olmayan sayfalar yeniden oluşturulacak. Devam edilsin mi?"))
        {
            return;
        }

        if (newSeed)
        {
            seed = (seed ?? LayoutSeed.FromProjectId(current.Id)) + 1;
        }

        IsBusy = true;
        Status = "Albüm oluşturuluyor…";
        try
        {
            var options = new LayoutOptions { Mode = SelectedMode.Mode, Seed = seed };
            var request = LayoutRequest.ForProject(current, BuiltInTemplates.All, options, clock.GetUtcNow());
            var result = await Task.Run(() => engine.Layout(request)).ConfigureAwait(true);
            UpdateProject(current.WithPages(result.Pages), "Otomatik oluşturma");
            SelectedPage = Pages.FirstOrDefault();
            ShowIssues(result.Issues);
            Status = string.Format(CultureInfo.CurrentCulture, "{0} sayfa oluşturuldu ({1} mod).", result.Pages.Count, SelectedMode.Name);
        }
        catch (InvalidOperationException ex)
        {
            dialogs.Warn("Albüm oluşturulamadı: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void RunPreflight()
    {
        if (project is null)
        {
            return;
        }

        var result = ProjectPreflight.Check(project, new PreflightEnvironment { FontAvailable = FontCatalog.IsInstalled });
        ShowIssues(result);
        Status = result.HasErrors
            ? string.Format(CultureInfo.CurrentCulture, "Ön kontrol: {0} hata, {1} uyarı.", result.Errors.Count(), result.Warnings.Count())
            : string.Format(CultureInfo.CurrentCulture, "Ön kontrol: hata yok, {0} uyarı.", result.Warnings.Count());
    }

    public void EditProfile()
    {
        if (project is null)
        {
            return;
        }

        var edited = dialogs.EditProfile(project.Profile);
        if (edited is null)
        {
            return;
        }

        UpdateProject(project.WithProfile(edited), "Profil");
        RunPreflight();
        Status = "Profil güncellendi; ön kontrol yenilendi.";
    }

    /// <summary>
    /// Baskı PDF'i (Karar 11 §3). Önce ön kontrol: hata varsa çıktı alınmaz, uyarı varsa onay istenir (Karar 03 §7.2).
    /// </summary>
    public async Task ExportPdfAsync()
    {
        var current = project;
        if (current is null)
        {
            return;
        }

        var preflight = ProjectPreflight.Check(current, new PreflightEnvironment { FontAvailable = FontCatalog.IsInstalled });
        ShowIssues(preflight);
        if (preflight.HasErrors)
        {
            dialogs.Warn(string.Format(
                CultureInfo.CurrentCulture,
                "Ön kontrolde {0} hata var; düzeltilmeden PDF alınamaz. Ayrıntılar sağ panelde.\n\nİlk hata: {1}",
                preflight.Errors.Count(),
                preflight.Errors.First().Message));
            return;
        }

        var warnings = preflight.Warnings.Count();
        if (warnings > 0
            && !dialogs.Confirm(string.Format(CultureInfo.CurrentCulture, "Ön kontrolde {0} uyarı var (sağ panelde). Yine de PDF oluşturulsun mu?", warnings)))
        {
            return;
        }

        var path = dialogs.AskPdfPath(current.Name + ".pdf");
        if (path is null)
        {
            return;
        }

        IsBusy = true;
        Status = "PDF oluşturuluyor…";
        try
        {
            var settings = PdfExportSettings.FromProfile(current.Profile) with { Creator = AppInfo.Name + " " + AppInfo.Version };
            var result = await Task.Run(() => PdfAlbumExporter.Export(current, path, settings)).ConfigureAwait(true);
            ShowIssues(preflight.Merge(result.Issues));
            Status = string.Format(CultureInfo.CurrentCulture, "PDF hazır: {0} sayfa → {1}", result.PageCount, Path.GetFileName(path));
            if (result.Issues.HasErrors)
            {
                dialogs.Warn("PDF oluşturuldu ancak bazı öğeler eklenemedi. Ayrıntılar sağ panelde.");
            }
            else if (result.Files.Count > 0)
            {
                dialogs.Reveal(result.Files[0]);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException)
        {
            dialogs.Warn("PDF oluşturulamadı: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ToggleLock()
    {
        if (project is null || SelectedPage is null)
        {
            return;
        }

        var index = SelectedPage.Index;
        var page = SelectedPage.Page;
        UpdateProject(project.ReplacePage(page with { Locked = !page.Locked, UpdatedAt = clock.GetUtcNow() }), page.Locked ? "Kilidi aç" : "Kilitle");
        SelectedPage = Pages.ElementAtOrDefault(index);
    }

    public void Dispose()
    {
        StopAllWatchers();
        ReleaseLock();
    }

    private void MovePage(int delta)
    {
        if (SelectedPage is null)
        {
            return;
        }

        // Kitap görünümünde bir sonraki/önceki çift sayfanın soluna gidilir.
        var target = IsBookView ? SelectedPage.Index - (SelectedPage.Index % 2) + (2 * delta) : SelectedPage.Index + delta;
        target = Math.Clamp(target, 0, Pages.Count - 1);
        if (target != SelectedPage.Index)
        {
            SelectedPage = Pages[target];
        }
    }

    private void ShowIssues(ValidationResult result)
    {
        Issues.Clear();
        var pageNumbers = new Dictionary<string, int>(StringComparer.Ordinal);
        if (project is not null)
        {
            for (var i = 0; i < project.Pages.Count; i++)
            {
                pageNumbers[project.Pages[i].Id] = i + 1;
            }
        }

        foreach (var issue in result.Issues.OrderByDescending(i => i.Severity))
        {
            Issues.Add(new IssueItemViewModel(issue, issue.PageId is { } id && pageNumbers.TryGetValue(id, out var n) ? n : null));
        }
    }

    private bool ConfirmDiscard()
    {
        if (project is null || !IsDirty)
        {
            return true;
        }

        return dialogs.AskSaveChanges(project.Name) switch
        {
            SaveDecision.Save => Save(saveAs: false),
            SaveDecision.Discard => true,
            _ => false,
        };
    }

    private bool AcquireLock(string path)
    {
        if (ProjectLock.TryAcquire(path, clock, out var acquired, out var holder))
        {
            projectLock = acquired;
            return true;
        }

        var who = holder is null
            ? "başka bir uygulama"
            : string.Format(CultureInfo.CurrentCulture, "{0} ({1}, {2:g})", holder.User, holder.Machine, holder.AcquiredAt.ToLocalTime());
        if (!dialogs.Confirm($"Bu proje {who} tarafından kullanılıyor görünüyor.\n\nÖnceki bir çökmeden kalmış olabilir. Kilit kaldırılıp devam edilsin mi?"))
        {
            return false;
        }

        ProjectLock.ForceRelease(path);
        if (ProjectLock.TryAcquire(path, clock, out acquired, out _))
        {
            projectLock = acquired;
        }

        return true;
    }

    private void ReleaseLock()
    {
        projectLock?.Dispose();
        projectLock = null;
    }

    private void SetProject(AlbumProject value, string? path, bool dirty)
    {
        filePath = path;
        seed = null;
        Issues.Clear();
        UpdateProject(value, label: null, record: false);
        history.Clear();
        savedProject = dirty ? null : value;
        IsDirty = dirty;
        SelectedPage = Pages.FirstOrDefault();
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(HasNoProject));
    }

    /// <param name="value">Yeni proje.</param>
    /// <param name="label">Geri alma adımının açıklaması (ör. "Şablon").</param>
    /// <param name="record">Önceki hâl geri alma geçmişine yazılsın mı (açma/yeni projede hayır).</param>
    private void UpdateProject(AlbumProject value, string? label = "Düzenleme", bool record = true)
    {
        var selectedIndex = SelectedPage?.Index;
        SelectedPage = null;
        var previousProject = project;
        if (record && previousProject is not null && !ReferenceEquals(previousProject, value))
        {
            history.Record(previousProject, label);
        }

        project = value;
        IsDirty = !ReferenceEquals(value, savedProject);

        var used = value.Pages.SelectMany(p => p.Slots).Select(s => s.Placement?.AssetId).OfType<string>().ToHashSet(StringComparer.Ordinal);
        Assets.Clear();
        foreach (var asset in value.Photos)
        {
            Assets.Add(new AssetItemViewModel(asset, used.Contains(asset.Id)));
        }

        RebuildGallery();

        // Değişmeyen sayfaların görünüm modelleri (ve yüklenmiş görüntüleri) korunur; düzenlemede titreme olmaz.
        var reusable = previousProject is not null && ReferenceEquals(previousProject.Assets, value.Assets) && ReferenceEquals(previousProject.Profile, value.Profile)
            ? Pages.ToDictionary(p => p.Page, (IEqualityComparer<AlbumPage>)ReferenceEqualityComparer.Instance)
            : null;
        Pages.Clear();
        for (var i = 0; i < value.Pages.Count; i++)
        {
            Pages.Add(reusable is not null && reusable.TryGetValue(value.Pages[i], out var kept) && kept.Index == i
                ? kept
                : new PageItemViewModel(value.Pages[i], i, value));
        }

        if (selectedIndex is { } index)
        {
            SelectedPage = Pages.ElementAtOrDefault(Math.Min(index, Pages.Count - 1));
        }

        OnPropertyChanged(nameof(Project));
        OnPropertyChanged(nameof(HasProject));
        OnPropertyChanged(nameof(HasNoProject));
        OnPropertyChanged(nameof(HasPhotos));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PhotoSummary));
        OnPropertyChanged(nameof(ProfileSummary));
        OnPropertyChanged(nameof(SelectedPageInfo));
        RefreshDesign(value);
        RefreshCommands();
    }

    private void RefreshCommands()
    {
        NewCommand.NotifyCanExecuteChanged();
        OpenCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        SaveAsCommand.NotifyCanExecuteChanged();
        ImportCommand.NotifyCanExecuteChanged();
        AutoLayoutCommand.NotifyCanExecuteChanged();
        ShuffleCommand.NotifyCanExecuteChanged();
        PreflightCommand.NotifyCanExecuteChanged();
        EditProfileCommand.NotifyCanExecuteChanged();
        ExportPdfCommand.NotifyCanExecuteChanged();
        ToggleLockCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
        RefreshShellCommands();
        RefreshPageCommands();
        RefreshTextCommands();
        RefreshDesignCommands();
        RefreshEditCommands();
    }

    private static IReadOnlyList<TemplateDefinition> UsedTemplates(AlbumProject project)
    {
        var ids = project.Pages.Select(p => p.TemplateId).OfType<string>().ToHashSet(StringComparer.Ordinal);
        return [.. BuiltInTemplates.All.Where(t => ids.Contains(t.Id))];
    }

    private static string TemplateName(string? id) => id is null ? "Şablonsuz" : BuiltInTemplates.Find(id)?.Name ?? id;
}
