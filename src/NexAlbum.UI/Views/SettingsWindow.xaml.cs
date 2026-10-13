using System.IO;
using System.Windows;
using Microsoft.Win32;
using NexAlbum.Integrations.Editing;
using NexAlbum.Integrations.Photoshop;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.Views;

/// <summary>Bu bilgisayara özgü ayarlar: tema (Karar 17), harici düzenleyici ve düzenlenmiş dosyaların klasörü (Karar 20).</summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        EditorBox.Text = settings.EditorPath ?? string.Empty;
        FolderBox.Text = settings.EditsFolder ?? EditFolders.DefaultRoot;
        var dark = ThemeService.Parse(settings.Theme) == AppTheme.Gece;
        GeceTheme.IsChecked = dark;
        FildisiTheme.IsChecked = !dark;
        var photoshop = PhotoshopLocator.FindExecutable();
        DetectedText.Text = photoshop is null ? "Bu bilgisayarda Photoshop bulunamadı." : "Bulunan Photoshop: " + photoshop;
    }

    public AppSettings? Result { get; private set; }

    private void OnBrowseEditor(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Düzenleyici programı seçin", Filter = "Programlar (*.exe)|*.exe" };
        if (dialog.ShowDialog(this) == true)
        {
            EditorBox.Text = dialog.FileName;
        }
    }

    private void OnBrowseFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Düzenlenmiş dosyaların klasörü" };
        if (dialog.ShowDialog(this) == true)
        {
            FolderBox.Text = dialog.FolderName;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var editor = EditorBox.Text.Trim();
        if (editor.Length > 0 && !File.Exists(editor))
        {
            MessageBox.Show(this, "Düzenleyici programı bulunamadı:\n" + editor, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var folder = FolderBox.Text.Trim();
        Result = settings with
        {
            EditorPath = editor.Length == 0 ? null : editor,
            EditsFolder = folder.Length == 0 || string.Equals(folder, EditFolders.DefaultRoot, StringComparison.OrdinalIgnoreCase) ? null : folder,
            Theme = ThemeService.ToSetting(FildisiTheme.IsChecked == true ? AppTheme.Fildisi : AppTheme.Gece),
        };
        DialogResult = true;
    }
}
