using System.Globalization;
using FluentAssertions;
using Jint;
using NexAlbum.Core.Effects;
using NexAlbum.Core.Video;
using NexAlbum.Export.Proof;
using Xunit;

namespace NexAlbum.Export.Tests;

/// <summary>HTML provadaki efekt betiği (effects.js), Core'daki C# hesabıyla aynı kareyi vermeli (Karar 23).</summary>
public class EffectsScriptTests
{
    private static readonly Lazy<Engine> Js = new(() =>
    {
        var engine = new Engine(o => o.Culture(CultureInfo.InvariantCulture));
        engine.Execute(ProofAssets.EffectsScript);
        return engine;
    });

    private static string Kind(TransitionKind kind) => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(kind.ToString());

    public static TheoryData<TransitionKind> Kinds()
    {
        var data = new TheoryData<TransitionKind>();
        foreach (var e in TransitionEffects.All)
        {
            data.Add(e.Kind);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ScriptMatchesCoreForEveryEffect(TransitionKind kind)
    {
        double[] progress = [0, 0.05, 0.21, 0.37, 0.5, 0.63, 0.81, 0.97, 1];
        (double Aspect, EffectPoint? Origin, uint Seed)[] cases = [(16.0 / 9, null, 1), (9.0 / 16, null, 42), (1.5, new EffectPoint(1.1, 0.27), 7), (1, new EffectPoint(0.1, 0.9), 123456789)];
        foreach (var (aspect, origin, seed) in cases)
        {
            foreach (var p in progress)
            {
                var expected = EffectDescriber.Describe(TransitionEffects.Compute(kind, p, aspect, origin, seed));
                var js = Js.Value;
                var originJs = origin is { } o ? js.Evaluate($"[{N(o.X)},{N(o.Y)}]") : Jint.Native.JsValue.Null;
                var frame = js.Invoke(js.Evaluate("NexEffects.compute"), Kind(kind), p, aspect, originJs, (double)seed);
                var actual = js.Invoke(js.Evaluate("NexEffects.describe"), frame).AsString();
                actual.Should().Be(expected, $"{kind} p={p} oran={aspect} tohum={seed}");
            }
        }
    }

    [Fact]
    public void ScriptListsTheSameEffectsInTheSameOrder()
    {
        var names = Js.Value.Evaluate("NexEffects.all.map(function (e) { return e.kind + '|' + e.name + '|' + e.usesOrigin; }).join(';')").AsString();

        names.Should().Be(string.Join(';', TransitionEffects.All.Select(e => Kind(e.Kind) + "|" + e.Name + "|" + (e.UsesOrigin ? "true" : "false"))));
        foreach (var e in TransitionEffects.All)
        {
            Js.Value.Evaluate($"NexEffects.durationFactor('{Kind(e.Kind)}')").AsNumber().Should().Be(TransitionEffects.DurationFactor(e.Kind), e.Kind.ToString());
        }
    }

    [Fact]
    public void FocusPullMatches()
    {
        foreach (var d in new[] { -2.5, -1, -0.4, 0, 0.3, 1.7 })
        {
            var f = FocusPull.Of(d, 7);
            var js = Js.Value.Evaluate($"(function(){{var f=NexEffects.focusOf({N(d)},7);return [f.scale,f.blur,f.opacity,f.saturation].join(' ');}})()").AsString();
            js.Should().Be(string.Join(' ', new[] { f.Scale, f.Blur, f.Opacity, f.Saturation }.Select(v => v.ToString("R", CultureInfo.InvariantCulture))));
        }
    }

    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
}
