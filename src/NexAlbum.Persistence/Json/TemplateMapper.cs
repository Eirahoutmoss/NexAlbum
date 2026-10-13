using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Json.Dtos;
using static NexAlbum.Persistence.Json.MapContext;

namespace NexAlbum.Persistence.Json;

/// <summary>Şablon ↔ JSON (Karar 09 §10). Yuvalar yüzde ("%") birimindedir.</summary>
internal static class TemplateMapper
{
    private static readonly TextEnum<TemplateCategory> Categories = new();
    private static readonly TextEnum<TemplateOrientation> Orientations = new();
    private static readonly TextEnum<PageOrientation> SlotOrientations = new();
    private static readonly TextEnum<CropMode> CropModes = new();
    private static readonly TextEnum<SlotShape> Shapes = new();

    public static TemplateDto ToDto(TemplateDefinition t) => new()
    {
        SchemaVersion = 1,
        Id = t.Id,
        Name = t.Name,
        Description = t.Description,
        Category = Categories.Format(t.Category),
        Tags = [.. t.Tags],
        Orientation = Orientations.Format(t.Orientation),
        PhotoCount = new PhotoCountDto { Min = t.PhotoCount.Min, Max = t.PhotoCount.Max },
        Slots = [.. t.Slots.Select(SlotToDto)],
        TextFields = [.. t.TextFields.Select(f => RectToDto(f.Id, f.Rect, f.Placeholder))],
        Background = t.Background is { } b ? ProjectMapper.FillToPageDto(b) : null,
        Gutter = t.GutterPercent,
        OuterMargin = t.OuterMarginPercent,
        Rules = new TemplateRulesDto { PreserveAspectRatio = t.Rules.PreserveAspectRatio, AllowUserEdit = t.Rules.AllowUserEdit },
        Preview = t.Preview,
        Author = t.Author,
        Version = t.Version,
    };

    public static TemplateDefinition FromDto(TemplateDto d, string path) => At(path, () => Map(d, path));

    private static TemplateDefinition Map(TemplateDto d, string path)
    {
        var count = Required(d.PhotoCount, path + ".photoCount");
        return new TemplateDefinition
        {
            Id = Required(d.Id, path + ".id"),
            Name = Required(d.Name, path + ".name"),
            Description = d.Description,
            Category = Categories.Parse(d.Category, path + ".category", TemplateCategory.Custom),
            Tags = [.. d.Tags ?? []],
            Orientation = Orientations.Parse(d.Orientation, path + ".orientation", TemplateOrientation.Any),
            PhotoCount = new PhotoCountRange(RequiredValue(count.Min, path + ".photoCount.min"), RequiredValue(count.Max, path + ".photoCount.max")),
            Slots = [.. (d.Slots ?? []).Select((s, i) => SlotFromDto(s, $"{path}.slots[{i}]"))],
            TextFields = [.. (d.TextFields ?? []).Select((s, i) => new TemplateTextField { Id = Required(s.Id, $"{path}.textFields[{i}].id"), Rect = PercentFrom(s, $"{path}.textFields[{i}]"), Placeholder = s.Placeholder })],
            Background = d.Background is { } b ? ProjectMapper.FillFromPageDto(b, path + ".background") : null,
            GutterPercent = d.Gutter ?? 0,
            OuterMarginPercent = d.OuterMargin ?? 0,
            Rules = new TemplateRules(d.Rules?.PreserveAspectRatio ?? true, d.Rules?.AllowUserEdit ?? true),
            Preview = d.Preview,
            Author = d.Author ?? "builtin",
            Version = d.Version ?? "1.0.0",
        };
    }

    private static TemplateSlotDto SlotToDto(TemplateSlot s)
    {
        var dto = RectToDto(s.Id, s.Rect, null);
        dto.CropMode = CropModes.Format(s.CropMode);
        dto.Priority = s.Priority;
        dto.RecommendedOrientation = s.RecommendedOrientations.Count > 0 ? [.. s.RecommendedOrientations.Select(SlotOrientations.Format)] : null;
        dto.MinAspectRatio = s.MinAspectRatio;
        dto.MaxAspectRatio = s.MaxAspectRatio;
        dto.Shape = Shapes.Format(s.Shape);
        dto.Polygon = ProjectMapper.PolygonToDto(s.Polygon);
        dto.CornerRadius = s.CornerRadiusMm;
        return dto;
    }

    private static TemplateSlot SlotFromDto(TemplateSlotDto s, string path) => new()
    {
        Id = Required(s.Id, path + ".id"),
        Rect = PercentFrom(s, path),
        CropMode = CropModes.Parse(s.CropMode, path + ".cropMode", CropMode.Cover),
        Priority = s.Priority ?? 1,
        RecommendedOrientations = [.. (s.RecommendedOrientation ?? []).Select((o, i) => SlotOrientations.Parse(o, $"{path}.recommendedOrientation[{i}]"))],
        MinAspectRatio = s.MinAspectRatio,
        MaxAspectRatio = s.MaxAspectRatio,
        Shape = Shapes.Parse(s.Shape, path + ".shape", SlotShape.Rectangle),
        Polygon = ProjectMapper.PolygonFromDto(s.Polygon, path + ".polygon"),
        CornerRadiusMm = s.CornerRadius ?? 0,
    };

    private static TemplateSlotDto RectToDto(string id, PercentRect r, string? placeholder) => new()
    {
        Id = id,
        X = r.X,
        Y = r.Y,
        Width = r.Width,
        Height = r.Height,
        Unit = "%",
        Placeholder = placeholder,
    };

    private static PercentRect PercentFrom(TemplateSlotDto s, string path)
    {
        if (s.Unit is not null && s.Unit != "%")
        {
            throw new FormatException($"'{path}.unit' şablon yuvaları yüzde (%) cinsinden olmalı.");
        }

        return new PercentRect(
            RequiredValue(s.X, path + ".x"),
            RequiredValue(s.Y, path + ".y"),
            RequiredValue(s.Width, path + ".width"),
            RequiredValue(s.Height, path + ".height"));
    }
}
