using System.Windows;
using Microsoft.Win32;
using NexAlbum.Persistence.Format;
using NexAlbum.UI.Views;

namespace NexAlbum.UI.Services;

/// <summary>WPF diyaloglarıyla <see cref="IDialogService"/>.</summary>
public sealed class DialogService(Window owner) : IDialogService
{
    private const string ProjectFilter = "NexAlbum projesi (*.albumproj)|*.albumproj";

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
