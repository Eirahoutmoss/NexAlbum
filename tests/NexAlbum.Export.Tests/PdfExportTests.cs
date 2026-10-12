using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Export.Pdf;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace NexAlbum.Export.Tests;

public sealed class PdfExportTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);
    private readonly string dir = Path.Combine(Path.GetTempPath(), "nexalbum-export-tests", Guid.NewGuid().ToString("N"));

    public PdfExportTests()
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

    private string Write(string name, byte[] data)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllBytes(path, data);
        return path;
    }

    private static AlbumProfile Profile(bool cropMarks = true) => new()
    {
        Id = "p",
        Name = "30×30",
        Verified = true,
        Page = new PageSpec(new SizeMm(300, 300)),
        Bleed = new EdgeSpec(EdgesMm.Uniform(3)),
        SafeArea = new EdgeSpec(EdgesMm.Uniform(5)),
        Resolution = new ResolutionSpec(300),
        Output = new OutputSpec(cropMarks: cropMarks, cropMarkOffsetMm: 3, cropMarkLengthMm: 5),
    };

    private AlbumProject Project(AlbumProfile? profile = null)
    {
        var jpeg = Write("foto.jpg", Fixtures.Jpeg);
        var png = Write("foto.png", Fixtures.Png);
        var gif = Write("foto.gif", Fixtures.Gif);
        PhotoAsset Asset(string id, string path, int w, int h, int? exif = null) => new()
        {
            Id = id,
            SourcePath = path,
            FileName = Path.GetFileName(path),
            PixelWidth = w,
            PixelHeight = h,
            ExifOrientation = exif,
            ImportedAt = Now,
        };

        Slot Slot(string id, string asset, double x) => new()
        {
            Id = id,
            Bounds = new RectMm(x, 20, 120, 120),
            Placement = new PhotoPlacement { AssetId = asset },
        };

        var project = AlbumProject.Create("Örnek Albüm", profile ?? Profile(), Now, "0.1.0", Guid.NewGuid())
            .AddAssets([Asset("jpg", jpeg, 60, 40, 6), Asset("png", png, 20, 30), Asset("gif", gif, 20, 30), Asset("yok", Path.Combine(dir, "yok.jpg"), 10, 10)]);

        return project.WithPages(
        [
            new AlbumPage
            {
                Id = "p1",
                Background = BackgroundFill.Solid(Core.Common.RgbaColor.Parse("#F5F5F5")),
                Elements =
                [
                    new BackgroundElement { Id = "bg", Fill = new BackgroundFill { Mode = BackgroundMode.Gradient, Gradient = new GradientSpec(Core.Common.RgbaColor.White, Core.Common.RgbaColor.Black) } },
                    Slot("s1", "jpg", 20),
                    Slot("s2", "png", 160),
                    new TextElement { Id = "t", Bounds = new RectMm(20, 260, 260, 15), Content = "15 Eylül 2024", Font = new FontSpec { Family = "Arial" }, Alignment = TextAlignment.Center },
                ],
            },
            new AlbumPage { Id = "p2", Elements = [Slot("s3", "jpg", 20) with { CropMode = CropMode.Contain, Placement = new PhotoPlacement { AssetId = "jpg", Rotation = 15, Scale = 1.2 } }] },
            new AlbumPage { Id = "p3", Elements = [Slot("s4", "gif", 20), Slot("s5", "yok", 160)] },
        ]);
    }

    [Fact]
    public void WritesOnePrintReadyPdfWithBoxes()
    {
        var path = Path.Combine(dir, "album.pdf");
        var progress = new List<int>();
        var result = PdfAlbumExporter.Export(Project(), path, progress: new SyncProgress(progress.Add));

        result.Files.Should().Equal(Path.GetFullPath(path));
        result.PageCount.Should().Be(3);
        progress.Should().Equal(1, 2, 3);
        File.Exists(path + ".tmp").Should().BeFalse();

        using var pdf = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        pdf.PageCount.Should().Be(3);
        var page = pdf.Pages[0];
        page.Width.Millimeter.Should().BeApproximately(322, 0.01);
        page.Height.Millimeter.Should().BeApproximately(322, 0.01);
        (page.TrimBox.Width * 25.4 / 72).Should().BeApproximately(300, 0.01);
        (page.BleedBox.Width * 25.4 / 72).Should().BeApproximately(306, 0.01);
        (page.TrimBox.X1 * 25.4 / 72).Should().BeApproximately(11, 0.01);
        pdf.Info.Title.Should().Be("Örnek Albüm");
    }

    [Fact]
    public void UnsupportedAndMissingPhotosAreReportedNotThrown()
    {
        var result = PdfAlbumExporter.Export(Project(), Path.Combine(dir, "album.pdf"));
        var unreadable = result.Issues.Errors.Where(i => i.Code == "export.photo.unreadable").ToList();
        unreadable.Should().HaveCount(2);
        unreadable.Select(i => i.ElementId).Should().BeEquivalentTo(new[] { "s4", "s5" });
        unreadable.Single(i => i.ElementId == "s5").Message.Should().Contain("bulunamadı");
    }

    [Fact]
    public void JpegIsEmbeddedWithoutRecompression()
    {
        var path = Path.Combine(dir, "album.pdf");
        PdfAlbumExporter.Export(Project(), path, new PdfExportSettings { Pages = "2" });
        var bytes = File.ReadAllBytes(path);

        // Karar 11 §3.2: kaynak çözünürlükte gömme. JPEG akışı birebir PDF'in içinde olmalı.
        IndexOf(bytes, Fixtures.Jpeg[^200..]).Should().BeGreaterThan(0);
    }

    [Fact]
    public void SeparateFilesAndPageSelection()
    {
        var path = Path.Combine(dir, "album.pdf");
        var result = PdfAlbumExporter.Export(Project(), path, new PdfExportSettings { Pages = "1,3", SeparateFiles = true });
        result.Files.Select(f => Path.GetFileName(f)).Should().Equal("album_001.pdf", "album_003.pdf");
        result.Files.Should().OnlyContain(f => File.Exists(f));
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void WithoutBleedOrMarksThePageIsTheTrimSize()
    {
        var path = Path.Combine(dir, "album.pdf");
        PdfAlbumExporter.Export(Project(Profile(cropMarks: false)), path, new PdfExportSettings { IncludeBleed = false, IncludeCropMarks = false });
        using var pdf = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        pdf.Pages[0].Width.Millimeter.Should().BeApproximately(300, 0.01);
    }

    [Fact]
    public void MissingBleedIsNotInvented()
    {
        var profile = Profile() with { Bleed = EdgeSpec.Unknown, Color = new ColorSpec(ColorMode.Cmyk, "Fogra39.icc") };
        var path = Path.Combine(dir, "album.pdf");
        var result = PdfAlbumExporter.Export(Project(profile), path, new PdfExportSettings { IncludeCropMarks = false });
        result.Issues.Contains("export.bleed.missing").Should().BeTrue();
        result.Issues.Contains("export.icc.not-embedded").Should().BeTrue();
        using var pdf = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        pdf.Pages[0].Width.Millimeter.Should().BeApproximately(300, 0.01);
    }

    [Fact]
    public void BackgroundImagesAreReported()
    {
        var project = Project();
        project = project.ReplacePage(project.Pages[0] with { Background = new BackgroundFill { Mode = BackgroundMode.Image, ImageAssetId = "jpg" } });
        PdfAlbumExporter.Export(project, Path.Combine(dir, "a.pdf"), new PdfExportSettings { Pages = "1" })
            .Issues.Contains("export.background-image.unsupported").Should().BeTrue();
    }

    [Fact]
    public void CancellationStopsWithoutLeavingAFile()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var path = Path.Combine(dir, "album.pdf");
        FluentActions.Invoking(() => PdfAlbumExporter.Export(Project(), path, cancellationToken: cts.Token)).Should().Throw<OperationCanceledException>();
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void NumberedPaths()
    {
        PdfAlbumExporter.NumberedPath(Path.Combine("a", "album.pdf"), 7).Should().Be(Path.Combine("a", "album_007.pdf"));
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }

    private sealed class SyncProgress(Action<int> report) : IProgress<int>
    {
        public void Report(int value) => report(value);
    }
}
