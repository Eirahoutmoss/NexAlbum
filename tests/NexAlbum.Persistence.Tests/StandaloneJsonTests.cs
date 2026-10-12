using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Json;
using Xunit;

namespace NexAlbum.Persistence.Tests;

public class StandaloneJsonTests
{
    [Fact]
    public void FullProfileRoundTrips()
    {
        var profile = Samples.FullProfile();
        var json = ProfileJson.Serialize(profile);
        ProfileJson.Deserialize(json).Should().BeEquivalentTo(profile, o => o.ComparingRecordsByMembers());
        json.Should().Contain("\"$schema\": \"https://nexalbum.dev/schemas/profile-v1.json\"");
        json.Should().Contain("\"targetDPI\": 300");
    }

    [Fact]
    public void BuiltInProfilesRoundTripWithUnknownsKeptUnknown()
    {
        foreach (var profile in BuiltInProfiles.All)
        {
            ProfileJson.Deserialize(ProfileJson.Serialize(profile)).Should().BeEquivalentTo(profile, o => o.ComparingRecordsByMembers());
        }
    }

    [Fact]
    public void Decision03ExampleProfileIsReadable()
    {
        const string json = """
            {
              "$schema": "https://nexalbum.dev/schemas/profile-v1.json",
              "schemaVersion": 1,
              "id": "kullanici-profil-01",
              "name": "Fotoğrafçı Ahmet — 30×30 Layflat",
              "source": "user",
              "verified": true,
              "page": { "width": 300, "height": 300, "unit": "mm", "orientation": "square" },
              "spread": { "enabled": false },
              "bleed": { "top": 3, "right": 3, "bottom": 3, "left": 3, "unit": "mm", "verified": true },
              "safeArea": { "top": 5, "right": 5, "bottom": 5, "left": 5, "unit": "mm", "verified": true },
              "gutter": { "width": 0, "unit": "mm", "verified": true },
              "resolution": { "targetDPI": 300, "minDPI": 200 },
              "color": { "mode": "cmyk", "profile": "CoatedFOGRA39.icc", "profileEmbedded": false },
              "cover": { "type": "hardcover", "spine": { "width": 1.2, "unit": "cm", "calculationMethod": "manual" } },
              "warnings": [ { "level": "info", "message": "Kullanıcı tanımlı." } ]
            }
            """;

        var profile = ProfileJson.Deserialize(json);
        profile.Bleed.Value!.Value.Top.Should().Be(3);
        profile.Color.Mode.Should().Be(ColorMode.Cmyk, "okurken büyük/küçük harf duyarsız");
        profile.Cover!.Spine!.WidthMm.Should().BeApproximately(12, 1e-9, "cm → mm");
        ProfilePreflight.Check(profile).Contains("profile.cover-size.missing").Should().BeTrue();
    }

    [Fact]
    public void PartiallyEnteredEdgesAreRejectedNotCompleted()
    {
        const string json = """{ "id": "p", "name": "P", "bleed": { "top": 3 } }""";
        FluentActions.Invoking(() => ProfileJson.Deserialize(json))
            .Should().Throw<ProjectFormatException>().WithMessage("*bleed.right*");
    }

    [Fact]
    public void HalfPageSizeIsRejected()
    {
        const string json = """{ "id": "p", "name": "P", "page": { "width": 300 } }""";
        FluentActions.Invoking(() => ProfileJson.Deserialize(json))
            .Should().Throw<ProjectFormatException>().WithMessage("*page.height*");
    }

