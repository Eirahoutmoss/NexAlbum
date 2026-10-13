using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI.Views;

/// <summary>Hikâye filmi penceresi (Karar 21): ayarlar solda, önizleme ortada, elle bölüm efektleri sağda.</summary>
public partial class VideoWindow : Window, IVideoDialogs
{
    public VideoWindow(VideoViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        viewModel.Dialogs = this;
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.RenderPreviewNow();
        Closing += (_, e) =>
        {
            if (viewModel.IsExporting && MessageBox.Show(this, "Dışa aktarma sürüyor. İptal edip kapatılsın mı?", AppInfo.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            viewModel.CancelExportCommand.Execute(null);
        };
    }

    public VideoViewModel ViewModel { get; }

    public string? AskVideoPath(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "MP4 video (*.mp4)|*.mp4",
            Title = "Hikâye filmini kaydet",
            FileName = string.Concat(suggestedName.Split(Path.GetInvalidFileNameChars())),
            DefaultExt = ".mp4",
            AddExtension = true,
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    public string? AskMusicFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Müzik klasörü" };
        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    public string? AskFfmpeg()
    {
        var dialog = new OpenFileDialog { Filter = "FFmpeg (ffmpeg.exe)|ffmpeg.exe;ffmpeg|Tüm dosyalar|*.*", Title = "ffmpeg dosyasını gösterin" };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    public bool Confirm(string message) =>
        MessageBox.Show(this, message, AppInfo.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void Warn(string message) => MessageBox.Show(this, message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void Reveal(string path)
    {
        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
    }
}
