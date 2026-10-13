using FluentAssertions;
using NexAlbum.Core.Video;
using NexAlbum.Rendering.Video;
using SkiaSharp;
using Xunit;

namespace NexAlbum.Video.Tests;

/// <summary>Bellekteki renkli fotoğraflar: kırmızı yatay, mavi dikey (çizimin doğrulanabilmesi için düz renk).</summary>
internal sealed class ColorPhotos : IPhotoSource
{
    public static SKImage Solid(int w, int h, SKColor color)
    {
        using var surface = SKSurface.Create(new SKImageInfo(w, h));
        surface.Canvas.Clear(color);
        return surface.Snapshot();
    }

    private readonly Dictionary<string, SKImage> images = new(StringComparer.Ordinal)
    {
        ["red"] = Solid(600, 400, SKColors.Red),
        ["blue"] = Solid(400, 600, SKColors.Blue),
    };

    public SKImage? Get(string assetId, int minLongSide) => images.GetValueOrDefault(assetId);
}

public class VideoRenderingTests
{
    private static readonly VideoFormat Hd = VideoFormat.Find("fullhd");

    private static VideoTimeline Timeline(TransitionKind transition, VideoLook look = VideoLook.Natural) => new(
        Hd,
        30,
        [
            new PhotoClip(0, 3, TransitionKind.Cut, 0, look, "red", new Frame01(0, 0.2, 1, 0.6), new Frame01(0, 0.2, 1, 0.6)),
            new PhotoClip(2, 3, transition, 1, look, "blue", new Frame01(0, 0, 1, 1), new Frame01(0, 0, 1, 1)) { BlurredBackdrop = true },
        ],
        5,
        false,
        false,
        0,
        []);

    private static SKColor Center(FrameRenderer renderer, double t, int w = 320, int h = 180)
    {
        using var bitmap = renderer.RenderBitmap(w, h, t);
        return bitmap.GetPixel(w / 2, h / 2);
    }

    [Fact]
    public void PhotosFillTheFrameAndFadeInFromBlack()
    {
        var renderer = new FrameRenderer(Timeline(TransitionKind.Crossfade), new ColorPhotos());

        Center(renderer, 1.0).Red.Should().BeGreaterThan(240, "kırmızı fotoğraf kareyi kaplar");
        Center(renderer, 0).Red.Should().BeLessThan(10, "başta karadan açılır");
        Center(renderer, 3.5).Blue.Should().BeGreaterThan(200, "dikey fotoğraf ortada bütün görünür");
    }

    [Fact]
    public void CrossfadeMixesBothPhotosAtTheMiddle()
    {
        var mid = Center(new FrameRenderer(Timeline(TransitionKind.Crossfade), new ColorPhotos()), 2.5);
        mid.Red.Should().BeInRange(60, 200);
        mid.Blue.Should().BeInRange(60, 200);
    }

    [Theory]
    [InlineData(TransitionKind.DipToBlack)]
    [InlineData(TransitionKind.DipToWhite)]
    [InlineData(TransitionKind.Iris)]
    [InlineData(TransitionKind.PushLeft)]
    [InlineData(TransitionKind.PushUp)]
    [InlineData(TransitionKind.ZoomThrough)]
    [InlineData(TransitionKind.FocusShift)]
    [InlineData(TransitionKind.Cut)]
    public void EveryTransitionStartsWithTheFirstAndEndsWithTheSecond(TransitionKind kind)
    {
        var renderer = new FrameRenderer(Timeline(kind), new ColorPhotos());

        using (var start = renderer.RenderBitmap(320, 180, 2.02))
        {
            start.GetPixel(80, 45).Red.Should().BeGreaterThan(150, kind + ": başta ilk fotoğraf");
        }

        Center(renderer, 2.99).Blue.Should().BeGreaterThan(150, kind.ToString());
        if (kind == TransitionKind.DipToWhite)
        {
            var white = Center(renderer, 2.5);
            (white.Red + white.Green + white.Blue).Should().BeGreaterThan(700, "ortada beyaz");
        }
    }

    [Fact]
    public void LooksChangeColorsOnlyInTheVideo()
    {
        var gray = Center(new FrameRenderer(Timeline(TransitionKind.Crossfade, VideoLook.BlackWhite), new ColorPhotos()), 1);
        Math.Abs(gray.Red - gray.Green).Should().BeLessThan(3, "siyah-beyaz");
        Math.Abs(gray.Green - gray.Blue).Should().BeLessThan(3);
        gray.Red.Should().BeInRange(60, 110, "kırmızının parlaklığı korunur (kaydırma 0–1 ölçeğinde)");

        var warm = Center(new FrameRenderer(Timeline(TransitionKind.Crossfade, VideoLook.Warm), new ColorPhotos()), 1);
        warm.Green.Should().BeLessThan(20, "sıcak ton yeşili doyurmaz");
        warm.Red.Should().BeGreaterThan(240);

        var sepia = Center(new FrameRenderer(Timeline(TransitionKind.Crossfade, VideoLook.Sepia), new ColorPhotos()), 1);
        sepia.Red.Should().BeGreaterThan(sepia.Blue, "sepya sıcaktır");
        FrameRenderer.LookFilter(VideoLook.Natural).Should().BeNull();
    }

    [Fact]
    public void TitlesVignetteAndGrainRender()
    {
        var timeline = new VideoTimeline(
            VideoFormat.Find("reels"), 24, [new TitleClip(0, 3, TransitionKind.Cut, 0, VideoLook.Natural, "Ayşe & Mert", "13 HAZİRAN 2026")], 3, true, true, 0, []);
        var renderer = new FrameRenderer(timeline, new ColorPhotos());

        using var frame = renderer.RenderBitmap(180, 320, 1.5);
        var bright = 0;
        for (var y = 0; y < frame.Height; y += 2)
        {
            for (var x = 0; x < frame.Width; x += 2)
            {
                if (frame.GetPixel(x, y).Red > 150)
                {
                    bright++;
                }
            }
        }

        bright.Should().BeGreaterThan(5, "başlık yazısı açık renkle çizilir");
        frame.GetPixel(0, 0).Red.Should().BeLessThan(40, "köşeler vinyetle koyu");

        var buffer = new byte[180 * 320 * 4];
        renderer.RenderInto(buffer, 180, 320, 1.5);
        buffer.Should().Contain(b => b > 150);
        FluentActions.Invoking(() => renderer.RenderInto(new byte[10], 180, 320, 1)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MissingPhotosDoNotBreakTheFrame()
    {
        var timeline = new VideoTimeline(Hd, 30, [new PhotoClip(0, 2, TransitionKind.Cut, 0, VideoLook.Natural, "yok", new Frame01(0, 0, 1, 1), new Frame01(0, 0, 1, 1))], 2, false, false, 0, []);
        var color = Center(new FrameRenderer(timeline, new ColorPhotos()), 1);
        color.Alpha.Should().Be(255);
    }

    [Fact]
    public void FilesAreDecodedWithOrientation()
    {
        var path = Path.Combine(Path.GetTempPath(), "nexalbum-decode-" + Guid.NewGuid().ToString("N") + ".png");
        using (var image = ColorPhotos.Solid(800, 400, SKColors.Green))
        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var file = File.Create(path))
        {
            data.SaveTo(file);
        }

        try
        {
            using var decoded = FilePhotoSource.Decode(path, 400);
            decoded.Should().NotBeNull();
            decoded!.Width.Should().BeGreaterThanOrEqualTo(400);
            decoded.Width.Should().BeGreaterThan(decoded.Height);
            FilePhotoSource.Decode(path + ".yok", 100).Should().BeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
