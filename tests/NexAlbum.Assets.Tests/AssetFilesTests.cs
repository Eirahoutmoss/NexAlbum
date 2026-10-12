using FluentAssertions;
using NexAlbum.Assets.Import;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Assets.Tests;

public class AssetFilesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    private static PhotoAsset Asset(string id, string path, AssetSourceType type = AssetSourceType.Reference, bool missing = false) => new()
    {
        Id = id,
        SourcePath = path,
        SourceType = type,
        FileName = Path.GetFileName(path),
        PixelWidth = 10,
        PixelHeight = 10,
        ImportedAt = Now,
        Missing = missing,
    };

    private static AlbumProject Project(params PhotoAsset[] assets) =>
        AlbumProject.Create("Test", new AlbumProfile { Id = "p", Name = "P", Page = new PageSpec(new SizeMm(300, 300)) }, Now, "0.1.0", Guid.NewGuid())
            .AddAssets(assets);

    [Fact]
    public void MissingFilesAreFlaggedAndFoundOnesCleared()
    {
        var project = Project(
            Asset("a", "/yok/a.jpg"),
            Asset("b", "/var/b.jpg", missing: true),
            Asset("c", "assets/originals/c.jpg", AssetSourceType.Embedded));

        var refreshed = AssetFiles.RefreshMissing(project, p => p == "/var/b.jpg");
        refreshed.FindAsset("a")!.Missing.Should().BeTrue();
        refreshed.FindAsset("b")!.Missing.Should().BeFalse();
        refreshed.FindAsset("c")!.Missing.Should().BeFalse("gömülü varlık paketin içindedir");
        AssetFiles.Missing(refreshed).Select(a => a.Id).Should().Equal("a");
    }

    [Fact]
    public void RefreshUsesTheFileSystemByDefault()
    {
        using var dir = new TempDir();
        var real = dir.Write("a.jpg", [1]);
        var project = Project(Asset("a", real, missing: true), Asset("b", Path.Combine(dir.Path, "yok.jpg")));
        var refreshed = AssetFiles.RefreshMissing(project);
        refreshed.FindAsset("a")!.Missing.Should().BeFalse();
        refreshed.FindAsset("b")!.Missing.Should().BeTrue();
    }

    [Fact]
    public void RelinkPointsAssetToNewLocation()
    {
        using var dir = new TempDir();
        var moved = dir.Write("tasindi/IMG_1.jpg", [1]);
        var project = Project(Asset("a", "/eski/IMG_1.jpg", missing: true));

        var relinked = AssetFiles.Relink(project, "a", moved);
        relinked.FindAsset("a")!.SourcePath.Should().Be(moved);
        relinked.FindAsset("a")!.Missing.Should().BeFalse();
        FluentActions.Invoking(() => AssetFiles.Relink(project, "yok", moved)).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void RelinkFromFolderMatchesByUniqueFileName()
    {
        using var dir = new TempDir();
        dir.Write("yeni/IMG_1.jpg", [1]);
        dir.Write("yeni/alt/IMG_2.jpg", [1]);
        dir.Write("yeni/x/IMG_3.jpg", [1]);
        dir.Write("yeni/y/IMG_3.jpg", [1]);
        var project = Project(
            Asset("a", "/eski/IMG_1.jpg", missing: true),
            Asset("b", "/eski/IMG_2.jpg", missing: true),
            Asset("c", "/eski/IMG_3.jpg", missing: true));

        var result = AssetFiles.RelinkFromFolder(project, Path.Combine(dir.Path, "yeni"));
        result.FindAsset("a")!.Missing.Should().BeFalse();
        result.FindAsset("b")!.Missing.Should().BeFalse();
        result.FindAsset("c")!.Missing.Should().BeTrue("aynı adda iki dosya varken tahmin yapılmaz");

        AssetFiles.RelinkFromFolder(project, Path.Combine(dir.Path, "yeni"), recursive: false).FindAsset("b")!.Missing.Should().BeTrue();
    }
}
