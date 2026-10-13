using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Yuva çerçevesini elle düzenleme (Karar 10 Rev. 4): «Çerçeve» kipinde seçili yuva gövdesinden taşınır, tutamaçlarla
/// boyutlandırılır, geometrik yuvalarda köşeler tek tek sürüklenir. Sürüklerken sayfa ortasına, kenarlara, güvenli alana ve diğer
/// öğelere yapışır (Alt: serbest, Shift: oran korunur). Bırakınca projeye yazılır; geri alınabilir.
/// </summary>
public sealed partial class MainViewModel
{
    private bool isFrameEditing;
    private PageItemViewModel? frameEditPage;

    public RelayCommand ToggleFrameEditingCommand { get; private set; } = null!;

    public RelayCommand ResetFrameCornersCommand { get; private set; } = null!;

    /// <summary>Çerçeve kipi: seçili yuvanın çerçevesi ve köşeleri düzenlenir (yuvadan yuvaya taşıma yerine).</summary>
    public bool IsFrameEditing
    {
        get => isFrameEditing;
        set
        {
            if (isFrameEditing == value)
            {
                return;
            }

            isFrameEditing = value;
            if (value)
            {
                IsPositioning = false;
                Status = SelectedSlot is null
                    ? "Çerçeve: düzenlemek istediğiniz yuvayı seçin."
                    : FrameHint;
            }

            SyncFrame(SelectedSlot);
            OnPropertyChanged(nameof(IsFrameEditing));
            OnPropertyChanged(nameof(FrameButtonText));
        }
    }

    public string FrameButtonText => IsFrameEditing ? "Çerçeveyi bitir" : "Çerçeveyi düzenle";

    private static string FrameHint =>
        "Çerçeve: içinden sürükleyip taşıyın, tutamaçlarla boyutlandırın, köşe noktalarını sürükleyip biçimi değiştirin. Alt: serbest · Shift: oran · Esc: bitir.";

    /// <summary>Sürüklerken önizleme: tutamaç ya da gövde (mm cinsinden yer değiştirme).</summary>
    public void DragFrame(FrameHandle handle, RectMm start, double dxMm, double dyMm, double toleranceMm, bool snap, bool keepAspect)
    {
        if (SelectedPage is not { FrameEdit: { } edit } page)
        {
            return;
        }

        var limits = new RectMm(0, 0, page.CanvasWidth, page.CanvasHeight);
        var rect = SlotFrameEditor.Resize(start, handle, dxMm, dyMm, limits, keepAspect);
        var guides = new List<SnapGuide>();
        if (snap && toleranceMm > 0)
        {
            var others = Others(page, edit.Slot);
            if (handle == FrameHandle.Body)
            {
                var result = SnapGuides.Snap(rect, page.PageArea, page.SafeArea, others, toleranceMm);
                rect = SlotFrameEditor.Move(rect, result.X - rect.X, result.Y - rect.Y, limits);
                guides.AddRange(result.Guides);
            }
            else
            {
                rect = SnapEdges(rect, handle, page, others, toleranceMm, limits, guides);
            }
        }

        edit.SetRect(rect);
        page.ShowSnapLines(guides);
    }

    /// <summary>Köşe sürükleme önizlemesi: köşe yuva içinde kalır; dışbükeylik bozulacaksa önceki konumda kalır.</summary>
    public void DragFrameCorner(int index, double xMm, double yMm)
    {
        if (SelectedPage is not { FrameEdit: { UnitCorners: { } corners } edit })
        {
            return;
        }

        var r = edit.Rect;
        if (SlotFrameEditor.MoveCorner(corners, index, (xMm - r.X) / r.Width, (yMm - r.Y) / r.Height) is { } moved)
        {
            edit.SetCorners(moved);
        }
    }

    /// <summary>Sürükleme bitti: çerçeve (ve değiştiyse köşeler) projeye yazılır.</summary>
    public void CommitFrame()
    {
        if (SelectedPage is not { FrameEdit: { } edit } page)
        {
            return;
        }

        page.ShowSnapLines([]);
        var r = edit.Rect;
        var bounds = new RectMm(r.X + page.CanvasBounds.X, r.Y + page.CanvasBounds.Y, r.Width, r.Height);
        var corners = edit.CornersChanged ? edit.UnitCorners : null;
        var slotId = edit.Slot.Id;
        RememberSlot(slotId);
        var now = clock.GetUtcNow();
        var status = string.Format(CultureInfo.CurrentCulture, "Çerçeve: {0}.", edit.SizeText);
        if (!EditSelectedPage(p => SlotFrameEditor.SetFrame(p, slotId, bounds, corners, now), status))
        {
            // Kilitli sayfa ya da değişiklik yok: önizleme geri alınır.
            SyncFrame(SelectedSlot);
        }
    }

