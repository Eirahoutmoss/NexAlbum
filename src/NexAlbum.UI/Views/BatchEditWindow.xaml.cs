using System.ComponentModel;
using System.Windows;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI.Views;

/// <summary>Toplu düzenleme penceresi (Karar 20). Mantık <see cref="BatchEditViewModel"/>'dedir.</summary>
public partial class BatchEditWindow : Window
{
    private readonly BatchEditViewModel viewModel;

    public BatchEditWindow(BatchEditViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        Closing += OnClosing;
    }

    /// <summary>İş sürerken kapatılmaz; önce iptal edilir.</summary>
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (viewModel.IsRunning)
        {
            viewModel.CancelCommand.Execute(null);
            e.Cancel = true;
        }
    }
}
