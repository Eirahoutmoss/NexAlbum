using System.IO.Compression;
using System.Text.Json.Nodes;
using FluentAssertions;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Packaging;
using Xunit;

namespace NexAlbum.Persistence.Tests;

/// <summary>Karar 09 §16.2 hata senaryoları ve §21 güvenlik.</summary>
public class FailureTests
{
    [Fact]
    public void RandomBytesAreACorruptArchive()
    {
        PackageBuilder.LoadFails([1, 2, 3, 4, 5]).Error.Should().Be(ProjectLoadError.CorruptArchive);
    }

    [Fact]
    public void MissingFileIsReportedAsUnopenable()
    {
        using var dir = new TempDir();
        FluentActions.Invoking(() => ProjectPackageReader.Load(dir.File("yok.albumproj")))
            .Should().Throw<ProjectFormatException>().Which.Error.Should().Be(ProjectLoadError.CorruptArchive);
    }

    [Fact]
    public void ZipWithoutManifestIsNotAProject()
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip => zip.GetEntry(ProjectFormat.ManifestEntry)!.Delete());
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.NotAProject);
    }

    [Fact]
    public void ZipWithoutProjectJsonIsNotAProject()
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip => zip.GetEntry(ProjectFormat.ProjectEntry)!.Delete());
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.NotAProject);
    }

    [Fact]
    public void ForeignManifestIsNotAProject()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ManifestEntry, n => n["format"] = "baska-format");
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.NotAProject);

        var noVersion = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ManifestEntry, n => n.AsObject().Remove("schemaVersion"));
        PackageBuilder.LoadFails(noVersion).Error.Should().Be(ProjectLoadError.NotAProject);
    }

    [Fact]
    public void BrokenManifestJsonIsNotAProject()
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip =>
        {
            zip.GetEntry(ProjectFormat.ManifestEntry)!.Delete();
            PackageBuilder.Write(zip, ProjectFormat.ManifestEntry, "{ bozuk");
        });
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.NotAProject);
    }

    [Theory]
    [InlineData(ProjectFormat.ManifestEntry)]
    [InlineData(ProjectFormat.ProjectEntry)]
    public void NewerSchemaAsksUserToUpdate(string entry)
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), entry, n => n["schemaVersion"] = 99);
        var ex = PackageBuilder.LoadFails(package);
        ex.Error.Should().Be(ProjectLoadError.TooNew);
        ex.Message.Should().Contain("güncelleyin");
    }

    [Fact]
    public void InvalidSchemaVersionTypeIsInvalidData()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["schemaVersion"] = "bir");
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.InvalidData);
    }

    [Fact]
    public void BrokenProjectJsonIsInvalidData()
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip =>
        {
            zip.GetEntry(ProjectFormat.ProjectEntry)!.Delete();
            PackageBuilder.Write(zip, ProjectFormat.ProjectEntry, "[1, 2");
        });
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.InvalidData);
    }

    [Fact]
    public void MissingRequiredFieldNamesTheField()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n.AsObject().Remove("name"));
        var ex = PackageBuilder.LoadFails(package);
        ex.Error.Should().Be(ProjectLoadError.InvalidData);
        ex.Message.Should().Contain("'name'");
    }

    [Fact]
    public void InvalidValueReportsItsPath()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["pages"]![0]!["elements"]![1]!["width"] = -5);
        var ex = PackageBuilder.LoadFails(package);
        ex.Error.Should().Be(ProjectLoadError.InvalidData);
        ex.Message.Should().Contain("pages[0].elements[1]");
    }

    [Theory]
    [InlineData("type", "dalga")]
    [InlineData("cropMode", "esnek")]
    public void UnknownEnumTextIsInvalidData(string field, string value)
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            if (field == "type")
            {
                n["pages"]![0]!["type"] = value;
            }
            else
            {
                n["pages"]![0]!["elements"]![1]![field] = value;
            }
        });
        PackageBuilder.LoadFails(package).Message.Should().Contain(value);
    }

    [Fact]
    public void UnknownElementTypeIsInvalidData()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["pages"]![0]!["elements"]![1]!["type"] = "video");
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.InvalidData);
    }

    [Fact]
    public void PixelUnitsAreRejectedBecauseDpiIsUnknown()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["pages"]![0]!["elements"]![1]!["unit"] = "px");
        PackageBuilder.LoadFails(package).Message.Should().Contain("DPI");
    }

    [Fact]
    public void OtherPhysicalUnitsAreConvertedToMm()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            var slot = n["pages"]![0]!["elements"]![1]!;
            slot["unit"] = "cm";
            slot["x"] = 2;
            slot["width"] = 26;
        });
        var slotBounds = PackageBuilder.Load(package).Project.Pages[0].Slots.Single().Bounds;
        slotBounds.X.Should().BeApproximately(20, 1e-9);
        slotBounds.Width.Should().BeApproximately(260, 1e-9);
    }

    [Fact]
    public void UnknownFieldsAreIgnoredForForwardCompatibility()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            n["gelecekAlani"] = new JsonObject { ["x"] = 1 };
            n["pages"]![0]!["yeniOzellik"] = true;
        });
        var loaded = PackageBuilder.Load(package);
        loaded.Project.Name.Should().Be("Ahmet & Zeynep Düğün Albümü");
        loaded.ChecksumMismatches.Should().Contain(ProjectFormat.ProjectEntry);
    }

    [Fact]
    public void TamperingIsReportedButDoesNotBlockOpening()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n => n["description"] = "değiştirildi");
        var loaded = PackageBuilder.Load(package);
        loaded.ChecksumMismatches.Should().Equal(ProjectFormat.ProjectEntry);
        loaded.Project.Description.Should().Be("değiştirildi");

        PackageBuilder.Load(package, new LoadOptions { VerifyChecksums = false }).ChecksumMismatches.Should().BeEmpty();
    }

    [Fact]
    public void MissingFileListedInChecksumsIsReported()
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip => zip.GetEntry(ProjectFormat.TemplatesEntry)!.Delete());
        PackageBuilder.Load(package).ChecksumMismatches.Should().Contain(ProjectFormat.TemplatesEntry);
    }

    [Fact]
    public void MissingChecksumsFileIsReported()
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip => zip.GetEntry(ProjectFormat.ChecksumsEntry)!.Delete());
        PackageBuilder.Load(package).ChecksumMismatches.Should().Contain(ProjectFormat.ChecksumsEntry);
    }

    [Fact]
    public void UnsupportedChecksumAlgorithmIsRejected()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ChecksumsEntry, n => n["algorithm"] = "md5");
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.InvalidData);
    }

    [Theory]
    [InlineData("../kaçış.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/x.dll")]
    [InlineData("assets/../../x")]
    public void PathTraversalEntriesAreRejected(string entry)
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip => PackageBuilder.Write(zip, entry, "x"));
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.UnsafeArchive);
    }

    [Fact]
    public void ZipBombIsRejected()
    {
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip =>
        {
            using var s = zip.CreateEntry("assets/bomba.bin", CompressionLevel.SmallestSize).Open();
            s.Write(new byte[20 * 1024 * 1024]);
        });
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.UnsafeArchive);
    }

    [Fact]
    public void SizeLimitsAreEnforced()
    {
        var valid = PackageBuilder.Valid();
        using (var ms = new MemoryStream(valid))
        {
            FluentActions.Invoking(() => ProjectPackageReader.Load(ms, new LoadOptions { MaxJsonBytes = 100 }))
                .Should().Throw<ProjectFormatException>().Which.Error.Should().Be(ProjectLoadError.UnsafeArchive);
        }

        using (var ms = new MemoryStream(valid))
        {
            FluentActions.Invoking(() => ProjectPackageReader.Load(ms, new LoadOptions { MaxTotalBytes = 100 }))
                .Should().Throw<ProjectFormatException>().Which.Error.Should().Be(ProjectLoadError.UnsafeArchive);
        }
    }

    [Fact]
    public void ExcessivelyNestedJsonIsRejected()
    {
        var deep = string.Concat(Enumerable.Repeat("[", 200)) + string.Concat(Enumerable.Repeat("]", 200));
        var package = PackageBuilder.Edit(PackageBuilder.Valid(), zip =>
        {
            zip.GetEntry(ProjectFormat.ProjectEntry)!.Delete();
            PackageBuilder.Write(zip, ProjectFormat.ProjectEntry, "{\"schemaVersion\":1,\"x\":" + deep + "}");
        });
        PackageBuilder.LoadFails(package).Error.Should().Be(ProjectLoadError.InvalidData);
    }
}
