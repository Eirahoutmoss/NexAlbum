using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class SlotMatcherTests
{
    private static ClassifiedPhoto Photo(string id, int w, int h, bool favorite = false, int index = 0) =>
        PhotoClassifier.Classify(TestData.Asset(id, w, h) with { Favorite = favorite }, index);

    private static LayoutSlot Slot(string id, double w, double h, int priority = 1, params PageOrientation[] recommended) =>
        new(new TemplateSlot { Id = id, Rect = new PercentRect(0, 0, 10, 10), Priority = priority, RecommendedOrientations = recommended }, new RectMm(0, 0, w, h));

    [Fact]
    public void PhotosGoToSlotsOfMatchingShape()
    {
        var slots = new[] { Slot("tall", 100, 150), Slot("wide", 150, 100) };
        var photos = new[] { Photo("p-portrait", 4000, 6000), Photo("p-landscape", 6000, 4000) };

        var result = SlotMatcher.Match(photos, slots);
        result.Select(a => (a.Slot.Template.Id, a.Photo!.Asset.Id)).Should().Equal(("tall", "p-portrait"), ("wide", "p-landscape"));
        result.Should().OnlyContain(a => a.CropLoss < 1e-9);
    }

    [Fact]
    public void FavoritesClaimPrioritySlotsFirst()
    {
        var slots = new[] { Slot("main", 150, 100, priority: 1), Slot("small", 150, 100, priority: 2) };
        var photos = new[] { Photo("normal", 6000, 4000), Photo("favori", 6000, 4000, favorite: true) };
        var result = SlotMatcher.Match(photos, slots);
        result.Single(a => a.Slot.Template.Id == "main").Photo!.Asset.Id.Should().Be("favori");
    }

    [Fact]
    public void MissingPhotosLeaveEmptySlotsInTemplateOrder()
    {
        var slots = new[] { Slot("a", 100, 100), Slot("b", 150, 100), Slot("c", 100, 100) };
        var result = SlotMatcher.Match([Photo("p", 6000, 4000)], slots);
        result.Select(a => a.Slot.Template.Id).Should().Equal("a", "b", "c");
        result.Single(a => a.Photo is not null).Slot.Template.Id.Should().Be("b");
        result.Count(a => a.Photo is null).Should().Be(2);
    }

    [Fact]
    public void MorePhotosThanSlotsIsRejected()
    {
        FluentActions.Invoking(() => SlotMatcher.Match([Photo("a", 1, 1), Photo("b", 1, 1)], [Slot("s", 1, 1)]))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CropLossIsAreaFractionRemoved()
    {
        SlotMatcher.CropLoss(1.5, 1.5).Should().Be(0);
        SlotMatcher.CropLoss(1.5, 1).Should().BeApproximately(1 - (1 / 1.5), 1e-12);
        SlotMatcher.CropLoss(1, 1.5).Should().BeApproximately(1 - (1 / 1.5), 1e-12);
    }

    [Fact]
    public void RecommendedOrientationOverridesSlotShape()
    {
        var squareSlotForPortraits = Slot("s", 100, 100, 1, PageOrientation.Portrait);
        SlotMatcher.OrientationMismatch(Photo("p", 4000, 6000), squareSlotForPortraits).Should().BeFalse();
        SlotMatcher.OrientationMismatch(Photo("l", 6000, 4000), squareSlotForPortraits).Should().BeTrue();
    }

    [Fact]
    public void AspectRatioLimitsArePenalised()
    {
        var limited = new LayoutSlot(new TemplateSlot { Id = "s", Rect = new PercentRect(0, 0, 1, 1), MinAspectRatio = 1.2, MaxAspectRatio = 2 }, new RectMm(0, 0, 150, 100));
        var free = Slot("f", 150, 100);
        var portrait = Photo("p", 4000, 6000);
        (SlotMatcher.Cost(portrait, limited) - SlotMatcher.Cost(portrait, free)).Should().BeApproximately(1, 1e-9);

        var panorama = Photo("w", 9000, 3000);
        (SlotMatcher.Cost(panorama, limited) - SlotMatcher.Cost(panorama, free)).Should().BeApproximately(1, 1e-9);
    }
}
