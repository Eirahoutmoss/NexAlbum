using System.IO;
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

            var editor = new ProfileEditorWindow(Core.Profiles.BuiltInProfiles.All[0]);
            editor.Result.Should().BeNull();
            editor.Close();

            var settings = new SettingsWindow(new Services.AppSettings());
            settings.Result.Should().BeNull();
            settings.Close();

            var batch = new BatchEditWindow(new BatchEditViewModel([new EditScopeOption(EditScope.All, "Tüm fotoğraflar", [])], lastRecipe: null, new SmokeHost()));
            batch.DataContext.Should().BeOfType<BatchEditViewModel>();
            batch.Close();

            var page = new PageView();
            page.Should().NotBeNull();

            main.Close();
            about.Close();
            newProject.Close();
        });
    }

    private sealed class SmokeHost : IBatchEditHost
    {
        public Integrations.Photoshop.PhotoshopEngine Photoshop { get; } = new(new Integrations.Photoshop.ComPhotoshopHost());

        public TimeProvider Clock => TimeProvider.System;

        public string OutputFolder => Path.GetTempPath();

        public string? ToolPath(string presetId) => null;

        public void RememberToolPath(string presetId, string path)
        {
        }

        public string? AskExecutable() => null;

        public void Apply(Integrations.Editing.EditBatchResult result)
        {
        }
    }
}
