using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NexAlbum.Core.Profiles;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Json;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.Views;

/// <summary>Yeni proje: ad ve baskı profili (Karar 03: profil kullanıcı tanımlıdır).</summary>
public partial class NewProjectWindow : Window
{
    private readonly List<AlbumProfile> profiles = [.. BuiltInProfiles.All];

    public NewProjectWindow()
    {
        InitializeComponent();
        ProfileBox.ItemsSource = profiles;
        ProfileBox.SelectedIndex = profiles.FindIndex(p => p.Id == "builtin-30x30");
    }

    public NewProjectChoice? Choice { get; private set; }

    private void OnProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileBox.SelectedItem is not AlbumProfile profile)
        {
            ProfileNote.Text = string.Empty;
            return;
        }

        ProfileNote.Text = profile.Source == ProfileSource.Builtin
            ? "Bu bir örnek profildir: yalnızca sayfa ölçüsü içerir. Taşma payı, güvenli alan ve DPI değerleri baskı evinizden alınıp girilmeden çıktı alınamaz."
            : profile.Description ?? string.Empty;
    }

    private void OnLoadProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Profil (*.json)|*.json", Title = "Profil dosyası seç" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var profile = ProfileJson.Deserialize(File.ReadAllText(dialog.FileName));
            if (profile.Page.Size is null)
            {
                MessageBox.Show(this, "Profilde sayfa ölçüsü yok.", AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            profiles.Add(profile);
            ProfileBox.Items.Refresh();
            ProfileBox.SelectedItem = profile;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ProjectFormatException)
        {
            MessageBox.Show(this, "Profil okunamadı: " + ex.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnCreate(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text) || ProfileBox.SelectedItem is not AlbumProfile profile)
        {
            MessageBox.Show(this, "Proje adı ve profil seçilmeli.", AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Choice = new NewProjectChoice(NameBox.Text.Trim(), profile);
        DialogResult = true;
    }
}
