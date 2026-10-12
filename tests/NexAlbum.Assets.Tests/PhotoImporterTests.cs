using FluentAssertions;
using NexAlbum.Assets.Import;
using NexAlbum.Assets.Metadata;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Assets.Tests;

public class PhotoImporterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    private static AlbumProject Project() =>
        AlbumProject.Create("Test", new AlbumProfile { Id = "p", Name = "P", Page = new PageSpec(new SizeMm(300, 300)) }, Now, "0.1.0", Guid.NewGuid());

    private static PhotoImporter Importer(IPhotoMetadataReader? reader = null) =>
        new(reader ?? new MetadataExtractorReader(), new FixedClock(Now));

    [Fact]
    public void ImportsSupportedFilesInNameOrderWithSequentialIds()
    {
        using var dir = new TempDir();
        dir.Write("b.jpg", Fixtures.Jpeg);
        dir.Write("a.PNG", Fixtures.Png);
        dir.Write("notlar.txt", [1, 2, 3]);
        dir.Write("alt/c.jpg", Fixtures.Jpeg);

        var result = Importer().ImportFolder(dir.Path, Project());

        result.Imported.Select(a => (a.Id, a.FileName)).Should().Equal(("asset-0001", "a.PNG"), ("asset-0002", "b.jpg"));
        result.Skipped.Should().BeEmpty();

        var jpeg = result.Imported[1];
        jpeg.PixelWidth.Should().Be(6000);
        jpeg.ExifOrientation.Should().Be(6);
        jpeg.DisplayWidth.Should().Be(4000, "EXIF 6 → 90° döndürülmüş dikey fotoğraf");
        jpeg.CameraModel.Should().Be("TestCam");
        jpeg.FileSize.Should().Be(Fixtures.Jpeg.Length);
        jpeg.ImportedAt.Should().Be(Now);
        jpeg.SourceType.Should().Be(AssetSourceType.Reference);
        jpeg.FileHash.Should().BeNull("varsayılan olarak özet hesaplanmaz");
        Path.IsPathRooted(jpeg.SourcePath).Should().BeTrue();
    }

    [Fact]
    public void RecursiveImportIncludesSubfolders()
    {
        using var dir = new TempDir();
        dir.Write("a.jpg", Fixtures.Jpeg);
        dir.Write("alt/b.jpg", Fixtures.Jpeg);
        Importer().ImportFolder(dir.Path, Project(), new ImportOptions { Recursive = true }).Imported.Should().HaveCount(2);
    }

    [Fact]
    public void HashCanBeComputed()
    {
        using var dir = new TempDir();
        dir.Write("a.jpg", Fixtures.Jpeg);
        var asset = Importer().ImportFolder(dir.Path, Project(), new ImportOptions { ComputeHash = true }).Imported.Single();
        asset.FileHash.Should().StartWith("sha256:").And.HaveLength(7 + 64);
    }

    [Fact]
    public void FilesAlreadyInProjectAreNotDuplicatedAndIdsContinue()
    {
        using var dir = new TempDir();
        dir.Write("a.jpg", Fixtures.Jpeg);
        var project = Project();
        project = Importer().ImportFolder(dir.Path, project).ApplyTo(project);

        dir.Write("b.jpg", Fixtures.Jpeg);
        var again = Importer().ImportFolder(dir.Path, project);

        again.AlreadyInProject.Should().ContainSingle().Which.Should().EndWith("a.jpg");
        again.Imported.Should().ContainSingle().Which.Id.Should().Be("asset-0002");
    }

    [Fact]
    public void UnreadableFilesAreSkippedWithAReason()
    {
        using var dir = new TempDir();
        dir.Write("bozuk.jpg", "jpeg değil"u8.ToArray());
        dir.Write("iyi.jpg", Fixtures.Jpeg);

        var result = Importer().ImportFolder(dir.Path, Project());
        result.Imported.Should().ContainSingle();
        result.Skipped.Should().ContainSingle().Which.Path.Should().EndWith("bozuk.jpg");
    }

    [Fact]
    public void ZeroSizedMetadataIsRejected()
    {
        using var dir = new TempDir();
        var file = dir.Write("a.jpg", [1]);
        var result = Importer(new FakeReader(new PhotoMetadata { Width = 0, Height = 10 })).ImportFiles([file], Project());
        result.Imported.Should().BeEmpty();
        result.Skipped.Should().ContainSingle();
    }

    [Fact]
    public void IoErrorsAreReportedNotThrown()
    {
        using var dir = new TempDir();
        var result = Importer().ImportFiles([Path.Combine(dir.Path, "yok.jpg")], Project());
        result.Skipped.Should().ContainSingle();
    }

    [Fact]
    public void ProgressAndCancellation()
    {
        using var dir = new TempDir();
        var files = Enumerable.Range(1, 3).Select(i => dir.Write($"{i}.jpg", Fixtures.Jpeg)).ToList();
        var reported = new List<int>();
        Importer().ImportFiles(files, Project(), progress: new SyncProgress(reported.Add));
        reported.Should().Equal(1, 2, 3);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        FluentActions.Invoking(() => Importer().ImportFiles(files, Project(), cancellationToken: cts.Token))
            .Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void DefaultImporterUsesSystemClock()
    {
        using var dir = new TempDir();
        dir.Write("a.jpg", Fixtures.Jpeg);
        new PhotoImporter().ImportFolder(dir.Path, Project()).Imported.Single().ImportedAt.Should().BeAfter(Now.AddYears(-5));
    }

    private sealed class FakeReader(PhotoMetadata? result) : IPhotoMetadataReader
    {
        public PhotoMetadata? Read(Stream stream) => result;
    }

    private sealed class SyncProgress(Action<int> report) : IProgress<int>
    {
        public void Report(int value) => report(value);
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
