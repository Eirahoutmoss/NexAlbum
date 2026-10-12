using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Packaging;
using Xunit;

namespace NexAlbum.Persistence.Tests;

public class RoundTripTests
{
    private static SaveOptions Options(params PackageFile[] files) => new()
    {
        AppVersion = "0.1.0",
        Templates = [Samples.FourGrid()],
        Files = files,
        Clock = new FixedClock(Samples.Now),
    };

    [Fact]
    public void FullProjectSurvivesSaveAndLoadWithoutLoss()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");
        var original = Samples.FullProject();

        ProjectPackageWriter.Save(path, original, Options());
        var loaded = ProjectPackageReader.Load(path);

        loaded.Project.Should().BeEquivalentTo(original, o => o.ComparingRecordsByMembers().RespectingRuntimeTypes());
        loaded.Templates.Should().ContainSingle().Which.Should().BeEquivalentTo(Samples.FourGrid(), o => o.ComparingRecordsByMembers());
        loaded.Profiles.Should().ContainSingle().Which.Should().BeEquivalentTo(original.Profile, o => o.ComparingRecordsByMembers());
        loaded.ChecksumMismatches.Should().BeEmpty();
        loaded.Migrated.Should().BeFalse();
        loaded.MigrationBackup.Should().BeNull();
    }

    [Fact]
    public void SavingTwiceProducesIdenticalJson()
    {
        using var dir = new TempDir();
        var first = dir.File("a.albumproj");
        var second = dir.File("b.albumproj");

        ProjectPackageWriter.Save(first, Samples.FullProject(), Options());
        ProjectPackageWriter.Save(second, ProjectPackageReader.Load(first).Project, Options());

        ReadEntry(first, ProjectFormat.ProjectEntry).Should().Be(ReadEntry(second, ProjectFormat.ProjectEntry));
    }

    [Fact]
    public void ManifestDescribesThePackage()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");
        ProjectPackageWriter.Save(path, Samples.FullProject(), Options() with { Portable = true, AppVersion = "0.2.0" });

        var loaded = ProjectPackageReader.Load(path);
        loaded.Manifest.Should().Be(new PackageManifest("1.0", 1, Samples.Now, "0.2.0", Samples.FullProject().Id, "Ahmet & Zeynep Düğün Albümü", Portable: true));
        loaded.Project.AppVersion.Should().Be("0.2.0");
        loaded.Entries.Should().Contain(new[]
        {
            ProjectFormat.ManifestEntry, ProjectFormat.ProjectEntry, ProjectFormat.ProfilesEntry,
            ProjectFormat.TemplatesEntry, ProjectFormat.ChecksumsEntry, ProjectFormat.ReadmeEntry,
        });
    }

    [Fact]
    public void ProjectJsonIsReadableAndFollowsDecision09()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");
        ProjectPackageWriter.Save(path, Samples.FullProject(), Options());
        var json = ReadEntry(path, ProjectFormat.ProjectEntry);

        json.Should().Contain("\"schemaVersion\": 1");
        json.Should().Contain("\"name\": \"Ahmet & Zeynep Düğün Albümü\"", "Türkçe karakterler ve & kaçışsız yazılmalı");
        json.Should().Contain("\"type\": \"slot\"");
        json.Should().Contain("\"type\": \"text\"");
        json.Should().Contain("\"type\": \"background\"");
        json.Should().Contain("\"unit\": \"mm\"");
        json.Should().Contain("\"minDPI\": 200");
        json.Should().Contain("\"targetDPI\": 300");
        json.Should().Contain("\"mode\": \"CMYK\"");
        json.Should().Contain("\"type\": \"frontCover\"");
        json.Should().Contain("\"orientation\": \"square\"");
        json.Should().Contain("\"cropMode\": \"contain\"");
        json.Should().Contain("\"index\": 1");
        json.Should().Contain("\"usedInPages\": [\n        \"page-001\"\n      ]".Replace("\n", Environment.NewLine, StringComparison.Ordinal));
        json.Should().NotContain("\"description\": null", "null alanlar yazılmaz");
    }

    [Fact]
    public void BinaryFilesAreStoredAndExtractable()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");
        var thumb = Enumerable.Range(0, 5000).Select(i => (byte)(i % 251)).ToArray();
        ProjectPackageWriter.Save(path, Samples.FullProject(), Options(PackageFile.FromBytes(@"assets\thumbnails\asset-001.jpg", thumb)));

        var loaded = ProjectPackageReader.Load(path);
        loaded.Entries.Should().Contain("assets/thumbnails/asset-001.jpg");
        loaded.ChecksumMismatches.Should().BeEmpty();

        using var extracted = new MemoryStream();
        ProjectPackageReader.ExtractEntry(path, "assets/thumbnails/asset-001.jpg", extracted);
        extracted.ToArray().Should().Equal(thumb);

        FluentActions.Invoking(() => ProjectPackageReader.ExtractEntry(path, "assets/yok.jpg", Stream.Null)).Should().Throw<FileNotFoundException>();
        FluentActions.Invoking(() => ProjectPackageReader.ExtractEntry(path, "../x", Stream.Null)).Should().Throw<ProjectFormatException>();
    }

    [Fact]
    public void FilesFromDiskAreStreamed()
    {
        using var dir = new TempDir();
        var source = dir.File("orijinal.jpg");
        File.WriteAllBytes(source, [1, 2, 3, 4]);
        var path = dir.File("album.albumproj");
        ProjectPackageWriter.Save(path, Samples.FullProject(), Options(PackageFile.FromDisk("assets/originals/asset-002.jpg", source)));

        using var extracted = new MemoryStream();
        ProjectPackageReader.ExtractEntry(path, "assets/originals/asset-002.jpg", extracted);
        extracted.ToArray().Should().Equal(1, 2, 3, 4);
    }

    [Theory]
    [InlineData("project.json")]
    [InlineData("assets/a.jpg")]
    public void ReservedOrDuplicateFilePathsAreRejected(string second)
    {
        using var dir = new TempDir();
        var files = second == "project.json"
            ? new[] { PackageFile.FromBytes(second, [1]) }
            : [PackageFile.FromBytes("assets/a.jpg", [1]), PackageFile.FromBytes(second, [2])];
        FluentActions.Invoking(() => ProjectPackageWriter.Save(dir.File("x.albumproj"), Samples.FullProject(), Options(files)))
            .Should().Throw<ArgumentException>();
        File.Exists(dir.File("x.albumproj")).Should().BeFalse();
        File.Exists(dir.File("x.albumproj.tmp")).Should().BeFalse();
    }

    [Fact]
    public void ProjectCreatedFromBuiltInProfileRoundTrips()
    {
        using var dir = new TempDir();
        var path = dir.File("ornek.albumproj");
        var project = AlbumProject.Create("Örnek", BuiltInProfiles.Find("builtin-20x30")!, Samples.Now, "0.1.0", Guid.NewGuid());
        ProjectPackageWriter.Save(path, project, Options());

        var loaded = ProjectPackageReader.Load(path).Project;
        loaded.Profile.Bleed.Value.Should().BeNull("bilinmeyen değer kaydedip açınca da bilinmeyen kalmalı");
        loaded.Profile.Resolution.TargetDpi.Should().BeNull();
        loaded.Should().BeEquivalentTo(project, o => o.ComparingRecordsByMembers().RespectingRuntimeTypes());
    }

    internal static string ReadEntry(string package, string entry)
    {
        using var zip = ZipFile.OpenRead(package);
        using var reader = new StreamReader(zip.GetEntry(entry)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
