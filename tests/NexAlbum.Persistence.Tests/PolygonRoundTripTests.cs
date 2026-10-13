using System.IO.Compression;
using FluentAssertions;
using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Packaging;
using Xunit;

namespace NexAlbum.Persistence.Tests;

/// <summary>Şema v2: çokgen yuvalar ve biçimli çerçeveler kayıpsız saklanır (Karar 10 Rev. 1).</summary>
public class PolygonRoundTripTests
{
    [Fact]
    public void PolygonSlotsShapesAndGeometricTemplatesSurviveSaveAndLoad()
    {
        using var dir = new TempDir();
        var path = dir.File("geometrik.albumproj");
        var triangle = new List<UnitPoint> { new(0, 0), new(1, 0), new(0, 1) };
        var original = Samples.FullProject();
        var page0 = original.Pages[0];
        var page1 = original.Pages[1];
        var hexagon = FramePresets.Find("hexagon-gold")!.ApplyTo(new Slot { Id = "slot-hex", Bounds = new RectMm(10, 10, 60, 40), ZIndex = 2 });
        var project = original
            .ReplacePage(page0 with
            {
                Elements = [.. page0.Elements.Select(e => e is Slot s ? s with { Shape = SlotShape.Polygon, Polygon = triangle } : e), hexagon],
            })
            .ReplacePage(page1 with
            {
                Elements = [.. page1.Elements.Select(e => e is Slot s ? FramePresets.Find("arch-mat")!.ApplyTo(s) : e)],
            });
        var slots = page0.Slots.ToList();
        var template = GeometricTemplates.All.First(t => t.Id == "geo-triangles-3");

        ProjectPackageWriter.Save(path, project, new SaveOptions { AppVersion = project.AppVersion, Templates = [template] });
        var loaded = ProjectPackageReader.Load(path);

        loaded.Project.Should().BeEquivalentTo(project, o => o.ComparingRecordsByMembers().RespectingRuntimeTypes());
        var first = loaded.Project.Pages[0].Slots.Single(s => s.Id == slots[0].Id);
        first.Polygon.Should().Equal(triangle);
        loaded.Templates.Should().ContainSingle().Which.Should().BeEquivalentTo(template, o => o.ComparingRecordsByMembers());
        loaded.Templates[0].Slots.Count(s => s.Polygon is not null).Should().Be(2);

        using var zip = ZipFile.OpenRead(path);
        using var reader = new StreamReader(zip.GetEntry(ProjectFormat.ProjectEntry)!.Open());
        var json = reader.ReadToEnd();
        json.Should().Contain("\"shape\": \"polygon\"").And.Contain("\"polygon\": [").And.Contain("\"shape\": \"hexagon\"").And.Contain("\"shape\": \"arch\"");
    }

    [Fact]
    public void InvalidPolygonIsRejectedWithAPath()
    {
        var package = PackageBuilder.EditJson(PackageBuilder.Valid(), ProjectFormat.ProjectEntry, n =>
        {
            var slot = n["pages"]![0]!["elements"]!.AsArray().First(e => e!["type"]!.GetValue<string>() == "slot")!;
            slot["shape"] = "polygon";
            slot["polygon"] = new System.Text.Json.Nodes.JsonArray(
                new System.Text.Json.Nodes.JsonObject { ["x"] = 0, ["y"] = 0 },
                new System.Text.Json.Nodes.JsonObject { ["x"] = 2, ["y"] = 0 },
                new System.Text.Json.Nodes.JsonObject { ["x"] = 0, ["y"] = 1 });
        });

        var error = PackageBuilder.LoadFails(package);
        error.Error.Should().Be(ProjectLoadError.InvalidData);
        error.Message.Should().Contain("polygon");
    }
}
