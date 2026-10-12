using System.Windows;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI;

/// <summary>
/// Ana pencere. İş mantığı ViewModel'de tutulur (MVVM, Karar 08 §13).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
