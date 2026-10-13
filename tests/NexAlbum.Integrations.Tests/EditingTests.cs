using FluentAssertions;
using NexAlbum.Assets.Metadata;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Integrations.CommandLine;
using NexAlbum.Integrations.Editing;
using NexAlbum.Integrations.Photoshop;
using Xunit;

namespace NexAlbum.Integrations.Tests;

public sealed class EditingTests : IDisposable
{
    /// <summary>20×30 piksel geçerli PNG.</summary>
    internal static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAABQAAAAeCAIAAACjcKk8AAAAIklEQVR4nGPkWhDFQC5gIlvnqOZRzaOaRzWPah7VPAQ0AwAeUgFAi1B09gAAAABJRU5ErkJggg==");

    private static readonly DateTimeOffset Now = new(2026, 10, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly string dir = Path.Combine(Path.GetTempPath(), "nexalbum-integrations", Guid.NewGuid().ToString("N"));

    public EditingTests()
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

    private string Photo(string name)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllBytes(path, Png);
        return path;
    }

    private static PhotoAsset Asset(string id, string path) => new()
    {
        Id = id,
        SourcePath = path,
        FileName = Path.GetFileName(path),
        PixelWidth = 20,
        PixelHeight = 30,
    };

    private static EditRecipe CopyRecipe(string name = "Kopya") => OperatingSystem.IsWindows()
        ? new EditRecipe { Engine = EditEngineKind.CommandLine, Name = name, Executable = Path.Combine(Environment.SystemDirectory, "cmd.exe"), Arguments = ["/c", "copy", "/y", "{input}", "{output}"], Output = EditOutputFormat.Png }
        : new EditRecipe { Engine = EditEngineKind.CommandLine, Name = name, Executable = "/bin/cp", Arguments = ["{input}", "{output}"], Output = EditOutputFormat.Png };

    [Fact]
    public async Task CommandLineBatchProducesReadableEditsWithoutTouchingOriginals()
    {
        var a = Photo("a.png");
        var b = Photo("b.png");
        var before = File.GetLastWriteTimeUtc(a);
        var output = Path.Combine(dir, "duzenli");
        var progress = new List<EditProgress>();
        var runner = new EditBatchRunner(new CommandLineEngine(), new MetadataExtractorReader(), new FixedClock(Now));

        var result = await runner.RunAsync([Asset("asset-0001", a), Asset("asset-0002", b), Asset("asset-0003", Path.Combine(dir, "yok.png"))], CopyRecipe("Sıcak Ton"), output, new SyncProgress<EditProgress>(progress.Add));

        result.Cancelled.Should().BeFalse();
        result.Edits.Should().HaveCount(2);
        var edit = result.Edits["asset-0001"];
        edit.Path.Should().Be(Path.GetFullPath(Path.Combine(output, "asset-0001-sicak-ton.png")));
        edit.PixelWidth.Should().Be(20);
        edit.PixelHeight.Should().Be(30);
        edit.Recipe.Should().Be("Sıcak Ton");
        edit.CreatedAt.Should().Be(Now);
        File.Exists(edit.Path).Should().BeTrue();
        Directory.GetFiles(output).Should().NotContain(f => f.Contains(".yarim", StringComparison.Ordinal), "geçici dosya kalmaz");
        File.GetLastWriteTimeUtc(a).Should().Be(before, "orijinale yazılmaz");

        result.Failures.Should().ContainSingle().Which.Should().Be(new EditFailure("asset-0003", "yok.png", "orijinal dosya bulunamadı"));
        progress.Should().HaveCount(4).And.EndWith(new EditProgress(3, 3, null));

        var project = AlbumProject.Create("Test", BuiltInProfiles.All[0], Now, "0.1.0", Guid.NewGuid())
            .AddAssets([Asset("asset-0001", a), Asset("asset-0002", b)]);
        var applied = result.ApplyTo(project);
        applied.FindAsset("asset-0001")!.IsEditActive.Should().BeTrue();
        applied.Settings.Editing.LastRecipe!.Name.Should().Be("Sıcak Ton");
    }

