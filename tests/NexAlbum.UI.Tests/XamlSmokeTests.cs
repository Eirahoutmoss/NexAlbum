using System.Windows;
using FluentAssertions;
using NexAlbum.UI.ViewModels;
using NexAlbum.UI.Views;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>
/// Tüm pencereler gerçekten oluşturulur: XAML ayrıştırma, kaynak anahtarları ve tür adları çalışma anında denetlenir.
/// (Application tek örneklidir; bu yüzden tek test içinde.)
/// </summary>
public class XamlSmokeTests
{
    [Fact]
    public void AllWindowsLoadTheirXaml()
    {
        Sta.Run(() =>
        {
            var app = Application.Current as App ?? new App();
            app.InitializeComponent();

            var main = new MainWindow();
            main.DataContext.Should().BeOfType<MainViewModel>();
            main.ViewModel.HasNoProject.Should().BeTrue();

            var about = new AboutWindow();
            about.Title.Should().Contain("hakkında");

            var newProject = new NewProjectWindow();
            newProject.Choice.Should().BeNull();

            var page = new PageView();
            page.Should().NotBeNull();

            main.Close();
            about.Close();
            newProject.Close();
        });
    }
}
