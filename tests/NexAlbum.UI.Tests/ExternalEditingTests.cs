using System.IO;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Integrations.Editing;
using NexAlbum.Integrations.Photoshop;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

public sealed partial class MainViewModelTests
{
    private async Task<(MainViewModel Vm, FakeDialogs Dialogs, string Original)> ProjectWithRealPhoto(FakePhotoshop? photoshop = null)
    {
        var original = Path.Combine(dir, "IMG_0001.png");
        File.WriteAllBytes(original, TinyPng);
        var dialogs = new FakeDialogs { NewProject = new NewProjectChoice("Rötuş", BuiltInProfiles.All[0]), PhotoFiles = [original] };
        var settings = new MemoryAppSettingsStore(new AppSettings { EditsFolder = Path.Combine(dir, "duzenli"), EditorPath = Path.Combine(dir, "editor.exe") });
        var vm = new MainViewModel(dialogs, settings: settings, photoshop: photoshop ?? new FakePhotoshop()) { WatchEditorSaves = false };
        vm.NewProject();
        await vm.ImportFilesAsync();
        var project = vm.Project!;
        vm.Open(SaveWithPage(project));
        return (vm, dialogs, original);
    }

    private string SaveWithPage(AlbumProject project)
    {
        var path = Path.Combine(dir, "rotus.albumproj");
        var asset = project.Photos.Single();
        project = project.WithPages([new AlbumPage
        {
            Id = "p1",
            Elements = [new Slot { Id = "s1", Bounds = new RectMm(10, 10, 100, 150), Placement = new PhotoPlacement { AssetId = asset.Id } }],
        }]);
        Persistence.Packaging.ProjectPackageWriter.Save(path, project, new Persistence.Packaging.SaveOptions());
        return path;
    }

    [Fact]
    public async Task OpenInEditorWorksOnACopyAndPicksUpTheSave()
    {
        var (vm, dialogs, original) = await ProjectWithRealPhoto();
        using var _ = vm;
        var originalBytes = File.ReadAllBytes(original);
        vm.SelectSlot(vm.SelectedPage!.Slots.Single());
        vm.OpenInEditorCommand.CanExecute(null).Should().BeTrue();

        vm.OpenInEditor();

        var (file, exe) = dialogs.Launched.Single();
        exe.Should().Be(Path.Combine(dir, "editor.exe"));
        file.Should().StartWith(Path.Combine(dir, "duzenli")).And.EndWith("-elle.png");
        File.Exists(file).Should().BeTrue();

        WriteWhenFree(file, [.. TinyPng, 1, 2, 3]);
        vm.OnExternalEditSaved(vm.Project!.Photos[0].Id, file);

        var asset = vm.Project!.Photos[0];
        asset.IsEditActive.Should().BeTrue();
        asset.Edit!.Tool.Should().Be("editor");
        File.ReadAllBytes(original).Should().Equal(originalBytes, "orijinal değişmez");
        vm.SelectedSlot!.Asset!.SourcePath.Should().Be(file, "tuval düzenlenmiş dosyayı gösterir");
        vm.SelectedSlotHasEdit.Should().BeTrue();
        vm.SelectedSlotEditInfo.Should().StartWith("Düzenlendi: editor");
        vm.Assets.Single().EditBadge.Should().Be("Düzenlendi");

        vm.ShowOriginal = true;
        vm.Project!.Photos[0].UseEdit.Should().BeFalse();
        vm.SelectedSlot!.Asset!.SourcePath.Should().Be(original);
        vm.ShowOriginal.Should().BeTrue();
        vm.Assets.Single().EditBadge.Should().Be("Orijinal gösteriliyor");
        vm.ShowOriginal = false;

        vm.OpenInEditor();
        dialogs.Launched.Last().File.Should().Be(file, "süren düzenleme yeniden açılır");

        vm.RemoveEditCommand.CanExecute(null).Should().BeTrue();
        vm.RemoveEdit();
        vm.Project!.Photos[0].Edit.Should().BeNull();
        File.Exists(file).Should().BeTrue("düzenlenmiş dosya silinmez");

        WriteWhenFree(file, "bozuk"u8.ToArray());
        vm.OnExternalEditSaved(vm.Project!.Photos[0].Id, file);
        vm.Project!.Photos[0].Edit.Should().BeNull();
        vm.Status.Should().Contain("okunamadı");
    }

