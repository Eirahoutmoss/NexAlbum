using FluentAssertions;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;
using Xunit;

namespace NexAlbum.Core.Tests.Projects;

public class PhotoEditTests
{
    private static readonly PreflightEnvironment AllPresent = new() { IccProfileExists = _ => true, FontAvailable = _ => true };

    private static PhotoEdit Edit(string path = @"C:\duzenli\a1-sicak-ton.jpg", int width = 6000, int height = 4000, bool missing = false) => new()
    {
        Path = path,
        PixelWidth = width,
        PixelHeight = height,
        Tool = "Photoshop",
        Recipe = "Stüdyo / Sıcak Ton",
        CreatedAt = TestData.Now,
        Missing = missing,
    };

    [Fact]
    public void EffectiveSourceFollowsTheEditOnlyWhenActive()
    {
        var asset = TestData.Asset("a1", 4000, 6000, orientation: 6) with { Edit = Edit(width: 3000, height: 2000) };

        var effective = asset.Effective();
        effective.SourcePath.Should().Be(@"C:\duzenli\a1-sicak-ton.jpg");
        effective.DisplayWidth.Should().Be(3000, "düzenlenmiş dosyanın yönü ayrıca okunur; EXIF yönü taşınmaz");
        effective.Id.Should().Be("a1");
        effective.FileName.Should().Be(asset.FileName, "galeride orijinal adı görünür");

        var off = asset with { UseEdit = false };
        off.Effective().Should().BeSameAs(off, "önce/sonra: orijinal gösterilir");
        off.IsEditActive.Should().BeFalse();
        (asset with { Edit = Edit(missing: true) }).Effective().SourcePath.Should().Be(asset.SourcePath, "kayıp düzenlemede orijinal kullanılır");
        TestData.Asset("x").Effective().Should().Be(TestData.Asset("x"));
    }

    [Fact]
    public void EditOfAMissingOriginalStillRenders()
    {
        var asset = TestData.Asset("gone", missing: true) with { Edit = Edit() };
        asset.Effective().Missing.Should().BeFalse();
    }

    [Fact]
    public void ApplyRemoveAndToggle()
    {
        var project = TestData.Project().AddAsset(TestData.Asset("a1"));

        project = PhotoEdits.Apply(project, "a1", Edit());
        project.FindAsset("a1")!.IsEditActive.Should().BeTrue();

        project = PhotoEdits.SetUseEdit(project, "a1", use: false);
        project.FindAsset("a1")!.IsEditActive.Should().BeFalse();
        PhotoEdits.SetUseEdit(project, "a1", use: false).Should().BeSameAs(project);

        project = PhotoEdits.Apply(project, "a1", Edit(path: @"C:\duzenli\yeni.jpg"));
        project.FindAsset("a1")!.UseEdit.Should().BeTrue("yeni düzenleme etkin gelir");

        project = PhotoEdits.Remove(project, "a1");
        project.FindAsset("a1")!.Edit.Should().BeNull();
        PhotoEdits.Remove(project, "a1").Should().BeSameAs(project);
        FluentActions.Invoking(() => PhotoEdits.Remove(project, "yok")).Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void MissingEditFilesAreDetected()
    {
        var project = TestData.Project().AddAssets([TestData.Asset("a1") with { Edit = Edit() }, TestData.Asset("a2")]);
        project = PhotoEdits.RefreshMissing(project, _ => false);
        project.FindAsset("a1")!.Edit!.Missing.Should().BeTrue();
        PhotoEdits.RefreshMissing(project, _ => false).Should().BeSameAs(project);
        PhotoEdits.RefreshMissing(project, _ => true).FindAsset("a1")!.Edit!.Missing.Should().BeFalse();
    }

    [Fact]
    public void PreflightUsesTheEffectiveSource()
    {
        var page = TestData.Page("p1", TestData.Slot("s1", assetId: "tiny"), TestData.Slot("s2", x: 300, assetId: "gone"));
        var baseProject = TestData.Project().AddAssets([TestData.Asset("tiny", 600, 400), TestData.Asset("gone", missing: true)]).AddPage(page);

        var before = ProjectPreflight.Check(baseProject, AllPresent);
        before.Issues.Should().Contain(i => i.Code == "project.photo.dpi-critical" || i.Code == "project.photo.dpi-low");
        before.Issues.Should().Contain(i => i.Code == "project.photo.source-missing");

        var edited = PhotoEdits.Apply(PhotoEdits.Apply(baseProject, "tiny", Edit(width: 9000, height: 6000)), "gone", Edit(path: @"C:\duzenli\gone.jpg"));
        var after = ProjectPreflight.Check(edited, AllPresent);
        after.Issues.Should().NotContain(i => i.Code == "project.photo.source-missing", "düzenlenmiş dosya yerinde");
        after.Issues.Where(i => i.ElementId == "s1").Should().NotContain(i => i.Code.StartsWith("project.photo.dpi", StringComparison.Ordinal));

        var lost = PhotoEdits.RefreshMissing(edited, p => !p.Contains("gone", StringComparison.Ordinal));
        ProjectPreflight.Check(lost, AllPresent).Issues.Should().Contain(i => i.Code == "project.photo.edit-missing" && i.ElementId == "gone")
            .And.Contain(i => i.Code == "project.photo.source-missing", "orijinal de kayıp");
    }

    [Theory]
    [InlineData("Stüdyo / Sıcak Ton", "studyo-sicak-ton")]
    [InlineData("  Işık & Gölge  ", "isik-golge")]
    [InlineData("???", "duzenleme")]
    [InlineData("Çok uzun bir action adı ki dosya adına sığmaması gerekir", "cok-uzun-bir-action-adi-ki-dosya")]
    public void SlugIsAsciiAndDeterministic(string name, string slug)
    {
        var recipe = new EditRecipe { Engine = EditEngineKind.Photoshop, Name = name };
        recipe.Slug.Should().Be(slug);
        recipe.OutputFileName("asset-0001").Should().Be("asset-0001-" + slug + ".jpg");
        (recipe with { Output = EditOutputFormat.Png }).Extension.Should().Be(".png");
    }

    [Fact]
    public void RecipeProblemsAreExplained()
    {
        new EditRecipe { Engine = EditEngineKind.Photoshop, Name = "x" }.Problem.Should().Contain("Action");
        new EditRecipe { Engine = EditEngineKind.Photoshop, Name = "x", ActionSet = "S", Action = "A" }.Problem.Should().BeNull();
        new EditRecipe { Engine = EditEngineKind.CommandLine, Name = "x" }.Problem.Should().Contain("yolu");
        new EditRecipe { Engine = EditEngineKind.CommandLine, Name = "x", Executable = "dt.exe", Arguments = ["{input}"] }.Problem.Should().Contain("{output}");
        new EditRecipe { Engine = EditEngineKind.CommandLine, Name = "x", Executable = "dt.exe", Arguments = ["{input}", "--out={output}"] }.Problem.Should().BeNull();
        FluentActions.Invoking(() => new EditRecipe { Engine = EditEngineKind.Photoshop, Name = "x", JpegQuality = 13 }).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RecipeIsRemembered()
    {
        var recipe = new EditRecipe { Engine = EditEngineKind.Photoshop, Name = "x", ActionSet = "S", Action = "A" };
        PhotoEdits.RememberRecipe(TestData.Project(), recipe).Settings.Editing.LastRecipe.Should().Be(recipe);
    }
}
