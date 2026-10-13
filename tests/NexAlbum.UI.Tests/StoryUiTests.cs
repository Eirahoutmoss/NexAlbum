using System.IO;
using FluentAssertions;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Hikâye sekmesi: bölümleri bul, adlandır, birleştir, böl, galeride süz, bölümlere göre oluştur.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public async Task StoryChaptersAreDetectedEditedAndUsedForTheAlbum()
    {
        var path = WeddingProject();
        using var vm = new MainViewModel(new FakeDialogs { SavePath = path }, settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(path);
        vm.HasStory.Should().BeFalse();

        vm.DetectStory();
        vm.Chapters.Select(c => c.Name).Should().Equal("Sabah", "Öğle", "Öğleden sonra", "Akşam");
        vm.Chapters[0].PhotoCount.Should().Be(6);
        vm.Chapters[0].Summary.Should().Contain("09:00").And.Contain("6 fotoğraf");
        vm.Chapters[0].Preview.Should().HaveCount(5);
        vm.Timeline.Should().NotBeEmpty();
        vm.StorySummary.Should().Contain("4 bölüm").And.Contain("1 fotoğrafın çekim zamanı yok");
        vm.PanelTabIndex.Should().Be(MainViewModel.StoryTabIndex);
        vm.Assets.Where(a => a.Asset.CapturedAt is not null).Should().OnlyContain(a => a.ChapterColor != null);

        vm.SelectedChapter = vm.Chapters[1];
        vm.GalleryFilter.Should().Be(GalleryFilter.Chapter);
        vm.GalleryRows.SelectMany(r => r.Items).Should().HaveCount(6).And.OnlyContain(i => i.Asset.Id.StartsWith("gelin", StringComparison.Ordinal));
        vm.IsFilterAll = true;
        vm.SelectedChapter.Should().BeNull("başka süzgeç seçilince bölüm süzgeci kalkar");

        vm.Chapters[3].Name = "Akşam ve Kutlama";
        vm.Chapters[3].Name.Should().Be("Akşam ve Kutlama");
        vm.MergeChapter(vm.Chapters[2]);
        vm.Chapters.Should().HaveCount(3);
        vm.Chapters[1].PhotoCount.Should().Be(12, "dış çekim gelin almaya katıldı");

        vm.SelectedAsset = vm.Assets.Single(a => a.Asset.Id == "dis-00");
        vm.SplitStoryHere();
        vm.Chapters.Select(c => c.Name).Should().Equal("Sabah", "Öğle", "Öğleden sonra", "Akşam ve Kutlama");

        vm.StoryOpeners = true;
        await vm.BuildFromStoryCommand.ExecuteAsync(null);
        vm.Pages.Where(p => p.HasChapterLabel).Select(p => p.ChapterLabel).Should().Equal("Sabah", "Öğle", "Öğleden sonra", "Akşam ve Kutlama");
        vm.Pages.First().Texts.Should().ContainSingle().Which.Content.Should().Be("Sabah", "ilk sayfa bölüm açılışı");

        vm.Save(saveAs: false).Should().BeTrue();
        var saved = ProjectPackageReader.Load(path).Project.Story;
        saved.Chapters.Select(c => c.Name).Should().Equal("Sabah", "Öğle", "Öğleden sonra", "Akşam ve Kutlama");
        saved.Openers.Should().BeTrue();

        vm.UndoCommand.Execute(null);
        vm.UndoCommand.Execute(null);
        vm.StoryOpeners.Should().BeFalse("geri al: önce oluşturma, sonra açılış ayarı");
        vm.ClearStory();
        vm.HasStory.Should().BeFalse();
        vm.Pages.Should().NotContain(p => p.HasChapterLabel);
    }

    [Fact]
    public void ReadyNamesAreAppliedFromTheMenu()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(WeddingProject());
        vm.DetectStory();
        vm.Chapters[0].Suggestions.Select(x => x.Name).Should().Contain("Kutlama");

        vm.Chapters[0].Suggestions.Single(x => x.Name == "Kutlama").ApplyCommand.Execute(null);

        vm.Chapters[0].Name.Should().Be("Kutlama");
        vm.UndoToolTip.Should().Contain("Bölüm adı");
    }

    [Fact]
    public void StoryNeedsCaptureTimes()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(2));
        vm.DetectStory();
        vm.HasStory.Should().BeFalse();
        vm.Status.Should().Contain("çekim zamanı");
        vm.SplitStoryHereCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void CameraClocksAreShiftedSyncedAndUndone()
    {
        var path = Path.Combine(dir, "iki-makine.albumproj");
        var day = new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.FromHours(3));
        int[] moments = [542, 551, 577, 604, 708, 735, 783, 821, 920, 969, 1075, 1170];
        var photos = new List<PhotoAsset>();
        for (var i = 0; i < moments.Length; i++)
        {
            photos.Add(Photo($"a-{i:00}", day.AddMinutes(moments[i]), "Canon EOS R5"));
            photos.Add(Photo($"a-{i:00}b", day.AddMinutes(moments[i]).AddSeconds(20), "Canon EOS R5"));
            if (i % 3 != 2)
            {
                photos.Add(Photo($"b-{i:00}", day.AddMinutes(moments[i] - 67).AddSeconds(15), "NIKON Z 6_2"));
            }
        }

        var project = SampleProject(0).AddAssets(photos);
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion });
        using var vm = new MainViewModel(new FakeDialogs { SavePath = path }, settings: new MemoryAppSettingsStore());
        vm.History.MergeWindow = TimeSpan.Zero;
        vm.Open(path);

        vm.CameraClockItems.Select(c => c.Name).Should().Equal("Canon EOS R5", "NIKON Z 6_2");
        vm.HasSeveralCameras.Should().BeTrue();

        // Elle: yazılan fark uygulanır, okunamayan yazı eskiye döner.
        vm.CameraClockItems[1].Offset = "+1:00";
        vm.CameraClockItems[1].Offset.Should().Be("+1 sa");
        vm.Project!.Assets.Single(a => a.Id == "b-00").CapturedAt.Should().Be(day.AddMinutes(542 - 7).AddSeconds(15));
        vm.CameraClockItems[1].Offset = "yarın";
        vm.Status.Should().Contain("okunamadı");
        vm.CameraClockItems[1].Offset.Should().Be("+1 sa");

        // Otomatik eşitleme gerçek farkı bulur; geri alınabilir.
        vm.SyncClocksCommand.Execute(null);
        vm.CameraClockItems[1].Offset.Should().Be("+1 sa 7 dk");
        vm.UndoToolTip.Should().Contain("Saatleri eşitle");
        vm.UndoCommand.Execute(null);
        vm.CameraClockItems[1].Offset.Should().Be("+1 sa");

        // Aynı anın iki fotoğrafı: başvuru + eşitle.
        vm.SelectedAsset = vm.Assets.Single(a => a.Asset.Id == "a-01");
        vm.MarkClockReferenceCommand.Execute(null);
        vm.SelectedAsset = vm.Assets.Single(a => a.Asset.Id == "b-01");
        vm.MatchClockToReferenceCommand.CanExecute(null).Should().BeTrue();
        vm.MatchClockToReferenceCommand.Execute(null);
        vm.Project!.Assets.Single(a => a.Id == "b-01").CapturedAt.Should().Be(day.AddMinutes(551));

        vm.CameraClockItems[1].ResetCommand.Execute(null);
        vm.CameraClockItems[1].IsShifted.Should().BeFalse();

        vm.CameraClockItems[1].HourForwardCommand.Execute(null);
        vm.Save(saveAs: false).Should().BeTrue();
        ProjectPackageReader.Load(path).Project.Assets.Single(a => a.Id == "b-00").ClockOffset.Should().Be(TimeSpan.FromHours(1));

        static PhotoAsset Photo(string id, DateTimeOffset at, string camera) => new()
        {
            Id = id,
            SourcePath = $@"C:\yok\{id}.jpg",
            FileName = id + ".jpg",
            PixelWidth = 6000,
            PixelHeight = 4000,
            CapturedAt = at,
            CameraModel = camera,
            ImportedAt = at,
        };
    }

    private string WeddingProject()
    {
        var path = Path.Combine(dir, "dugun.albumproj");
        var day = new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.FromHours(3));
        var photos = new List<PhotoAsset>();
        void Burst(string prefix, double hour, int count)
        {
            for (var i = 0; i < count; i++)
            {
                photos.Add(new PhotoAsset
                {
                    Id = $"{prefix}-{i:00}",
                    SourcePath = $@"C:\yok\{prefix}-{i:00}.jpg",
                    FileName = $"{prefix}-{i:00}.jpg",
                    PixelWidth = i % 2 == 0 ? 6000 : 4000,
                    PixelHeight = i % 2 == 0 ? 4000 : 6000,
                    CapturedAt = day.AddHours(hour).AddMinutes(i * 4),
                    ImportedAt = day,
                });
            }
        }

        Burst("hazirlik", 9, 6);
        Burst("gelin", 13, 6);
        Burst("dis", 16, 6);
        Burst("dugun", 21, 7);
        photos.Add(new PhotoAsset { Id = "zamansiz", SourcePath = @"C:\yok\z.jpg", FileName = "z.jpg", PixelWidth = 6000, PixelHeight = 4000, ImportedAt = day });
        var project = SampleProject(0).AddAssets(photos);
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion });
        return path;
    }
}
