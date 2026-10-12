using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using NexAlbum.Core.Projects;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI;

/// <summary>
/// Fare ve sürükle-bırak etkileşimi. Yalnızca olayları görünüm modeline çevirir; kurallar <see cref="MainViewModel"/>
/// ve PageEditor'dadır. Tuval birimi mm olduğundan yuva içindeki fare konumu doğrudan mm verir.
/// </summary>
public partial class MainWindow
{
    /// <summary>Galeriden sürüklenen fotoğrafın kimliği.</summary>
    private const string AssetFormat = "NexAlbum.AssetId";

    /// <summary>Tuvalde yuvadan sürüklenen yuvanın kimliği.</summary>
    private const string SlotFormat = "NexAlbum.SlotId";

    private Point galleryStart;
    private AssetItemViewModel? galleryItem;

    private Point canvasStart;
    private SlotItemViewModel? pressedSlot;
    private FrameworkElement? pressedElement;
    private PhotoPlacement? panStart;
    private bool panning;
    private SlotItemViewModel? dropTarget;

    // ── Galeri ─────────────────────────────────────────────────────────

    private void OnGalleryMouseDown(object sender, MouseButtonEventArgs e)
    {
        galleryStart = e.GetPosition(null);
        galleryItem = (e.OriginalSource as FrameworkElement)?.DataContext as AssetItemViewModel;
    }

