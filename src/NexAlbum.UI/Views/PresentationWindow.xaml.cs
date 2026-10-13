using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI.Views;

/// <summary>
/// Tam ekran sunum (F5): ← → / boşluk / PgUp PgDn / Home End ile gezinme, Esc ile çıkış; ekranın solu geri, sağı ileri.
/// Sayfa geçişinde kısa bir solma vardır (yalnızca sunumda; düzenleyicide sayfa değişimi animasyonsuzdur, Karar 12 §17.3).
/// Hareketsiz kalınca alt bilgi ve imleç gizlenir.
/// </summary>
public partial class PresentationWindow : Window
{
    private static readonly TimeSpan HudDelay = TimeSpan.FromSeconds(2.5);
    private readonly DispatcherTimer idle;

    public PresentationWindow(PresentationViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelChanged;
        idle = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = HudDelay };
        idle.Tick += (_, _) => HideHud();
        Loaded += (_, _) => ShowHud();
        Closed += (_, _) =>
        {
            idle.Stop();
            viewModel.PropertyChanged -= OnViewModelChanged;
            viewModel.Close();
        };
    }

    public PresentationViewModel ViewModel { get; }

    private void OnKey(object sender, KeyEventArgs e)
    {
        var handled = e.Key switch
        {
            Key.Right or Key.Down or Key.Space or Key.PageDown or Key.Enter => Do(ViewModel.Next),
            Key.Left or Key.Up or Key.Back or Key.PageUp => Do(ViewModel.Previous),
            Key.Home => Do(ViewModel.First),
            Key.End => Do(ViewModel.Last),
            Key.Escape or Key.F5 => CloseAndHandle(),
            _ => false,
        };
        e.Handled = handled;
        ShowHud();
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        if (e.GetPosition(Root).X < Root.ActualWidth * 0.35)
        {
            ViewModel.Previous();
        }
        else
        {
            ViewModel.Next();
        }

        ShowHud();
    }

    private void OnActivity(object sender, MouseEventArgs e) => ShowHud();

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PresentationViewModel.Current))
        {
            Stage.BeginAnimation(OpacityProperty, new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new QuadraticEase() });
        }
    }

    private void ShowHud()
    {
        Hud.BeginAnimation(OpacityProperty, null);
        Hud.Opacity = 1;
        Cursor = null;
        idle.Stop();
        idle.Start();
    }

    private void HideHud()
    {
        idle.Stop();
        Hud.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(400)));
        Cursor = Cursors.None;
    }

    private static bool Do(Func<bool> action)
    {
        action();
        return true;
    }

    private bool CloseAndHandle()
    {
        Close();
        return true;
    }
}
