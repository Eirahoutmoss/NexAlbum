using System.IO;
using FluentAssertions;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Packaging;
using NexAlbum.UI.Services;
using NexAlbum.UI.ViewModels;
using Xunit;

namespace NexAlbum.UI.Tests;

/// <summary>Kitap görünümü, sunum modu ve şablon galerisi süzgeci.</summary>
public sealed partial class MainViewModelTests
{
    [Fact]
    public void BookViewPairsPagesAndNavigatesBySpread()
    {
        var store = new MemoryAppSettingsStore();
        using var vm = new MainViewModel(new FakeDialogs(), settings: store);
        vm.Open(PagedProject(5));
        var single = vm.Zoom.DisplayWidth;

        vm.ToggleBookViewCommand.Execute(null);

        vm.IsBookView.Should().BeTrue();
        store.Load().BookView.Should().BeTrue("tercih bu bilgisayarda saklanır");
        vm.SpreadLeft.Should().BeSameAs(vm.Pages[0]);
        vm.SpreadRight.Should().BeSameAs(vm.Pages[1]);
        vm.IsLeftSelected.Should().BeTrue();
        vm.PageNavText.Should().Be("1–2 / 5");
        vm.Zoom.HasPage.Should().BeTrue();
        single.Should().BeGreaterThan(0);

        vm.SelectedPage = vm.Pages[1];
        vm.IsRightSelected.Should().BeTrue("karşı sayfaya tıklanınca o sayfa etkin olur, çift sayfa aynı kalır");
        vm.SpreadLeft.Should().BeSameAs(vm.Pages[0]);

        vm.NextPageCommand.Execute(null);
        vm.SelectedPage.Should().BeSameAs(vm.Pages[2], "sonraki çift sayfanın solu");
        vm.PageNavText.Should().Be("3–4 / 5");

        vm.NextPageCommand.Execute(null);
        vm.SpreadLeft.Should().BeSameAs(vm.Pages[4]);
        vm.SpreadRight.Should().BeNull("son sayfa tek kalır");
        vm.PageNavText.Should().Be("5 / 5");
        vm.NextPageCommand.CanExecute(null).Should().BeFalse();

        vm.PreviousPageCommand.Execute(null);
        vm.SelectedPage.Should().BeSameAs(vm.Pages[2]);

        vm.IsBookView = false;
        vm.SpreadLeft.Should().BeNull();
        vm.PageNavText.Should().Be("3 / 5");
        store.Load().BookView.Should().BeFalse();
    }

    [Fact]
    public void BookViewPreferenceIsRestored()
    {
        var store = new MemoryAppSettingsStore(new AppSettings { BookView = true });
        using var vm = new MainViewModel(new FakeDialogs(), settings: store);
        vm.Open(PagedProject(2));

        vm.IsBookView.Should().BeTrue();
        vm.SpreadRight.Should().BeSameAs(vm.Pages[1]);
    }

    [Fact]
    public void PresentStartsAtTheSelectedSpread()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.PresentCommand.CanExecute(null).Should().BeFalse("proje yokken");
        vm.Open(PagedProject(5));
        PresentationViewModel? shown = null;
        vm.PresentationRequested += (_, p) => shown = p;

        vm.SelectedPage = vm.Pages[3];
        vm.PresentCommand.Execute(null);

        shown.Should().NotBeNull();
        shown!.IsSpreads.Should().BeTrue();
        shown.CounterText.Should().Be("3–4 / 5");
        shown.Title.Should().Be("Örnek");
    }

    [Fact]
    public void TemplateGalleryCanShowOnlyGeometricLayouts()
    {
        using var vm = new MainViewModel(new FakeDialogs(), settings: new MemoryAppSettingsStore());
        vm.Open(PagedProject(1));
        vm.SelectedTemplateCount = vm.TemplateCounts[0];
        var all = vm.Templates.Count;

        vm.SelectedTemplateStyle = vm.TemplateStyles.Single(s => s.Style == TemplateStyle.Geometric);

        vm.HasTemplates.Should().BeTrue("kare sayfada geometrik şablonlar vardır");
        vm.Templates.Should().OnlyContain(t => t.IsGeometric);
        vm.Templates.Count.Should().BeLessThan(all);
        vm.Templates.SelectMany(t => t.Cells).Should().OnlyContain(c => c.Fill != null);

        vm.SelectedTemplateStyle = vm.TemplateStyles.Single(s => s.Style == TemplateStyle.Grid);
        vm.Templates.Should().OnlyContain(t => t.Template.Category == TemplateCategory.Grid);
    }

    private string PagedProject(int pages)
    {
        var path = Path.Combine(dir, $"sayfalar-{pages}.albumproj");
        var project = SampleProject(pages);
        project = project.WithPages(project.Assets.Select((a, i) => new AlbumPage
        {
            Id = $"p{i}",
            Elements = [new Slot { Id = "s", Bounds = new RectMm(10, 10, 100, 100), Placement = new PhotoPlacement { AssetId = a.Id } }],
        }));
        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion });
        return path;
    }
}