    private void OnGalleryMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || galleryItem is not { IsMissing: false } item || !Moved(galleryStart, e.GetPosition(null)))
        {
            return;
        }

        galleryItem = null;
        DragDrop.DoDragDrop(Gallery, new DataObject(AssetFormat, item.Asset.Id), DragDropEffects.Copy);
        ClearDropTarget();
    }

    private void OnGalleryDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is AssetItemViewModel item)
        {
            ViewModel.PlaceIntoSelection(item.Asset.Id);
        }
    }

    private async void OnGalleryDrop(object sender, DragEventArgs e)
    {
        if (Files(e) is { } files)
        {
            e.Handled = true;
            await ViewModel.ImportPathsAsync(files).ConfigureAwait(true);
        }
    }

    private async void OnBackgroundDrop(object sender, DragEventArgs e)
    {
        if (Files(e) is { } files)
        {
            e.Handled = true;
            await ViewModel.ImportPathsAsync(files, AssetRole.Background).ConfigureAwait(true);
        }
    }

    private void OnFileDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && ViewModel.HasProject ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    // ── Tuval: seçim, kaydırma, yuvadan yuvaya taşıma ──────────────────

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        var slot = SlotAt(e.OriginalSource as DependencyObject, out var element);
        if (slot is null)
        {
            if (IsPageBackground(e.OriginalSource as DependencyObject))
            {
                ViewModel.SelectSlot(null);
            }

            return;
        }

        ViewModel.SelectSlot(slot);
        pressedSlot = ViewModel.SelectedSlot;
        pressedElement = element;
        canvasStart = e.GetPosition(element);
        panStart = pressedSlot?.Placement;
        panning = panStart is not null && Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        if (panning)
        {
            CanvasArea.CaptureMouse();
            Mouse.OverrideCursor = Cursors.SizeAll;
        }

        e.Handled = true;
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (pressedSlot is null || pressedElement is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = e.GetPosition(pressedElement);
        if (panning && panStart is not null)
        {
            ViewModel.PreviewPan(pressedSlot, panStart, position.X - canvasStart.X, position.Y - canvasStart.Y);
            return;
        }

        if (!pressedSlot.IsEmpty && Moved(pressedElement.PointToScreen(canvasStart), pressedElement.PointToScreen(position)))
        {
            var source = pressedSlot;
            ResetPress();
            DragDrop.DoDragDrop(CanvasArea, new DataObject(SlotFormat, source.Id), DragDropEffects.Move);
            ClearDropTarget();
        }
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (panning && pressedSlot is not null)
        {
            ViewModel.CommitPlacement(pressedSlot);
        }

        ResetPress();
    }

    private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (SlotAt(e.OriginalSource as DependencyObject, out _) is { IsEmpty: false } slot)
        {
            ViewModel.ZoomSlot(slot, e.Delta > 0 ? 1.1 : 1 / 1.1);
            e.Handled = true;
        }
    }

    private void OnCanvasDragOver(object sender, DragEventArgs e)
    {
        var slot = SlotUnder(e);
        var files = e.Data.GetDataPresent(DataFormats.FileDrop);
        var accepted = ViewModel.HasProject && (files
            || (slot is not null && (e.Data.GetDataPresent(AssetFormat) || e.Data.GetDataPresent(SlotFormat))));
        e.Effects = !accepted ? DragDropEffects.None : e.Data.GetDataPresent(SlotFormat) ? DragDropEffects.Move : DragDropEffects.Copy;
        SetDropTarget(accepted ? slot : null);
        e.Handled = true;
    }

    private void OnCanvasDragLeave(object sender, DragEventArgs e) => ClearDropTarget();

    private async void OnCanvasDrop(object sender, DragEventArgs e)
    {
        var slot = SlotUnder(e);
        ClearDropTarget();
        e.Handled = true;
        if (Files(e) is { } files)
        {
            await ViewModel.ImportPathsAsync(files, AssetRole.Photo, slot).ConfigureAwait(true);
        }
        else if (slot is not null && e.Data.GetData(AssetFormat) is string assetId)
        {
            ViewModel.PlacePhoto(slot, assetId);
        }
        else if (slot is not null && e.Data.GetData(SlotFormat) is string sourceId)
        {
            ViewModel.MoveSlot(sourceId, slot);
        }
    }

    // ── Yardımcılar ────────────────────────────────────────────────────

    private static bool Moved(Point start, Point now) =>
        Math.Abs(now.X - start.X) >= SystemParameters.MinimumHorizontalDragDistance
        || Math.Abs(now.Y - start.Y) >= SystemParameters.MinimumVerticalDragDistance;

    private static string[]? Files(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files ? files : null;

    /// <summary>
    /// Tıklanan öğeden yukarı çıkarak yuvayı bulur; dönen öğe yuva şablonunun kökü (yuvanın sol üst köşesi = 0,0 mm).
    /// Sayfa şeridindeki küçük resimler tuval dışında olduğundan buraya gelmez.
    /// </summary>
    private static SlotItemViewModel? SlotAt(DependencyObject? source, out FrameworkElement? root)
    {
        root = null;
        SlotItemViewModel? slot = null;
        for (var current = source; current is not null; current = Parent(current))
        {
            if (current is FrameworkElement { DataContext: SlotItemViewModel s } fe)
            {
                slot = s;
                root = fe;
            }
            else if (slot is not null)
            {
                break;
            }
        }

        return slot;
    }

    private static DependencyObject? Parent(DependencyObject child) =>
        child is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(child) : LogicalTreeHelper.GetParent(child);

    /// <summary>Boş tuvale ya da sayfa fonuna tıklanınca seçim kalkar; düğmelere tıklamak seçimi korur.</summary>
    private static bool IsPageBackground(DependencyObject? source)
    {
        for (var current = source; current is not null; current = Parent(current))
        {
            if (current is ButtonBase or TextBoxBase)
            {
                return false;
            }
        }

        return true;
    }

    private SlotItemViewModel? SlotUnder(DragEventArgs e)
    {
        var hit = VisualTreeHelper.HitTest(CanvasArea, e.GetPosition(CanvasArea))?.VisualHit;
        return SlotAt(hit, out _);
    }

    private void SetDropTarget(SlotItemViewModel? slot)
    {
        if (ReferenceEquals(slot, dropTarget))
        {
            return;
        }

        ClearDropTarget();
        dropTarget = slot;
        if (slot is not null)
        {
            slot.IsDropTarget = true;
        }
    }

    private void ClearDropTarget()
    {
        if (dropTarget is not null)
        {
            dropTarget.IsDropTarget = false;
            dropTarget = null;
        }
    }

    private void ResetPress()
    {
        if (panning)
        {
            CanvasArea.ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        panning = false;
        pressedSlot = null;
        pressedElement = null;
        panStart = null;
    }
}
