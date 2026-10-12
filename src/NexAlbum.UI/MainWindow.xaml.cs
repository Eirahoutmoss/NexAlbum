using System.ComponentModel;
using System.IO;
using System.Windows;
using NexAlbum.Persistence.Format;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI;

/// <summary>
/// Ana pencere (Karar 12 §3: beş bölge). İş mantığı <see cref="MainViewModel"/>'dedir.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel(new DialogService(this));
        DataContext = ViewModel;
        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += (_, _) => ViewModel.Dispose();
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
}
