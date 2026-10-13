using FluentAssertions;
using NexAlbum.Core.Layout;
using NexAlbum.Core.Projects;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class CameraClocksTests
{
    private static readonly DateTimeOffset Day = new(2026, 6, 13, 0, 0, 0, TimeSpan.FromHours(3));

    /// <summary>
    /// İki makine aynı olayı çekiyor: A'nın saati doğru, B'nin saati 1 sa 7 dk geri. Anlar düzensiz aralıklarla (gerçek bir
    /// günün yoğunlaşmaları gibi); B her anın bir kısmını çeker.
    /// </summary>
    private static AlbumProject TwoCameras()
    {
        var moments = new[] { 9 * 60 + 2, 9 * 60 + 11, 9 * 60 + 37, 10 * 60 + 4, 11 * 60 + 48, 12 * 60 + 15, 13 * 60 + 3, 13 * 60 + 41, 15 * 60 + 20, 16 * 60 + 9, 17 * 60 + 55, 19 * 60 + 30 };
        var assets = new List<PhotoAsset>();
        for (var i = 0; i < moments.Length; i++)
        {
            var at = Day.AddMinutes(moments[i]);
            assets.Add(TestData.Asset($"a-{i:00}") with { CapturedAt = at, CameraModel = "Canon EOS R5" });
            assets.Add(TestData.Asset($"a-{i:00}b") with { CapturedAt = at.AddSeconds(20), CameraModel = "Canon EOS R5" });
            if (i % 3 != 2)
            {
                assets.Add(TestData.Asset($"b-{i:00}") with { CapturedAt = at.AddMinutes(-67).AddSeconds(15), CameraModel = "NIKON Z 6_2" });
            }
        }

        assets.Add(TestData.Asset("telefon") with { CapturedAt = Day.AddHours(14) });
        return TestData.Project().AddAssets(assets);
    }

    [Fact]
    public void CamerasAreListedByPhotoCount()
    {
        var cameras = CameraClocks.Cameras(TwoCameras());

        cameras.Select(c => c.Key).Should().Equal("Canon EOS R5", "NIKON Z 6_2", CameraClocks.UnknownCamera);
        cameras[0].Count.Should().Be(24);
        cameras.Should().OnlyContain(c => c.Offset == TimeSpan.Zero);
    }

    [Fact]
    public void OffsetShiftsOnlyThatCameraAndKeepsTheOriginalTime()
    {
        var project = TwoCameras();
        var nikonBefore = project.Assets.First(a => a.Id == "b-00").CapturedAt;

        var fixedProject = project.WithOffset("NIKON Z 6_2", TimeSpan.FromMinutes(67));

        var nikon = fixedProject.Assets.First(a => a.Id == "b-00");
        nikon.CapturedAt.Should().Be(nikonBefore!.Value.AddMinutes(67));
        nikon.OriginalCapturedAt.Should().Be(nikonBefore);
        fixedProject.Assets.First(a => a.Id == "a-00").Should().Be(project.Assets.First(a => a.Id == "a-00"));

        // Düzeltmeyi değiştirmek birikmez: orijinalden yeniden hesaplanır; sıfırlamak eski hale döndürür.
        var again = fixedProject.WithOffset("NIKON Z 6_2", TimeSpan.FromMinutes(60)).Assets.First(a => a.Id == "b-00");
        again.CapturedAt.Should().Be(nikonBefore.Value.AddMinutes(60));
        fixedProject.WithOffset("NIKON Z 6_2", TimeSpan.Zero).Assets.Should().Equal(project.Assets);
        fixedProject.WithOffset("NIKON Z 6_2", TimeSpan.FromMinutes(67)).Should().BeSameAs(fixedProject);
    }

    [Fact]
    public void PhotosAddedLaterGetTheCameraOffset()
    {
        var project = TwoCameras().WithOffset("NIKON Z 6_2", TimeSpan.FromMinutes(67));
        var late = TestData.Asset("b-gec") with { CapturedAt = Day.AddHours(20), CameraModel = "NIKON Z 6_2" };

        var added = project.AddAssets([late]).Assets.Single(a => a.Id == "b-gec");

        added.ClockOffset.Should().Be(TimeSpan.FromMinutes(67));
        added.CapturedAt.Should().Be(Day.AddHours(20).AddMinutes(67));
    }

    [Fact]
    public void SuggestionFindsTheShiftBetweenTwoCameras()
    {
        var suggestions = CameraClocks.SuggestAll(TwoCameras());

        suggestions.Should().ContainKey("NIKON Z 6_2").WhoseValue.Should().Be(TimeSpan.FromMinutes(67));
        suggestions.Should().NotContainKey(CameraClocks.UnknownCamera, "tek fotoğrafla öneri yapılmaz");
    }

    [Fact]
    public void NoSuggestionWithoutClearOverlap()
    {
        var reference = Enumerable.Range(0, 10).Select(i => Day.AddHours(9).AddMinutes(i * 3)).ToList();
        var target = Enumerable.Range(0, 10).Select(i => Day.AddHours(9).AddMinutes(i * 3).AddSeconds(10)).ToList();

        CameraClocks.SuggestOffset(reference, target).Should().Be(TimeSpan.Zero, "saatler zaten aynı");
        CameraClocks.SuggestOffset(reference, target.Take(2)).Should().BeNull("çok az fotoğraf");
    }

    [Fact]
    public void MatchingTwoPhotosGivesTheOffset()
    {
        var project = TwoCameras();
        var reference = project.Assets.First(a => a.Id == "a-01");
        var target = project.Assets.First(a => a.Id == "b-01");

        CameraClocks.OffsetToMatch(reference, target).Should().Be(TimeSpan.FromMinutes(67) - TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void StoryUsesCorrectedTimes()
    {
        // Nikon'un saati 1 saat geri: düzeltmeden önce sabah fotoğrafları ayrı bir "erken" bölüm gibi görünür.
        var project = TwoCameras();
        var fixedProject = project.WithOffset("NIKON Z 6_2", TimeSpan.FromMinutes(67));

        var plan = StoryDetector.Detect(fixedProject.Photos);
        var nikonMorning = fixedProject.Assets.First(a => a.Id == "b-00");
        var canonMorning = fixedProject.Assets.First(a => a.Id == "a-00");

        plan.ChapterOf(nikonMorning).Should().Be(plan.ChapterOf(canonMorning));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(67, "+1 sa 7 dk")]
    [InlineData(-12, "−12 dk")]
    [InlineData(1500, "+1 g 1 sa")]
    public void OffsetIsFormattedShortly(int minutes, string expected) =>
        CameraClocks.Format(TimeSpan.FromMinutes(minutes)).Should().Be(expected);

    [Theory]
    [InlineData("+1:07", 67 * 60)]
    [InlineData("-0:12", -12 * 60)]
    [InlineData("1:07:30", (67 * 60) + 30)]
    [InlineData("67", 67 * 60)]
    [InlineData("−12 dk", -12 * 60)]
    [InlineData("+1 sa 7 dk", 67 * 60)]
    [InlineData("2 g", 2 * 24 * 3600)]
    [InlineData("0", 0)]
    [InlineData("1,5", 90)]
    public void TypedOffsetsAreRead(string text, int seconds)
    {
        CameraClocks.TryParse(text, out var offset).Should().BeTrue();
        offset.Should().Be(TimeSpan.FromSeconds(seconds));
        CameraClocks.TryParse(CameraClocks.Format(offset), out var back).Should().BeTrue("gösterim geri okunabilir");
        back.Should().Be(offset);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1:75")]
    [InlineData("3 hafta")]
    [InlineData("+")]
    [InlineData("40 g")]
    public void InvalidOffsetsAreRejected(string text) => CameraClocks.TryParse(text, out _).Should().BeFalse();
}
