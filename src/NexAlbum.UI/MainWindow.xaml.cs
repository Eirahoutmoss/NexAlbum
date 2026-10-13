using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NexAlbum.Persistence.Format;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI;

/// <summary>
/// Ana pencere (Karar 12 §3: beş bölge; Karar 17 §8: marka başlığı). İş mantığı <see cref="MainViewModel"/>'dedir;
/// burada yalnızca pencere çerçevesi, odak ve klavye gezinmesi gibi görünüm işleri vardır.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Galeri karesinin en küçük genişliği (DIP); sütun sayısı buna göre hesaplanır.</summary>
    private const double MinTileWidth = 84;

    public MainWindow()
        : this(new JsonAppSettingsStore())
    {
    }

    /// <param name="settings">Uygulama ayarları (testlerde bellek içi; kullanıcının ayar dosyasına dokunulmaz).</param>
    public MainWindow(IAppSettingsStore settings)
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new DialogService(this), settings: settings);
        DataContext = ViewModel;
        ViewModel.ThemeChanged += (_, theme) => ThemeService.Apply(theme);
        ViewModel.PresentationRequested += (_, presentation) => new Views.PresentationWindow(presentation) { Owner = this }.ShowDialog();
        ViewModel.FilmRequested += (_, film) =>
        {
            using (film)
            {
                new Views.VideoWindow(film) { Owner = this }.ShowDialog();
            }
        };
        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += (_, _) => ViewModel.Dispose();
        StateChanged += (_, _) => UpdateMaximizedInset();
    }

    public MainViewModel ViewModel { get; }

    /// <summary>Komut satırından verilen .albumproj dosyasını açar (dosya ilişkilendirme için).</summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1
            && args[1].EndsWith(ProjectFormat.FileExtension, StringComparison.OrdinalIgnoreCase)
            && File.Exists(args[1]))
        {
            ViewModel.Open(args[1]);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!ViewModel.CanClose())
        {
            e.Cancel = true;
        }
    }

    // ── Pencere çerçevesi ───────────────────────────────────────────────

    private void OnMinimize(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void OnCloseWindow(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    /// <summary>WindowChrome'lu pencere ekranı kaplarken çerçeve kalınlığı kadar ekran dışına taşar; içerik o kadar içeri alınır.</summary>
    private void UpdateMaximizedInset()
    {
        var frame = SystemParameters.WindowNonClientFrameThickness;
        RootBorder.Margin = WindowState == WindowState.Maximized
            ? new Thickness(frame.Left, frame.Left, frame.Right, frame.Bottom)
            : default;
    }

    // ── Komut paleti ────────────────────────────────────────────────────

    private void OnPaletteVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        PaletteOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)));
        _ = Dispatcher.InvokeAsync(
            () =>
            {
                PaletteBox.Focus();
                Keyboard.Focus(PaletteBox);
            },
            DispatcherPriority.Input);
    }

    private void OnPaletteKeyDown(object sender, KeyEventArgs e)
    {
        var palette = ViewModel.Palette;
        switch (e.Key)
        {
            case Key.Down:
                palette.MoveSelection(1);
                break;
            case Key.Up:
                palette.MoveSelection(-1);
                break;
            case Key.Enter:
                palette.ExecuteSelected();
                break;
            case Key.Escape:
                palette.Close();
                break;
            default:
                return;
        }

        e.Handled = true;
        if (palette.Selected is { } selected)
        {
            PaletteList.ScrollIntoView(selected);
        }
    }

    private void OnPaletteItemClick(object sender, MouseButtonEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is PaletteResult result)
        {
            e.Handled = true;
            ViewModel.Palette.Execute(result);
        }
    }

    /// <summary>Kartın dışına (karartılmış zemine) tıklanınca palet kapanır.</summary>
    /// <summary>Bölüm adı menüsü sol tıkla açılır (sağ tık beklenmez).</summary>
    private void OnChapterNamesClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }

    private void OnPaletteBackdropDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, PaletteOverlay))
        {
            ViewModel.Palette.Close();
        }
    }

    private void OnPaletteCardDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

    // ── Metin ───────────────────────────────────────────────────────────

    /// <summary>Tuvalde metne çift tıklanınca «Metin» sekmesindeki yazı kutusuna odaklanılır, yazının tamamı seçilir.</summary>
    private void FocusTextEditor()
    {
        ViewModel.PanelTabIndex = MainViewModel.TextTabIndex;
        _ = Dispatcher.InvokeAsync(
            () =>
            {
                TextContentBox.Focus();
                TextContentBox.SelectAll();
            },
            DispatcherPriority.Input);
    }

    // ── Galeri ızgarası ve tuval ölçüsü ─────────────────────────────────

    private void OnGallerySizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Kenar boşlukları (10 + 6) ve kaydırma çubuğu (10) düşülür.
        var usable = e.NewSize.Width - 26;
        ViewModel.GalleryColumns = Math.Max(2, (int)(usable / MinTileWidth));
    }

    private void OnCanvasScrollerSizeChanged(object sender, SizeChangedEventArgs e) =>
        ViewModel.Zoom.SetViewport(e.NewSize.Width, e.NewSize.Height);
}
