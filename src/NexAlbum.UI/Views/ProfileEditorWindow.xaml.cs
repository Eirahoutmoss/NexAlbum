using System.IO;
using System.Windows;
using Microsoft.Win32;
using NexAlbum.Core.Profiles;
using NexAlbum.Persistence.Json;
using NexAlbum.UI.ViewModels;

namespace NexAlbum.UI.Views;

/// <summary>Projenin baskı profilini düzenleme (Karar 03 §6).</summary>
public partial class ProfileEditorWindow : Window
{
    private readonly ProfileForm form;

    public ProfileEditorWindow(AlbumProfile profile)
    {
        InitializeComponent();
        form = new ProfileForm(profile);
        ColorBox.ItemsSource = ProfileForm.ColorModes;
        DataContext = form;
    }

    public AlbumProfile? Result { get; private set; }

    private AlbumProfile? Build()
    {
        var profile = form.TryBuild(DateTimeOffset.Now, out var errors, out var warnings);
        if (profile is null)
        {
            Messages.Text = string.Join(Environment.NewLine, errors);
            return null;
        }

        if (warnings.Count > 0
            && MessageBox.Show(this, string.Join(Environment.NewLine, warnings) + "\n\nYine de kullanılsın mı?", AppInfo.Name, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return null;
        }

        return profile;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        Result = Build();
        if (Result is not null)
        {
            DialogResult = true;
        }
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        var profile = Build();
        if (profile is null)
        {
            return;
        }

        var dialog = new SaveFileDialog { Filter = "Profil (*.json)|*.json", FileName = profile.Id + ".json", Title = "Profili kaydet" };
        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, ProfileJson.Serialize(profile));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Messages.Text = "Kaydedilemedi: " + ex.Message;
            }
        }
    }
}
