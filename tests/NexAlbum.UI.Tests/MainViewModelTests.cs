using System.IO;
using FluentAssertions;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

public sealed partial class MainViewModelTests : IDisposable
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

    [Fact]
    public void FramesAndBackgroundsApplyToSelectedPageOrAllUnlockedPages()
    {
        var path = Path.Combine(dir, "tasarim.albumproj");
        var project = SampleProject(3);
        project = project.WithPages(project.Assets.Select((a, i) => new AlbumPage
        {
            Id = $"p{i}",
            Elements = [new Slot { Id = "s", Bounds = new RectMm(10, 10, 100, 100), Placement = new PhotoPlacement { AssetId = a.Id } }],
        }));
        ProjectPackageWriter.Save(path, project, new SaveOptions());
        var dialogs = new FakeDialogs();
        using var vm = new MainViewModel(dialogs);
        vm.Open(path);

        vm.ApplyToSelectedPage.Should().BeTrue("varsayılan kapsam seçili sayfa");
        vm.Frames.Should().Contain(f => f.Id == "polaroid");
        vm.ApplyFrame(vm.Frames.Single(f => f.Id == "mat-white"));
        vm.Project!.Pages[0].Slots.Single().Mat.Should().NotBeNull();
        vm.Project.Pages[1].Slots.Single().Mat.Should().BeNull();
        vm.Project.Settings.Design.FrameStyleId.Should().Be("none", "tek sayfa işlemi proje varsayılanını değiştirmez");
        vm.SelectedPage!.Number.Should().Be(1, "seçim korunur");
        vm.SelectedPage.Slots.Single().PhotoWidth.Should().BeLessThan(100, "paspartu fotoğraf alanını daraltır");

        vm.SelectedPage = vm.Pages[1];
        vm.ToggleLock();
        vm.ApplyToAllPages = true;
        vm.BackgroundOpacity = 0.5;
        vm.ApplyBackground(vm.BackgroundGradients[0]);
        vm.Project.Pages[0].Background.Mode.Should().Be(BackgroundMode.Gradient);
        vm.Project.Pages[0].Background.Opacity.Should().Be(0.5);
        vm.Project.Pages[1].Background.Should().Be(BackgroundFill.White, "kilitli sayfa atlanır");
        vm.Project.Pages[2].Background.Mode.Should().Be(BackgroundMode.Gradient);
        vm.Project.Settings.Design.Background!.Mode.Should().Be(BackgroundMode.Gradient);
        vm.Status.Should().Contain("1 kilitli");
        vm.Pages[0].Background.Should().BeAssignableTo<System.Windows.Media.LinearGradientBrush>();

        vm.ApplyFrame(vm.Frames.Single(f => f.Id == "polaroid"));
        vm.Project.Settings.Design.FrameStyleId.Should().Be("polaroid");
        vm.Project.Pages[2].Slots.Single().StyleId.Should().Be("polaroid");

        vm.CustomColor = "f5efe6";
        vm.ApplyCustomColor();
        vm.Project.Pages[0].Background.Color.Should().Be(RgbaColor.Parse("#F5EFE6"));
        vm.CustomColor = "renk değil";
        vm.ApplyCustomColor();
        dialogs.Warnings.Should().ContainSingle().Which.Should().Contain("Renk");
    }

    [Fact]
    public async Task TemplateGalleryFiltersByPagePhotoCountAndReplacesTheLayout()
    {
        var path = Path.Combine(dir, "sablon.albumproj");
        ProjectPackageWriter.Save(path, SampleProject(12), new SaveOptions());
        using var vm = new MainViewModel(new FakeDialogs());
        vm.Open(path);
        await vm.AutoLayoutAsync(newSeed: false);

        var page = vm.SelectedPage!;
        vm.SelectedTemplateCount.Count.Should().Be(page.PhotoCount, "süzgeç seçili sayfanın fotoğraf sayısına geçer");
        vm.Templates.Should().NotBeEmpty().And.OnlyContain(t => t.Template.PhotoCount.Accepts(page.PhotoCount));
        vm.Templates.Should().OnlyContain(t => t.Cells.Count == t.Template.Slots.Count);

        var other = vm.Templates.First(t => t.Id != page.TemplateId);
        vm.ApplyTemplate(other);
        vm.Project!.Pages[0].TemplateId.Should().Be(other.Id);
        vm.Project.Pages[0].Slots.Count(s => !s.IsEmpty).Should().Be(page.PhotoCount, "fotoğraflar korunur");

        vm.SelectedTemplateCount = vm.TemplateCounts[0];
        vm.Templates.Count.Should().BeGreaterThan(50, "tüm şablonlar");

        vm.SelectedTemplateCount = vm.TemplateCounts.Single(c => c.Count == 1);
        var before = vm.Project.Pages[0].Slots.Count(s => !s.IsEmpty);
        vm.ApplyTemplate(vm.Templates[0]);
        vm.Project.Pages[0].Slots.Count(s => !s.IsEmpty).Should().Be(1);
        if (before > 1)
        {
            vm.Status.Should().Contain("çıkarıldı");
            vm.Project.UnusedAssets().Should().HaveCount(before - 1);
        }

        vm.ToggleLock();
        vm.ApplyTemplateCommand.CanExecute(vm.Templates[0]).Should().BeFalse("kilitli sayfanın şablonu değişmez");
    }

    [Fact]
    public async Task SpacingRelayoutsUnlockedPagesAndCanBeReset()
    {
        var path = Path.Combine(dir, "bosluk.albumproj");
        ProjectPackageWriter.Save(path, SampleProject(12), new SaveOptions());
        using var vm = new MainViewModel(new FakeDialogs());
        vm.Open(path);
        await vm.AutoLayoutAsync(newSeed: false);
        vm.SpacingSummary.Should().Contain("kendi");
        var before = vm.Project!.Pages.SelectMany(p => p.Slots).Select(s => s.Bounds).ToList();

        vm.GapPercent = 6;
        vm.MarginPercent = 8;
        vm.ApplySpacing();

        vm.Project!.Settings.Design.GapPercent.Should().Be(6);
        vm.Project.Settings.Design.MarginPercent.Should().Be(8);
        vm.Project.Pages.SelectMany(p => p.Slots).Select(s => s.Bounds).Should().NotEqual(before);
        vm.Project.Pages.SelectMany(p => p.Slots).Should().OnlyContain(s => s.Placement != null, "fotoğraflar korunur");
        vm.SpacingSummary.Should().Contain("6");

        vm.ResetSpacing();
        vm.Project.Settings.Design.GapPercent.Should().BeNull();
        vm.Project.Pages.SelectMany(p => p.Slots).Select(s => s.Bounds).Should().Equal(before);
        vm.GapPercent.Should().Be(MainViewModel.DefaultGapPercent);
    }

    [Fact]
    public async Task BackgroundImagesAreImportedSeparatelyFromPhotos()
    {
        var image = Path.Combine(dir, "doku.png");
        File.WriteAllBytes(image, TinyPng);
        var dialogs = new FakeDialogs { NewProject = new NewProjectChoice("Fon", BuiltInProfiles.All[0]), BackgroundFiles = [image] };
        using var vm = new MainViewModel(dialogs);
        vm.NewProject();

        await vm.ImportBackgroundsAsync();

        vm.BackgroundImages.Should().ContainSingle();
        vm.HasBackgroundImages.Should().BeTrue();
        vm.Assets.Should().BeEmpty("fon görselleri fotoğraf galerisine karışmaz");
        vm.HasPhotos.Should().BeFalse();
        vm.Project!.Backgrounds.Should().ContainSingle().Which.Role.Should().Be(AssetRole.Background);

        vm.ApplyBackground(vm.BackgroundImages[0]);
        vm.Project.Settings.Design.Background!.ImageAssetId.Should().Be(vm.Project.Backgrounds[0].Id, "sayfa yokken proje varsayılanı olur");
    }

    [Fact]
    public async Task PhotosCanBePickedOneByOneOrDroppedAsFilesAndFolders()
    {
        var a = Path.Combine(dir, "a.png");
        var b = Path.Combine(dir, "b.png");
        var sub = Directory.CreateDirectory(Path.Combine(dir, "klasor", "alt")).FullName;
        var c = Path.Combine(sub, "c.png");
        foreach (var f in new[] { a, b, c })
        {
            File.WriteAllBytes(f, TinyPng);
        }

        File.WriteAllText(Path.Combine(sub, "not.txt"), "görsel değil");
        var dialogs = new FakeDialogs { NewProject = new NewProjectChoice("Seçme", BuiltInProfiles.All[0]), PhotoFiles = [a] };
        using var vm = new MainViewModel(dialogs);
        vm.NewProject();

        await vm.ImportFilesAsync();
        vm.Assets.Should().ContainSingle().Which.FileName.Should().Be("a.png");

        await vm.ImportPathsAsync([a, b, Path.Combine(dir, "klasor")]);
        vm.Assets.Select(x => x.FileName).Should().BeEquivalentTo(new[] { "a.png", "b.png", "c.png" }, "klasörler alt klasörleriyle taranır, görsel olmayan atlanır");
        vm.Status.Should().Contain("2 fotoğraf eklendi").And.Contain("1 zaten projedeydi");

        await vm.ImportPathsAsync([Path.Combine(sub, "not.txt")]);
        vm.Status.Should().Contain("desteklenen görsel yok");
    }

    [Fact]
    public void PhotosArePlacedSwappedPannedZoomedAndRemoved()
    {
        var path = Path.Combine(dir, "duzen.albumproj");
        var project = SampleProject(4);
        project = project.WithPages(
        [
            new AlbumPage
            {
                Id = "p1",
                Elements =
                [
                    new Slot { Id = "s1", Bounds = new RectMm(10, 10, 100, 100), Placement = new PhotoPlacement { AssetId = "asset-0001" } },
                    new Slot { Id = "s2", Bounds = new RectMm(120, 10, 100, 150), Placement = new PhotoPlacement { AssetId = "asset-0002" } },
                    new Slot { Id = "s3", Bounds = new RectMm(10, 120, 100, 100) },
                ],
            },
            new AlbumPage { Id = "p2", Elements = [new Slot { Id = "t1", Bounds = new RectMm(10, 10, 100, 100) }] },
        ]);
        ProjectPackageWriter.Save(path, project, new SaveOptions());
        var dialogs = new FakeDialogs();
        using var vm = new MainViewModel(dialogs);
        vm.Open(path);
        var secondPage = vm.Pages[1];

        // Galeride çift tık: seçim yoksa ilk boş yuvaya.
        vm.PlaceIntoSelection("asset-0003").Should().BeTrue();
        SlotById(vm, "s3").Placement!.AssetId.Should().Be("asset-0003");
        vm.SelectedSlot!.Id.Should().Be("s3", "yerleştirilen yuva seçili kalır");
        vm.Pages[1].Should().BeSameAs(secondPage, "değişmeyen sayfanın görünüm modeli korunur");
        vm.Assets.Single(x => x.Asset.Id == "asset-0003").IsUsed.Should().BeTrue();

        // Galeriden sürükle-bırak dolu yuvaya: eskisi kullanılmamış olur.
        vm.PlacePhoto(vm.SelectedPage!.Slots.Single(x => x.Id == "s1"), "asset-0004").Should().BeTrue();
        SlotById(vm, "s1").Placement!.AssetId.Should().Be("asset-0004");
        vm.Project!.UnusedAssets().Select(x => x.Id).Should().Equal("asset-0001");

        // Yuvadan yuvaya: yer değiştirir.
        vm.MoveSlot("s1", vm.SelectedPage!.Slots.Single(x => x.Id == "s2")).Should().BeTrue();
        SlotById(vm, "s1").Placement!.AssetId.Should().Be("asset-0002");
        SlotById(vm, "s2").Placement!.AssetId.Should().Be("asset-0004");
        vm.Status.Should().Contain("yer değiştirdi");

        // Seçili yuvada yakınlaştırma ve kaydırma (sınırlı).
        vm.SelectSlot(vm.SelectedPage!.Slots.Single(x => x.Id == "s1"));
        vm.SelectedSlotInfo.Should().Contain("dpi");
        vm.SlotZoomPercent = 200;
        SlotById(vm, "s1").Placement!.Scale.Should().Be(2);
        vm.SlotZoomPercent.Should().Be(200);
        vm.ZoomSlot(vm.SelectedSlot!, 100);
        SlotById(vm, "s1").Placement!.Scale.Should().Be(Core.Layout.PageEditor.MaxScale);

        var slot = vm.SelectedSlot!;
        vm.PreviewPan(slot, slot.Placement!, deltaXMm: 1000, deltaYMm: 0);
        slot.Placement!.OffsetX.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(1);
        SlotById(vm, "s1").Placement!.OffsetX.Should().Be(0, "önizleme kaydedilmez");
        vm.CommitPlacement(slot);
        SlotById(vm, "s1").Placement!.OffsetX.Should().BeGreaterThan(0);

        vm.CenterSelectedPhoto();
        SlotById(vm, "s1").Placement.Should().Be(new PhotoPlacement { AssetId = "asset-0002" });

        vm.FitButtonText.Should().Be("Tamamını göster");
        vm.ToggleFit();
        SlotById(vm, "s1").CropMode.Should().Be(CropMode.Contain);
        vm.FitButtonText.Should().Be("Yuvayı kapla");

        // Tek yuvaya çerçeve.
        vm.ApplyToSelectedSlot = true;
        vm.ApplyFrame(vm.Frames.Single(f => f.Id == "polaroid"));
        SlotById(vm, "s1").StyleId.Should().Be("polaroid");
        SlotById(vm, "s2").StyleId.Should().BeNull();

        vm.ClearSelectedSlot();
        SlotById(vm, "s1").Placement.Should().BeNull();

        // Kilitli sayfa düzenlenmez.
        vm.ToggleLock();
        vm.PlaceIntoSelection("asset-0001").Should().BeFalse();
        vm.Status.Should().Contain("kilitli");

        // Başka sayfaya geçince yuva seçimi kalkar.
        vm.SelectedPage = vm.Pages[1];
        vm.SelectedSlot.Should().BeNull();
    }

    private static Slot SlotById(MainViewModel vm, string id) => vm.Project!.Pages.SelectMany(p => p.Slots).Single(s => s.Id == id);

    internal static readonly byte[] TinyPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABQAAAAeCAIAAACjcKk8AAAAIklEQVR4nGPkWhDFQC5gIlvnqOZRzaOaRzWPah7VPAQ0AwAeUgFAi1B09gAAAABJRU5ErkJggg==");

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

        public IReadOnlyList<string>? BackgroundFiles { get; init; }

        public IReadOnlyList<string>? AskBackgroundImages() => BackgroundFiles;

        public IReadOnlyList<string>? PhotoFiles { get; init; }

        public IReadOnlyList<string>? AskPhotoFiles() => PhotoFiles;

        public string? Executable { get; set; }

        public List<(string File, string? Executable)> Launched { get; } = [];

        public AppSettings? EditedSettings { get; set; }

        public Action<BatchEditViewModel>? OnBatchEdit { get; set; }

        public string? AskExecutable() => Executable;

        public void LaunchEditor(string file, string? executable) => Launched.Add((file, executable));

        public AppSettings? EditAppSettings(AppSettings settings) => EditedSettings;

        public void ShowBatchEdit(BatchEditViewModel viewModel) => OnBatchEdit?.Invoke(viewModel);

        public AlbumProfile? EditedProfile { get; set; }

        public string? PdfPath { get; set; }

        public string? Revealed { get; private set; }

        public AlbumProfile? EditProfile(AlbumProfile profile) => EditedProfile;

        public string? AskPdfPath(string suggestedName) => PdfPath;

        public string? ImagePath { get; set; }

        public string? AskImagePath(string suggestedName) => ImagePath;

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
