using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>Hikâye panelinde bir makinenin saat düzeltmesi satırı.</summary>
public sealed class CameraClockItemViewModel(MainViewModel owner, CameraClock camera) : ObservableObject
{
    private readonly CameraClock info = camera;

    public string Key => info.Key;

    public string Name => info.Key;

    public string Details => string.Format(CultureInfo.CurrentCulture, "{0} fotoğraf", info.Count);

    public bool IsShifted => info.Offset != TimeSpan.Zero;

    /// <summary>Düzeltme ("+1 sa 7 dk"); yazınca uygulanır (geri alınabilir). Okunamayan değer eskiye döner.</summary>
    public string Offset
    {
        get => CameraClocks.Format(info.Offset);
        set
        {
            if (CameraClocks.TryParse(value, out var offset))
            {
                owner.SetCameraOffset(Key, offset);
            }
            else
            {
                owner.ReportClockInputError();
                OnPropertyChanged();
            }
        }
    }

    public IRelayCommand ResetCommand => field ??= new RelayCommand(() => owner.SetCameraOffset(info.Key, TimeSpan.Zero), () => IsShifted);

    public IRelayCommand HourBackCommand => field ??= new RelayCommand(() => owner.SetCameraOffset(info.Key, info.Offset - TimeSpan.FromHours(1)));

    public IRelayCommand HourForwardCommand => field ??= new RelayCommand(() => owner.SetCameraOffset(info.Key, info.Offset + TimeSpan.FromHours(1)));
}

/// <summary>
/// Makine saat farkı düzeltme (Karar 09 Rev. 6): her makine için saat kaydırma, otomatik eşitleme önerisi ve "bu iki fotoğraf
/// aynı anda çekildi" eşitlemesi. Değişiklikler geri alınabilir; hikâye bölümleri, sıralama ve film düzeltilmiş zamanı kullanır.
/// </summary>
public sealed partial class MainViewModel
{
    private string? clockReferenceId;

    public ObservableCollection<CameraClockItemViewModel> CameraClockItems { get; } = [];

    /// <summary>Panelde makine saatleri bölümü görünsün mü (çekim zamanı olan en az bir fotoğraf).</summary>
    public bool HasCameraClocks => CameraClockItems.Count > 0;

    /// <summary>Birden çok makine var mı (otomatik eşitleme için).</summary>
    public bool HasSeveralCameras => CameraClockItems.Count > 1;

    public RelayCommand SyncClocksCommand { get; private set; } = null!;

    public RelayCommand MarkClockReferenceCommand { get; private set; } = null!;

    public RelayCommand MatchClockToReferenceCommand { get; private set; } = null!;

    /// <summary>"Aynı an" eşitlemesinin başvuru fotoğrafı seçili mi?</summary>
    public bool HasClockReference => clockReferenceId is not null && project?.Assets.Any(a => a.Id == clockReferenceId) == true;

    internal void ReportClockInputError() => Status = "Saat farkı okunamadı. Örnek: +1:07, -0:12, +1 sa 7 dk ya da 67 (dakika).";

    public void SetCameraOffset(string cameraKey, TimeSpan offset)
    {
        if (project is null)
        {
            return;
        }

        var changed = project.WithOffset(cameraKey, offset);
        if (ReferenceEquals(changed, project))
        {
            return;
        }

        UpdateProject(changed.Touch(clock.GetUtcNow()), "Makine saati", record: true);
        Status = string.Format(CultureInfo.CurrentCulture, "{0} saati {1} düzeltildi. Bölümleri yeniden bulmak için «Bölümleri bul».", cameraKey, CameraClocks.Format(offset));
    }

    /// <summary>En çok fotoğraflı makineyi başvuru alarak diğerlerini çekim yoğunluklarına göre eşitler.</summary>
    public void SyncClocks()
    {
        if (project is null)
        {
            return;
        }

        var suggestions = CameraClocks.SuggestAll(project);
        var changed = project;
        foreach (var (key, offset) in suggestions)
        {
            changed = changed.WithOffset(key, offset);
        }

        if (ReferenceEquals(changed, project))
        {
            Status = suggestions.Count == 0
                ? "Belirgin bir saat farkı bulunamadı. Gerekirse farkı elle yazın ya da aynı anın iki fotoğrafıyla eşitleyin."
                : "Makine saatleri zaten uyumlu.";
            return;
        }

        UpdateProject(changed.Touch(clock.GetUtcNow()), "Saatleri eşitle", record: true);
        Status = "Saatler eşitlendi: " + string.Join(", ", suggestions.Select(s => s.Key + " " + CameraClocks.Format(s.Value)))
            + ". Sonucu zaman çizelgesinde görün; gerekirse geri alın.";
    }

    /// <summary>Galeride seçili fotoğrafı "aynı an" eşitlemesinin başvurusu yapar.</summary>
    public void MarkClockReference()
    {
        if (SelectedAsset?.Asset is not { CapturedAt: not null } asset)
        {
            return;
        }

        clockReferenceId = asset.Id;
        RefreshClockCommands();
        Status = string.Format(CultureInfo.CurrentCulture, "Başvuru: {0} ({1}). Şimdi başka makinenin aynı anı çeken fotoğrafına sağ tıklayıp «Saati başvuruya eşitle».", asset.FileName, CameraClocks.KeyOf(asset));
    }

    /// <summary>Seçili fotoğrafın makinesini, başvuru fotoğrafıyla aynı ana gelecek şekilde kaydırır.</summary>
    public void MatchClockToReference()
    {
        if (project is null || SelectedAsset?.Asset is not { CapturedAt: not null } target
            || project.Assets.FirstOrDefault(a => a.Id == clockReferenceId) is not { CapturedAt: not null } reference)
        {
            return;
        }

        var key = CameraClocks.KeyOf(target);
        if (string.Equals(key, CameraClocks.KeyOf(reference), StringComparison.Ordinal))
        {
            Status = "İki fotoğraf aynı makineden; başka makinenin fotoğrafını seçin.";
            return;
        }

        SetCameraOffset(key, CameraClocks.OffsetToMatch(reference, target));
    }

    private void RefreshCameraClocks(AlbumProject value)
    {
        CameraClockItems.Clear();
        foreach (var camera in CameraClocks.Cameras(value))
        {
            CameraClockItems.Add(new CameraClockItemViewModel(this, camera));
        }

        OnPropertyChanged(nameof(HasCameraClocks));
        OnPropertyChanged(nameof(HasSeveralCameras));
        OnPropertyChanged(nameof(HasClockReference));
    }

    private void InitializeCameraClocks()
    {
        SyncClocksCommand = new RelayCommand(SyncClocks, () => HasSeveralCameras && !IsBusy);
        MarkClockReferenceCommand = new RelayCommand(MarkClockReference, () => SelectedAsset?.Asset.CapturedAt is not null && !IsBusy);
        MatchClockToReferenceCommand = new RelayCommand(
            MatchClockToReference,
            () => HasClockReference && SelectedAsset?.Asset.CapturedAt is not null && SelectedAsset.Asset.Id != clockReferenceId && !IsBusy);
    }

    private void RefreshClockCommands()
    {
        SyncClocksCommand?.NotifyCanExecuteChanged();
        MarkClockReferenceCommand?.NotifyCanExecuteChanged();
        MatchClockToReferenceCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasClockReference));
    }
}