    [Fact]
    public async Task ToolErrorsAreReportedPerFile()
    {
        var a = Photo("a.png");
        var failing = OperatingSystem.IsWindows()
            ? CopyRecipe() with { Arguments = ["/c", "echo", "{input}", "{output}", ">nul", "&", "echo", "bozuk", "ayar", "1>&2", "&", "exit", "3"] }
            : CopyRecipe() with { Executable = "/bin/sh", Arguments = ["-c", "echo bozuk ayar >&2; exit 3", "{input}", "{output}"] };
        var runner = new EditBatchRunner(new CommandLineEngine(), new MetadataExtractorReader());

        var result = await runner.RunAsync([Asset("x", a), Asset("y", a)], failing, Path.Combine(dir, "out"));

        result.Edits.Should().BeEmpty();
        result.Failures.Should().HaveCount(2, "bir dosyanın hatası işi durdurmaz");
        result.Failures[0].Reason.Should().Contain("3 koduyla").And.Contain("bozuk ayar");

        var missing = CopyRecipe() with { Executable = Path.Combine(dir, "olmayan-arac.exe") };
        var none = await runner.RunAsync([Asset("x", a)], missing, Path.Combine(dir, "out"));
        none.Failures.Single().Reason.Should().Contain("başlatılamadı");
        result.ApplyTo(AlbumProject.Create("T", BuiltInProfiles.All[0], Now, "0.1.0", Guid.NewGuid())).Settings.Editing.LastRecipe.Should().BeNull("başarı yoksa tarif yazılmaz");
    }

    [Fact]
    public async Task OutputsThatCannotBeReadAreRejected()
    {
        var a = Photo("a.png");
        var engine = new FakeEngine(request => File.WriteAllText(request.OutputPath, "görüntü değil"));
        var result = await new EditBatchRunner(engine, new MetadataExtractorReader()).RunAsync([Asset("x", a)], FakeEngine.Recipe, Path.Combine(dir, "o"));
        result.Failures.Single().Reason.Should().Be("çıktı dosyası okunamadı");
        Directory.GetFiles(Path.Combine(dir, "o")).Should().BeEmpty();

        var silent = new FakeEngine(_ => { });
        (await new EditBatchRunner(silent, new MetadataExtractorReader()).RunAsync([Asset("x", a)], FakeEngine.Recipe, Path.Combine(dir, "o")))
            .Failures.Single().Reason.Should().Be("araç çıktı dosyası üretmedi");
    }

    [Fact]
    public async Task CancellationAndTimeoutStopTheBatch()
    {
        var a = Photo("a.png");
        using var cts = new CancellationTokenSource();
        var calls = 0;
        var engine = new FakeEngine(request =>
        {
            calls++;
            File.WriteAllBytes(request.OutputPath, Png);
            cts.Cancel();
        });
        var result = await new EditBatchRunner(engine, new MetadataExtractorReader()).RunAsync([Asset("x", a), Asset("y", a)], FakeEngine.Recipe, Path.Combine(dir, "c"), cancellationToken: cts.Token);
        result.Cancelled.Should().BeTrue();
        calls.Should().Be(1);
        result.Edits.Should().ContainKey("x", "biten dosya korunur");

        var slow = new FakeEngine(_ => { }, delay: TimeSpan.FromSeconds(30));
        var timed = await new EditBatchRunner(slow, new MetadataExtractorReader()) { PerFileTimeout = TimeSpan.FromMilliseconds(100) }
            .RunAsync([Asset("x", a), Asset("y", a)], FakeEngine.Recipe, Path.Combine(dir, "t"));
        timed.Cancelled.Should().BeTrue();
        timed.Failures.Should().ContainSingle().Which.Reason.Should().Contain("zaman aşımı");
    }

