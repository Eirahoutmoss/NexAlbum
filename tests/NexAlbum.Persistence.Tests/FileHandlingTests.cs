using FluentAssertions;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Packaging;
using Xunit;

namespace NexAlbum.Persistence.Tests;

public class FileHandlingTests
{
    private static SaveOptions Options => new() { AppVersion = "0.1.0", Clock = new FixedClock(Samples.Now) };

    [Fact]
    public void OverwritingLeavesNoTemporaryFiles()
    {
        using var dir = new TempDir();
        var path = dir.File("alt/klasor/album.albumproj");
        ProjectPackageWriter.Save(path, Samples.FullProject(), Options);
        ProjectPackageWriter.Save(path, Samples.FullProject() with { Name = "Yeni ad" }, Options);

        ProjectPackageReader.Load(path).Project.Name.Should().Be("Yeni ad");
        Directory.GetFiles(Path.GetDirectoryName(path)!).Should().ContainSingle();
    }

    [Fact]
    public void FailedSaveKeepsThePreviousFileIntact()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");
        ProjectPackageWriter.Save(path, Samples.FullProject(), Options);
        var before = File.ReadAllBytes(path);

        var failing = new PackageFile("assets/thumbnails/x.jpg", () => throw new IOException("disk hatası"));
        FluentActions.Invoking(() => ProjectPackageWriter.Save(path, Samples.FullProject() with { Name = "Yarım" }, Options with { Files = [failing] }))
            .Should().Throw<IOException>();

        File.ReadAllBytes(path).Should().Equal(before, "yarım dosya asla kaydedilmiş görünmemeli");
        File.Exists(ProjectFiles.TempPath(path)).Should().BeFalse();
        File.Exists(ProjectFiles.BackupPath(path)).Should().BeFalse();
    }

    [Fact]
    public void HelperPathsFollowDecision09()
    {
        ProjectFiles.TempPath("a.albumproj").Should().Be("a.albumproj.tmp");
        ProjectFiles.BackupPath("a.albumproj").Should().Be("a.albumproj.bak");
        ProjectFiles.AutosavePath("a.albumproj").Should().Be("a.albumproj.autosave");
        ProjectFiles.LockPath("a.albumproj").Should().Be("a.albumproj.lock");
        ProjectFiles.MigrationBackupPath("a.albumproj", 1).Should().Be("a.albumproj.bak-v1");
    }

    [Fact]
    public void NewerAutosaveIsDetected()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");
        ProjectFiles.HasNewerAutosave(path).Should().BeFalse();

        ProjectPackageWriter.Save(path, Samples.FullProject(), Options);
        ProjectFiles.HasNewerAutosave(path).Should().BeFalse();

        var autosave = ProjectFiles.AutosavePath(path);
        ProjectPackageWriter.Save(autosave, Samples.FullProject(), Options);
        File.SetLastWriteTimeUtc(autosave, File.GetLastWriteTimeUtc(path).AddMinutes(1));
        ProjectFiles.HasNewerAutosave(path).Should().BeTrue();

        File.Delete(path);
        ProjectFiles.HasNewerAutosave(path).Should().BeTrue();
    }

    [Fact]
    public void LockPreventsSecondInstance()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");

        ProjectLock.TryAcquire(path, new FixedClock(Samples.Now), out var first, out var holder).Should().BeTrue();
        holder.Should().BeNull();
        using (first)
        {
            first!.Info.ProcessId.Should().Be(Environment.ProcessId);
            first.Path.Should().Be(ProjectFiles.LockPath(path));

            ProjectLock.TryAcquire(path, TimeProvider.System, out var second, out var other).Should().BeFalse();
            second.Should().BeNull();
            if (other is not null)
            {
                other.ProcessId.Should().Be(Environment.ProcessId);
                other.AcquiredAt.Should().Be(Samples.Now);
            }
        }

        File.Exists(ProjectFiles.LockPath(path)).Should().BeFalse("kilit kapanınca silinmeli");
        ProjectLock.TryAcquire(path, TimeProvider.System, out var again, out _).Should().BeTrue();
        again!.Dispose();
        again.Dispose();
    }

    [Fact]
    public void StaleLockCanBeForceReleased()
    {
        using var dir = new TempDir();
        var path = dir.File("album.albumproj");
        File.WriteAllText(ProjectFiles.LockPath(path), "çöken örnekten kalan");

        ProjectLock.TryAcquire(path, TimeProvider.System, out _, out var holder).Should().BeFalse();
        holder.Should().BeNull("okunamayan kilit bilgisi null döner");

        ProjectLock.ForceRelease(path);
        ProjectLock.ForceRelease(path);
        ProjectLock.TryAcquire(path, TimeProvider.System, out var acquired, out _).Should().BeTrue();
        acquired!.Dispose();
    }

    [Theory]
    [InlineData("assets/thumbnails/a.jpg", "assets/thumbnails/a.jpg")]
    [InlineData(@"assets\thumbnails\a.jpg", "assets/thumbnails/a.jpg")]
    public void SafePathsAreNormalized(string input, string expected)
    {
        PackagePaths.Normalize(input).Should().Be(expected);
        PackagePaths.IsSafe(input).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("../a")]
    [InlineData("a/../b")]
    [InlineData("./a")]
    [InlineData("a//b")]
    [InlineData("/a")]
    [InlineData(@"\\server\share\a")]
    [InlineData("C:\\a")]
    public void UnsafePathsAreRejected(string input)
    {
        PackagePaths.IsSafe(input).Should().BeFalse();
        FluentActions.Invoking(() => PackagePaths.Normalize(input))
            .Should().Throw<ProjectFormatException>().Which.Error.Should().Be(ProjectLoadError.UnsafeArchive);
    }

    [Fact]
    public void NulCharacterIsRejected()
    {
        PackagePaths.IsSafe("a" + '\0' + "b").Should().BeFalse();
    }

    [Fact]
    public void ExceptionConstructorsKeepContext()
    {
        new ProjectFormatException().Error.Should().Be(ProjectLoadError.InvalidData);
        new ProjectFormatException("x").Message.Should().Be("x");
        var inner = new InvalidOperationException();
        new ProjectFormatException("x", inner).InnerException.Should().BeSameAs(inner);
    }
}
