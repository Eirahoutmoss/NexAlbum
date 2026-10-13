using FluentAssertions;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Video;
using Xunit;

namespace NexAlbum.Core.Tests.Video;

public class VideoPlannerTests
{
    private static readonly TimeSpan Istanbul = TimeSpan.FromHours(3);

    /// <summary>Dört bölümlük düğün: 4 + 6 + 5 + 5 fotoğraf; bazıları dikey, ikisi favori.</summary>
    private static AlbumProject Wedding()
    {
        var day = new DateTimeOffset(2026, 6, 13, 0, 0, 0, Istanbul);
        var photos = new List<PhotoAsset>();
        void Burst(string prefix, double hour, int count)
        {
            for (var i = 0; i < count; i++)
            {
                photos.Add(TestData.Asset($"{prefix}-{i}", i % 3 == 0 ? 4000 : 6000, i % 3 == 0 ? 6000 : 4000) with
                {
                    CapturedAt = day.AddHours(hour).AddMinutes(i * 5),
                    Favorite = prefix == "gelin" && i == 4 || prefix == "dugun" && i == 2,
                });
            }
        }

        Burst("hazirlik", 9, 4);
        Burst("gelin", 13, 6);
        Burst("dis", 16, 5);
        Burst("dugun", 21, 5);
        var project = TestData.Project().AddAssets(photos);
        return project with { Story = StoryDetector.Detect(project.Photos) };
    }

    [Fact]
    public void FilmTellsTheWholeStoryWithTitlesAndOverlappingTransitions()
    {
        var project = Wedding();
        var timeline = VideoPlanner.Plan(project, new VideoSettings { FormatId = "4k", StyleId = "cinematic", Fps = 24 });

        timeline.Format.Width.Should().Be(3840);
        timeline.Clips.OfType<PhotoClip>().Should().HaveCount(20, "film tüm hikâyeyi anlatır");
        timeline.Clips.OfType<TitleClip>().Select(t => t.Title).Should().Equal(project.Name, "Hazırlık", "Gelin Alma", "Dış Çekim", "Düğün");
        timeline.Clips.OfType<TitleClip>().First().Subtitle.Should().Be("13 HAZİRAN 2026");
        timeline.Clips[0].Start.Should().Be(0);

        for (var i = 1; i < timeline.Clips.Count; i++)
        {
            var previous = timeline.Clips[i - 1];
            var clip = timeline.Clips[i];
            (previous.End - clip.Start).Should().BeApproximately(clip.TransitionSeconds, 1e-9, "geçiş süresince iki öğe örtüşür");
        }

        timeline.Duration.Should().BeApproximately(timeline.Clips[^1].End, 1e-9);
        timeline.FrameCount.Should().Be((int)Math.Ceiling(timeline.Duration * 24));
        timeline.Vignette.Should().BeTrue();

        var favorite = timeline.Clips.OfType<PhotoClip>().Single(c => c.AssetId == "gelin-4");
        var ordinary = timeline.Clips.OfType<PhotoClip>().Single(c => c.AssetId == "gelin-3");
        (favorite.Duration - favorite.TransitionSeconds).Should().BeGreaterThan(ordinary.Duration - ordinary.TransitionSeconds - 0.01, "favori daha uzun kalır");

        VideoPlanner.Plan(project, new VideoSettings { FormatId = "4k", StyleId = "cinematic", Fps = 24 }).Should().BeEquivalentTo(timeline, o => o.RespectingRuntimeTypes(), "deterministik");
    }

    [Fact]
    public void SocialClipFitsTheTargetAndKeepsFavorites()
    {
        var timeline = VideoPlanner.Plan(Wedding(), new VideoSettings { FormatId = "reels", StyleId = "dynamic", TargetSeconds = 15 });

        timeline.Format.Height.Should().Be(1920);
        timeline.Duration.Should().BeInRange(10, 20);
        var photos = timeline.Clips.OfType<PhotoClip>().Select(c => c.AssetId).ToList();
        photos.Should().Contain(["gelin-4", "dugun-2"], "favoriler önce seçilir");
        photos.Should().Contain(p => p.StartsWith("hazirlik", StringComparison.Ordinal)).And.Contain(p => p.StartsWith("dis", StringComparison.Ordinal), "her bölümden fotoğraf");
        timeline.Clips.OfType<TitleClip>().Should().ContainSingle("kısa klipte yalnızca açılış başlığı");
        timeline.Notes.Should().Contain(n => n.Contains("Hedef 15 sn", StringComparison.Ordinal));
    }