    [Fact]
    public async Task RecipeMustMatchTheEngine()
    {
        var runner = new EditBatchRunner(new CommandLineEngine(), new MetadataExtractorReader());
        await FluentActions.Awaiting(() => runner.RunAsync([], FakeEngine.Recipe with { Engine = EditEngineKind.Photoshop, ActionSet = "S", Action = "A" }, dir))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => runner.RunAsync([], CopyRecipe() with { Arguments = [] }, dir))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*{input}*");
    }

    [Fact]
    public void ArgumentsAreSubstitutedWithoutAShell()
    {
        var recipe = CopyRecipe() with { Arguments = ["--in={input}", "x y", "{output}"] };
        CommandLineEngine.BuildArguments(recipe, @"C:\a b\ç.jpg", "o.jpg").Should().Equal(@"--in=C:\a b\ç.jpg", "x y", "o.jpg");
        new CommandLineEngine(Path.Combine(dir, "yok.exe")).CheckStatus().Available.Should().BeFalse();
        new CommandLineEngine().CheckStatus().Available.Should().BeTrue();
        CommandLinePresets.All.Should().OnlyContain(p => p.Arguments.Contains("{input}") || p.Arguments.Any(a => a.Contains("{input}", StringComparison.Ordinal)));
        CommandLinePresets.Darktable.Locate(_ => false).Should().BeNull();
        CommandLinePresets.Darktable.ToRecipe("dt", "x").Problem.Should().BeNull();
    }

    [Fact]
    public void ProjectFolderNameIsSafeAndStable()
    {
        var project = AlbumProject.Create("Ahmet: Zeynep / Düğün?", BuiltInProfiles.All[0], Now, "0.1.0", Guid.Parse("12345678-9abc-def0-1234-56789abcdef0"));
        var folder = EditFolders.ForProject(dir, project);
        Path.GetFileName(folder).Should().StartWith("Ahmet_ Zeynep _ Düğün").And.EndWith("-12345678");
        Path.GetDirectoryName(folder).Should().Be(dir);
        EditFolders.DefaultRoot.Should().EndWith("Düzenlenmiş");
    }

    [Fact]
    public void WorkingFileIsACopyOrTheExistingEdit()
    {
        var original = Photo("IMG_1.PNG");
        var folder = Path.Combine(dir, "w");
        var working = ExternalEditor.PrepareWorkingFile(Asset("asset-0001", original), folder);
        working.Should().Be(Path.GetFullPath(Path.Combine(folder, "asset-0001-elle.png")));
        File.ReadAllBytes(working).Should().Equal(Png);

        var edited = Asset("asset-0001", original) with { Edit = new PhotoEdit { Path = working, PixelWidth = 20, PixelHeight = 30, Tool = "x" }, UseEdit = false };
        ExternalEditor.PrepareWorkingFile(edited, folder).Should().Be(working, "süren düzenleme açılır, üzerine kopyalanmaz");

        FluentActions.Invoking(() => ExternalEditor.PrepareWorkingFile(Asset("z", Path.Combine(dir, "yok.png")), folder)).Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public async Task WatcherReportsASaveOnce()
    {
        var file = Photo("izlenen.png");
        using var watcher = new EditFileWatcher(file, TimeSpan.FromMilliseconds(150));
        var saved = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        watcher.Saved += (_, path) =>
        {
            Interlocked.Increment(ref count);
            saved.TrySetResult(path);
        };

        await Task.Delay(100);
        File.WriteAllBytes(file, [.. Png, 0]);
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddSeconds(5));

        (await saved.Task.WaitAsync(TimeSpan.FromSeconds(10))).Should().Be(Path.GetFullPath(file));
        await Task.Delay(500);
        count.Should().Be(1, "olaylar birleştirilir");
    }

    [Fact]
    public async Task PhotoshopEngineTalksThroughTheHost()
    {
        var host = new FakeHost("OK");
        var engine = new PhotoshopEngine(host);
        var recipe = new EditRecipe { Engine = EditEngineKind.Photoshop, Name = "Sıcak", ActionSet = "Stüdyo", Action = "Sıcak \"Ton\"" };

        await engine.ProcessAsync(new EditRequest(@"C:\f\a.jpg", @"C:\o\a.jpg", recipe), CancellationToken.None);
        host.Scripts.Single().Should().Contain("app.doAction(\"Sıcak \\\"Ton\\\"\", \"Stüdyo\")").And.Contain("new File(\"C:\\\\f\\\\a.jpg\")");

        host.Reply = "ERR:Action bulunamadı (satır 7)";
        await FluentActions.Awaiting(() => engine.ProcessAsync(new EditRequest("a", "b", recipe), CancellationToken.None))
            .Should().ThrowAsync<EditEngineException>().WithMessage("Photoshop: Action bulunamadı*");

        host.Reply = "tuhaf";
        await FluentActions.Awaiting(() => engine.ProcessAsync(new EditRequest("a", "b", recipe), CancellationToken.None))
            .Should().ThrowAsync<EditEngineException>().WithMessage("*beklenmeyen*");

        await FluentActions.Awaiting(() => engine.ProcessAsync(new EditRequest("a", "b", recipe with { Action = null }), CancellationToken.None))
            .Should().ThrowAsync<EditEngineException>();

        host.Reply = "Stüdyo\tSıcak\r\nStüdyo\tSoğuk\n\nbozuk satır\nVarsayılan\tVinyet";
        (await engine.ListActionsAsync()).Should().Equal(new PhotoshopAction("Stüdyo", "Sıcak"), new PhotoshopAction("Stüdyo", "Soğuk"), new PhotoshopAction("Varsayılan", "Vinyet"));
        engine.CheckStatus().Available.Should().BeTrue();
        engine.Kind.Should().Be(EditEngineKind.Photoshop);
    }

    [Fact]
    public void PhotoshopScriptsEscapeEverythingAndPickTheFormat()
    {
        PhotoshopScripts.Js("a\"b\\c\nd\u2028\u0001").Should().Be("\"a\\\"b\\\\c\\nd\\u2028\\u0001\"");
        var recipe = new EditRecipe { Engine = EditEngineKind.Photoshop, Name = "x", ActionSet = "S", Action = "A", JpegQuality = 10 };
        var jpeg = PhotoshopScripts.Process("in", "out", recipe);
        jpeg.Should().Contain("jpg.quality = 10;").And.Contain("embedColorProfile = true").And.Contain("DialogModes.NO").And.Contain("DONOTSAVECHANGES");
        PhotoshopScripts.Process("in", "out", recipe with { Output = EditOutputFormat.Png }).Should().Contain("PNGSaveOptions").And.NotContain("JPEGSaveOptions");
        PhotoshopScripts.ListActions().Should().Contain("executeActionGet");
        PhotoshopScripts.ParseActions(null).Should().BeEmpty();
        new PhotoshopAction("S", "A").DisplayName.Should().Be("S / A");
    }

    [Fact]
    public void ComHostIsUnavailableOutsideWindows()
    {
        using var host = new ComPhotoshopHost();
        if (!OperatingSystem.IsWindows())
        {
            host.CheckStatus().Available.Should().BeFalse();
            FluentActions.Invoking(() => host.RunScript("1")).Should().Throw<EditEngineException>();
            PhotoshopLocator.FindExecutable().Should().BeNull();
        }
        else
        {
            host.CheckStatus().Should().NotBeNull("Photoshop kurulu olmayabilir; yalnızca çökmemeli");
        }
    }

    private sealed class FakeEngine(Action<EditRequest> work, TimeSpan? delay = null) : IEditEngine
    {
        public static EditRecipe Recipe { get; } = new() { Engine = EditEngineKind.CommandLine, Name = "Sahte", Executable = "x", Arguments = ["{input}", "{output}"], Output = EditOutputFormat.Png };

        public EditEngineKind Kind => EditEngineKind.CommandLine;

        public string Name => "Sahte";

        public EngineStatus CheckStatus() => EngineStatus.Ready;

        public async Task ProcessAsync(EditRequest request, CancellationToken cancellationToken)
        {
            if (delay is { } d)
            {
                await Task.Delay(d, cancellationToken);
            }

            work(request);
        }
    }

    private sealed class FakeHost(string reply) : IPhotoshopHost
    {
        public string Reply { get; set; } = reply;

        public List<string> Scripts { get; } = [];

        public EngineStatus CheckStatus() => EngineStatus.Ready;

        public string RunScript(string script)
        {
            Scripts.Add(script);
            return Reply;
        }
    }

    private sealed class SyncProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
