using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Validation;
using Xunit;

namespace NexAlbum.Core.Tests.Profiles;

public class ProfileValidationTests
{
    private static AlbumProfile Valid => TestData.CompleteProfile();

    [Fact]
    public void CompleteVerifiedProfilePassesPreflight()
    {
        var result = ProfilePreflight.Check(Valid, _ => true);
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void MissingPageSizeIsAnError()
    {
        var result = ProfilePreflight.Check(Valid with { Page = new PageSpec() });
        result.Errors.Should().Contain(i => i.Code == "profile.page-size.missing");
    }

    [Fact]
    public void ZeroBleedIsAnEnteredValueNotMissing()
    {
        var result = ProfilePreflight.Check(Valid with { Bleed = new EdgeSpec(EdgesMm.Uniform(0)), SafeArea = new EdgeSpec(EdgesMm.Uniform(0)) });
        result.Contains("profile.bleed.missing").Should().BeFalse();
        result.Contains("profile.bleed.small").Should().BeTrue();
    }

    [Fact]
    public void MissingOptionalValuesAreWarnings()
    {
        var profile = Valid with
        {
            Resolution = ResolutionSpec.Unknown,
            Color = ColorSpec.Unknown,
            Verified = false,
        };

        var result = ProfilePreflight.Check(profile);
        result.HasErrors.Should().BeFalse();
        result.Warnings.Select(w => w.Code).Should().BeEquivalentTo(
            new[] { "profile.dpi.missing", "profile.color-mode.missing", "profile.unverified" });
    }

    [Fact]
    public void HardcoverWithoutSpineWidthIsWarned()
    {
        var profile = Valid with { Cover = new CoverSpec(CoverType.Hardcover) };
        var result = ProfilePreflight.Check(profile);
        result.Contains("profile.cover-size.missing").Should().BeTrue();
        result.Contains("profile.spine.missing").Should().BeTrue();
    }

    [Fact]
    public void CompleteCoverRaisesNoCoverWarnings()
    {
        var size = new SizeMm(300, 300);
        var profile = Valid with { Cover = new CoverSpec(CoverType.Hardcover, size, size, new SpineSpec(12)) };
        var result = ProfilePreflight.Check(profile);
        result.Contains("profile.cover-size.missing").Should().BeFalse();
        result.Contains("profile.spine.missing").Should().BeFalse();
    }

    [Fact]
    public void SoftcoverDoesNotRequireSpine()
    {
        var size = new SizeMm(300, 300);
        var profile = Valid with { Cover = new CoverSpec(CoverType.Softcover, size, size) };
        ProfilePreflight.Check(profile).Contains("profile.spine.missing").Should().BeFalse();
    }

    [Fact]
    public void MissingIccFileIsWarnedOnlyWhenCheckerProvided()
    {
        ProfilePreflight.Check(Valid, _ => false).Contains("profile.icc.not-found").Should().BeTrue();
        ProfilePreflight.Check(Valid).Contains("profile.icc.not-found").Should().BeFalse();
    }

    [Fact]
    public void BleedLargerThanPageIsAnError()
    {
        var profile = Valid with { Bleed = new EdgeSpec(EdgesMm.Uniform(300)), SafeArea = new EdgeSpec(EdgesMm.Uniform(300)) };
        ProfileValidator.Validate(profile).Contains("profile.bleed.too-large").Should().BeTrue();
    }

    [Fact]
    public void SafeAreaCoveringPageIsAnError()
    {
        var profile = Valid with { SafeArea = new EdgeSpec(EdgesMm.Uniform(150)) };
        ProfileValidator.Validate(profile).Contains("profile.safe-area.too-large").Should().BeTrue();
    }

    [Fact]
    public void SafeAreaSmallerThanBleedIsAnError()
    {
        var profile = Valid with { SafeArea = new EdgeSpec(new EdgesMm(5, 5, 2, 5)) };
        ProfileValidator.Validate(profile).Errors.Should().ContainSingle(i => i.Code == "profile.safe-area.smaller-than-bleed");
    }

    [Fact]
    public void GutterWiderThanPageIsAnError()
    {
        var profile = Valid with { Gutter = new GutterSpec(400) };
        ProfileValidator.Validate(profile).Contains("profile.gutter.too-large").Should().BeTrue();
    }

    [Fact]
    public void MinDpiAboveTargetIsAnError()
    {
        var profile = Valid with { Resolution = new ResolutionSpec(200, 300) };
        ProfileValidator.Validate(profile).Contains("profile.dpi.min-above-target").Should().BeTrue();
    }

    [Theory]
    [InlineData(100, "profile.dpi.low")]
    [InlineData(800, "profile.dpi.high")]
    public void ExtremeDpiIsWarned(int dpi, string code)
    {
        var profile = Valid with { Resolution = new ResolutionSpec(dpi) };
        var result = ProfileValidator.Validate(profile);
        result.HasErrors.Should().BeFalse();
        result.Warnings.Should().ContainSingle(i => i.Code == code);
    }

    [Fact]
    public void LargeSafeAreaIsWarned()
    {
        var profile = Valid with { SafeArea = new EdgeSpec(EdgesMm.Uniform(25)) };
        ProfileValidator.Validate(profile).Contains("profile.safe-area.large").Should().BeTrue();
    }

    [Fact]
    public void UnknownValuesAreNotValidatedAgainstEachOther()
    {
        var profile = new AlbumProfile { Id = "bos", Name = "Özel ölçü" };
        ProfileValidator.Validate(profile).Issues.Should().BeEmpty();
    }

    [Fact]
    public void SpecsRejectImpossibleValues()
    {
        FluentActions.Invoking(() => new GutterSpec(-1)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new ResolutionSpec(0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new ResolutionSpec(300, -5)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new SpineSpec(0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new PaperSpec(weightGsm: 0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new OutputSpec(cropMarkLengthMm: 0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => new OutputSpec(cropMarkOffsetMm: -1)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProfileRequiresIdentityAndNonNullSections()
    {
        FluentActions.Invoking(() => new AlbumProfile { Id = " ", Name = "x" }).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Valid with { Bleed = null! }).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => Valid with { Version = "" }).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SpecsKeepOptionalInformation()
    {
        var paper = new PaperSpec("matte", 250, "standart");
        paper.Type.Should().Be("matte");
        paper.WeightGsm.Should().Be(250);
        paper.Note.Should().Be("standart");

        var spine = new SpineSpec(12, SpineCalculationMethod.Manual, verified: true, note: "baskı evi");
        spine.Verified.Should().BeTrue();
        spine.Note.Should().Be("baskı evi");
        spine.Method.Should().Be(SpineCalculationMethod.Manual);

        var output = new OutputSpec(true, 3, 5, registrationMarks: true, colorBars: true, pageInfo: true);
        output.RegistrationMarks.Should().BeTrue();
        output.ColorBars.Should().BeTrue();
        output.PageInfo.Should().BeTrue();

        new GutterSpec(4, true, "not").Note.Should().Be("not");
        new ResolutionSpec(300, 200, "not").Note.Should().Be("not");
    }

    [Fact]
    public void ValidationResultHelpers()
    {
        var result = new ValidationResult([new ValidationIssue("a", ValidationSeverity.Info, "bilgi")]);
        result.HasErrors.Should().BeFalse();
        result.HasWarnings.Should().BeFalse();
        ValidationResult.Empty.Merge(result).Issues.Should().HaveCount(1);
    }
}