    [Fact]
    public void PortraitPhotosInWideVideoGetABlurredBackdrop()
    {
        var timeline = VideoPlanner.Plan(Wedding(), new VideoSettings { FormatId = "fullhd" });
        var portrait = timeline.Clips.OfType<PhotoClip>().First(c => c.AssetId == "gelin-0");
        var wide = timeline.Clips.OfType<PhotoClip>().First(c => c.AssetId == "gelin-1");

        portrait.BlurredBackdrop.Should().BeTrue();
        wide.BlurredBackdrop.Should().BeFalse();
        wide.From.Should().NotBe(wide.To, "Ken Burns hareketi");
        foreach (var f in new[] { wide.From, wide.To })
        {
            f.X.Should().BeGreaterThanOrEqualTo(-1e-9);
            (f.X + f.Width).Should().BeLessThanOrEqualTo(1 + 1e-9);
            (f.Width * 6000 / (f.Height * 4000)).Should().BeApproximately(16.0 / 9, 1e-6, "pencere video oranındadır");
        }
    }

    [Fact]
    public void ReduceMotionAndManualOverridesAreRespected()
    {
        var project = Wedding();
        var chapter = project.Story.Chapters[2];
        var settings = new VideoSettings { FormatId = "fullhd", StyleId = "dynamic", ReduceMotion = true }
            .WithOverride(new ChapterVideoOverride(chapter.Id, TransitionKind.DipToWhite, VideoLook.BlackWhite));

        var timeline = VideoPlanner.Plan(project, settings);

        timeline.Clips.OfType<PhotoClip>().Should().OnlyContain(c => c.From == c.To, "hareket azaltma");
        timeline.Clips.Select(c => c.TransitionIn).Should().OnlyContain(t => t == TransitionKind.Cut || t == TransitionKind.Crossfade || t == TransitionKind.DipToBlack || t == TransitionKind.DipToWhite);
        timeline.Clips.OfType<PhotoClip>().Where(c => c.AssetId.StartsWith("dis", StringComparison.Ordinal)).Should().OnlyContain(c => c.Look == VideoLook.BlackWhite);
        timeline.Clips.OfType<TitleClip>().Single(t => t.Title == "Dış Çekim").TransitionIn.Should().Be(TransitionKind.DipToWhite);

        settings.WithOverride(new ChapterVideoOverride(chapter.Id)).Overrides.Should().BeEmpty("boş seçim elle ayarı kaldırır");
    }

    [Fact]
    public void CutsSnapToTheBeat()
    {
        var beats = Enumerable.Range(0, 400).Select(i => 0.5 + (i * 0.5)).ToList(); // 120 BPM, 0,5 sn sessiz giriş
        var grid = new BeatGrid(120, beats, 0.9, 0.5, 200);

        var timeline = VideoPlanner.Plan(Wedding(), new VideoSettings { FormatId = "fullhd", StyleId = "dynamic" }, grid);

        timeline.MusicOffset.Should().Be(0.5);
        foreach (var clip in timeline.Clips.Skip(1).Where(c => c.TransitionSeconds > 0))
        {
            var cut = clip.Start + (clip.TransitionSeconds / 2);
            Math.Abs((cut * 2) - Math.Round(cut * 2)).Should().BeLessThan(1e-6, "kesme yarım saniyelik vuruş ızgarasında");
        }

        timeline.Notes.Should().Contain(n => n.Contains("120 BPM", StringComparison.Ordinal));
    }

    [Fact]
    public void TimelineReportsWhatIsOnScreen()
    {
        var timeline = VideoPlanner.Plan(Wedding(), new VideoSettings { FormatId = "fullhd" });
        var second = timeline.Clips[1];

        timeline.At(0.1).Current.Should().Be(timeline.Clips[0]);
        timeline.At(0.1).Next.Should().BeNull();
        var (current, next, progress) = timeline.At(second.Start + (second.TransitionSeconds / 2));
        current.Should().Be(timeline.Clips[0]);
        next.Should().Be(second);
        progress.Should().BeApproximately(0.5, 1e-6);

        VideoPlanner.Plan(TestData.Project(), new VideoSettings()).Clips.Should().BeEmpty();
    }
}
