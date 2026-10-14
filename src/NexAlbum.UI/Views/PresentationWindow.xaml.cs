using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NexAlbum.Core.Effects;
using NexAlbum.UI.Controls;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI.Views;

/// <summary>
/// Tam ekran sunum (F5, Karar 23): ← → / boşluk / PgUp PgDn / Home End ile gezinme, Esc ile çıkış; ekranın solu geri, sağı
/// ileri. Sayfa değişimi seçilen pro efektle (E: sıradaki efekt) ve tıklanan noktadan oynar; efekt filmdekiyle aynı çizimdir.
/// P otomatik oynatma (altta dolan çizgi), G «Göz at» odak şeridi. Mercek imleci gidilecek yönü gösterir. Hareketsiz kalınca
/// alt bilgi ve imleç gizlenir.
/// </summary>
public partial class PresentationWindow : Window
{
    private static readonly TimeSpan HudDelay = TimeSpan.FromSeconds(2.5);
    private static readonly Color Backdrop = Color.FromRgb(0x0A, 0x0A, 0x0B);
    private readonly DispatcherTimer idle;
    private readonly DispatcherTimer auto;
    private readonly EffectPlayer player;
    private readonly List<ContentControl> cards = [];
    private Point? pendingOrigin;
    private double stripPosition;
    private double stripTarget;
    private Point? dragStart;
    private bool stripAnimating;
    private double dragFrom;
    private bool dragged;

