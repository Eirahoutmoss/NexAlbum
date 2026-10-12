using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Export.Pdf;
using Xunit;

namespace NexAlbum.Export.Tests;

public class GeometryTests
{
    [Fact]
    public void SheetWithBleedAndMarksFollowsDecision11()
    {
        var sheet = SheetGeometry.Create(new SizeMm(300, 300), EdgesMm.Uniform(3), cropMarks: true);

        // 300 + 2×(3 taşma + 3 uzaklık + 5 işaret) = 322 mm.
        sheet.Media.Should().Be(new SizeMm(322, 322));
        sheet.Trim.Should().Be(new RectMm(11, 11, 300, 300));
        sheet.Bleed.Should().Be(new RectMm(8, 8, 306, 306));
        sheet.CropMarks.Should().HaveCount(8);

        // Sol üst yatay işaret: trim üst kenarı hizasında, x = 0 … 5.
        sheet.CropMarks[0].Should().Be(new MarkLine(0, 11, 5, 11));

        // Sağ alt dikey işaret: trim sağ kenarı hizasında, taşma payı + 3 mm sonra başlar.
        sheet.CropMarks[7].Should().Be(new MarkLine(311, 317, 311, 322));
    }

    [Fact]
    public void MarksNeverEnterTheBleedArea()
    {
        var sheet = SheetGeometry.Create(new SizeMm(200, 300), new EdgesMm(2, 3, 4, 5), cropMarks: true);
        foreach (var m in sheet.CropMarks)
        {
            var line = new RectMm(Math.Min(m.X1, m.X2), Math.Min(m.Y1, m.Y2), Math.Abs(m.X2 - m.X1), Math.Abs(m.Y2 - m.Y1));
            sheet.Bleed.Intersects(line).Should().BeFalse();
            new RectMm(0, 0, sheet.Media.Width, sheet.Media.Height).Contains(line).Should().BeTrue();
        }
    }

    [Fact]
    public void UnequalBleedIsKeptPerEdge()
    {
        var sheet = SheetGeometry.Create(new SizeMm(200, 300), new EdgesMm(2, 3, 4, 5), cropMarks: false);
        sheet.Media.Should().Be(new SizeMm(208, 306));
        sheet.Trim.Should().Be(new RectMm(5, 2, 200, 300));
        sheet.CropMarks.Should().BeEmpty();
    }

    [Fact]
    public void NoBleedMeansTrimEqualsMedia()
    {
        var sheet = SheetGeometry.Create(new SizeMm(150, 130), null, cropMarks: false);
        sheet.Media.Should().Be(new SizeMm(150, 130));
        sheet.Trim.Should().Be(sheet.Bleed);
    }

