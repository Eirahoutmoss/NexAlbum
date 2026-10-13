using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>Çerçeve düzenleme tutamacı (tuval koordinatı, mm).</summary>
public sealed class FrameHandleViewModel(FrameHandle kind) : ObservableObject
{
    private double x;
    private double y;

    public FrameHandle Kind { get; } = kind;

    public double X
    {
        get => x;
        set => SetProperty(ref x, value);
    }

    public double Y
    {
        get => y;
        set => SetProperty(ref y, value);
    }

    /// <summary>Tutamaç üstündeki imleç.</summary>
    public System.Windows.Input.Cursor Cursor => Kind switch
    {
        FrameHandle.TopLeft or FrameHandle.BottomRight => System.Windows.Input.Cursors.SizeNWSE,
        FrameHandle.TopRight or FrameHandle.BottomLeft => System.Windows.Input.Cursors.SizeNESW,
        FrameHandle.Left or FrameHandle.Right => System.Windows.Input.Cursors.SizeWE,
        FrameHandle.Top or FrameHandle.Bottom => System.Windows.Input.Cursors.SizeNS,
        _ => System.Windows.Input.Cursors.SizeAll,
    };
}

/// <summary>Geometrik yuvada sürüklenebilir köşe (tuval koordinatı, mm).</summary>
public sealed class FrameCornerViewModel(int index) : ObservableObject
{
    private double x;
    private double y;

    public int Index { get; } = index;

    public double X
    {
        get => x;
        set => SetProperty(ref x, value);
    }

    public double Y
    {
        get => y;
        set => SetProperty(ref y, value);
    }
}

/// <summary>
/// Seçili yuvanın çerçeve düzenleme katmanı: sürüklerken yalnızca bu katman değişir (yuva bırakınca yeniden çizilir).
/// Koordinatlar tuvale (taşma payı alanının sol üst köşesi) göredir, mm.
/// </summary>
public sealed class FrameEditViewModel : ObservableObject
{
    private RectMm rect;
    private IReadOnlyList<UnitPoint>? corners;

    public FrameEditViewModel(SlotItemViewModel slot, double handleSize)
    {
        ArgumentNullException.ThrowIfNull(slot);
        Slot = slot;
        HandleSize = handleSize;
        rect = new RectMm(slot.X, slot.Y, slot.Width, slot.Height);
        corners = SlotFrameEditor.Corners(slot.Slot);
        Handles = [.. Enum.GetValues<FrameHandle>().Where(h => h != FrameHandle.Body).Select(h => new FrameHandleViewModel(h))];
        Corners = corners is null ? [] : [.. corners.Select((_, i) => new FrameCornerViewModel(i))];
        Layout();
    }

    public SlotItemViewModel Slot { get; }

    /// <summary>Tutamaç kenarı (mm; sayfa ölçüsüne göre, ekranda okunur büyüklükte).</summary>
    public double HandleSize { get; }

    public double HalfHandle => HandleSize / 2;

    /// <summary>Köşe tutamaçları çapı.</summary>
    public double CornerSize => HandleSize * 1.15;

    public double HalfCorner => CornerSize / 2;

    public double StrokeWidth => HandleSize / 8;

    public RectMm Rect => rect;

    public double X => rect.X;

    public double Y => rect.Y;

    public double Width => rect.Width;

    public double Height => rect.Height;

    /// <summary>Düzenlenebilir köşeler (normalize); daire ve yuvarlak köşelide null.</summary>
    public IReadOnlyList<UnitPoint>? UnitCorners => corners;

    /// <summary>Köşeler elle değiştirildi mi?</summary>
    public bool CornersChanged { get; private set; }

    public bool HasCorners => Corners.Count > 0;

    public ObservableCollection<FrameHandleViewModel> Handles { get; }

    public ObservableCollection<FrameCornerViewModel> Corners { get; }

    /// <summary>Biçimin önizleme çizgisi (tuval koordinatı).</summary>
    public PointCollection Outline { get; private set; } = [];

    /// <summary>"120 × 80 mm" — sürüklerken gösterilir.</summary>
    public string SizeText => string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:0.#} × {1:0.#} mm", rect.Width, rect.Height);

    public void SetRect(RectMm value)
    {
        rect = value;
        Layout();
    }

    public void SetCorners(IReadOnlyList<UnitPoint> value)
    {
        corners = value;
        CornersChanged = true;
        Layout();
    }

    private void Layout()
    {
        foreach (var h in Handles)
        {
            var (fx, fy) = h.Kind switch
            {
                FrameHandle.TopLeft => (0.0, 0.0),
                FrameHandle.Top => (0.5, 0.0),
                FrameHandle.TopRight => (1.0, 0.0),
                FrameHandle.Right => (1.0, 0.5),
                FrameHandle.BottomRight => (1.0, 1.0),
                FrameHandle.Bottom => (0.5, 1.0),
                FrameHandle.BottomLeft => (0.0, 1.0),
                _ => (0.0, 0.5),
            };
            h.X = rect.X + (fx * rect.Width) - HalfHandle;
            h.Y = rect.Y + (fy * rect.Height) - HalfHandle;
        }

        if (corners is not null)
        {
            for (var i = 0; i < Corners.Count && i < corners.Count; i++)
            {
                Corners[i].X = rect.X + (corners[i].X * rect.Width) - HalfCorner;
                Corners[i].Y = rect.Y + (corners[i].Y * rect.Height) - HalfCorner;
            }
        }

        var outline = new PointCollection();
        IReadOnlyList<UnitPoint> shape = corners ?? [new(0, 0), new(1, 0), new(1, 1), new(0, 1)];
        foreach (var c in shape)
        {
            outline.Add(new Point(rect.X + (c.X * rect.Width), rect.Y + (c.Y * rect.Height)));
        }

        outline.Freeze();
        Outline = outline;
        OnPropertyChanged(nameof(Outline));
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
        OnPropertyChanged(nameof(SizeText));
    }
}
