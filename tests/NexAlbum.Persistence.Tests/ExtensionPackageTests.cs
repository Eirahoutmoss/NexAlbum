using System.IO.Compression;
using FluentAssertions;
using NexAlbum.Core.Extensions;
using NexAlbum.Core.Video;
using NexAlbum.Persistence.Extensions;
using Xunit;

namespace NexAlbum.Persistence.Tests;

public class ExtensionPackageTests
{
    /// <summary>Depodaki örnek paket (samples/eklentiler/ornek-sinema) — biçim belgesiyle aynı dosya.</summary>
    private static string SampleManifest()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "NexAlbum.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return File.ReadAllText(Path.Combine(dir!, "samples", "eklentiler", "ornek-sinema", "manifest.json"));
    }

    [Fact]
    public void SamplePackageIsRead()
    {
        var problems = new List<ExtensionProblem>();
        var package = ExtensionPackageReader.Read(SampleManifest(), "ornek", problems);

        problems.Should().BeEmpty();
        package.Id.Should().Be("ornek-sinema");
        package.Name.Should().Be("Örnek Sinema Paketi");
        package.VideoStyles.Select(s => s.Id).Should().Equal("ornek-sinema/noir", "ornek-sinema/yaz-esintisi", "ornek-sinema/ritim");
        var noir = package.VideoStyles[0];
        noir.Look.Should().Be(VideoLook.BlackWhite);
        noir.Vignette.Should().BeTrue();
        noir.Grain.Should().BeTrue();
        noir.Transitions.Should().Equal(TransitionKind.Crossfade, TransitionKind.DipToBlack, TransitionKind.FocusShift);
        noir.PhotoSeconds.Should().Be(4.2);
    }

    [Fact]
    public void InvalidStylesAreSkippedWithAReason()
    {
        var problems = new List<ExtensionProblem>();
        var package = ExtensionPackageReader.Read("""
            {
              "format": "nexalbum-extension", "formatVersion": 1, "id": "deneme", "name": "Deneme",
              "videoStyles": [
                { "id": "iyi", "name": "İyi" },
                { "id": "kotu", "name": "Kötü", "transitions": ["uçuş"] },
                { "id": "uzun", "name": "Uzun", "photoSeconds": 99 },
                { "id": "Büyük Harf", "name": "Ad" }
              ],
              "textStyles": []
            }
            """, "deneme", problems);

        package.VideoStyles.Should().ContainSingle().Which.Id.Should().Be("deneme/iyi");
        problems.Should().HaveCount(4);
        problems.Should().Contain(p => p.Message.Contains("textStyles", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""{ "format": "baska" }""")]
    [InlineData("""{ "format": "nexalbum-extension", "formatVersion": 2, "id": "x" }""")]
    [InlineData("""{ "format": "nexalbum-extension", "formatVersion": 1 }""")]
    [InlineData("""{ "format": "nexalbum-extension", "formatVersion": 1, "id": "../kacak" }""")]
    public void BrokenManifestsAreRejected(string json) =>
        FluentActions.Invoking(() => ExtensionPackageReader.Read(json, "x")).Should().Throw<FormatException>();

    [Fact]
    public void FolderAndZipPackagesAreLoadedAndDuplicatesSkipped()
    {
        using var dir = new TempDir();
        var folderPackage = Path.Combine(dir.Path, "a-sinema");
        Directory.CreateDirectory(folderPackage);
        File.WriteAllText(Path.Combine(folderPackage, "manifest.json"), SampleManifest());

        var zipPath = dir.File("b-ikinci.nexpaket");
        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open());
            writer.Write("""{ "format": "nexalbum-extension", "formatVersion": 1, "id": "ikinci", "name": "İkinci", "videoStyles": [ { "id": "sade", "name": "Sade" } ] }""");
        }

        File.Copy(zipPath, dir.File("c-kopya.nexpaket"));
        File.WriteAllText(dir.File("d-bozuk.nexpaket"), "zip değil");
        Directory.CreateDirectory(Path.Combine(dir.Path, "e-bos-klasor"));

        var result = ExtensionPackageReader.LoadFolder(dir.Path);

        result.Packages.Select(p => p.Id).Should().Equal("ornek-sinema", "ikinci");
        result.Problems.Should().HaveCount(2, "kopya paket ve bozuk dosya");
        ExtensionPackageReader.ReadPackageFile(zipPath).Id.Should().Be("ikinci");
        ExtensionPackageReader.LoadFolder(dir.File("yok")).Packages.Should().BeEmpty();
    }

    [Fact]
    public void CatalogAddsPackageStylesAfterTheBuiltIns()
    {
        var package = ExtensionPackageReader.Read(SampleManifest(), "ornek");
        var duplicate = package with { Name = "Kopya" };

        var catalog = new ExtensionCatalog([package, duplicate]);

        catalog.Packages.Should().ContainSingle();
        catalog.VideoStyles.Take(VideoStyle.BuiltIn.Count).Should().Equal(VideoStyle.BuiltIn);
        catalog.VideoStyles.Should().HaveCount(VideoStyle.BuiltIn.Count + 3);
        var noir = catalog.FindVideoStyle("ornek-sinema/noir");
        noir.Package.Should().Be("Örnek Sinema Paketi");
        noir.DisplayName.Should().Be("Noir · Örnek Sinema Paketi");
        catalog.FindVideoStyle("kaldirilmis/stil").Should().Be(VideoStyle.BuiltIn[0], "paketi olmayan stil yerleşiğe düşer");
        catalog.HasVideoStyle("kaldirilmis/stil").Should().BeFalse();
        catalog.HasVideoStyle("cinematic").Should().BeTrue();
    }
}
