using FluentAssertions;
using NexAlbum.Core.Layout;
using Xunit;

namespace NexAlbum.Core.Tests.Layout;

public class TextLinesTests
{
    /// <summary>Her karakter 1 birim.</summary>
    private static double Measure(string s) => s.Length;

    [Fact]
    public void WordsWrapToTheBoxWidth()
    {
        TextLines.Wrap("Bazı anlar bir ömür sürer", 10, Measure).Should().Equal("Bazı anlar", "bir ömür", "sürer");
    }

    [Fact]
    public void ManualLineBreaksAndEmptyLinesArePreserved()
    {
        TextLines.Wrap("Elif\r\n\nCan", 50, Measure).Should().Equal("Elif", string.Empty, "Can");
    }

    [Fact]
    public void ALongWordIsNotSplit()
    {
        TextLines.Wrap("Kapadokya'da bir sabah", 5, Measure).Should().Equal("Kapadokya'da", "bir", "sabah");
    }
}
