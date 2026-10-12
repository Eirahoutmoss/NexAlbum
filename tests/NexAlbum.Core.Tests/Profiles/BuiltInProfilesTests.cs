using FluentAssertions;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Validation;
using Xunit;

namespace NexAlbum.Core.Tests.Profiles;

public class BuiltInProfilesTests
{
    [Fact]
    public void ProvidesTheEightSampleSizesFromDecision03()
    {
        BuiltInProfiles.All.Should().HaveCount(8);
        BuiltInProfiles.All.Select(p => p.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void SamplesInventNoPrintValues()
    {
        // Karar 03 §5.3: yalnızca sayfa ölçüsü vardır; gerisi kullanıcıya bırakılır.
        foreach (var p in BuiltInProfiles.All)
        {
            p.Source.Should().Be(ProfileSource.Builtin);
            p.Verified.Should().BeFalse();
            p.Page.Size.Should().NotBeNull();
            p.Bleed.Value.Should().BeNull();
            p.SafeArea.Value.Should().BeNull();
            p.Gutter.WidthMm.Should().BeNull();
            p.Resolution.TargetDpi.Should().BeNull();
            p.Resolution.MinDpi.Should().BeNull();
            p.Color.Mode.Should().BeNull();
            p.Color.IccProfile.Should().BeNull();
            p.Cover.Should().BeNull();
            p.Warnings.Should().ContainSingle(w => w.Message == BuiltInProfiles.SampleWarning);
        }
    }

    [Fact]
    public void SamplesCannotBeExportedWithoutUserValues()
    {
        foreach (var p in BuiltInProfiles.All)
        {
            var result = ProfilePreflight.Check(p);
            result.HasErrors.Should().BeTrue();
            result.Contains("profile.bleed.missing").Should().BeTrue();
            result.Contains("profile.safe-area.missing").Should().BeTrue();
        }
    }

    [Fact]
    public void LayflatSampleRequiresGutter()
    {
        var layflat = BuiltInProfiles.Find("builtin-30x30-layflat")!;
        layflat.Spread.Enabled.Should().BeTrue();
        ProfilePreflight.Check(layflat).Contains("profile.gutter.missing").Should().BeTrue();
    }

    [Fact]
    public void FindReturnsNullForUnknownId()
    {
        BuiltInProfiles.Find("yok").Should().BeNull();
        BuiltInProfiles.Find("builtin-20x30")!.Page.Size!.Value.Height.Should().Be(300);
    }

    [Fact]
    public void SampleProfilesAreInternallyConsistent()
    {
        foreach (var p in BuiltInProfiles.All)
        {
            ProfileValidator.Validate(p).Issues.Should().BeEmpty();
        }
    }
}
