using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class PlacementDpiTests
{
    // 254 mm = 10 inç; hesapları elle doğrulamayı kolaylaştırır.
    private static Slot SquareSlot(CropMode mode = CropMode.Cover) =>
        TestData.Slot(width: 254, height: 254, assetId: "a") with { CropMode = mode };

    [Fact]
    public void CoverUsesTheShorterSide()
    {
        // 6000×4000 → kare yuvayı kaplamak için 4000 px 10 inçe yayılır: 400 DPI.
        var slot = SquareSlot();
        PlacementDpi.Calculate(TestData.Asset(), slot, slot.Placement!).Should().BeApproximately(400, 1e-9);
    }

    [Fact]
    public void ContainUsesTheLongerSide()
    {
        var slot = SquareSlot(CropMode.Contain);
        PlacementDpi.Calculate(TestData.Asset(), slot, slot.Placement!).Should().BeApproximately(600, 1e-9);
    }

    [Fact]
    public void FillReportsTheWorstAxis()
    {
        var slot = SquareSlot(CropMode.Fill);
        PlacementDpi.Calculate(TestData.Asset(), slot, slot.Placement!).Should().BeApproximately(400, 1e-9);
    }

    [Fact]
    public void CropAndZoomReduceResolution()
    {
        var slot = SquareSlot();
        var placement = slot.Placement! with { Crop = new NormalizedRect(0, 0, 0.5, 0.5), Scale = 2 };
        // Kırpma: 3000×2000 → 200 DPI; 2× yakınlaştırma → 100 DPI.
        PlacementDpi.Calculate(TestData.Asset(), slot, placement).Should().BeApproximately(100, 1e-9);
    }

    [Fact]
    public void ExifRotationIsTakenIntoAccount()
    {
        var slot = TestData.Slot(width: 254, height: 381, assetId: "a");
        var portrait = TestData.Asset(orientation: 6); // 4000×6000 görünür
        PlacementDpi.Calculate(portrait, slot, slot.Placement!).Should().BeApproximately(400, 1e-9);
    }

    [Fact]
    public void UnknownCropModeIsRejected()
    {
        var slot = SquareSlot() with { CropMode = (CropMode)42 };
        FluentActions.Invoking(() => PlacementDpi.Calculate(TestData.Asset(), slot, slot.Placement!))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
}