    [Fact]
    public async Task BatchEditWithACommandLineToolPreviewsThenAppliesToTheScope()
    {
        var (vm, dialogs, original) = await ProjectWithRealPhoto();
        using var _ = vm;
        BatchEditViewModel? shown = null;
        dialogs.OnBatchEdit = b => shown = b;
        vm.BatchEditCommand.Execute(null);
        var batch = shown!;

        batch.UsePhotoshop.Should().BeFalse("Photoshop kullanılamıyorsa komut satırı seçili gelir");
        batch.Scopes.Select(s => s.Assets.Count).Should().Equal(0, 1, 1, 1, 1);
        batch.SelectedScope.Scope.Should().Be(EditScope.Page, "boş kapsam atlanır");

        batch.Executable = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        batch.ArgumentsText = "/c\ncopy\n/y\n{input}\n{output}";
        batch.OutputPng = true;
        batch.RecipeName = "Kopya Testi";
        batch.Problem.Should().BeNull();
        batch.TryCommand.CanExecute(null).Should().BeTrue();

        await batch.RunAsync(previewOnly: true);

        batch.ResultText.Should().StartWith("1 fotoğraf düzenlendi");
        batch.Failures.Should().BeEmpty();
        batch.HasPreview.Should().BeTrue();
        batch.BeforeImage.Should().NotBeNull();
        var asset = vm.Project!.Photos[0];
        asset.Edit!.Recipe.Should().Be("Kopya Testi");
        asset.Edit.Path.Should().EndWith("-kopya-testi.png");
        vm.Project.Settings.Editing.LastRecipe!.Executable.Should().EndWith("cmd.exe");
        vm.AppSettings.ToolPaths.Should().ContainKey(batch.SelectedPreset.Id);
        File.ReadAllBytes(original).Should().Equal(TinyPng);

        batch.SelectedScope = batch.Scopes.Single(s => s.Scope == EditScope.All);
        await batch.RunAsync(previewOnly: false);
        batch.Progress.Should().Be(1);
        vm.Status.Should().Contain("Kopya Testi");

        BatchEditViewModel? again = null;
        dialogs.OnBatchEdit = b => again = b;
        vm.BatchEditCommand.Execute(null);
        again!.Scopes.Single(s => s.Scope == EditScope.Unedited).Assets.Should().BeEmpty("düzenlenmiş fotoğraf bu kapsamda yer almaz");
        dialogs.OnBatchEdit = b => shown = b;

        batch.ArgumentsText = "{input}";
        batch.Problem.Should().Contain("{output}");
        batch.RunCommand.CanExecute(null).Should().BeFalse();

        // Tarif projede saklanır; pencere yeniden açılınca aynı ayarlarla gelir.
        vm.BatchEditCommand.Execute(null);
        shown!.RecipeName.Should().Be("Kopya Testi");
        shown.UseCommandLine.Should().BeTrue();
        shown.OutputPng.Should().BeTrue();
    }

    [Fact]
    public async Task PhotoshopActionsAreListedAndFailuresReported()
    {
        var photoshop = new FakePhotoshop { Available = true, Reply = "Stüdyo\tSıcak Ton\nStüdyo\tSoğuk Ton" };
        var (vm, dialogs, _) = await ProjectWithRealPhoto(photoshop);
        using var __ = vm;
        BatchEditViewModel? batch = null;
        dialogs.OnBatchEdit = b => batch = b;
        vm.BatchEditCommand.Execute(null);

        batch!.UsePhotoshop.Should().BeTrue();
        await batch.LoadActionsAsync();
        batch.Actions.Should().HaveCount(2);
        batch.SelectedAction.Should().Be(new PhotoshopAction("Stüdyo", "Sıcak Ton"));
        batch.RecipeName.Should().Be("Stüdyo / Sıcak Ton");

        photoshop.Reply = "ERR:Action içinde kaydetme adımı var";
        batch.SelectedScope = batch.Scopes.Single(s => s.Scope == EditScope.Album);
        await batch.RunAsync(previewOnly: false);
        batch.Failures.Should().ContainSingle().Which.Reason.Should().Contain("kaydetme adımı");
        vm.Project!.Photos[0].Edit.Should().BeNull("başarısız dosya bağlanmaz");
        photoshop.Scripts.Last().Should().Contain("app.doAction(\"Sıcak Ton\", \"Stüdyo\")");

        photoshop.Available = false;
        await batch.RunAsync(previewOnly: true);
        batch.ResultText.Should().Contain("bulunamadı");
    }

    [Fact]
    public void SettingsAreEditedAndSaved()
    {
        var store = new MemoryAppSettingsStore();
        var dialogs = new FakeDialogs { EditedSettings = new AppSettings { EditorPath = @"C:\gimp.exe" } };
        using var vm = new MainViewModel(dialogs, settings: store, photoshop: new FakePhotoshop());
        vm.EditSettings();
        store.Load().EditorPath.Should().Be(@"C:\gimp.exe");
        vm.AppSettings.EditorPath.Should().Be(@"C:\gimp.exe");
    }

    [Fact]
    public void JsonSettingsSurviveBadFiles()
    {
        var path = Path.Combine(dir, "ayar", "settings.json");
        var store = new JsonAppSettingsStore(path);
        store.Load().EditorPath.Should().BeNull();
        store.Save(new AppSettings { EditorPath = "x", ToolPaths = new(StringComparer.Ordinal) { ["darktable"] = "dt" } });
        store.Load().ToolPaths["darktable"].Should().Be("dt");
        File.WriteAllText(path, "{bozuk");
        store.Load().Should().NotBeNull();
    }

    internal sealed class FakePhotoshop : IPhotoshopHost
    {
        public bool Available { get; set; }

        public string Reply { get; set; } = "OK";

        public List<string> Scripts { get; } = [];

        public EngineStatus CheckStatus() => Available ? EngineStatus.Ready : EngineStatus.Unavailable("Photoshop bulunamadı (test).");

        public string RunScript(string script)
        {
            Scripts.Add(script);
            return Reply;
        }
    }

    /// <summary>
    /// Düzenleyicinin kaydını taklit eder. Tuval görüntüyü arka planda okurken dosya kısa süre kilitli olabilir (gerçek bir
    /// düzenleyici de kaydetmeyi yeniler); en fazla 3 sn denenir.
    /// </summary>
    private static void WriteWhenFree(string path, byte[] data)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                File.WriteAllBytes(path, data);
                return;
            }
            catch (IOException) when (attempt < 30)
            {
                Thread.Sleep(100);
            }
        }
    }
}
