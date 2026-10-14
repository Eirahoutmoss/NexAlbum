using FluentAssertions;
using NexAlbum.Core.Projects;
using NexAlbum.UI.ViewModels;
using NexAlbum.UI.Views;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Sunum penceresi: efektli geçiş karesi ve «Göz at» odak şeridi pencerede gerçekten kurulur (Karar 23).</summary>
[Collection(WpfApp.Collection)]
public sealed class PresentationWindowTests
{
    [Fact]
    public void BrowseStripOpensMovesAndCloses() => WpfApp.Run(() =>
    {
        var project = AlbumProject.Create("Sunum", Core.Profiles.BuiltInProfiles.Find("builtin-30x30")!, DateTimeOffset.UtcNow, "0.1.0")
            .WithPages(Enumerable.Range(0, 7).Select(i => new AlbumPage { Id = $"p{i}" }));
        var window = new PresentationWindow(new PresentationViewModel(project, startPageIndex: 0, spreads: true))
        {
            WindowState = System.Windows.WindowState.Normal,
            Width = 960,
            Height = 600,
            ShowActivated = false,
        };
        window.Show();
        try
        {
            window.UpdateLayout();
            window.ToggleBrowse();
            window.ViewModel.Browsing.Should().BeTrue();
            window.SetStripPosition(1.4);
            window.UpdateLayout();
            WpfApp.Pump(TimeSpan.FromMilliseconds(200));

            window.ToggleBrowse();
            window.ViewModel.Browsing.Should().BeFalse();

            window.ShowEffectFrame(window.ViewModel.Next, new System.Windows.Point(700, 300), 0.5);
            WpfApp.Pump(TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            window.Close();
        }
    });
}
