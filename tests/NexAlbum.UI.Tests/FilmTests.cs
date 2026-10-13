using FluentAssertions;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Video;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Hikâye filmi penceresinin görünüm modeli (Karar 21).</summary>
[Collection(WpfApp.Collection)]
public class FilmTests
{
    private static AlbumProject Wedding()
    {
        var day = new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.FromHours(3));
        var photos = Enumerable.Range(0, 12).Select(i => new PhotoAsset
        {
            Id = $"p{i:00}",
            SourcePath = $@"C:\yok\p{i:00}.jpg",
            FileName = $"p{i:00}.jpg",
            PixelWidth = 6000,
            PixelHeight = 4000,
            CapturedAt = day.AddHours(i < 6 ? 10 : 20).AddMinutes(i * 3),
            ImportedAt = day,
        });
        var project = AlbumProject.Create("Ayşe & Mert", Core.Profiles.BuiltInProfiles.All[0], day, "0.1.0").AddAssets(photos);
        return project with { Story = StoryDetector.Detect(project.Photos) };
    }

    [Fact]
    public void SettingsArePlannedPreviewedAndSaved()
    {
        WpfApp.Run(() =>
        {
            VideoSettings? saved = null;
            var app = new AppSettings();
            using var film = new VideoViewModel(Wedding(), s => saved = s, () => app, s => app = s);

            film.SelectedFormat.Id.Should().Be("reels", "varsayılan sosyal dikey");
            film.SelectedDuration.Seconds.Should().Be(30);
            film.Chapters.Select(c => c.Name).Should().Equal("Hazırlık", "Düğün");
            film.Summary.Should().Contain("1080×1920");
            film.TimeText.Should().StartWith("0:00 / ");
            film.FfmpegStatus.Should().NotBeNullOrWhiteSpace();

            film.SelectedFormat = VideoFormat.Find("4k");
            saved!.FormatId.Should().Be("4k");
            saved.TargetSeconds.Should().BeNull("filme geçince tüm hikâye");
            film.SelectedDuration.Name.Should().Be("Tüm hikâye");
            film.Timeline.Clips.OfType<PhotoClip>().Should().HaveCount(12);

            film.SelectedStyle = VideoStyle.Find("nostalgia");
            film.Timeline.Grain.Should().BeTrue();
            film.SelectedFps = 24;
            film.Timeline.Fps.Should().Be(24);

            film.Chapters[1].Transition = VideoViewModel.TransitionChoices.Single(c => c.Value == TransitionKind.Iris);
            film.Chapters[1].Look = VideoViewModel.LookChoices.Single(c => c.Value == VideoLook.BlackWhite);
            saved.Overrides.Should().ContainSingle();
            saved.Overrides[0].Transition.Should().Be(TransitionKind.Iris);
            saved.Overrides[0].Look.Should().Be(VideoLook.BlackWhite);
            film.Chapters[1].Transition.Value.Should().Be(TransitionKind.Iris);
            film.Chapters[1].Transition = VideoViewModel.TransitionChoices[0];
            film.Chapters[1].Look = VideoViewModel.LookChoices[0];
            saved.Overrides.Should().BeEmpty("«Otomatik» elle ayarı kaldırır");

            film.ReduceMotion = true;
            film.Timeline.Clips.OfType<PhotoClip>().Should().OnlyContain(c => c.From == c.To);

            film.PreviewSeconds = film.Duration / 2;
            film.RenderPreviewNow();
            film.Preview.Should().NotBeNull();
            var (w, h) = film.PreviewSize;
            film.Preview!.PixelWidth.Should().Be(w);
            (w / (double)h).Should().BeApproximately(16.0 / 9, 0.02);
            film.SelectedTrack.Title.Should().Be("Müziksiz");
            film.MusicStatus.Should().Contain("Müziksiz");
        });
    }
}

/// <summary>Ana pencereden film: komut, ayarların projeye yazılması ve geri alma.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void FilmSettingsLiveInTheProjectAndAreUndoable()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.FilmCommand.CanExecute(null).Should().BeFalse("proje yok");
        vm.CreateFilm().Should().BeNull();

        vm.Open(PagedProject(2));
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.FilmCommand.CanExecute(null).Should().BeTrue();
        VideoViewModel? opened = null;
        vm.FilmRequested += (_, f) => opened = f;
        vm.OpenFilm();
        opened.Should().NotBeNull();
        opened!.Dispose();

        vm.SaveVideoSettings(new VideoSettings { FormatId = "2k", StyleId = "cinematic" });
        vm.Project!.Video.FormatId.Should().Be("2k");
        vm.IsDirty.Should().BeTrue();
        vm.UndoToolTip.Should().Contain("Film ayarları");
        vm.UndoCommand.Execute(null);
        vm.Project!.Video.Should().Be(VideoSettings.Default);
    }
}
