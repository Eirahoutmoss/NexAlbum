using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

public class ProfileFormTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuiltInProfileStartsEmptyAndBecomesAUserProfile()
    {
        var form = new ProfileForm(BuiltInProfiles.Find("builtin-30x30")!);
        form.BleedTop.Should().BeEmpty("örnek profilde değer yok; form da uydurmaz");
        form.ColorMode.Mode.Should().BeNull();

        form.BleedTop = form.BleedRight = form.BleedBottom = form.BleedLeft = "3";
        form.SafeTop = form.SafeRight = form.SafeBottom = form.SafeLeft = "5,5";
        form.TargetDpi = "300";
        form.MinDpi = "200";
        form.ColorMode = ProfileForm.ColorModes.Single(o => o.Mode == ColorMode.Cmyk);
        form.IccProfile = " CoatedFOGRA39.icc ";
        form.CropMarks = true;
        form.Verified = true;

        var profile = form.TryBuild(Now, out var errors, out var warnings);
        errors.Should().BeEmpty();
        warnings.Should().BeEmpty();
        profile.Should().NotBeNull();
        profile!.Source.Should().Be(ProfileSource.User);
        profile.Id.Should().Be("kullanici-30x30");
        profile.Bleed.Value.Should().Be(EdgesMm.Uniform(3));
        profile.SafeArea.Value.Should().Be(EdgesMm.Uniform(5.5), "virgül ondalık ayırıcı olarak kabul edilir");
        profile.Resolution.TargetDpi.Should().Be(300);
        profile.Color.IccProfile.Should().Be("CoatedFOGRA39.icc");
        profile.Output.CropMarks.Should().BeTrue();
        profile.Verified.Should().BeTrue();
        profile.VerifiedAt.Should().Be(Now);
        profile.Version.Should().Be("1.1.0");
        profile.Warnings.Should().BeEmpty("örnek profil uyarısı kullanıcı profiline taşınmaz");
        ProfilePreflight.Check(profile).HasErrors.Should().BeFalse();
    }

    [Fact]
    public void PartialEdgesAreRejectedNotCompleted()
    {
        var form = new ProfileForm(BuiltInProfiles.All[0]) { BleedTop = "3" };
        form.TryBuild(Now, out var errors, out _).Should().BeNull();
        errors.Should().ContainSingle().Which.Should().Contain("dört kenar");
    }

    [Theory]
    [InlineData("abc", "", "")]
    [InlineData("", "-1", "")]
    [InlineData("", "", "0")]
    public void InvalidNumbersAreReported(string gutter, string bleed, string dpi)
    {
        var form = new ProfileForm(BuiltInProfiles.All[0]) { Gutter = gutter, TargetDpi = dpi };
        if (bleed.Length > 0)
        {
            form.BleedTop = form.BleedRight = form.BleedBottom = form.BleedLeft = bleed;
        }

        form.TryBuild(Now, out var errors, out _).Should().BeNull();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void LogicalErrorsBlockAndWarningsAreReturned()
    {
        var form = new ProfileForm(BuiltInProfiles.All[0]);
        form.BleedTop = form.BleedRight = form.BleedBottom = form.BleedLeft = "5";
        form.SafeTop = form.SafeRight = form.SafeBottom = form.SafeLeft = "3";
        form.TryBuild(Now, out var errors, out _).Should().BeNull();
        errors.Should().Contain(e => e.Contains("taşma payından küçük", StringComparison.Ordinal));

        form.SafeTop = form.SafeRight = form.SafeBottom = form.SafeLeft = "6";
        form.BleedTop = form.BleedRight = form.BleedBottom = form.BleedLeft = "1";
        var profile = form.TryBuild(Now, out errors, out var warnings);
        profile.Should().NotBeNull();
        errors.Should().BeEmpty();
        warnings.Should().ContainSingle().Which.Should().Contain("çok küçük");
    }

    [Fact]
    public void EmptyNameIsRejected()
    {
        var form = new ProfileForm(BuiltInProfiles.All[0]) { Name = "  " };
        form.TryBuild(Now, out var errors, out _).Should().BeNull();
        errors.Should().Contain("Profil adı boş olamaz.");
    }

    [Theory]
    [InlineData("1.0.0", "1.1.0")]
    [InlineData("2.5.3", "2.6.0")]
    [InlineData("tuhaf", "1.1.0")]
    public void VersionIsBumpedOnEdit(string version, string expected)
    {
        ProfileForm.NextVersion(version).Should().Be(expected);
    }

    [Theory]
    [InlineData("3", 3)]
    [InlineData("3,5", 3.5)]
    [InlineData(" 2.25 ", 2.25)]
    public void NumbersAcceptCommaOrDot(string text, double expected)
    {
        ProfileForm.TryParseNumber(text, out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    [Fact]
    public void ExistingUserProfileKeepsIdentityAndUnverifiesWhenUnchecked()
    {
        var user = new AlbumProfile
        {
            Id = "benim",
            Name = "Benim",
            Verified = true,
            VerifiedAt = Now.AddDays(-1),
            Page = new PageSpec(new SizeMm(300, 300)),
            Bleed = new EdgeSpec(EdgesMm.Uniform(3)),
            SafeArea = new EdgeSpec(EdgesMm.Uniform(5)),
        };
        var form = new ProfileForm(user);
        form.PageSize.Should().Contain("300");
        form.Verified = false;
        var profile = form.TryBuild(Now, out _, out _)!;
        profile.Id.Should().Be("benim");
        profile.Verified.Should().BeFalse();
        profile.VerifiedAt.Should().BeNull();
    }
}
