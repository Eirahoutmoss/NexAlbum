using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Seçili yuvadaki fotoğrafın tahribatsız ayarları (efekt belgesi §4.3): pozlama, parlaklık, kontrast, sıcaklık, doygunluk,
/// siyah-beyaz, düzeltme açısı ve çeyrek döndürme. Ayarlar yerleşimde saklanır; ekran ve PDF aynı renk matrisini kullanır.
/// Kaydırıcılar −100…+100 gösterir; her değişiklik geri alınabilir.
/// </summary>
public sealed partial class MainViewModel
{
    private const string AdjustLabel = "Fotoğraf ayarı";
    private bool compareAdjustments;

    public RelayCommand RotateLeftCommand { get; private set; } = null!;

    public RelayCommand RotateRightCommand { get; private set; } = null!;

    public RelayCommand ResetAdjustmentsCommand { get; private set; } = null!;

    public RelayCommand AdjustmentsToPageCommand { get; private set; } = null!;

    private ImageAdjustments CurrentAdjustments => SelectedSlot?.Slot.Placement?.Adjustments ?? ImageAdjustments.None;

    public double AdjustExposure
    {
        get => CurrentAdjustments.Exposure * 100;
        set => Adjust(a => a with { Exposure = Unit(value) });
    }

    public double AdjustBrightness
    {
        get => CurrentAdjustments.Brightness * 100;
        set => Adjust(a => a with { Brightness = Unit(value) });
    }

    public double AdjustContrast
    {
        get => CurrentAdjustments.Contrast * 100;
        set => Adjust(a => a with { Contrast = Unit(value) });
    }

    public double AdjustTemperature
    {
        get => CurrentAdjustments.Temperature * 100;
        set => Adjust(a => a with { Temperature = Unit(value) });
    }

    public double AdjustSaturation
    {
        get => CurrentAdjustments.Saturation * 100;
        set => Adjust(a => a with { Saturation = Unit(value) });
    }

    public bool AdjustBlackWhite
    {
        get => CurrentAdjustments.BlackWhite;
        set => Adjust(a => a with { BlackWhite = value });
    }

    /// <summary>Düzeltme açısı (±15°): eğri ufku doğrultur; fotoğraf yuvayı kaplamaya devam eder.</summary>
    public double Straighten
    {
        get => SelectedSlot?.Slot.Placement?.Rotation ?? 0;
        set
        {
            if (SelectedSlot is { IsEmpty: false } slot)
            {
                var now = clock.GetUtcNow();
                RememberSlot(slot.Id);
                EditSelectedPage(p => PhotoEditing.SetStraighten(p, slot.Id, value, now), "Düzeltme");
                Status = string.Format(CultureInfo.CurrentCulture, "Düzeltme: {0:0.#}°", Straighten);
            }
        }
    }

    public bool HasAdjustments => !CurrentAdjustments.IsNone || Straighten != 0;

    /// <summary>Öncesi/sonrası: ayarlar ekranda geçici olarak kapatılır (proje ve PDF değişmez).</summary>
    public bool CompareAdjustments
    {
        get => compareAdjustments;
        set
        {
            if (SetProperty(ref compareAdjustments, value) && SelectedSlot is { } slot)
            {
                slot.ShowUnadjusted = value;
            }
        }
    }

    /// <summary>Fotoğrafı saat yönünde (+1) ya da tersine (−1) 90° döndürür; albümün her yerinde geçerlidir.</summary>
    public void RotateSelectedPhoto(int quarterTurns)
    {
        if (project is null || SelectedSlot is not { Asset: { } asset } slot)
        {
            return;
        }

        RememberSlot(slot.Id);
        UpdateProject(PhotoEditing.RotateQuarter(project, asset.Id, quarterTurns, clock.GetUtcNow()), "Döndürme");
        Status = asset.FileName + (quarterTurns > 0 ? " sağa" : " sola") + " döndürüldü (albümün her yerinde).";
    }

