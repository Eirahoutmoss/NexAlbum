using FluentAssertions;
using NexAlbum.Core.Video;
using Xunit;

namespace NexAlbum.Core.Tests.Video;

public class BeatDetectorTests
{
    /// <summary>Sentetik müzik: sessiz giriş, ardından her vuruşta kısa, sönümlenen tık + hafif uğultu.</summary>
    private static float[] Clicks(double bpm, double seconds, double leadIn, int rate = 11025)
    {
        var samples = new float[(int)(seconds * rate)];
        var period = 60 / bpm;
        for (var i = 0; i < samples.Length; i++)
        {
            var t = i / (double)rate;
            if (t < leadIn)
            {
                continue;
            }

            var sinceBeat = (t - leadIn) % period;
            var click = Math.Exp(-sinceBeat * 40) * Math.Sin(2 * Math.PI * 880 * t);
            samples[i] = (float)((0.8 * click) + (0.05 * Math.Sin(2 * Math.PI * 110 * t)));
        }

        return samples;
    }

    [Theory]
    [InlineData(120)]
    [InlineData(96)]
    [InlineData(140)]
    public void TempoAndBeatsAreFound(double bpm)
    {
        var grid = BeatDetector.Detect(Clicks(bpm, 30, 1.5), 11025);

        grid.IsUsable.Should().BeTrue();
        grid.Bpm.Should().BeApproximately(bpm, bpm * 0.04);
        grid.LeadIn.Should().BeApproximately(1.5, 0.05, "sessiz giriş atlanır");
        var period = 60 / bpm;
        grid.Beats.Where(b => b > 2).Take(10).Should().OnlyContain(b => Math.Abs(((b - 1.5) / period) - Math.Round((b - 1.5) / period)) < 0.12, "vuruşlar tıklara denk gelir");
        grid.Duration.Should().BeApproximately(30, 0.01);
    }

    [Fact]
    public void SilenceAndShortAudioGiveNoGrid()
    {
        BeatDetector.Detect(new float[11025 * 10], 11025).IsUsable.Should().BeFalse();
        BeatDetector.Detect(new float[100], 11025).Beats.Should().BeEmpty();
    }

    [Fact]
    public void SnapMovesToTheNearestBeatWithinTolerance()
    {
        var grid = new BeatGrid(120, [0.5, 1.0, 1.5, 2.0, 2.5], 0.8, 0, 3);
        grid.Snap(1.2, 0.3).Should().Be(1.0);
        grid.Snap(1.3, 0.3).Should().Be(1.5);
        grid.Snap(1.25, 0.1).Should().Be(1.25, "tolerans dışında kalır");
        BeatGrid.None(3).Snap(1.2, 1).Should().Be(1.2);
    }
}
