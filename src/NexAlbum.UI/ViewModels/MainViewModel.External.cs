using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Assets.Metadata;
using NexAlbum.Core.Projects;
using NexAlbum.Integrations.Editing;
using NexAlbum.Integrations.Photoshop;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Harici düzenleyiciler (Karar 20): düzenleyicide aç + kayıt izleme, toplu düzenleme, önce/sonra, düzenlemeyi kaldır.
/// Orijinal dosyalar hiçbir koşulda değişmez.
/// </summary>
public sealed partial class MainViewModel : IBatchEditHost
{
    private readonly Dictionary<string, EditFileWatcher> watchers = new(StringComparer.Ordinal);
    private IAppSettingsStore settingsStore = null!;
    private AppSettings appSettings = new();
    private PhotoshopEngine? photoshop;
    private IPhotoshopHost? photoshopHost;
    private SynchronizationContext? uiContext;
    private AssetItemViewModel? selectedAsset;

    public RelayCommand OpenInEditorCommand { get; private set; } = null!;

    public RelayCommand BatchEditCommand { get; private set; } = null!;

    public RelayCommand RemoveEditCommand { get; private set; } = null!;

    public RelayCommand EditSettingsCommand { get; private set; } = null!;

    public AppSettings AppSettings => appSettings;

    /// <summary>Düzenleyicide kaydedilen dosyalar izlensin mi? (Testler olayları doğrudan çağırır.)</summary>
    public bool WatchEditorSaves { get; set; } = true;

