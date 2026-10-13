using System.Windows;
using System.Windows.Controls;
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

    /// <summary>Sayfa şeridinde sürüklenen sayfa (aynı süreç içi; nesnenin kendisi taşınır).</summary>
    private const string PageFormat = "NexAlbum.Page";

    private Point stripStart;
    private PageItemViewModel? stripPage;
    private PageItemViewModel? stripMark;

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

    /// <summary>Sürüklemeye dönüşmeyen tıklama fotoğrafı seçer (sağ tık menüsü ve "Düzenleyicide aç" bunun üzerinde çalışır).</summary>
    private void OnGalleryMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (galleryItem is { } item)
        {
            ViewModel.SelectedAsset = item;
        }

        galleryItem = null;
    }

    private void OnGalleryRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is AssetItemViewModel item)
        {
            ViewModel.SelectedAsset = item;
        }
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

    // ── Sayfa şeridi: sürükleyerek sıralama ────────────────────────────

    private void OnStripMouseDown(object sender, MouseButtonEventArgs e)
    {
        stripStart = e.GetPosition(null);
        stripPage = (e.OriginalSource as FrameworkElement)?.DataContext as PageItemViewModel;
    }

    private void OnStripMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || stripPage is not { } page || !Moved(stripStart, e.GetPosition(null)))
        {
            return;
        }

        stripPage = null;
        DragDrop.DoDragDrop(PageStrip, new DataObject(PageFormat, page), DragDropEffects.Move);
        ClearStripMark();
    }

    private void OnStripDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(PageFormat) is not PageItemViewModel)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var insert = StripInsertIndex(e);
        SetStripMark(insert < ViewModel.Pages.Count ? ViewModel.Pages[insert] : null);
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnStripDragLeave(object sender, DragEventArgs e) => ClearStripMark();

    private void OnStripDrop(object sender, DragEventArgs e)
    {
        ClearStripMark();
        e.Handled = true;
        if (e.Data.GetData(PageFormat) is not PageItemViewModel page || !ViewModel.Pages.Contains(page))
        {
            return;
        }

        // Ekleme konumu sayfa çıkarılmadan önceki sıraya göredir; sağa taşırken bir eksiltilir.
        var insert = StripInsertIndex(e);
        ViewModel.MovePageTo(page, insert > page.Index ? insert - 1 : insert);
    }

    /// <summary>Fare, üzerindeki küçük resmin sol yarısındaysa o sayfanın önü, sağ yarısındaysa arkası; boşlukta sona.</summary>
    private int StripInsertIndex(DragEventArgs e)
    {
        for (var current = e.OriginalSource as DependencyObject; current is not null; current = ParentOf(current))
        {
            if (current is ListBoxItem { DataContext: PageItemViewModel page } item)
            {
                return e.GetPosition(item).X < item.ActualWidth / 2 ? page.Index : page.Index + 1;
            }
        }

        return ViewModel.Pages.Count;
    }

    private void SetStripMark(PageItemViewModel? page)
    {
        if (ReferenceEquals(page, stripMark))
        {
            return;
        }

        ClearStripMark();
        stripMark = page;
        page?.IsDropBefore = true;
    }

    private void ClearStripMark()
    {
        stripMark?.IsDropBefore = false;
        stripMark = null;
    }

    // ── Tuval: seçim, kaydırma, yuvadan yuvaya taşıma ──────────────────

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        FocusPageOf(e.OriginalSource as DependencyObject);
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
        // Ctrl + tekerlek: tuvali yakınlaştır (Karar 12 §10.3); düz tekerlek yuvadaki fotoğrafı yakınlaştırır.
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ViewModel.Zoom.ZoomBy(e.Delta > 0 ? 1.1 : 1 / 1.1);
            e.Handled = true;
            return;
        }

        if (SlotAt(e.OriginalSource as DependencyObject, out _) is { IsEmpty: false } slot)
        {
            FocusPageOf(e.OriginalSource as DependencyObject);
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
        if (slot is not null && !e.Data.GetDataPresent(SlotFormat))
        {
            // Kitap görünümünde karşı sayfaya bırakılan fotoğraf o sayfaya gider (yuvalar arası taşıma yalnızca aynı sayfada).
            FocusPageOf(VisualTreeHelper.HitTest(CanvasArea, e.GetPosition(CanvasArea))?.VisualHit);
        }

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
        for (var current = source; current is not null; current = ParentOf(current))
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

    private static DependencyObject? ParentOf(DependencyObject child) =>
        child is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(child) : LogicalTreeHelper.GetParent(child);

    /// <summary>Boş tuvale ya da sayfa fonuna tıklanınca seçim kalkar; düğmelere tıklamak seçimi korur.</summary>
    private static bool IsPageBackground(DependencyObject? source)
    {
        for (var current = source; current is not null; current = ParentOf(current))
        {
            if (current is ButtonBase or TextBoxBase)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Kitap görünümünde tıklanan/bırakılan öğenin sayfası seçili değilse önce o sayfa seçilir; düzenleme işlemleri
    /// her zaman seçili sayfada çalışır.
    /// </summary>
    private void FocusPageOf(DependencyObject? source)
    {
        for (var current = source; current is not null; current = ParentOf(current))
        {
            if (current is FrameworkElement { DataContext: PageItemViewModel page })
            {
                if (!ReferenceEquals(page, ViewModel.SelectedPage) && ViewModel.Pages.Contains(page))
                {
                    ViewModel.SelectedPage = page;
                }

                return;
            }
        }
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
