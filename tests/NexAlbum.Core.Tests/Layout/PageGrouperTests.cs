using FluentAssertions;
using NexAlbum.Core.Layout;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class PageGrouperTests
{
    private static readonly int[] AllCounts = [1, 2, 3, 4, 5, 6, 9];

    private static IReadOnlyList<PageGroup> Group(int n, LayoutOptions options, int[]? supported = null, ulong seed = 1) =>
        PageGrouper.Group(n, supported ?? AllCounts, options, new DeterministicRandom(seed));

    [Theory]
    [InlineData(10, 1, 4)]
    [InlineData(7, 2, 4)]
    [InlineData(100, 1, 6)]
    [InlineData(3, 2, 6)]
    public void QuickModeIsBalancedAndRespectsLimits(int n, int min, int max)
    {
        var groups = Group(n, new LayoutOptions { Mode = LayoutMode.Quick, MinPhotosPerPage = min, MaxPhotosPerPage = max });
        groups.Sum(g => g.PhotoCount).Should().Be(n);
        groups.Should().OnlyContain(g => g.PhotoCount >= min && g.PhotoCount <= max && g.EmptySlots == 0);
        (groups.Max(g => g.PhotoCount) - groups.Min(g => g.PhotoCount)).Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void SevenPhotosWithMinimumTwoLeaveNoSingleton()
    {
        var groups = Group(7, new LayoutOptions { Mode = LayoutMode.Quick, MinPhotosPerPage = 2, MaxPhotosPerPage = 4 });
        groups.Select(g => g.PhotoCount).Should().NotContain(1);
    }

    [Fact]
    public void TargetPageCountDistributesEvenly()
    {
        var groups = Group(10, new LayoutOptions { TargetPageCount = 3 });
        groups.Select(g => g.PhotoCount).Should().Equal(4, 3, 3);
    }

    [Fact]
    public void FixedDensityIsExactAndLeftoverGetsEmptySlots()
    {
        var groups = Group(10, new LayoutOptions { Mode = LayoutMode.Controlled, FixedPhotosPerPage = 3 });
        groups.Select(g => (g.PhotoCount, g.SlotCount)).Should().Equal((3, 3), (3, 3), (3, 3), (1, 3));
        groups[^1].EmptySlots.Should().Be(2);
    }

    [Fact]
    public void GapsInSupportedCountsAreWorkedAround()
    {
        var groups = Group(5, new LayoutOptions { Mode = LayoutMode.Quick }, [2, 4]);
        groups.Sum(g => g.PhotoCount).Should().Be(5);
        groups.Sum(g => g.EmptySlots).Should().Be(1);
        groups.Should().OnlyContain(g => g.SlotCount == 2 || g.SlotCount == 4);

        var six = Group(6, new LayoutOptions { Mode = LayoutMode.Quick }, [2, 4]);
        six.Sum(g => g.EmptySlots).Should().Be(0);
    }

    [Fact]
    public void SinglePhotoWithOnlyLargerTemplatesUsesEmptySlots()
    {
        var groups = Group(1, new LayoutOptions(), [2]);
        groups.Should().ContainSingle().Which.Should().Be(new PageGroup(1, 2));
    }

    [Fact]
    public void BalancedModeVariesDensityDeterministically()
    {
        var options = new LayoutOptions { Mode = LayoutMode.Balanced, MinPhotosPerPage = 1, MaxPhotosPerPage = 6 };
        var a = Group(60, options, seed: 5);
        var b = Group(60, options, seed: 5);
        a.Should().Equal(b);
        a.Sum(g => g.PhotoCount).Should().Be(60);
        a.Select(g => g.PhotoCount).Distinct().Count().Should().BeGreaterThan(1, "dengeli mod farklı yoğunluklar kullanır");
        a.Should().OnlyContain(g => g.PhotoCount >= 1 && g.PhotoCount <= 6);
    }

    [Fact]
    public void EdgeCases()
    {
        Group(0, new LayoutOptions()).Should().BeEmpty();
        FluentActions.Invoking(() => Group(5, new LayoutOptions { FixedPhotosPerPage = 7 })).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => Group(5, new LayoutOptions { MinPhotosPerPage = 7, MaxPhotosPerPage = 8 })).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => Group(-1, new LayoutOptions())).Should().Throw<ArgumentOutOfRangeException>();
    }
}
