using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Yuvadaki fotoğrafı konumlandırma: imlecin olduğu noktaya yakınlaşma, yön tuşlarıyla kaydırma, 3×3 hizalama ve
/// "konumlandırma kipi" (çift tıkla: düz sürükleme fotoğrafı yuvanın içinde kaydırır). Metin seçiliyse yön tuşları metni taşır.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Yön tuşu adımı: yuva ölçüsünün %1'i (Shift ile %10).</summary>
    public const double NudgeStep = 0.01;

    /// <summary>Metin için yön tuşu adımı (mm; Shift ile 10 katı).</summary>
    public const double TextNudgeMm = 0.5;

    private bool isPositioning;

    /// <summary>Fotoğrafın görünen bölgesini seçer; parametre "yatay dikey" (−1 sol/üst, 0 orta, 1 sağ/alt), ör. "0 0".</summary>
    public RelayCommand<string> AlignPhotoCommand { get; private set; } = null!;

    public RelayCommand TogglePositioningCommand { get; private set; } = null!;

    /// <summary>
    /// Konumlandırma kipi: seçili yuvada düz sürükleme fotoğrafı kaydırır (yuvadan yuvaya taşımaz). Başka yuva seçilince,
    /// Esc'e basınca ya da boş yere tıklanınca kapanır.
    /// </summary>
    public bool IsPositioning
    {
        get => isPositioning && SelectedSlot is { IsEmpty: false };
        set
        {
            var on = value && SelectedSlot is { IsEmpty: false };
            if (isPositioning != on)
            {
                isPositioning = on;
                SelectedSlot?.IsPositioning = on;
                OnPropertyChanged(nameof(IsPositioning));
                OnPropertyChanged(nameof(PositioningButtonText));
                if (on)
                {
                    Status = "Konumlandırma: sürükleyerek kaydırın, tekerlekle imlecin olduğu yere yakınlaşın, yön tuşlarıyla ince ayar yapın. Bitirmek için Esc.";
                }
            }
        }
    }

    public string PositioningButtonText => IsPositioning ? "Konumlandırmayı bitir" : "Konumlandır";

    /// <summary>Tekerlekle, imlecin altındaki nokta yerinde kalacak biçimde yakınlaştırır.</summary>
    /// <param name="slot">Yuva.</param>
    /// <param name="factor">Çarpan (&gt;1 yakınlaştırır).</param>
    /// <param name="xMm">İmlecin yuva içindeki konumu (yuvanın sol üst köşesine göre, mm).</param>
    /// <param name="yMm">İmlecin yuva içindeki konumu (mm).</param>
    public void ZoomSlotAt(SlotItemViewModel slot, double factor, double xMm, double yMm)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.Placement is not { } placement || slot.Asset is not { } asset)
        {
            return;
        }

        var area = slot.Slot.PhotoArea;
        var bounds = slot.Slot.Bounds;
        var anchorX = (bounds.X + xMm - (area.X + (area.Width / 2))) / area.Width;
        var anchorY = (bounds.Y + yMm - (area.Y + (area.Height / 2))) / area.Height;
        var zoomed = PageEditor.ZoomAt(placement, area, asset.DisplayAspectRatio, slot.Slot.CropMode, factor, Math.Clamp(anchorX, -0.5, 0.5), Math.Clamp(anchorY, -0.5, 0.5));
        SetPlacement(slot, zoomed, "Yakınlaştırma");
    }

    /// <summary>Yön tuşları: metin seçiliyse metni (mm), değilse seçili yuvadaki fotoğrafı (yuva oranı) kaydırır.</summary>
    /// <returns>Bir şey kaydırıldıysa true (tuş işlenmiş sayılır).</returns>
    public bool Nudge(int dx, int dy, bool large)
    {
        if (SelectedText is { } text && SelectedPage is { } page)
        {
            var step = TextNudgeMm * (large ? 10 : 1);
            var x = text.Text.Bounds.X + (dx * step);
            var y = text.Text.Bounds.Y + (dy * step);
            var now = clock.GetUtcNow();
            KeepTextSelected(text.Id);
            EditSelectedPage(p => PageTextEditor.Move(p, text.Id, x, y, page.CanvasBounds, now), "Metin taşındı.");
            return true;
        }

        if (SelectedSlot is not { Placement: { } placement, Asset: { } asset } slot)
        {
            return false;
        }

        var f = NudgeStep * (large ? 10 : 1);
        SetPlacement(slot, PageEditor.Nudge(placement, slot.Slot.PhotoArea, asset.DisplayAspectRatio, slot.Slot.CropMode, dx * f, dy * f), "Fotoğraf kaydırıldı");
        return true;
    }

    /// <summary>+ / − tuşları: seçili yuvadaki fotoğrafı yuvanın ortasına göre yakınlaştırır.</summary>
    public bool ZoomSelected(double factor)
    {
        if (SelectedSlot is not { IsEmpty: false } slot)
        {
            return false;
        }

        ZoomSlotAt(slot, factor, slot.Slot.Bounds.Width / 2, slot.Slot.Bounds.Height / 2);
        return true;
    }

    public void AlignPhoto(string? where)
    {
        if (SelectedSlot is not { Placement: { } placement, Asset: { } asset } slot || where is null)
        {
            return;
        }

        var parts = where.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var h)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
        {
            return;
        }

        SetPlacement(slot, PageEditor.Align(placement, slot.Slot.PhotoArea, asset.DisplayAspectRatio, slot.Slot.CropMode, h, v), h == 0 && v == 0 ? "Ortalandı" : "Hizalandı");
    }

    /// <summary>Yuva seçimi değişince (yeniden kurulan aynı yuva hariç) kip kapanır.</summary>
    private void SyncPositioning(SlotItemViewModel? slot)
    {
        slot?.IsPositioning = isPositioning && slot is { IsEmpty: false };
        OnPropertyChanged(nameof(IsPositioning));
        OnPropertyChanged(nameof(PositioningButtonText));
        RefreshPositioningCommands();
    }

    private void InitializePositioning()
    {
        AlignPhotoCommand = new RelayCommand<string>(AlignPhoto, _ => SelectedSlot is { IsEmpty: false } && SelectedPage is { IsLocked: false });
        TogglePositioningCommand = new RelayCommand(() => IsPositioning = !IsPositioning, () => SelectedSlot is { IsEmpty: false });
    }

    private void RefreshPositioningCommands()
    {
        AlignPhotoCommand?.NotifyCanExecuteChanged();
        TogglePositioningCommand?.NotifyCanExecuteChanged();
    }
}
