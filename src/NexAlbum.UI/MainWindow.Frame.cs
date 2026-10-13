using System.Windows;
using System.Windows.Input;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI;

/// <summary>
/// Çerçeve kipinde fare: tutamaçla boyutlandırma, köşe sürükleme ve gövdeden taşıma (Karar 10 Rev. 4). Tuval birimi mm olduğundan
/// sayfa kökündeki fare konumu doğrudan mm verir.
/// </summary>
public partial class MainWindow
{
    private FrameworkElement? frameOrigin;
    private Point frameStart;
    private FrameHandle frameHandle;
    private int frameCorner = -1;
    private Point frameCornerStart;
    private RectMm frameStartRect;
    private bool frameMoved;

    /// <summary>Çerçeve kipindeyse sürüklemeyi başlatır; tıklanan yuva seçilir. Başladıysa true.</summary>
    private bool BeginFrameDrag(MouseButtonEventArgs e)
    {
        if (!ViewModel.IsFrameEditing)
        {
            return false;
        }

        var source = e.OriginalSource as DependencyObject;
        var origin = PageRootOf(source);
        if (origin is null)
        {
            return false;
        }

        frameCorner = -1;
        frameHandle = FrameHandle.Body;
        var onHandle = false;
        for (var current = source; current is not null; current = ParentOf(current))
        {
            if (current is FrameworkElement { DataContext: FrameCornerViewModel corner })
            {
                frameCorner = corner.Index;
                onHandle = true;
                break;
            }

            if (current is FrameworkElement { DataContext: FrameHandleViewModel handle })
            {
                frameHandle = handle.Kind;
                onHandle = true;
                break;
            }
        }

        if (!onHandle)
        {
            if (SlotAt(source, out _) is not { } slot)
            {
                return false;
            }

            ViewModel.SelectSlot(slot);
        }

        if (ViewModel.SelectedPage?.FrameEdit is not { } edit)
        {
            return false;
        }

        if (frameCorner >= 0)
        {
            var c = edit.Corners[frameCorner];
            frameCornerStart = new Point(c.X + edit.HalfCorner, c.Y + edit.HalfCorner);
        }

        frameStartRect = edit.Rect;
        frameOrigin = origin;
        frameStart = e.GetPosition(origin);
        frameMoved = false;
        CanvasArea.CaptureMouse();
        return true;
    }

    private void DragFrame(MouseEventArgs e)
    {
        var origin = frameOrigin!;
        var p = e.GetPosition(origin);
        if (!frameMoved && !Moved(origin.PointToScreen(frameStart), origin.PointToScreen(p)))
        {
            return;
        }

        frameMoved = true;
        var dx = p.X - frameStart.X;
        var dy = p.Y - frameStart.Y;
        if (frameCorner >= 0)
        {
            ViewModel.DragFrameCorner(frameCorner, frameCornerStart.X + dx, frameCornerStart.Y + dy);
            return;
        }

        // Yapışma toleransı ekranda ~8 piksel: yakınlaştırmaya göre mm'ye çevrilir. Alt: serbest, Shift: oran korunur.
        var tolerance = Math.Abs(origin.PointFromScreen(new Point(8, 0)).X - origin.PointFromScreen(new Point(0, 0)).X);
        ViewModel.DragFrame(
            frameHandle,
            frameStartRect,
            dx,
            dy,
            tolerance,
            snap: !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt),
            keepAspect: Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
    }

    private void EndFrameDrag()
    {
        frameOrigin = null;
        CanvasArea.ReleaseMouseCapture();
        if (frameMoved)
        {
            ViewModel.CommitFrame();
        }
        else
        {
            ViewModel.SelectedPage?.ShowSnapLines([]);
        }

        frameMoved = false;
    }

    /// <summary>Sayfa tuvalinin kökü (DataContext sayfa; birimi mm).</summary>
    private static FrameworkElement? PageRootOf(DependencyObject? source)
    {
        for (var current = source; current is not null; current = ParentOf(current))
        {
            if (current is FrameworkElement { DataContext: PageItemViewModel } page)
            {
                return page;
            }
        }

        return null;
    }
}
