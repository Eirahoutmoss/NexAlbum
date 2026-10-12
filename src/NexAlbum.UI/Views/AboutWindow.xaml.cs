using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace NexAlbum.UI.Views;

/// <summary>Hakkında penceresi (Karar 12 §4.2).</summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = "Sürüm " + AppInfo.Version;
        AuthorText.Text = AppInfo.Author;
        AuthorLink.NavigateUri = new Uri(AppInfo.AuthorUrl);
        AuthorLinkText.Text = AppInfo.AuthorUrl.Replace("https://", string.Empty, StringComparison.Ordinal);
        RepoLink.NavigateUri = new Uri(AppInfo.RepositoryUrl);
        RepoLinkText.Text = AppInfo.RepositoryUrl.Replace("https://", string.Empty, StringComparison.Ordinal);
    }

    private void OnNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        e.Handled = true;
    }
}
