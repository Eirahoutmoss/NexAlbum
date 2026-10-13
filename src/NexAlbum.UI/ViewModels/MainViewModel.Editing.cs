using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Assets.Import;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Elle düzenleme ve içe aktarma: tek tek dosya seçme, sürükle-bırak (Gezgin'den, galeriden, yuvadan yuvaya),
/// yuva seçimi, yuva içinde kaydırma/yakınlaştırma, sığdırma modu, fotoğrafı çıkarma. Kilitli sayfa değişmez.
/// </summary>
public sealed partial class MainViewModel
{
    private SlotItemViewModel? selectedSlot;
    private string? selectedSlotId;
    private string? selectedSlotPageId;

    public AsyncRelayCommand ImportFilesCommand { get; private set; } = null!;

    public RelayCommand ClearSlotCommand { get; private set; } = null!;

    public RelayCommand CenterPhotoCommand { get; private set; } = null!;

    public RelayCommand ToggleFitCommand { get; private set; } = null!;

    /// <summary>Tuvalde seçili yuva (yalnızca seçili sayfada).</summary>
    public SlotItemViewModel? SelectedSlot
    {
        get => selectedSlot;
        private set
        {
            var previous = selectedSlot;
            if (SetProperty(ref selectedSlot, value))
            {
                if (previous is not null)
                {
                    previous.IsSelected = false;
                }

                if (value is not null)
                {
                    value.IsSelected = true;
                }

                OnPropertyChanged(nameof(HasSelectedSlot));
                OnPropertyChanged(nameof(SelectedSlotHasPhoto));
                OnPropertyChanged(nameof(SelectedSlotInfo));
                OnPropertyChanged(nameof(SlotZoomPercent));
                OnPropertyChanged(nameof(FitButtonText));
                RefreshEditCommands();
                RefreshGalleryCommands();
                SyncPositioning(value);
                RefreshAdjustments(value);
                SyncFrame(value);
            }
        }
    }

    public bool HasSelectedSlot => SelectedSlot is not null;

    public bool SelectedSlotHasPhoto => SelectedSlot is { IsEmpty: false };

    public string SelectedSlotInfo
    {
        get
        {
            if (SelectedSlot is not { } slot)
            {
                return string.Empty;
            }

            if (slot.Asset is not { } asset || slot.Placement is not { } placement)
            {
                return "Boş yuva — galeriden bir fotoğraf sürükleyip bırakın ya da galeride çift tıklayın.";
            }

            var dpi = PlacementDpi.Calculate(asset, slot.Slot, placement);
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0}\nEtkin çözünürlük: {1:0} dpi · {2}",
                asset.FileName,
                dpi,
                slot.Slot.CropMode == CropMode.Contain ? "sığdırılmış" : "kaplıyor");
        }
    }

    /// <summary>Seçili yuvadaki fotoğrafın yakınlaştırması (%100–%500).</summary>
    public double SlotZoomPercent
    {
        get => (SelectedSlot?.Placement?.Scale ?? 1) * 100;
        set
        {
            if (SelectedSlot is { Placement: { } placement } slot && Math.Abs((placement.Scale * 100) - value) > 0.5)
            {
                SetPlacement(slot, placement with { Scale = value / 100 }, "Yakınlaştırma");
            }
        }
    }

    public string FitButtonText => SelectedSlot?.Slot.CropMode == CropMode.Contain ? "Yuvayı kapla" : "Tamamını göster";

    /// <summary>Tuvalde tıklanan yuvayı seçer (null: seçimi kaldırır).</summary>
    public void SelectSlot(SlotItemViewModel? slot)
    {
        // Yuva seçimi (ya da boş tuvale tıklama) metin seçimini kaldırır.
        selectedTextId = null;
        SelectedText = null;
        if (!string.Equals(slot?.Id, selectedSlotId, StringComparison.Ordinal))
        {
            IsPositioning = false;
        }

        RememberSlot(slot?.Id);
        SelectedSlot = slot;
    }

    /// <summary>Galeriden ya da Gezgin'den gelen fotoğrafı yuvaya koyar; aynı sayfadaki başka yuvadaysa yer değiştirir.</summary>
    public bool PlacePhoto(SlotItemViewModel target, string assetId)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (project?.FindAsset(assetId) is not { IsPhoto: true } asset)
        {
            return false;
        }

        var now = clock.GetUtcNow();
        RememberSlot(target.Id);
        return EditSelectedPage(p => PageEditor.PlacePhoto(p, target.Id, asset.Id, now), asset.FileName + " yerleştirildi.");
    }

    /// <summary>Galeride çift tık: seçili yuvaya, yoksa sayfadaki ilk boş yuvaya koyar.</summary>
    public bool PlaceIntoSelection(string assetId)
    {
        var target = SelectedSlot ?? SelectedPage?.Slots.FirstOrDefault(s => s.IsEmpty);
        if (target is null)
        {
            Status = "Fotoğrafı koymak için tuvalde bir yuva seçin (sayfada boş yuva yok).";
            return false;
        }

        return PlacePhoto(target, assetId);
    }

    /// <summary>Yuvadan yuvaya sürükleme: iki yuvanın fotoğrafları yer değiştirir (hedef boşsa taşınır).</summary>
    public bool MoveSlot(string sourceSlotId, SlotItemViewModel target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.Equals(sourceSlotId, target.Id, StringComparison.Ordinal) || SelectedPage?.Page.FindElement(sourceSlotId) is not Slot)
        {
            return false;
        }

        var now = clock.GetUtcNow();
        RememberSlot(target.Id);
        return EditSelectedPage(p => PageEditor.Swap(p, sourceSlotId, target.Id, now), target.IsEmpty ? "Fotoğraf taşındı." : "Fotoğraflar yer değiştirdi.");
    }

    public void ClearSelectedSlot()
    {
        if (SelectedSlot is { IsEmpty: false } slot)
        {
            var now = clock.GetUtcNow();
            EditSelectedPage(p => PageEditor.Clear(p, slot.Id, now), "Fotoğraf yuvadan çıkarıldı; galeride «kullanılmamış» olarak duruyor.");
        }
    }

    public void CenterSelectedPhoto()
    {
        if (SelectedSlot is { Placement: { } placement } slot)
        {
            SetPlacement(slot, placement with { OffsetX = 0, OffsetY = 0, Scale = 1 }, "Fotoğraf ortalandı");
        }
    }

    /// <summary>Kapla (kırparak doldur) ↔ tamamını göster (sığdır).</summary>
    public void ToggleFit()
    {
        if (SelectedSlot is { } slot)
        {
            var mode = slot.Slot.CropMode == CropMode.Contain ? CropMode.Cover : CropMode.Contain;
            var now = clock.GetUtcNow();
            EditSelectedPage(p => PageEditor.SetCropMode(p, slot.Id, mode, now), mode == CropMode.Contain ? "Fotoğrafın tamamı gösteriliyor." : "Fotoğraf yuvayı kaplıyor.");
        }
    }

    /// <summary>Sürükleyerek kaydırma önizlemesi: başlangıç yerleşimine göre mm cinsinden kaydırır (kaydedilmez).</summary>
    public void PreviewPan(SlotItemViewModel slot, PhotoPlacement start, double deltaXMm, double deltaYMm)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(start);
        if (slot.Asset is not { } asset || slot.PhotoWidth <= 0 || slot.PhotoHeight <= 0)
        {
            return;
        }

        var moved = start with { OffsetX = Math.Clamp(start.OffsetX + (deltaXMm / slot.PhotoWidth), -1, 1), OffsetY = Math.Clamp(start.OffsetY + (deltaYMm / slot.PhotoHeight), -1, 1) };
        slot.Preview(PageEditor.Clamp(moved, slot.Slot.PhotoArea, asset.DisplayAspectRatio, slot.Slot.CropMode));
    }

    /// <summary>Önizlenen yerleşimi kaydeder.</summary>
    public void CommitPlacement(SlotItemViewModel slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.Placement is { } placement && placement != slot.Slot.Placement)
        {
            SetPlacement(slot, placement, "Fotoğraf kaydırıldı");
        }
    }

    /// <summary>Tekerlekle yakınlaştırma (çarpan: &gt;1 yakınlaştırır).</summary>
    public void ZoomSlot(SlotItemViewModel slot, double factor)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.Placement is { } placement)
        {
            RememberSlot(slot.Id);
            SetPlacement(slot, placement with { Scale = Math.Clamp(placement.Scale * factor, PageEditor.MinScale, PageEditor.MaxScale) }, "Yakınlaştırma");
        }
    }

    public async Task ImportFilesAsync()
    {
        var files = dialogs.AskPhotoFiles();
        if (files is { Count: > 0 })
        {
            await ImportPathsAsync(files).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Dosya ve klasörleri içe aktarır (klasörler alt klasörleriyle taranır). Hedef yuva verilmiş ve tek fotoğraf
    /// bırakılmışsa, fotoğraf o yuvaya da yerleştirilir.
    /// </summary>
    public async Task ImportPathsAsync(IReadOnlyList<string> paths, AssetRole role = AssetRole.Photo, SlotItemViewModel? target = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        var current = project;
        if (current is null || paths.Count == 0)
        {
            return;
        }

        var files = await Task.Run(() => ExpandImageFiles(paths)).ConfigureAwait(true);
        if (files.Count == 0)
        {
            Status = "Bırakılanlar arasında desteklenen görsel yok (JPEG, PNG, TIFF, WebP, BMP, GIF).";
            return;
        }

        var noun = role == AssetRole.Background ? "fon görseli" : "fotoğraf";
        await RunImportAsync(c => new PhotoImporter().ImportFiles(files, c, new ImportOptions { Role = role }), noun).ConfigureAwait(true);

        if (target is not null && role == AssetRole.Photo && files.Count == 1 && project is { } updated
            && updated.Assets.FirstOrDefault(a => a.IsPhoto && string.Equals(Path.GetFullPath(a.SourcePath), Path.GetFullPath(files[0]), StringComparison.OrdinalIgnoreCase)) is { } asset
            && SelectedPage?.Slots.FirstOrDefault(s => s.Id == target.Id) is { } slot)
        {
            PlacePhoto(slot, asset.Id);
        }
    }

    /// <summary>Ortak içe aktarma akışı: meşgul durumu, hata yakalama, özet.</summary>
    private async Task RunImportAsync(Func<AlbumProject, ImportResult> work, string noun)
    {
        var current = project;
        if (current is null)
        {
            return;
        }

        IsBusy = true;
        Status = "Görseller okunuyor…";
        try
        {
            var result = await Task.Run(() => work(current)).ConfigureAwait(true);
            if (result.Imported.Count > 0)
            {
                UpdateProject(result.ApplyTo(current), "İçe aktarma");
            }

            Status = string.Format(
                CultureInfo.CurrentCulture,
                "{0} {1} eklendi, {2} zaten projedeydi, {3} okunamadı.",
                result.Imported.Count,
                noun,
                result.AlreadyInProject.Count,
                result.Skipped.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            dialogs.Warn("Görseller okunamadı: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static List<string> ExpandImageFiles(IEnumerable<string> paths)
    {
        var files = new List<string>();
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                files.AddRange(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(f => ImportOptions.DefaultExtensions.Contains(Path.GetExtension(f)))
                    .Order(StringComparer.OrdinalIgnoreCase));
            }
            else if (File.Exists(path) && ImportOptions.DefaultExtensions.Contains(Path.GetExtension(path)))
            {
                files.Add(path);
            }
        }

        return files;
    }

    private void InitializeEditing()
    {
        ImportFilesCommand = new AsyncRelayCommand(ImportFilesAsync, () => HasProject && !IsBusy);
        ClearSlotCommand = new RelayCommand(ClearSelectedSlot, () => SelectedSlotHasPhoto && !IsBusy);
        CenterPhotoCommand = new RelayCommand(CenterSelectedPhoto, () => SelectedSlotHasPhoto && !IsBusy);
        ToggleFitCommand = new RelayCommand(ToggleFit, () => SelectedSlotHasPhoto && !IsBusy);
    }

    private void RefreshEditCommands()
    {
        ImportFilesCommand.NotifyCanExecuteChanged();
        ClearSlotCommand.NotifyCanExecuteChanged();
        CenterPhotoCommand.NotifyCanExecuteChanged();
        ToggleFitCommand.NotifyCanExecuteChanged();
        RefreshAdjustments(null);
        RefreshExternalCommands();
    }

    /// <summary>Sayfa yeniden kurulunca seçili yuvayı kimliğiyle geri bulur; başka sayfaya geçilince seçim kalkar.</summary>
    private void RestoreSlotSelection(PageItemViewModel? page)
    {
        if (page is null)
        {
            SelectedSlot = null;
            return;
        }

        // Yuva kimlikleri sayfalar arasında tekrarlanabilir (şablon yuvaları): seçim yalnızca aynı sayfada geri bulunur.
        var slot = selectedSlotId is null || !string.Equals(page.Page.Id, selectedSlotPageId, StringComparison.Ordinal)
            ? null
            : page.Slots.FirstOrDefault(s => string.Equals(s.Id, selectedSlotId, StringComparison.Ordinal));
        if (slot is null)
        {
            selectedSlotId = null;
            isPositioning = false;
        }

        // Aynı nesne yeniden seçilse de bilgi ve kaydırıcı güncellensin.
        SelectedSlot = null;
        SelectedSlot = slot;
    }

    private void RememberSlot(string? id)
    {
        selectedSlotId = id;
        selectedSlotPageId = id is null ? null : SelectedPage?.Page.Id;
    }

    private void SetPlacement(SlotItemViewModel slot, PhotoPlacement placement, string what)
    {
        if (slot.Asset is not { } asset)
        {
            return;
        }

        var now = clock.GetUtcNow();
        RememberSlot(slot.Id);
        if (!EditSelectedPage(p => PageEditor.SetPlacement(p, slot.Id, placement, asset.DisplayAspectRatio, now), null))
        {
            slot.Preview(slot.Slot.Placement ?? placement);
            return;
        }

        Status = string.Format(CultureInfo.CurrentCulture, "{0}: %{1:0}", what, (SelectedSlot?.Placement?.Scale ?? placement.Scale) * 100);
    }

    /// <summary>Seçili sayfayı değiştirir; kilitliyse dokunmaz. Değişiklik olduysa true.</summary>
    private bool EditSelectedPage(Func<AlbumPage, AlbumPage> change, string? status)
    {
        if (project is null || SelectedPage is null)
        {
            return false;
        }

        if (SelectedPage.IsLocked)
        {
            Status = "Sayfa kilitli; düzenlemek için önce kilidi açın.";
            return false;
        }

        var page = SelectedPage.Page;
        var changed = change(page);
        if (ReferenceEquals(changed, page))
        {
            return false;
        }

        UpdateProject(project.ReplacePage(changed), status is null ? "Düzenleme" : status.Split([';', '.'], 2)[0]);
        if (status is not null)
        {
            Status = status;
        }

        return true;
    }
}
