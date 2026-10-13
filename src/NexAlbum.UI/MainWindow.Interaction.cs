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

    /// <summary>Yuvadan sürüklenen fotoğrafın sayfası (şeritteki başka bir sayfaya taşımak için).</summary>
    private const string SourcePageFormat = "NexAlbum.SourcePageId";

    private PageItemViewModel? stripPhotoTarget;

    private Point stripStart;
    private PageItemViewModel? stripPage;
    private PageItemViewModel? stripMark;

    private Point galleryStart;
    private AssetItemViewModel? galleryItem;

    private TextItemViewModel? pressedText;
    private FrameworkElement? textOrigin;
    private Point textStart;
    private double textStartX;
    private double textStartY;
    private bool textMoved;

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
        SetStripPhotoTarget(null);
    }

    /// <summary>Sürüklemeye dönüşmeyen tıklama fotoğrafı seçer (sağ tık menüsü ve "Düzenleyicide aç" bunun üzerinde çalışır).</summary>
    private void OnGalleryMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (galleryItem is { } item)
        {
            ViewModel.SelectedAsset = item;
            Gallery.Focus();
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
        // Galeriden ya da tuvaldeki yuvadan gelen fotoğraf: üzerine gelinen sayfanın boş yuvasına konur.
        if (e.Data.GetDataPresent(AssetFormat) || e.Data.GetDataPresent(SourcePageFormat))
        {
            var target = StripPageUnder(e);
            var ok = MainViewModel.CanReceivePhoto(target)
                && !(e.Data.GetData(SourcePageFormat) is string dragged && string.Equals(dragged, target!.Page.Id, StringComparison.Ordinal));
            SetStripPhotoTarget(ok ? target : null);
            e.Effects = !ok ? DragDropEffects.None : e.Data.GetDataPresent(AssetFormat) ? DragDropEffects.Copy : DragDropEffects.Move;
            e.Handled = true;
            return;
        }

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

    private void OnStripDragLeave(object sender, DragEventArgs e)
    {
        ClearStripMark();
        SetStripPhotoTarget(null);
    }

    private void OnStripDrop(object sender, DragEventArgs e)
    {
        ClearStripMark();
        SetStripPhotoTarget(null);
        e.Handled = true;
        if (e.Data.GetData(AssetFormat) is string assetId)
        {
            ViewModel.PlaceOnPage(StripPageUnder(e), assetId);
            return;
        }

        if (e.Data.GetData(SourcePageFormat) is string sourcePage && e.Data.GetData(SlotFormat) is string slotId)
        {
            ViewModel.MovePhotoToPage(sourcePage, slotId, StripPageUnder(e));
            return;
        }

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

    private static PageItemViewModel? StripPageUnder(DragEventArgs e)
    {
        for (var current = e.OriginalSource as DependencyObject; current is not null; current = ParentOf(current))
        {
            if (current is ListBoxItem { DataContext: PageItemViewModel page })
            {
                return page;
            }
        }

        return null;
    }

    private void SetStripPhotoTarget(PageItemViewModel? page)
    {
        if (ReferenceEquals(page, stripPhotoTarget))
        {
            return;
        }

        stripPhotoTarget?.IsPhotoDropTarget = false;
        stripPhotoTarget = page;
        page?.IsPhotoDropTarget = true;
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

        // Yön tuşları ve +/− tuval odaktayken seçili yuvaya/metne gider (panel kaydırıcılarına değil).
        CanvasArea.Focus();

        // Çerçeve kipi: tutamaç, köşe ya da seçili yuvanın gövdesi sürüklenir.
        if (BeginFrameDrag(e))
        {
            e.Handled = true;
            return;
        }

        // Metinler yuvaların üstündedir: önce metin aranır. Tıkla: seç · sürükle: taşı · çift tıkla: yazıyı düzenle.
        if (TextAt(e.OriginalSource as DependencyObject, out var origin) is { } text && origin is not null)
        {
            ViewModel.SelectText(ViewModel.SelectedPage?.Texts.FirstOrDefault(t => t.Id == text.Id) ?? text);
            pressedText = ViewModel.SelectedText;
            textOrigin = origin;
            textStart = e.GetPosition(origin);
            textStartX = pressedText?.X ?? 0;
            textStartY = pressedText?.Y ?? 0;
            textMoved = false;
            if (e.ClickCount == 2)
            {
                pressedText = null;
                FocusTextEditor();
            }
            else
            {
                CanvasArea.CaptureMouse();
            }

            e.Handled = true;
            return;
        }

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
        if (e.ClickCount == 2 && slot is { IsEmpty: false })
        {
            // Çift tık: konumlandırma kipi (düz sürükleme fotoğrafı yuvanın içinde kaydırır).
            ViewModel.IsPositioning = !ViewModel.IsPositioning;
            ResetPress();
            e.Handled = true;
            return;
        }

        pressedSlot = ViewModel.SelectedSlot;
        pressedElement = element;
        canvasStart = e.GetPosition(element);
        panStart = pressedSlot?.Placement;
        panning = panStart is not null && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || ViewModel.IsPositioning);
        if (panning)
        {
            CanvasArea.CaptureMouse();
            Mouse.OverrideCursor = Cursors.SizeAll;
        }

        e.Handled = true;
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (frameOrigin is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            DragFrame(e);
            return;
        }

        if (pressedText is not null && textOrigin is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            var p = e.GetPosition(textOrigin);
            if (textMoved || Moved(textOrigin.PointToScreen(textStart), textOrigin.PointToScreen(p)))
            {
                textMoved = true;
                // Yapışma toleransı ekranda ~8 piksel: yakınlaştırmaya göre mm'ye çevrilir. Alt basılıyken serbest taşınır.
                var tolerance = Math.Abs(textOrigin.PointFromScreen(new Point(8, 0)).X - textOrigin.PointFromScreen(new Point(0, 0)).X);
                ViewModel.DragText(pressedText, textStartX, textStartY, p.X - textStart.X, p.Y - textStart.Y, tolerance, snap: !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt));
            }

            return;
        }

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
            var data = new DataObject(SlotFormat, source.Id);
            if (ViewModel.SelectedPage is { } origin)
            {
                data.SetData(SourcePageFormat, origin.Page.Id);
            }

            DragDrop.DoDragDrop(CanvasArea, data, DragDropEffects.Move);
            ClearDropTarget();
            SetStripPhotoTarget(null);
        }
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (frameOrigin is not null)
        {
            EndFrameDrag();
            return;
        }

        if (pressedText is not null)
        {
            var text = pressedText;
            pressedText = null;
            textOrigin = null;
            CanvasArea.ReleaseMouseCapture();
            if (textMoved)
            {
                ViewModel.CommitTextMove(text);
            }
            else
            {
                ViewModel.EndTextDrag();
            }

            return;
        }

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

        if (SlotAt(e.OriginalSource as DependencyObject, out var element) is { IsEmpty: false } slot && element is not null)
        {
            FocusPageOf(e.OriginalSource as DependencyObject);

            // İmlecin altındaki nokta yerinde kalır: fotoğrafın istenen bölgesine tekerlekle yaklaşılır.
            var at = e.GetPosition(element);
            ViewModel.ZoomSlotAt(ViewModel.SelectedPage?.Slots.FirstOrDefault(s => s.Id == slot.Id) ?? slot, e.Delta > 0 ? 1.1 : 1 / 1.1, at.X, at.Y);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Tuval odaktayken: yön tuşları seçili metni ya da yuvadaki fotoğrafı kaydırır (Shift: büyük adım), + / − yakınlaştırır,
    /// Home ortalar, Esc konumlandırmayı bitirir. Ctrl'lü kısayollar pencereye kalır (tuval yakınlaştırma).
    /// </summary>
    private void OnCanvasKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            return;
        }

        var large = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        e.Handled = e.Key switch
        {
            Key.Left => ViewModel.Nudge(-1, 0, large),
            Key.Right => ViewModel.Nudge(1, 0, large),
            Key.Up => ViewModel.Nudge(0, -1, large),
            Key.Down => ViewModel.Nudge(0, 1, large),
            Key.Add or Key.OemPlus => ViewModel.ZoomSelected(large ? 1.25 : 1.05),
            Key.Subtract or Key.OemMinus => ViewModel.ZoomSelected(1 / (large ? 1.25 : 1.05)),
            Key.Home when ViewModel.SelectedSlot is { IsEmpty: false } => Run(() => ViewModel.AlignPhoto("0 0")),
            Key.Escape when ViewModel.IsPositioning => Run(() => ViewModel.IsPositioning = false),
            Key.Escape when ViewModel.IsFrameEditing => Run(() => ViewModel.IsFrameEditing = false),
            _ => false,
        };
    }

    private static bool Run(Action action)
    {
        action();
        return true;
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
            // Kitap görünümünde karşı sayfadaki yuvaya bırakılırsa fotoğraf o sayfaya geçer (doluysa yer değiştirir).
            var targetPage = PageAt(VisualTreeHelper.HitTest(CanvasArea, e.GetPosition(CanvasArea))?.VisualHit);
            if (e.Data.GetData(SourcePageFormat) is string originPage && targetPage is not null && !string.Equals(originPage, targetPage.Page.Id, StringComparison.Ordinal))
            {
                ViewModel.MovePhotoToSlot(originPage, sourceId, targetPage, slot);
            }
            else
            {
                ViewModel.MoveSlot(sourceId, slot);
            }
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

    /// <summary>Tıklanan metin ve konum ölçümü için sayfa tuvalinin kökü (birimi mm).</summary>
    private static TextItemViewModel? TextAt(DependencyObject? source, out FrameworkElement? origin)
    {
        origin = null;
        TextItemViewModel? text = null;
        for (var current = source; current is not null; current = ParentOf(current))
        {
            if (current is FrameworkElement { DataContext: TextItemViewModel t })
            {
                text = t;
            }
            else if (text is not null && current is FrameworkElement { DataContext: PageItemViewModel } page)
            {
                origin = page;
                break;
            }
        }

        return text;
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
    private static PageItemViewModel? PageAt(DependencyObject? source)
    {
        for (var current = source; current is not null; current = ParentOf(current))
        {
            if (current is FrameworkElement { DataContext: PageItemViewModel page })
            {
                return page;
            }
        }

        return null;
    }

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