    public void ResetAdjustments()
    {
        if (SelectedSlot is not { IsEmpty: false } slot)
        {
            return;
        }

        var now = clock.GetUtcNow();
        RememberSlot(slot.Id);
        if (EditSelectedPage(p => PhotoEditing.SetStraighten(PhotoEditing.SetAdjustments(p, slot.Id, ImageAdjustments.None, now), slot.Id, 0, now), "Fotoğraf ayarları sıfırlandı."))
        {
            CompareAdjustments = false;
        }
    }

    /// <summary>Seçili fotoğrafın renk ayarlarını sayfadaki tüm fotoğraflara uygular (onayla; geri alınabilir).</summary>
    public void AdjustmentsToPage()
    {
        if (SelectedSlot is not { IsEmpty: false } || SelectedPage is not { } page)
        {
            return;
        }

        var count = page.Slots.Count(s => !s.IsEmpty);
        var adjustments = CurrentAdjustments;
        if (count < 2 || !dialogs.Confirm(string.Format(CultureInfo.CurrentCulture, "Bu fotoğrafın renk ayarları sayfadaki {0} fotoğrafın tümüne uygulansın mı?", count)))
        {
            return;
        }

        var now = clock.GetUtcNow();
        EditSelectedPage(p => PhotoEditing.SetAdjustmentsForAll(p, adjustments, now), string.Format(CultureInfo.CurrentCulture, "Renk ayarları {0} fotoğrafa uygulandı.", count));
    }

    private static double Unit(double percent) => Math.Round(Math.Clamp(percent, -100, 100)) / 100;

    private void Adjust(Func<ImageAdjustments, ImageAdjustments> change)
    {
        if (SelectedSlot is not { IsEmpty: false } slot)
        {
            return;
        }

        var updated = change(CurrentAdjustments);
        var now = clock.GetUtcNow();
        RememberSlot(slot.Id);
        EditSelectedPage(p => PhotoEditing.SetAdjustments(p, slot.Id, updated, now), AdjustLabel + " uygulandı; Ctrl+Z ile geri alınır.");
    }

    private void InitializePhotoAdjust()
    {
        RotateLeftCommand = new RelayCommand(() => RotateSelectedPhoto(-1), () => SelectedSlotHasPhoto && !IsBusy);
        RotateRightCommand = new RelayCommand(() => RotateSelectedPhoto(1), () => SelectedSlotHasPhoto && !IsBusy);
        ResetAdjustmentsCommand = new RelayCommand(ResetAdjustments, () => SelectedSlotHasPhoto && HasAdjustments && !IsBusy);
        AdjustmentsToPageCommand = new RelayCommand(AdjustmentsToPage, () => SelectedSlotHasPhoto && !IsBusy);
    }

    /// <summary>Seçim ya da proje değişince kaydırıcıları ve komutları yeniler; öncesi/sonrası kapanır.</summary>
    private void RefreshAdjustments(SlotItemViewModel? slot)
    {
        compareAdjustments = false;
        OnPropertyChanged(nameof(CompareAdjustments));
        if (slot is not null)
        {
            slot.ShowUnadjusted = false;
        }

        OnPropertyChanged(nameof(AdjustExposure));
        OnPropertyChanged(nameof(AdjustBrightness));
        OnPropertyChanged(nameof(AdjustContrast));
        OnPropertyChanged(nameof(AdjustTemperature));
        OnPropertyChanged(nameof(AdjustSaturation));
        OnPropertyChanged(nameof(AdjustBlackWhite));
        OnPropertyChanged(nameof(Straighten));
        OnPropertyChanged(nameof(HasAdjustments));
        RotateLeftCommand?.NotifyCanExecuteChanged();
        RotateRightCommand?.NotifyCanExecuteChanged();
        ResetAdjustmentsCommand?.NotifyCanExecuteChanged();
        AdjustmentsToPageCommand?.NotifyCanExecuteChanged();
    }
}