    /// <summary>Geometrik yuvayı düz dikdörtgene döndürür.</summary>
    public void ResetFrameCorners()
    {
        if (SelectedSlot is not { } slot)
        {
            return;
        }

        var bounds = slot.Slot.Bounds;
        var now = clock.GetUtcNow();
        RememberSlot(slot.Id);
        EditSelectedPage(p => SlotFrameEditor.SetFrame(p, slot.Id, bounds, [new(0, 0), new(1, 0), new(1, 1), new(0, 1)], now), "Yuva dikdörtgene döndü.");
    }

    private static List<RectMm> Others(PageItemViewModel page, SlotItemViewModel moving) =>
    [
        .. page.Slots.Where(s => !ReferenceEquals(s, moving) && s.Id != moving.Id).Select(s => new RectMm(s.X, s.Y, s.Width, s.Height)),
        .. page.Texts.Select(t => new RectMm(t.X, t.Y, t.Width, t.Height)),
    ];

    private static RectMm SnapEdges(RectMm rect, FrameHandle handle, PageItemViewModel page, List<RectMm> others, double tolerance, RectMm limits, List<SnapGuide> guides)
    {
        double left = rect.X, top = rect.Y, right = rect.Right, bottom = rect.Bottom;
        if (handle is FrameHandle.Left or FrameHandle.TopLeft or FrameHandle.BottomLeft)
        {
            (left, var g) = SnapGuides.SnapLine(left, vertical: true, page.PageArea, page.SafeArea, others, tolerance);
            guides.AddRange(g);
        }

        if (handle is FrameHandle.Right or FrameHandle.TopRight or FrameHandle.BottomRight)
        {
            (right, var g) = SnapGuides.SnapLine(right, vertical: true, page.PageArea, page.SafeArea, others, tolerance);
            guides.AddRange(g);
        }

        if (handle is FrameHandle.Top or FrameHandle.TopLeft or FrameHandle.TopRight)
        {
            (top, var g) = SnapGuides.SnapLine(top, vertical: false, page.PageArea, page.SafeArea, others, tolerance);
            guides.AddRange(g);
        }

        if (handle is FrameHandle.Bottom or FrameHandle.BottomLeft or FrameHandle.BottomRight)
        {
            (bottom, var g) = SnapGuides.SnapLine(bottom, vertical: false, page.PageArea, page.SafeArea, others, tolerance);
            guides.AddRange(g);
        }

        left = Math.Max(limits.X, left);
        top = Math.Max(limits.Y, top);
        right = Math.Min(limits.Right, right);
        bottom = Math.Min(limits.Bottom, bottom);
        if (right - left < SlotFrameEditor.MinSizeMm || bottom - top < SlotFrameEditor.MinSizeMm)
        {
            guides.Clear();
            return rect;
        }

        return new RectMm(left, top, right - left, bottom - top);
    }

    /// <summary>Çerçeve katmanı seçili yuvaya göre kurulur ya da kaldırılır.</summary>
    private void SyncFrame(SlotItemViewModel? slot)
    {
        var page = SelectedPage;
        if (frameEditPage is not null && !ReferenceEquals(frameEditPage, page))
        {
            frameEditPage.FrameEdit = null;
        }

        frameEditPage = page;
        if (page is null)
        {
            return;
        }

        page.FrameEdit = isFrameEditing && slot is not null && page.Slots.Contains(slot)
            ? new FrameEditViewModel(slot, page.HandleSizeMm)
            : null;
        ResetFrameCornersCommand?.NotifyCanExecuteChanged();
    }

    private void InitializeFrame()
    {
        ToggleFrameEditingCommand = new RelayCommand(() => IsFrameEditing = !IsFrameEditing, () => HasProject);
        ResetFrameCornersCommand = new RelayCommand(
            ResetFrameCorners,
            () => SelectedSlot is { Slot: { } s } && s.Shape is not (Core.Projects.SlotShape.Rectangle or Core.Projects.SlotShape.Circle or Core.Projects.SlotShape.Rounded));
    }
}
