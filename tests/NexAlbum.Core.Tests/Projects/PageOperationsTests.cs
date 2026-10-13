using FluentAssertions;
using NexAlbum.Core.Common;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class PageOperationsTests
{
    private static readonly DateTimeOffset Later = TestData.Now.AddHours(1);

    private static AlbumProject ThreePages() =>
        TestData.Project().WithPages(
        [
            TestData.Page("page-001", TestData.Slot("s1")) with { Background = BackgroundFill.Solid(RgbaColor.Parse("#F5EFE6")) },
            TestData.Page("page-002", TestData.Slot("s1")),
            TestData.Page("page-004", TestData.Slot("s1")),
        ]);

    [Fact]
    public void NextPageIdFillsTheFirstGap()
    {
        PageOperations.NextPageId(ThreePages()).Should().Be("page-003");
        PageOperations.NextPageId(TestData.Project()).Should().Be("page-001");
    }

    [Fact]
    public void BlankPageGoesAfterTheGivenPageWithItsBackground()
    {
        var (project, index) = PageOperations.AddBlank(ThreePages(), 0, Later);

        index.Should().Be(1);
        project.Pages.Select(p => p.Id).Should().Equal("page-001", "page-003", "page-002", "page-004");
        var page = project.Pages[1];
        page.Elements.Should().BeEmpty();
        page.Background.Color.Should().Be(RgbaColor.Parse("#F5EFE6"), "komşu sayfanın fonu alınır");
        page.CreatedAt.Should().Be(Later);
    }

    [Fact]
    public void BlankPageInAnEmptyAlbumIsTheFirstPage()
    {
        var (project, index) = PageOperations.AddBlank(TestData.Project(), -1, Later);
        index.Should().Be(0);
        project.Pages.Should().ContainSingle().Which.Background.Should().Be(BackgroundFill.White);
    }

    [Fact]
    public void DuplicateCopiesContentWithANewIdRightAfterTheOriginal()
    {
        var source = ThreePages();
        source = source.ReplacePage(source.Pages[1] with { Locked = true });

        var (project, index) = PageOperations.Duplicate(source, "page-002", Later);

        index.Should().Be(2);
        project.Pages.Select(p => p.Id).Should().Equal("page-001", "page-002", "page-003", "page-004");
        project.Pages[2].Elements.Should().BeEquivalentTo(source.Pages[1].Elements);
        project.Pages[2].Locked.Should().BeFalse("kopya düzenlenebilir başlar");
        project.Pages[2].UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void LockedPagesAreNeitherDeletedNorMovedNorJumpedOver()
    {
        var source = ThreePages();
        source = source.ReplacePage(source.Pages[1] with { Locked = true });

        PageOperations.Delete(source, "page-002").Should().BeNull();
        PageOperations.Delete(source, "page-001")!.Pages.Select(p => p.Id).Should().Equal("page-002", "page-004");

        PageOperations.Move(source, "page-002", 0).Should().BeNull("kilitli sayfa yerinden oynamaz");
        PageOperations.Move(source, "page-001", 2).Should().BeNull("kilitli sayfanın üstünden atlanamaz");
        PageOperations.Move(source, "page-001", 0).Should().BeSameAs(source, "sıra değişmez");
    }

    [Fact]
    public void MoveReordersAndClamps()
    {
        var moved = PageOperations.Move(ThreePages(), "page-004", -5)!;
        moved.Pages.Select(p => p.Id).Should().Equal("page-004", "page-001", "page-002");

        PageOperations.Move(ThreePages(), "page-001", 99)!.Pages.Select(p => p.Id).Should().Equal("page-002", "page-004", "page-001");
    }

    [Fact]
    public void UnknownPageIsRejected()
    {
        var act = () => PageOperations.Duplicate(ThreePages(), "yok", Later);
        act.Should().Throw<ArgumentException>();
    }
}
