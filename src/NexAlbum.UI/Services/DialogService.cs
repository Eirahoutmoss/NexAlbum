using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using NexAlbum.Core.Profiles;
using NexAlbum.Persistence.Format;
using NexAlbum.UI.Views;

namespace NexAlbum.UI.Services;

/// <summary>WPF diyaloglarıyla <see cref="IDialogService"/>.</summary>
public sealed class DialogService(Window owner) : IDialogService
{
    private const string ProjectFilter = "NexAlbum projesi (*.albumproj)|*.albumproj";

    private const string ImageFilter = "Görseller|*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.webp;*.bmp;*.gif|Tüm dosyalar|*.*";

    public NewProjectChoice? AskNewProject()
    {
        var window = new NewProjectWindow { Owner = owner };
        return window.ShowDialog() == true ? window.Choice : null;
    }

    public string? AskOpenProjectPath()
    {
        var dialog = new OpenFileDialog { Filter = ProjectFilter, Title = "Proje aç" };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? AskSaveProjectPath(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = ProjectFilter,
            Title = "Projeyi kaydet",
            FileName = suggestedName,
            DefaultExt = ProjectFormat.FileExtension,
            AddExtension = true,
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? AskPhotoFolder()
    {
        var dialog = new OpenFolderDialog { Title = "Fotoğraf klasörü seçin" };
        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }

    public IReadOnlyList<string>? AskPhotoFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Fotoğraf seçin (Ctrl veya Shift ile birden çok)",
            Filter = ImageFilter,
            Multiselect = true,
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileNames : null;
    }

    public IReadOnlyList<string>? AskBackgroundImages()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Fon görseli seçin",
            Filter = ImageFilter,
            Multiselect = true,
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileNames : null;
    }

    public AlbumProfile? EditProfile(AlbumProfile profile)
    {
        var window = new ProfileEditorWindow(profile) { Owner = owner };
        return window.ShowDialog() == true ? window.Result : null;
    }

    public string? AskPdfPath(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            Title = "Baskı PDF'i",
            FileName = suggestedName,
            DefaultExt = ".pdf",
            AddExtension = true,
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? AskImagePath(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JPEG önizleme (*.jpg)|*.jpg|PNG (*.png)|*.png",
            Title = "Sayfaları görüntü olarak kaydet (önizleme)",
            FileName = suggestedName,
            DefaultExt = ".jpg",
            AddExtension = true,
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public void Reveal(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true })?.Dispose();

    public string? AskExecutable()
    {
        var dialog = new OpenFileDialog { Title = "Program seçin", Filter = "Programlar (*.exe)|*.exe|Tüm dosyalar|*.*" };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public void LaunchEditor(string file, string? executable) => Integrations.Editing.ExternalEditor.Open(file, executable);

    public AppSettings? EditAppSettings(AppSettings settings)
    {
        var window = new SettingsWindow(settings) { Owner = owner };
        return window.ShowDialog() == true ? window.Result : null;
    }

    public void ShowBatchEdit(ViewModels.BatchEditViewModel viewModel) => new BatchEditWindow(viewModel) { Owner = owner }.ShowDialog();

    public void ShowAbout() => new AboutWindow { Owner = owner }.ShowDialog();

    public void Info(string message) => MessageBox.Show(owner, message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Warn(string message) => MessageBox.Show(owner, message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);

    public bool Confirm(string message) =>
        MessageBox.Show(owner, message, AppInfo.Name, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public SaveDecision AskSaveChanges(string projectName) =>
        MessageBox.Show(owner, $"\"{projectName}\" projesinde kaydedilmemiş değişiklikler var. Kaydedilsin mi?", AppInfo.Name, MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
        {
            MessageBoxResult.Yes => SaveDecision.Save,
            MessageBoxResult.No => SaveDecision.Discard,
            _ => SaveDecision.Cancel,
        };
}
