using System.IO;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

public sealed class MainViewModelTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "nexalbum-ui-tests", Guid.NewGuid().ToString("N"));

    public MainViewModelTests()
    {
        Directory.CreateDirectory(dir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void NewProjectStartsDirtyWithChosenProfile()
    {
        var dialogs = new FakeDialogs { NewProject = new NewProjectChoice("Düğün", BuiltInProfiles.Find("builtin-20x30")!) };
        using var vm = new MainViewModel(dialogs);

        vm.NewProject();

        vm.HasProject.Should().BeTrue();
        vm.Project!.PageSize.Should().Be(new SizeMm(200, 300));
        vm.IsDirty.Should().BeTrue();
        vm.Title.Should().Contain("Düğün").And.Contain("●");
        vm.ProfileSummary.Should().Contain("Bleed: girilmedi", "örnek profilde değer uydurulmaz");
        vm.SaveCommand.CanExecute(null).Should().BeTrue();
        vm.AutoLayoutCommand.CanExecute(null).Should().BeFalse("fotoğraf yokken");
    }

    [Fact]
    public async Task OpenLayoutLockPreflightAndSave()
    {
        var path = Path.Combine(dir, "album.albumproj");
        ProjectPackageWriter.Save(path, SampleProject(12), new SaveOptions { AppVersion = "0.1.0" });

        var dialogs = new FakeDialogs { SavePath = Path.Combine(dir, "kopya.albumproj") };
        using var vm = new MainViewModel(dialogs);
        vm.Open(path);

        vm.HasProject.Should().BeTrue();
        vm.IsDirty.Should().BeTrue("eksik dosyalar işaretlendi");
        vm.Assets.Should().HaveCount(12).And.OnlyContain(a => a.IsMissing);
        vm.AutoLayoutCommand.CanExecute(null).Should().BeTrue();

        await vm.AutoLayoutAsync(newSeed: false);
        vm.Pages.Should().NotBeEmpty();
        vm.SelectedPage.Should().BeSameAs(vm.Pages[0]);
        vm.Project!.UnusedAssets().Should().BeEmpty();
        var first = string.Join(",", vm.Project.Pages.Select(p => p.TemplateId));

        vm.ToggleLock();
        vm.SelectedPage!.IsLocked.Should().BeTrue();
        vm.LockButtonText.Should().Be("Kilidi aç");

        await vm.AutoLayoutAsync(newSeed: true);
        vm.Pages[0].IsLocked.Should().BeTrue("kilitli sayfa korunur");
        dialogs.Confirmations.Should().BeGreaterThan(0);
        string.Join(",", vm.Project.Pages.Select(p => p.TemplateId)).Should().NotBeNullOrEmpty();
        first.Should().NotBeNullOrEmpty();

        vm.RunPreflight();
        vm.Issues.Should().Contain(i => i.Issue.Code == "project.photo.source-missing");
        vm.Issues.First().Issue.Severity.Should().Be(Core.Validation.ValidationSeverity.Error, "hatalar önce listelenir");

        vm.Save(saveAs: true).Should().BeTrue();
        vm.IsDirty.Should().BeFalse();
        File.Exists(dialogs.SavePath).Should().BeTrue();
        ProjectPackageReader.Load(dialogs.SavePath!).Project.Pages.Should().HaveCount(vm.Pages.Count);
    }

    [Fact]
    public void PageNavigation()
    {
        var path = Path.Combine(dir, "nav.albumproj");
        var project = SampleProject(3);
        project = project.WithPages(project.Assets.Select((a, i) => new AlbumPage
        {
            Id = $"p{i}",
            Elements = [new Slot { Id = "s", Bounds = new RectMm(10, 10, 100, 100), Placement = new PhotoPlacement { AssetId = a.Id } }],
        }));
        ProjectPackageWriter.Save(path, project, new SaveOptions());

        using var vm = new MainViewModel(new FakeDialogs());
        vm.Open(path);

        vm.SelectedPage!.Number.Should().Be(1);
        vm.PreviousPageCommand.CanExecute(null).Should().BeFalse();
        vm.NextPageCommand.Execute(null);
        vm.NextPageCommand.Execute(null);
        vm.SelectedPage!.Number.Should().Be(3);
        vm.NextPageCommand.CanExecute(null).Should().BeFalse();
        vm.SelectedPageInfo.Should().StartWith("3 / 3");
        vm.SelectedPage.Slots.Single().IsMissing.Should().BeTrue();
    }

    [Fact]
    public void UnsavedChangesAreGuarded()
    {
        var dialogs = new FakeDialogs { NewProject = new NewProjectChoice("A", BuiltInProfiles.All[0]), SaveDecision = SaveDecision.Cancel };
        using var vm = new MainViewModel(dialogs);
        vm.NewProject();

        vm.CanClose().Should().BeFalse();
        dialogs.SaveDecision = SaveDecision.Discard;
        vm.CanClose().Should().BeTrue();
    }

    [Fact]
    public void CorruptFileIsReportedNotThrown()
    {
        var path = Path.Combine(dir, "bozuk.albumproj");
        File.WriteAllBytes(path, [1, 2, 3]);
        var dialogs = new FakeDialogs();
        using var vm = new MainViewModel(dialogs);
        vm.Open(path);
        vm.HasProject.Should().BeFalse();
        dialogs.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void ProfileTextShowsKnownValues()
    {
        var profile = new AlbumProfile
        {
            Id = "p",
            Name = "Benim",
            Verified = true,
            Bleed = new EdgeSpec(EdgesMm.Uniform(3)),
            SafeArea = new EdgeSpec(new EdgesMm(5, 6, 5, 6)),
            Spread = new SpreadSpec(true),
            Resolution = new ResolutionSpec(300),
            Color = new ColorSpec(ColorMode.Cmyk, "Fogra39.icc"),
        };
        var text = ProfileText.Summary(profile, new SizeMm(300, 300));
        text.Should().Contain("3 mm (her kenar)").And.Contain("sağ 6").And.Contain("Gutter: girilmedi").And.Contain("CMYK / Fogra39.icc").And.Contain("Kullanıcı doğruladı");
    }

    [Fact]
    public async Task ExportIsBlockedWhilePreflightHasErrors()
    {
        var path = Path.Combine(dir, "eksik.albumproj");
        var project = SampleProject(2);
        project = project.WithPages([new AlbumPage { Id = "p1", Elements = [new Slot { Id = "s", Bounds = new RectMm(20, 20, 100, 100) }] }]);
        ProjectPackageWriter.Save(path, project, new SaveOptions());

        var dialogs = new FakeDialogs { PdfPath = Path.Combine(dir, "album.pdf") };
        using var vm = new MainViewModel(dialogs);
        vm.Open(path);
        vm.ExportPdfCommand.CanExecute(null).Should().BeTrue();

        await vm.ExportPdfAsync();

        dialogs.Warnings.Should().ContainSingle().Which.Should().Contain("hata");
        File.Exists(dialogs.PdfPath).Should().BeFalse("örnek profilde taşma payı yok: Karar 03 §7.2 çıktıyı engeller");
        vm.Issues.Should().Contain(i => i.Issue.Code == "profile.bleed.missing");
    }

    [Fact]
    public async Task EditingTheProfileUnblocksExport()
    {
        var path = Path.Combine(dir, "tam.albumproj");
        var project = SampleProject(0).WithPages([new AlbumPage { Id = "p1", Elements = [new Slot { Id = "s", Bounds = new RectMm(20, 20, 100, 100) }] }]);
        ProjectPackageWriter.Save(path, project, new SaveOptions());

        var complete = project.Profile with
        {
            Bleed = new EdgeSpec(EdgesMm.Uniform(3)),
            SafeArea = new EdgeSpec(EdgesMm.Uniform(5)),
            Resolution = new ResolutionSpec(300),
            Color = new ColorSpec(ColorMode.Cmyk),
            Verified = true,
        };
        var dialogs = new FakeDialogs { EditedProfile = complete, PdfPath = Path.Combine(dir, "album.pdf") };
        using var vm = new MainViewModel(dialogs);
        vm.Open(path);

        vm.EditProfile();
        vm.Project!.Profile.Bleed.Value.Should().Be(EdgesMm.Uniform(3));
        vm.ProfileSummary.Should().Contain("3 mm (her kenar)");

        await vm.ExportPdfAsync();

        dialogs.Confirmations.Should().BeGreaterThan(0, "boş yuva ve ICC uyarıları onaylanır");
        File.Exists(dialogs.PdfPath).Should().BeTrue();
        dialogs.Revealed.Should().Be(Path.GetFullPath(dialogs.PdfPath!));
        vm.Status.Should().Contain("PDF hazır");
    }

    private static AlbumProject SampleProject(int photos)
    {
        var now = new DateTimeOffset(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
        var project = AlbumProject.Create("Örnek", BuiltInProfiles.Find("builtin-30x30")!, now, "0.1.0", Guid.NewGuid());
        return project.AddAssets(Enumerable.Range(1, photos).Select(i => new PhotoAsset
        {
            Id = $"asset-{i:0000}",
            SourcePath = $@"C:\yok\IMG_{i:0000}.jpg",
            FileName = $"IMG_{i:0000}.jpg",
            PixelWidth = i % 2 == 0 ? 4000 : 6000,
            PixelHeight = i % 2 == 0 ? 6000 : 4000,
            ImportedAt = now,
        }));
    }

    private sealed class FakeDialogs : IDialogService
    {
        public NewProjectChoice? NewProject { get; init; }

        public string? SavePath { get; init; }

        public SaveDecision SaveDecision { get; set; } = SaveDecision.Discard;

        public int Confirmations { get; private set; }

        public List<string> Warnings { get; } = [];

        public NewProjectChoice? AskNewProject() => NewProject;

        public string? AskOpenProjectPath() => null;

        public string? AskSaveProjectPath(string suggestedName) => SavePath;

        public string? AskPhotoFolder() => null;

        public AlbumProfile? EditedProfile { get; set; }

        public string? PdfPath { get; set; }

        public string? Revealed { get; private set; }

        public AlbumProfile? EditProfile(AlbumProfile profile) => EditedProfile;

        public string? AskPdfPath(string suggestedName) => PdfPath;

        public void Reveal(string path) => Revealed = path;

        public void ShowAbout()
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message) => Warnings.Add(message);

        public bool Confirm(string message)
        {
            Confirmations++;
            return true;
        }

        public SaveDecision AskSaveChanges(string projectName) => SaveDecision;
    }
}