    [Theory]
    [InlineData(null, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(2, 0, true)]
    [InlineData(3, 180, false)]
    [InlineData(4, 180, true)]
    [InlineData(5, 270, true)]
    [InlineData(6, 90, false)]
    [InlineData(7, 90, true)]
    [InlineData(8, 270, false)]
    public void ExifOrientationsMapToTransforms(int? exif, double rotation, bool mirror)
    {
        var t = OrientationTransform.FromExif(exif);
        t.RotationDegrees.Should().Be(rotation);
        t.MirrorX.Should().Be(mirror);
        t.SwapsAxes.Should().Be(rotation is 90 or 270);
    }

    [Fact]
    public void CoverFillsSlotAndCentersTheOverflow()
    {
        // 3:2 fotoğraf, kare yuva: yükseklik dolar, taşan genişlik iki yana eşit taşar.
        var rect = ImageFit.FullImageRect(new RectMm(0, 0, 100, 100), 1.5, new PhotoPlacement { AssetId = "a" }, CropMode.Cover);
        rect.Should().Be(new RectMm(-25, 0, 150, 100));
    }

    [Fact]
    public void ContainFitsInsideSlot()
    {
        var rect = ImageFit.FullImageRect(new RectMm(10, 10, 100, 100), 1.5, new PhotoPlacement { AssetId = "a" }, CropMode.Contain);
        rect.X.Should().BeApproximately(10, 1e-9);
        rect.Width.Should().BeApproximately(100, 1e-9);
        rect.Height.Should().BeApproximately(100 / 1.5, 1e-9);
        rect.Y.Should().BeApproximately(10 + ((100 - (100 / 1.5)) / 2), 1e-9);
    }

    [Fact]
    public void FillStretchesOnlyWhenExplicitlyRequested()
    {
        ImageFit.FullImageRect(new RectMm(0, 0, 100, 50), 1, new PhotoPlacement { AssetId = "a" }, CropMode.Fill)
            .Should().Be(new RectMm(0, 0, 100, 50));
    }

    [Fact]
    public void CropSelectionMapsToSlot()
    {
        // Sağ yarı seçildi: 1:1 görüntünün sağ yarısı (0,5:1) kare yuvayı kaplar.
        var placement = new PhotoPlacement { AssetId = "a", Crop = new NormalizedRect(0.5, 0, 0.5, 1) };
        var rect = ImageFit.FullImageRect(new RectMm(0, 0, 100, 100), 1, placement, CropMode.Cover);

        // Seçilen bölge (0,5:1) kare yuvayı genişliğe göre kaplar: 100×200, dikeyde ortalanır.
        rect.Width.Should().BeApproximately(200, 1e-9);
        rect.Height.Should().BeApproximately(200, 1e-9);
        rect.X.Should().BeApproximately(-100, 1e-9);
        rect.Y.Should().BeApproximately(-50, 1e-9);
    }

    [Fact]
    public void ScaleAndOffsetMoveTheImage()
    {
        var placement = new PhotoPlacement { AssetId = "a", Scale = 2, OffsetX = 0.1, OffsetY = -0.1 };
        var rect = ImageFit.FullImageRect(new RectMm(0, 0, 100, 100), 1, placement, CropMode.Cover);
        rect.Should().Be(new RectMm(-40, -60, 200, 200));
    }

    [Theory]
    [InlineData("all", 5, new[] { 1, 2, 3, 4, 5 })]
    [InlineData("", 2, new[] { 1, 2 })]
    [InlineData("tümü", 2, new[] { 1, 2 })]
    [InlineData("1-3, 5", 6, new[] { 1, 2, 3, 5 })]
    [InlineData("4,2,2,1-2", 5, new[] { 1, 2, 4 })]
    public void PageSelectionParses(string text, int count, int[] expected)
    {
        PageSelection.Parse(text, count).Should().Equal(expected);
    }

    [Theory]
    [InlineData("0-2")]
    [InlineData("3-1")]
    [InlineData("1-9")]
    [InlineData("a")]
    [InlineData("1-")]
    public void InvalidPageSelectionIsRejected(string text)
    {
        FluentActions.Invoking(() => PageSelection.Parse(text, 5)).Should().Throw<FormatException>();
    }

    [Fact]
    public void SettingsComeFromProfileAndValidate()
    {
        var profile = new Core.Profiles.AlbumProfile
        {
            Id = "p",
            Name = "P",
            Output = new Core.Profiles.OutputSpec(cropMarks: true, cropMarkOffsetMm: 2, cropMarkLengthMm: 6),
        };
        var settings = PdfExportSettings.FromProfile(profile);
        settings.IncludeCropMarks.Should().BeTrue();
        settings.CropMarkOffsetMm.Should().Be(2);
        settings.CropMarkLengthMm.Should().Be(6);
        PdfExportSettings.FromProfile(new Core.Profiles.AlbumProfile { Id = "p", Name = "P" }).IncludeCropMarks.Should().BeFalse();
        FluentActions.Invoking(() => settings with { CropMarkLengthMm = 0 }).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => SheetGeometry.Create(new SizeMm(1, 1), null, true, -1)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