public class PresentationTests
{
    [Fact]
    public void SpreadsPairPagesAndClampNavigation()
    {
        var project = Project(5);
        var show = new PresentationViewModel(project, startPageIndex: 0, spreads: true);

        show.Count.Should().Be(3);
        show.CounterText.Should().Be("1–2 / 5");
        show.CanGoPrevious.Should().BeFalse();
        show.Previous().Should().BeFalse();

        show.Next().Should().BeTrue();
        show.CounterText.Should().Be("3–4 / 5");
        show.Last().Should().BeTrue();
        show.Current.Right.Should().BeNull();
        show.CounterText.Should().Be("5 / 5");
        show.CanGoNext.Should().BeFalse();
        show.GoTo(99).Should().BeFalse("sonda kalır");
        show.First().Should().BeTrue();
        show.ViewWidth.Should().Be(show.PageWidth * 2);
    }

    [Fact]
    public void SinglePagesHideGuidesAndShowTheTrimmedPage()
    {
        var show = new PresentationViewModel(Project(3), startPageIndex: 2, spreads: false);

        show.Count.Should().Be(3);
        show.CounterText.Should().Be("3 / 3");
        show.Current.Left.ShowGuides.Should().BeFalse("sunumda kesim ve güvenli alan çizgileri görünmez");
        show.Current.Left.ShowBleedGuide.Should().BeFalse();
        show.PageWidth.Should().Be(300, "bitmiş ölçü (30×30 cm)");
        show.CanvasWidth.Should().BeGreaterThanOrEqualTo(show.PageWidth);
        show.ViewWidth.Should().Be(show.PageWidth);
        show.Close();
    }

    [Fact]
    public void EmptyProjectCannotBePresented()
    {
        var empty = AlbumProject.Create("Boş", Core.Profiles.BuiltInProfiles.Find("builtin-30x30")!, DateTimeOffset.UtcNow, "0.1.0");
        var act = () => new PresentationViewModel(empty, 0, spreads: true);
        act.Should().Throw<ArgumentException>();
    }

    private static AlbumProject Project(int pages)
    {
        var project = AlbumProject.Create("Sunum", Core.Profiles.BuiltInProfiles.Find("builtin-30x30")!, DateTimeOffset.UtcNow, "0.1.0");
        return project.WithPages(Enumerable.Range(0, pages).Select(i => new AlbumPage { Id = $"p{i}" }));
    }
}

public class EmptySlotHintTests
{
    [Fact]
    public void PolygonHintSitsOnTheCentroidAndShrinksInNarrowShapes()
    {
        var triangle = new List<UnitPoint> { new(0, 0), new(1, 0), new(0, 1) };
        var large = new Slot { Id = "t", Bounds = new RectMm(0, 0, 90, 90), Shape = SlotShape.Polygon, Polygon = triangle };
        var vm = new SlotItemViewModel(large, large.Bounds, asset: null, sample: false);

        vm.ShowEmptyLabel.Should().BeTrue();
        vm.EmptyOffsetX.Should().BeApproximately(-15, 0.01, "üçgenin ağırlık merkezi (30, 30); kutunun ortası (45, 45)");
        vm.EmptyOffsetY.Should().BeApproximately(-15, 0.01);
        vm.ShowEmptyHint.Should().BeTrue("iç teğet çemberin çapı ≈ 53 mm");

        var small = large with { Bounds = new RectMm(0, 0, 60, 60) };
        new SlotItemViewModel(small, small.Bounds, asset: null, sample: false).ShowEmptyHint.Should().BeFalse("dar üçgende yalnızca \"+\"");

        var rect = new Slot { Id = "r", Bounds = new RectMm(0, 0, 80, 50) };
        var plain = new SlotItemViewModel(rect, rect.Bounds, asset: null, sample: false);
        plain.EmptyOffsetX.Should().Be(0);
        plain.EmptyOffsetY.Should().Be(0);
        plain.ShowEmptyHint.Should().BeTrue();
    }
}
