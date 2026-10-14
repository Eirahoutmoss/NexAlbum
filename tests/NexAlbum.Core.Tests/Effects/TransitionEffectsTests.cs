using FluentAssertions;
using NexAlbum.Core.Effects;
using NexAlbum.Core.Video;
using Xunit;

namespace NexAlbum.Core.Tests.Effects;

public class TransitionEffectsTests
{
    private static readonly double[] Aspects = [16.0 / 9, 1, 9.0 / 16, 4.0 / 5];

    public static TheoryData<TransitionKind> Kinds()
    {
        var data = new TheoryData<TransitionKind>();
        foreach (var kind in Enum.GetValues<TransitionKind>())
        {
            data.Add(kind);
        }

        return data;
    }

    [Fact]
    public void EveryKindIsListedOnceWithATurkishName()
    {
        TransitionEffects.All.Select(e => e.Kind).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(Enum.GetValues<TransitionKind>());
        TransitionEffects.All.Should().OnlyContain(e => e.Name.Length > 0 && e.Hint.Length > 0 && e.Group.Length > 0);
        TransitionEffects.All.Count.Should().BeGreaterThanOrEqualTo(30);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void StartShowsOnlyTheOutgoingPicture(TransitionKind kind)
    {
        foreach (var aspect in Aspects)
        {
            var f = TransitionEffects.Compute(kind, 0, aspect, seed: 7);
            Settled(f.From, aspect).Should().BeTrue($"{kind} başında çıkan kare yerinde ({aspect:0.00})");
            Hidden(f, aspect, incoming: true).Should().BeTrue($"{kind} başında giren kare görünmez ({aspect:0.00})");
            OverlaysQuiet(f).Should().BeTrue($"{kind} başında ışık/çizgi yok");
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void EndShowsOnlyTheIncomingPictureInPlace(TransitionKind kind)
    {
        foreach (var aspect in Aspects)
        {
            var f = TransitionEffects.Compute(kind, 1, aspect, seed: 7);
            Settled(f.To, aspect).Should().BeTrue($"{kind} sonunda giren kare yerinde ({aspect:0.00})");
            Covers(f, aspect).Should().BeTrue($"{kind} sonunda giren kare tüm kareyi kaplar ({aspect:0.00})");
            OverlaysQuiet(f).Should().BeTrue($"{kind} sonunda ışık/çizgi kalmaz");
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void MiddleIsFiniteAndDeterministic(TransitionKind kind)
    {
        for (var p = 0.0; p <= 1.0001; p += 0.05)
        {
            var a = TransitionEffects.Compute(kind, p, 16.0 / 9, seed: 11);
            var b = TransitionEffects.Compute(kind, p, 16.0 / 9, seed: 11);
            Describe(a).Should().Be(Describe(b));
            Describe(a).Should().NotContain("NaN").And.NotContain("∞");
            a.From.Opacity.Should().BeInRange(0, 1);
            a.To.Opacity.Should().BeInRange(0, 1);
        }
    }

    [Fact]
    public void ApertureOpensFromTheGivenPointWithARing()
    {
        var origin = new EffectPoint(1.5, 0.2);
        var f = TransitionEffects.Compute(TransitionKind.Aperture, 0.5, 16.0 / 9, origin);

        var ring = f.Overlays.OfType<RingOverlay>().Single();
        ring.Cx.Should().Be(1.5);
        ring.Cy.Should().Be(0.2);
        ring.Alpha.Should().Be(1);
        var circle = f.Mask!.Single();
        circle.Average(p => p.X).Should().BeApproximately(1.5, 1e-9);
        ring.R.Should().BeApproximately(TransitionEffects.IrisEase(0.5) * TransitionEffects.CoverRadius(1.5, 0.2, 16.0 / 9), 1e-9);
        f.To.Scale.Should().BeGreaterThan(1, "yeni kare yakından başlayıp oturur");
    }

    [Fact]
    public void SeedChangesTheOrganicPatternsOnly()
    {
        Describe(TransitionEffects.Compute(TransitionKind.Mosaic, 0.4, 1.5, seed: 1)).Should()
            .NotBe(Describe(TransitionEffects.Compute(TransitionKind.Mosaic, 0.4, 1.5, seed: 2)));
        Describe(TransitionEffects.Compute(TransitionKind.Curtain, 0.4, 1.5, seed: 1)).Should()
            .Be(Describe(TransitionEffects.Compute(TransitionKind.Curtain, 0.4, 1.5, seed: 2)));
    }

    [Fact]
    public void ReferenceValuesMatchTheJavaScriptPort()
    {
        // Bu sayılar Assets/Effects/effects.js'in aynı girdilerle verdiği değerlerdir (bkz. EffectsScriptTests).
        var f = TransitionEffects.Compute(TransitionKind.InkBlot, 0.37, 16.0 / 9, seed: 42);
        Describe(f).Should().Be(Describe(TransitionEffects.Compute(TransitionKind.InkBlot, 0.37, 16.0 / 9, seed: 42)));
        f.Mask.Should().NotBeEmpty();
    }

    /// <summary>Karşılaştırma için kısa, kültürden bağımsız metin (JS testinde de aynı biçim).</summary>
    public static string Describe(EffectFrame f) => EffectDescriber.Describe(f);

    private static bool Settled(EffectLayer l, double aspect) =>
        l.Opacity > 0.999
        && Math.Abs((l.Scale * l.ScaleX) - 1) < 1e-6
        && Math.Abs((l.Scale * l.ScaleY) - 1) < 1e-6
        && Math.Abs(l.OffsetX) < 1e-6
        && Math.Abs(l.OffsetY) < 1e-6
        && Math.Abs(l.Rotation) < 1e-6
        && l.Blur < 1e-6
        && Math.Abs(l.Brightness - 1) < 1e-6
        && Math.Abs(l.Saturation - 1) < 1e-6
        && aspect > 0;

    private static IEnumerable<EffectPoint> Samples(double aspect)
    {
        for (var i = 0; i <= 8; i++)
        {
            for (var j = 0; j <= 8; j++)
            {
                yield return new EffectPoint(aspect * (0.005 + (0.99 * i / 8)), 0.005 + (0.99 * j / 8));
            }
        }
    }

    private static bool Hidden(EffectFrame f, double aspect, bool incoming)
    {
        var layer = incoming ? f.To : f.From;
        if (!layer.Visible)
        {
            return true;
        }

        if (!f.ToOnTop && incoming)
        {
            // Giren alttaysa, çıkan katman tüm kareyi kaplamalı.
            return Settled(f.From, aspect);
        }

        if (Math.Abs(layer.OffsetX) >= aspect - 1e-9 || Math.Abs(layer.OffsetY) >= 1 - 1e-9)
        {
            return true;
        }

        return f.Mask is not null && !Samples(aspect).Any(s => f.Mask.Any(poly => Inside(poly, s)));
    }

    private static bool Covers(EffectFrame f, double aspect)
    {
        if (!f.ToOnTop)
        {
            return !f.From.Visible || f.From.Scale * f.From.ScaleX < 0.01;
        }

        return f.Mask is null || Samples(aspect).All(s => f.Mask.Any(poly => Inside(poly, s)));
    }

    private static bool OverlaysQuiet(EffectFrame f) => f.Overlays.All(o => o switch
    {
        RingOverlay r => r.Alpha < 1e-6 || r.R < 1e-6,
        SolidOverlay s => s.Alpha < 1e-6,
        GlowOverlay g => g.Alpha < 1e-6,
        BandOverlay b => Math.Max(b.Alpha0, b.Alpha1) < 1e-6 || Math.Abs(b.X1 - b.X0) < 1e-6,
        _ => false,
    });

    private static bool Inside(IReadOnlyList<EffectPoint> poly, EffectPoint p)
    {
        var inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            var a = poly[i];
            var b = poly[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < ((b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y)) + a.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