    /// <summary>Galeride seçili fotoğraf (sağ tık menüsü bunun üzerinde çalışır).</summary>
    public AssetItemViewModel? SelectedAsset
    {
        get => selectedAsset;
        set
        {
            var previous = selectedAsset;
            if (SetProperty(ref selectedAsset, value))
            {
                previous?.IsSelected = false;
                value?.IsSelected = true;
                RefreshExternalCommands();
                ToggleFavoriteCommand?.NotifyCanExecuteChanged();
                SplitStoryHereCommand?.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Seçili yuvadaki fotoğrafın proje kaydı (etkin kaynak değil, orijinal bilgisiyle).</summary>
    public PhotoAsset? SelectedSlotAsset => SelectedSlot?.Asset is { } a ? project?.FindAsset(a.Id) : null;

    public bool SelectedSlotHasEdit => SelectedSlotAsset?.Edit is not null;

    public string SelectedSlotEditInfo => SelectedSlotAsset?.Edit is not { } edit
        ? string.Empty
        : string.Format(
            CultureInfo.CurrentCulture,
            "Düzenlendi: {0}{1} · {2:g}{3}",
            edit.Tool,
            edit.Recipe is { } r ? " · " + r : string.Empty,
            edit.CreatedAt.ToLocalTime(),
            edit.Missing ? " · dosya bulunamadı, orijinal kullanılıyor" : string.Empty);

    /// <summary>Önce/sonra: true iken seçili yuvada orijinal gösterilir (ekran, ön kontrol ve PDF'te).</summary>
    public bool ShowOriginal
    {
        get => SelectedSlotAsset is { Edit: not null, UseEdit: false };
        set
        {
            if (project is not null && SelectedSlotAsset is { Edit: not null } asset && asset.UseEdit == value)
            {
                UpdateProject(PhotoEdits.SetUseEdit(project, asset.Id, use: !value), "Önce/sonra");
                Status = value ? asset.FileName + ": orijinal gösteriliyor." : asset.FileName + ": düzenlenmiş sürüm gösteriliyor.";
            }
        }
    }

    PhotoshopEngine IBatchEditHost.Photoshop => photoshop ??= new PhotoshopEngine(photoshopHost ??= new ComPhotoshopHost());

    TimeProvider IBatchEditHost.Clock => clock;

    string IBatchEditHost.OutputFolder => EditsFolder();

    /// <summary>Bu projenin düzenlenmiş dosyalarının klasörü.</summary>
    public string EditsFolder() => EditFolders.ForProject(appSettings.EditsFolder ?? EditFolders.DefaultRoot, project ?? throw new InvalidOperationException("Proje yok."));

    string? IBatchEditHost.ToolPath(string presetId) => appSettings.ToolPaths.GetValueOrDefault(presetId);

    void IBatchEditHost.RememberToolPath(string presetId, string path)
    {
        var paths = new Dictionary<string, string>(appSettings.ToolPaths, StringComparer.Ordinal) { [presetId] = path };
        SaveSettings(appSettings with { ToolPaths = paths });
    }

    string? IBatchEditHost.AskExecutable() => dialogs.AskExecutable();

    void IBatchEditHost.Apply(EditBatchResult result) => ApplyEditResult(result);

    /// <summary>Toplu düzenleme penceresinin modeli: kapsamlar seçili yuva/galeri, sayfa, albüm ve tüm fotoğraflardır.</summary>
    public BatchEditViewModel? CreateBatchEdit()
    {
        if (project is null)
        {
            return null;
        }

        var selected = (SelectedSlotAsset ?? SelectedAsset?.Asset) is { IsPhoto: true } one ? [one] : Array.Empty<PhotoAsset>();
        var page = SelectedPage is null ? [] : Distinct(SelectedPage.Page.Slots.Select(s => s.Placement?.AssetId));
        var album = Distinct(project.Pages.SelectMany(p => p.Slots).Select(s => s.Placement?.AssetId));
        IReadOnlyList<EditScopeOption> scopes =
        [
            new(EditScope.Selected, "Seçili fotoğraf", selected),
            new(EditScope.Page, "Bu sayfadakiler", page),
            new(EditScope.Album, "Albümde kullanılanlar", album),
            new(EditScope.All, "Tüm fotoğraflar", project.Photos),
            new(EditScope.Unedited, "Henüz düzenlenmemişler", [.. project.Photos.Where(p => p.Edit is null)]),
        ];
        return new BatchEditViewModel(scopes, project.Settings.Editing.LastRecipe, this);
    }

    /// <summary>Toplu düzenleme sonucunu projeye bağlar; değişen görüntüler önbellekten atılır.</summary>
    public void ApplyEditResult(EditBatchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (project is null || result.Edits.Count == 0)
        {
            return;
        }

        foreach (var edit in result.Edits.Values)
        {
            ImageLoader.Shared.Invalidate(edit.Path);
        }

        UpdateProject(result.ApplyTo(project), "Toplu rötuş");
        Status = string.Format(CultureInfo.CurrentCulture, "{0} fotoğrafa «{1}» uygulandı; orijinaller değişmedi.", result.Edits.Count, result.Recipe.Name);
    }

    /// <summary>
    /// Fotoğrafı harici düzenleyicide açar (seçili yuvadaki, yoksa galeride seçili). Kaydedildiğinde albüm kendiliğinden
    /// yenilenir.
    /// </summary>
    public void OpenInEditor(string? assetId = null)
    {
        assetId ??= SelectedSlotAsset?.Id ?? SelectedAsset?.Asset.Id;
        if (project?.FindAsset(assetId ?? string.Empty) is not { } asset)
        {
            Status = "Önce tuvalde ya da galeride bir fotoğraf seçin.";
            return;
        }

        string file;
        try
        {
            file = ExternalEditor.PrepareWorkingFile(asset, EditsFolder());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            dialogs.Warn("Düzenlenecek dosya hazırlanamadı: " + ex.Message);
            return;
        }

        if (WatchEditorSaves)
        {
            Watch(asset.Id, file);
        }
        var editor = appSettings.EditorPath ?? PhotoshopLocator.FindExecutable();
        try
        {
            dialogs.LaunchEditor(file, editor);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            dialogs.Warn("Düzenleyici başlatılamadı: " + ex.Message + "\n\nAyarlar'dan düzenleyici programını seçebilirsiniz.");
            return;
        }

        Status = string.Format(
            CultureInfo.CurrentCulture,
            "{0} {1} içinde açıldı. Kaydettiğinizde albüm kendiliğinden güncellenir (orijinal değişmez).",
            asset.FileName,
            editor is null ? "varsayılan düzenleyici" : Path.GetFileNameWithoutExtension(editor));
    }

    /// <summary>Harici düzenleyici dosyayı kaydetti: boyutu okunur, düzenleme bağlanır, görüntüler yenilenir.</summary>
    public void OnExternalEditSaved(string assetId, string path)
    {
        if (project?.FindAsset(assetId) is not { } asset)
        {
            return;
        }

        PhotoMetadata? metadata = null;
        try
        {
            using var stream = File.OpenRead(path);
            metadata = new MetadataExtractorReader().Read(stream);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        if (metadata is null)
        {
            Status = asset.FileName + ": kaydedilen dosya okunamadı; düzenleyicide JPEG ya da PNG olarak kaydedin.";
            return;
        }

        var editor = appSettings.EditorPath ?? PhotoshopLocator.FindExecutable();
        ImageLoader.Shared.Invalidate(path);
        UpdateProject(PhotoEdits.Apply(project, asset.Id, new PhotoEdit
        {
            Path = Path.GetFullPath(path),
            PixelWidth = metadata.Width,
            PixelHeight = metadata.Height,
            ExifOrientation = metadata.Orientation is >= 1 and <= 8 ? metadata.Orientation : null,
            Tool = editor is null ? "Harici düzenleyici" : Path.GetFileNameWithoutExtension(editor),
            Recipe = asset.Edit?.Path == Path.GetFullPath(path) ? asset.Edit.Recipe : null,
            CreatedAt = clock.GetUtcNow(),
        }), "Harici düzenleme");
        Status = asset.FileName + ": düzenleyicideki değişiklik albüme yansıdı.";
    }

    /// <summary>Seçili fotoğrafın düzenlemesini kaldırır (dosya diskte kalır).</summary>
    public void RemoveEdit()
    {
        var asset = SelectedSlotAsset ?? SelectedAsset?.Asset;
        if (project is null || asset?.Edit is null)
        {
            return;
        }

        if (!dialogs.Confirm(asset.FileName + " düzenlemesi kaldırılsın mı? Fotoğraf orijinaline döner; düzenlenmiş dosya diskte kalır."))
        {
            return;
        }

        StopWatching(asset.Id);
        UpdateProject(PhotoEdits.Remove(project, asset.Id), "Düzenlemeyi kaldır");
        Status = asset.FileName + ": orijinaline döndü.";
    }

    public void EditSettings()
    {
        if (dialogs.EditAppSettings(appSettings) is { } updated)
        {
            SaveSettings(updated);
            SetTheme(ThemeService.Parse(updated.Theme), save: false);
            Status = "Ayarlar kaydedildi.";
        }
    }

    private void InitializeExternal(IAppSettingsStore? store, IPhotoshopHost? host)
    {
        uiContext = SynchronizationContext.Current;
        settingsStore = store ?? new MemoryAppSettingsStore();
        appSettings = settingsStore.Load();
        photoshopHost = host;
        OpenInEditorCommand = new RelayCommand(() => OpenInEditor(), () => HasProject && !IsBusy && (SelectedSlotAsset is not null || SelectedAsset is not null));
        BatchEditCommand = new RelayCommand(ShowBatchEdit, () => HasPhotos && !IsBusy);
        RemoveEditCommand = new RelayCommand(RemoveEdit, () => (SelectedSlotAsset ?? SelectedAsset?.Asset)?.Edit is not null && !IsBusy);
        EditSettingsCommand = new RelayCommand(EditSettings);
    }

    private void ShowBatchEdit()
    {
        if (CreateBatchEdit() is { } vm)
        {
            dialogs.ShowBatchEdit(vm);
        }
    }

    private void RefreshExternalCommands()
    {
        OnPropertyChanged(nameof(SelectedSlotHasEdit));
        OnPropertyChanged(nameof(SelectedSlotEditInfo));
        OnPropertyChanged(nameof(ShowOriginal));
        OpenInEditorCommand.NotifyCanExecuteChanged();
        BatchEditCommand.NotifyCanExecuteChanged();
        RemoveEditCommand.NotifyCanExecuteChanged();
    }

    private void SaveSettings(AppSettings updated)
    {
        appSettings = updated;
        try
        {
            settingsStore.Save(updated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            dialogs.Warn("Ayarlar kaydedilemedi: " + ex.Message);
        }

        OnPropertyChanged(nameof(AppSettings));
    }

    private void Watch(string assetId, string file)
    {
        StopWatching(assetId);
        var watcher = new EditFileWatcher(file);
        watcher.Saved += (_, path) =>
        {
            if (uiContext is { } context)
            {
                context.Post(_ => OnExternalEditSaved(assetId, path), null);
            }
            else
            {
                OnExternalEditSaved(assetId, path);
            }
        };
        watchers[assetId] = watcher;
    }

    private void StopWatching(string assetId)
    {
        if (watchers.Remove(assetId, out var watcher))
        {
            watcher.Dispose();
        }
    }

    private void StopAllWatchers()
    {
        foreach (var watcher in watchers.Values)
        {
            watcher.Dispose();
        }

        watchers.Clear();
    }

    private IReadOnlyList<PhotoAsset> Distinct(IEnumerable<string?> ids) =>
        [.. ids.OfType<string>().Distinct(StringComparer.Ordinal).Select(id => project!.FindAsset(id)).OfType<PhotoAsset>().Where(a => a.IsPhoto)];
}
