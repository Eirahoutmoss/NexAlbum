using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Templates;
using Xunit;

namespace NexAlbum.Core.Tests.Templates;

public class TemplateDefinitionTests
{
    private static TemplateDefinition FourGrid() => new()
    {
        Id = "template-4-grid",
        Name = "4'lü Izgara",
        Category = TemplateCategory.Grid,
        PhotoCount = new PhotoCountRange(4, 4),
        Slots =
        [
            new TemplateSlot { Id = "s1", Rect = new PercentRect(0, 0, 50, 50) },
            new TemplateSlot { Id = "s2", Rect = new PercentRect(50, 0, 50, 50) },
            new TemplateSlot { Id = "s3", Rect = new PercentRect(0, 50, 50, 50) },
            new TemplateSlot { Id = "s4", Rect = new PercentRect(50, 50, 50, 50) },
        ],
    };

    [Fact]
    public void ResolvesToAnyPageSize()
    {
        var slots = FourGrid().Resolve(new RectMm(0, 0, 300, 200));
        slots.Should().HaveCount(4);
        slots[3].Should().Be(new ResolvedSlot("s4", new RectMm(150, 100, 150, 100)));
    }

    [Fact]
    public void ResolvesInsideAnInsetArea()
    {
        var area = new RectMm(0, 0, 300, 300).Deflate(EdgesMm.Uniform(10));
        FourGrid().Resolve(area)[0].Bounds.Should().Be(new RectMm(10, 10, 140, 140));
    }

    [Theory]
    [InlineData(TemplateOrientation.Any, PageOrientation.Portrait, true)]
    [InlineData(TemplateOrientation.Square, PageOrientation.Square, true)]
    [InlineData(TemplateOrientation.Square, PageOrientation.Landscape, false)]
    [InlineData(TemplateOrientation.Portrait, PageOrientation.Portrait, true)]
    [InlineData(TemplateOrientation.Landscape, PageOrientation.Portrait, false)]
    [InlineData((TemplateOrientation)99, PageOrientation.Portrait, false)]
    public void FitsChecksOrientation(TemplateOrientation template, PageOrientation page, bool expected)
    {
        (FourGrid() with { Orientation = template }).Fits(page).Should().Be(expected);
    }

    [Fact]
    public void DuplicateSlotIdsAreRejected()
    {
        var slot = new TemplateSlot { Id = "s1", Rect = new PercentRect(0, 0, 10, 10) };
        FluentActions.Invoking(() => FourGrid() with { Slots = [slot, slot] }).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PhotoCountRangeValidates()
    {
        var range = new PhotoCountRange(2, 4);
        range.Accepts(3).Should().BeTrue();
        range.Accepts(5).Should().BeFalse();
        range.Accepts(1).Should().BeFalse();
        FluentActions.Invoking(() => new PhotoCountRange(3, 2)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new PhotoCountRange(-1, 2)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DefaultsAreSensible()
    {
        var t = FourGrid();
        t.Author.Should().Be("builtin");
        t.Version.Should().Be("1.0.0");
        t.Rules.PreserveAspectRatio.Should().BeTrue();
        t.TextFields.Should().BeEmpty();
        var field = new TemplateTextField { Id = "t1", Rect = new PercentRect(0, 90, 100, 10), Placeholder = "Tarih" };
        (t with { TextFields = [field] }).TextFields.Should().ContainSingle();
    }
}