    public PresentationWindow(PresentationViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        player = new EffectPlayer(EffectImage);
        idle = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = HudDelay };
        idle.Tick += (_, _) => HideHud();
        auto = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = PresentationViewModel.AutoplayInterval };
        auto.Tick += (_, _) => AutoAdvance();
        Loaded += (_, _) => ShowHud();
        Closed += (_, _) =>
        {
            idle.Stop();
            auto.Stop();
            player.Dispose();
            StopStrip();
            viewModel.Close();
        };
    }

    public PresentationViewModel ViewModel { get; }

    /// <summary>Efektli geçiş: sahnenin görüntüsü alınır, görünüm değişir, yeni görüntü alınır ve efekt oynar.</summary>
    public void Navigate(Func<bool> move, Point? origin = null)
    {
        ArgumentNullException.ThrowIfNull(move);
        if (player.IsPlaying)
        {
            player.Stop();
        }

        var before = Capture();
        if (!move())
        {
            return;
        }

        RestartTrack();
        if (before is null)
        {
            return;
        }

        pendingOrigin = origin;

        // Yeni görünüm çizildikten sonra görüntüsünü al.
        Dispatcher.InvokeAsync(
            () =>
            {
                var after = Capture();
                if (after is null)
                {
                    return;
                }

                var scale = before.PixelWidth / Math.Max(1, Root.ActualWidth);
                Point? px = pendingOrigin is { } o ? new Point(o.X * scale, o.Y * scale) : null;
                player.Play(before, after, ViewModel.Effect.Kind, px, ViewModel.EffectDuration, Backdrop);
            },
            DispatcherPriority.Loaded);
    }

    /// <summary>Efekti verilen ilerlemede durdurup gösterir (ekran görüntüsü ve testler için).</summary>
    public void ShowEffectFrame(Func<bool> move, Point? origin, double progress)
    {
        ArgumentNullException.ThrowIfNull(move);
        var before = Capture();
        move();
        Root.UpdateLayout();
        var after = Capture();
        if (before is null || after is null)
        {
            return;
        }

        var scale = before.PixelWidth / Math.Max(1, Root.ActualWidth);
        player.Play(before, after, ViewModel.Effect.Kind, origin is { } o ? new Point(o.X * scale, o.Y * scale) : null, TimeSpan.FromDays(1), Backdrop);
        player.Hold(progress);
    }

    /// <summary>Göz at kipini açar/kapatır.</summary>
    public void ToggleBrowse()
    {
        if (ViewModel.Browsing)
        {
            CloseBrowse(open: false);
            return;
        }

        player.Stop();
        ViewModel.Browsing = true;
        BuildStrip();
        stripPosition = stripTarget = ViewModel.Index;
        Browse.Visibility = Visibility.Visible;
        LayoutStrip();
    }

    /// <summary>Şeridi verilen konuma getirip çizer (ekran görüntüsü ve testler için).</summary>
    public void SetStripPosition(double position)
    {
        stripPosition = stripTarget = position;
        LayoutStrip();
    }

    private BitmapSource? Capture()
    {
        if (Root.ActualWidth < 2 || Root.ActualHeight < 2)
        {
            return null;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var width = (int)Math.Round(Root.ActualWidth * dpi.DpiScaleX);
        var height = (int)Math.Round(Root.ActualHeight * dpi.DpiScaleY);
        var bitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(Root);
        bitmap.Freeze();
        return bitmap;
    }

    private void OnKey(object sender, KeyEventArgs e)
    {
        if (ViewModel.Browsing)
        {
            e.Handled = e.Key switch
            {
                Key.Right or Key.Down or Key.PageDown => Run(() => MoveStrip(1)),
                Key.Left or Key.Up or Key.PageUp => Run(() => MoveStrip(-1)),
                Key.Enter or Key.Space => Run(() => CloseBrowse(open: true)),
                Key.Escape or Key.G => Run(() => CloseBrowse(open: false)),
                _ => false,
            };
            return;
        }

        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        var handled = e.Key switch
        {
            Key.Right or Key.Down or Key.Space or Key.PageDown or Key.Enter => Run(() => Navigate(ViewModel.Next)),
            Key.Left or Key.Up or Key.Back or Key.PageUp => Run(() => Navigate(ViewModel.Previous)),
            Key.Home => Run(() => Navigate(ViewModel.First)),
            Key.End => Run(() => Navigate(ViewModel.Last)),
            Key.E => Run(() => ViewModel.CycleEffect(shift ? -1 : 1)),
            Key.P => Run(ToggleAutoplay),
            Key.G => Run(ToggleBrowse),
            Key.Escape or Key.F5 => Run(Close),
            _ => false,
        };
        e.Handled = handled;
        ShowHud();
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        var at = e.GetPosition(Root);
        if (at.X < Root.ActualWidth * 0.35)
        {
            Navigate(ViewModel.Previous, at);
        }
        else
        {
            Navigate(ViewModel.Next, at);
        }

        ShowHud();
    }

    private void OnActivity(object sender, MouseEventArgs e)
    {
        ShowHud();
        if (ViewModel.Browsing)
        {
            Lens.Opacity = 0;
            return;
        }

        var at = e.GetPosition(Root);
        Canvas.SetLeft(Lens, at.X);
        Canvas.SetTop(Lens, at.Y);
        LensText.Text = at.X < Root.ActualWidth * 0.35 ? "ÖNCEKİ" : "SONRAKİ";
        Lens.Opacity = 1;
        Cursor = Cursors.None;
    }

    private void ToggleAutoplay()
    {
        ViewModel.Autoplay = !ViewModel.Autoplay;
        Track.Visibility = ViewModel.Autoplay ? Visibility.Visible : Visibility.Collapsed;
        RestartTrack();
    }

    private void AutoAdvance()
    {
        if (!ViewModel.CanGoNext)
        {
            ToggleAutoplay();
            return;
        }

        Navigate(ViewModel.Next);
    }

    private void RestartTrack()
    {
        auto.Stop();
        TrackScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        TrackScale.ScaleX = 0;
        if (!ViewModel.Autoplay)
        {
            return;
        }

        TrackScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0, 1, PresentationViewModel.AutoplayInterval));
        auto.Start();
    }

    private void ShowHud()
    {
        Hud.BeginAnimation(OpacityProperty, null);
        Hud.Opacity = 1;
        idle.Stop();
        idle.Start();
    }

    private void HideHud()
    {
        idle.Stop();
        Hud.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(400)));
        Lens.BeginAnimation(OpacityProperty, null);
        Lens.Opacity = 0;
        Cursor = Cursors.None;
    }

    // ── Göz at: odak şeridi ──────────────────────────────────────────────

    private void BuildStrip()
    {
        Strip.Children.Clear();
        cards.Clear();
        var template = (DataTemplate)FindResource("SpreadView");
        foreach (var view in ViewModel.Views)
        {
            var card = new ContentControl
            {
                Content = view,
                ContentTemplate = template,
                Focusable = false,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(),
                Effect = new BlurEffect { Radius = 0, KernelType = KernelType.Gaussian },
                Cursor = Cursors.Hand,
            };
            cards.Add(card);
            Strip.Children.Add(card);
        }
    }

    private void LayoutStrip()
    {
        var w = Strip.ActualWidth > 0 ? Strip.ActualWidth : ActualWidth;
        var h = Strip.ActualHeight > 0 ? Strip.ActualHeight : ActualHeight;
        if (w <= 0 || h <= 0 || cards.Count == 0)
        {
            return;
        }

        var aspect = ViewModel.ViewWidth / ViewModel.PageHeight;
        var cardW = Math.Min(w * 0.62, h * 0.62 * aspect);
        var cardH = cardW / aspect;
        var stride = cardW * 1.06;
        var centerY = h * 0.44;
        var blur = 0.009 * Math.Min(w, h);
        for (var i = 0; i < cards.Count; i++)
        {
            var d = i - stripPosition;
            var focus = FocusPull.Of(d, blur);
            var card = cards[i];
            card.Width = cardW;
            card.Height = cardH;
            Canvas.SetLeft(card, (w / 2) + (d * stride) - (cardW / 2));
            Canvas.SetTop(card, centerY - (cardH / 2));
            var scale = (ScaleTransform)card.RenderTransform;
            scale.ScaleX = scale.ScaleY = focus.Scale;
            card.Opacity = focus.Opacity;
            ((BlurEffect)card.Effect).Radius = focus.Blur;
            Panel.SetZIndex(card, 1000 - (int)Math.Round(Math.Abs(d) * 10));
        }

        var nearest = FocusPull.Nearest(stripPosition, cards.Count);
        BrowseLabel.Text = PresentationViewModel.Label(ViewModel.Views[nearest]);
        BrowseCounter.Text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:00} / {1:00}", nearest + 1, cards.Count);
        BrowseBarScale.ScaleX = cards.Count > 1 ? Math.Clamp(stripPosition / (cards.Count - 1), 0, 1) : 1;
        ViewModel.Preload(nearest);
    }

    /// <summary>
    /// Şerit hedefe kayarken her karede bir adım. Hedefe varınca kare olayından çıkılır: sürekli abonelik yazılım çiziminde
    /// (bulanık kartlar) ileti kuyruğunu doldurup boşta işleri (ve testleri) bekletiyordu.
    /// </summary>
    private void OnStripFrame(object? sender, EventArgs e)
    {
        if (!ViewModel.Browsing || dragStart is not null || Math.Abs(stripTarget - stripPosition) <= 0.0005)
        {
            if (ViewModel.Browsing && dragStart is null && stripPosition != stripTarget)
            {
                stripPosition = stripTarget;
                LayoutStrip();
            }

            StopStrip();
            return;
        }

        stripPosition += (stripTarget - stripPosition) * 0.18;
        LayoutStrip();
    }

    /// <summary>Şeridi hedefe doğru kaydırmaya başlar (zaten kayıyorsa bir şey yapmaz).</summary>
    private void AnimateStrip()
    {
        if (!stripAnimating)
        {
            stripAnimating = true;
            CompositionTarget.Rendering += OnStripFrame;
        }
    }

    private void StopStrip()
    {
        stripAnimating = false;
        CompositionTarget.Rendering -= OnStripFrame;
    }

    private void MoveStrip(int step)
    {
        stripTarget = Math.Clamp(Math.Round(stripTarget) + step, 0, cards.Count - 1);
        AnimateStrip();
    }

    private void CloseBrowse(bool open)
    {
        StopStrip();
        var target = FocusPull.Nearest(stripTarget, cards.Count);
        ViewModel.Browsing = false;
        Browse.Visibility = Visibility.Collapsed;
        Strip.Children.Clear();
        cards.Clear();
        if (open)
        {
            Navigate(() => ViewModel.GoTo(target));
        }
    }

    private void OnBrowseWheel(object sender, MouseWheelEventArgs e)
    {
        MoveStrip(e.Delta < 0 ? 1 : -1);
        e.Handled = true;
    }

    private void OnBrowseDown(object sender, MouseButtonEventArgs e)
    {
        dragStart = e.GetPosition(Browse);
        dragFrom = stripPosition;
        dragged = false;
        Browse.CaptureMouse();
        e.Handled = true;
    }

    private void OnBrowseMove(object sender, MouseEventArgs e)
    {
        if (dragStart is not { } start || e.LeftButton != MouseButtonState.Pressed || cards.Count == 0)
        {
            return;
        }

        var dx = e.GetPosition(Browse).X - start.X;
        if (!dragged && Math.Abs(dx) < 4)
        {
            return;
        }

        dragged = true;
        var stride = cards[0].Width * 1.06;
        stripPosition = Math.Clamp(dragFrom - (dx / Math.Max(1, stride)), -0.3, cards.Count - 0.7);
        stripTarget = stripPosition;
        LayoutStrip();
    }

    private void OnBrowseUp(object sender, MouseButtonEventArgs e)
    {
        Browse.ReleaseMouseCapture();
        var start = dragStart;
        dragStart = null;
        if (dragged)
        {
            stripTarget = FocusPull.Nearest(stripPosition, cards.Count);
            AnimateStrip();
            return;
        }

        // Tıklama: ortadaki kart açılır, yandaki ortaya gelir.
        if (start is not null && cards.Count > 0)
        {
            var w = Strip.ActualWidth;
            var stride = cards[0].Width * 1.06;
            var clicked = (int)Math.Round(stripPosition + ((e.GetPosition(Browse).X - (w / 2)) / Math.Max(1, stride)));
            clicked = Math.Clamp(clicked, 0, cards.Count - 1);
            if (clicked == FocusPull.Nearest(stripPosition, cards.Count))
            {
                stripTarget = clicked;
                CloseBrowse(open: true);
            }
            else
            {
                stripTarget = clicked;
                AnimateStrip();
            }
        }
    }

    private static bool Run(Action action)
    {
        action();
        return true;
    }
}
