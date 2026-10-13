using FluentAssertions;
using NexAlbum.Core.Design;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Design;

public class ColorMatrixTests
{
    [Fact]
    public void NoAdjustmentIsIdentity()
    {
        ColorMatrix.From(ImageAdjustments.None).IsIdentity.Should().BeTrue();
        ColorMatrix.From(ImageAdjustments.None).Apply(0.2, 0.5, 0.8).Should().Be((0.2, 0.5, 0.8));
    }

    [Fact]
    public void EachAdjustmentMovesColorsTheExpectedWay()
    {
        var gray = (0.5, 0.5, 0.5);

        Apply(new ImageAdjustments { Exposure = 0.5 }, 0.3, 0.3, 0.3).R.Should().BeGreaterThan(0.45, "pozlama ışığı katlar");
        Apply(new ImageAdjustments { Brightness = 0.4 }, 0.2, 0.2, 0.2).R.Should().BeApproximately(0.3, 1e-9);
        Apply(new ImageAdjustments { Contrast = 0.5 }, gray.Item1, gray.Item2, gray.Item3).R.Should().BeApproximately(0.5, 1e-9, "orta gri sabit");
        Apply(new ImageAdjustments { Contrast = 0.5 }, 0.8, 0.8, 0.8).R.Should().BeGreaterThan(0.8);
        Apply(new ImageAdjustments { Contrast = -0.5 }, 0.8, 0.8, 0.8).R.Should().BeLessThan(0.8);

        var warm = Apply(new ImageAdjustments { Temperature = 1 }, 0.5, 0.5, 0.5);
        warm.R.Should().BeGreaterThan(warm.B, "sıcak = kırmızı fazla, mavi az");
        var cool = Apply(new ImageAdjustments { Temperature = -1 }, 0.5, 0.5, 0.5);
        cool.B.Should().BeGreaterThan(cool.R);

        var bw = Apply(new ImageAdjustments { BlackWhite = true, Saturation = 0.8 }, 0.9, 0.2, 0.1);
        bw.R.Should().BeApproximately(bw.G, 1e-9, "siyah-beyaz doygunluğu ezer");
        bw.G.Should().BeApproximately(bw.B, 1e-9);
        bw.R.Should().BeApproximately((0.2126 * 0.9) + (0.7152 * 0.2) + (0.0722 * 0.1), 1e-9);

        var vivid = Apply(new ImageAdjustments { Saturation = 1 }, 0.6, 0.4, 0.4);
        (vivid.R - vivid.G).Should().BeGreaterThan(0.2, "doygunluk farkı büyütür");
    }

    [Fact]
    public void OrderFollowsTheEffectChain()
    {
        // Önce pozlama, sonra kontrast: 0,25 → ×2 = 0,5 → kontrastta sabit.
        var a = new ImageAdjustments { Exposure = 2.0 / 3, Contrast = 0.6 };
        Apply(a, 0.25, 0.25, 0.25).R.Should().BeApproximately(0.5, 1e-9);
        ColorMatrix.From(a).ToFloats().Should().HaveCount(20);
        ColorMatrix.From(a).Values[18].Should().Be(1, "alfa değişmez");
    }

    private static (double R, double G, double B) Apply(ImageAdjustments a, double r, double g, double b) => ColorMatrix.From(a).Apply(r, g, b);
}