    [Fact]
    public void InvalidProfilesAreRejected()
    {
        FluentActions.Invoking(() => ProfileJson.Deserialize("""{ "name": "kimliksiz" }""")).Should().Throw<ProjectFormatException>().WithMessage("*id*");
        FluentActions.Invoking(() => ProfileJson.Deserialize("""{ "id": "p", "name": "P", "schemaVersion": 2 }"""))
            .Should().Throw<ProjectFormatException>().Which.Error.Should().Be(ProjectLoadError.TooNew);
        FluentActions.Invoking(() => ProfileJson.Deserialize("bozuk")).Should().Throw<ProjectFormatException>();
        FluentActions.Invoking(() => ProfileJson.Deserialize("null")).Should().Throw<ProjectFormatException>();
        FluentActions.Invoking(() => ProfileJson.Deserialize("""{ "id": "p", "name": "P", "resolution": { "targetDPI": -1 } }"""))
            .Should().Throw<ProjectFormatException>();
        FluentActions.Invoking(() => ProfileJson.Deserialize("""{ "id": "p", "name": "P", "color": { "mode": "pantone" } }"""))
            .Should().Throw<ProjectFormatException>().WithMessage("*pantone*");
    }

    [Fact]
    public void TemplateRoundTripsWithPercentUnits()
    {
        var template = Samples.FourGrid();
        var json = TemplateJson.Serialize(template);
        json.Should().Contain("\"unit\": \"%\"");
        json.Should().Contain("\"category\": \"grid\"");
        TemplateJson.Deserialize(json).Should().BeEquivalentTo(template, o => o.ComparingRecordsByMembers());
    }

    [Fact]
    public void AllBuiltInTemplatesRoundTrip()
    {
        foreach (var template in BuiltInTemplates.All)
        {
            TemplateJson.Deserialize(TemplateJson.Serialize(template)).Should().BeEquivalentTo(template, o => o.ComparingRecordsByMembers());
        }
    }

    [Fact]
    public void Decision10SlotFieldsRoundTrip()
    {
        var template = Samples.FourGrid() with
        {
            Description = "Bir büyük üstte",
            GutterPercent = 2,
            OuterMarginPercent = 5,
            Slots =
            [
                new TemplateSlot
                {
                    Id = "slot-main",
                    Rect = new PercentRect(0, 0, 100, 65),
                    CropMode = CropMode.Contain,
                    Priority = 1,
                    RecommendedOrientations = [PageOrientation.Portrait, PageOrientation.Landscape],
                    MinAspectRatio = 0.5,
                    MaxAspectRatio = 2,
                    Shape = SlotShape.Rounded,
                    CornerRadiusMm = 3,
                },
            ],
        };

        var json = TemplateJson.Serialize(template);
        json.Should().Contain("\"recommendedOrientation\": [");
        json.Should().Contain("\"outerMargin\": 5");
        TemplateJson.Deserialize(json).Should().BeEquivalentTo(template, o => o.ComparingRecordsByMembers());
    }

    [Fact]
    public void TemplateSlotsMustBePercent()
    {
        const string json = """
            { "id": "t", "name": "T", "photoCount": { "min": 1, "max": 1 },
              "slots": [ { "id": "s1", "x": 0, "y": 0, "width": 100, "height": 100, "unit": "mm" } ] }
            """;
        FluentActions.Invoking(() => TemplateJson.Deserialize(json)).Should().Throw<ProjectFormatException>().WithMessage("*%*");
    }

    [Fact]
    public void TemplateValidation()
    {
        FluentActions.Invoking(() => TemplateJson.Deserialize("""{ "id": "t", "name": "T" }""")).Should().Throw<ProjectFormatException>().WithMessage("*photoCount*");
        FluentActions.Invoking(() => TemplateJson.Deserialize("""{ "id": "t", "name": "T", "photoCount": { "min": 1, "max": 1 }, "schemaVersion": 5 }"""))
            .Should().Throw<ProjectFormatException>().Which.Error.Should().Be(ProjectLoadError.TooNew);
        var minimal = TemplateJson.Deserialize("""{ "id": "t", "name": "T", "photoCount": { "min": 0, "max": 0 } }""");
        minimal.Slots.Should().BeEmpty();
        minimal.Author.Should().Be("builtin");
    }
}
