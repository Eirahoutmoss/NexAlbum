using NexAlbum.Core.Common;
using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Json.Dtos;
using static NexAlbum.Persistence.Json.MapContext;

namespace NexAlbum.Persistence.Json;

/// <summary>Proje ↔ project.json (Karar 09 §4–§9).</summary>
internal static class ProjectMapper
{
    private static readonly TextEnum<PageType> PageTypes = new();
    private static readonly TextEnum<SpreadPosition> SpreadPositions = new();
    private static readonly TextEnum<CropMode> CropModes = new();
    private static readonly TextEnum<TextEffectKind> EffectKinds = new();
    private static readonly TextEnum<SlotShape> Shapes = new();
    private static readonly TextEnum<TextAlignment> Alignments = new();
    private static readonly TextEnum<VerticalAlignment> VerticalAlignments = new();
    private static readonly TextEnum<BackgroundMode> BackgroundModes = new();
    private static readonly TextEnum<AssetSourceType> SourceTypes = new();
    private static readonly TextEnum<AssetRole> Roles = new();
    private static readonly TextEnum<EditEngineKind> EditEngines = new();
    private static readonly TextEnum<EditOutputFormat> EditOutputs = new();

    public static ProjectDto ToDto(AlbumProject p) => new()
    {
        SchemaVersion = ProjectFormat.CurrentSchemaVersion,
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        AppVersion = p.AppVersion,
        ProfileId = p.ProfileId,
        ProfileSnapshot = ProfileMapper.ToDto(p.Profile),
        Page = ProfileMapper.SizeToDto(p.PageSize),
        Pages = [.. p.Pages.Select((page, i) => PageToDto(page, i))],
        Assets = [.. p.Assets.Select(a => AssetToDto(a, p.PagesUsingAsset(a.Id)))],
        Settings = new SettingsDto
        {
            Export = new ExportSettingsDto { JpegQuality = p.Settings.Export.JpegQuality },
            Ui = new UiSettingsDto { Theme = p.Settings.Ui.Theme },
            Save = new SaveSettingsDto { AutosaveIntervalMinutes = p.Settings.Save.AutosaveIntervalMinutes },
            Design = new DesignSettingsDto
            {
                FrameStyle = p.Settings.Design.FrameStyleId,
                Gap = p.Settings.Design.GapPercent,
                Margin = p.Settings.Design.MarginPercent,
                Background = p.Settings.Design.Background is { } bg ? FillToPageDto(bg) : null,
            },
            Editing = p.Settings.Editing.LastRecipe is { } r ? new EditingSettingsDto { LastRecipe = RecipeToDto(r) } : null,
        },
        History = p.History is { } h ? new HistoryDto { Enabled = h.Enabled, MaxSteps = h.MaxSteps } : null,
        Story = p.Story.HasChapters || p.Story.Openers || !p.Story.Enabled
            ? new StoryDto
            {
                Enabled = p.Story.Enabled,
                Openers = p.Story.Openers,
                Chapters = [.. p.Story.Chapters.Select(c => new ChapterDto { Id = c.Id, Name = c.Name, Start = c.Start })],
            }
            : null,
    };

    public static AlbumProject FromDto(ProjectDto d) => At("project", () => Map(d));

    internal static PageBackgroundDto FillToPageDto(BackgroundFill f) => new()
    {
        Type = BackgroundModes.Format(f.Mode),
        Color = f.Color?.ToHex(),
        Gradient = f.Gradient is { } g ? GradientToDto(g) : null,
        Image = f.ImageAssetId is { } id ? new ImageRefDto { AssetId = id } : null,
        Opacity = f.Opacity,
    };

    internal static BackgroundFill FillFromPageDto(PageBackgroundDto? d, string path) => d is null
        ? BackgroundFill.White
        : FillFrom(d.Type, d.Color, d.Gradient, d.Image, d.Opacity, path);

    private static AlbumProject Map(ProjectDto d)
    {
        var profile = ProfileMapper.FromDto(Required(d.ProfileSnapshot, "profileSnapshot"), "profileSnapshot");
        var pageSize = ProfileMapper.SizeFromDtoOrNull(d.Page, "page") ?? profile.Page.Size
            ?? throw new FormatException("'page' alanı eksik ve profilde sayfa ölçüsü yok.");

        return new AlbumProject
        {
            Id = RequiredValue(d.Id, "id"),
            Name = Required(d.Name, "name"),
            Description = d.Description,
            CreatedAt = d.CreatedAt ?? default,
            UpdatedAt = d.UpdatedAt ?? d.CreatedAt ?? default,
            AppVersion = d.AppVersion ?? "0.0.0",
            Profile = profile,
            PageSize = pageSize,
            Pages = [.. (d.Pages ?? []).Select((page, i) => PageFromDto(page, $"pages[{i}]"))],
            Assets = [.. (d.Assets ?? []).Select((asset, i) => AssetFromDto(asset, $"assets[{i}]"))],
            Settings = At("settings", () => SettingsFromDto(d.Settings)),
            History = d.History is { } h ? new HistorySettings { Enabled = h.Enabled ?? true, MaxSteps = h.MaxSteps ?? 100 } : null,
            Story = d.Story is { } s ? At("story", () => StoryFromDto(s)) : StoryPlan.Empty,
        };
    }

    private static StoryPlan StoryFromDto(StoryDto d) => new()
    {
        Enabled = d.Enabled ?? true,
        Openers = d.Openers ?? false,
        Chapters = [.. (d.Chapters ?? []).Select((c, i) => new StoryChapter(
            Required(c.Id, $"chapters[{i}].id"),
            Required(c.Name, $"chapters[{i}].name"),
            RequiredValue(c.Start, $"chapters[{i}].start")))],
    };

    private static ProjectSettings SettingsFromDto(SettingsDto? d) => new()
    {
        Export = new ExportSettings { JpegQuality = d?.Export?.JpegQuality ?? 95 },
        Ui = new UiSettings { Theme = d?.Ui?.Theme ?? "light" },
        Save = new SaveSettings { AutosaveIntervalMinutes = d?.Save?.AutosaveIntervalMinutes ?? 5 },
        Design = d?.Design is { } design
            ? new DesignSettings
            {
                FrameStyleId = design.FrameStyle ?? FramePresets.NoneId,
                GapPercent = design.Gap,
                MarginPercent = design.Margin,
                Background = design.Background is { } bg ? FillFromPageDto(bg, "settings.design.background") : null,
            }
            : DesignSettings.Default,
        Editing = d?.Editing?.LastRecipe is { } recipe
            ? new EditingSettings { LastRecipe = RecipeFromDto(recipe, "settings.editing.lastRecipe") }
            : EditingSettings.Default,
    };

    private static EditRecipeDto RecipeToDto(EditRecipe r) => new()
    {
        Engine = EditEngines.Format(r.Engine),
        Name = r.Name,
        ActionSet = r.ActionSet,
        Action = r.Action,
        Executable = r.Executable,
        Arguments = r.Arguments.Count > 0 ? [.. r.Arguments] : null,
        Output = EditOutputs.Format(r.Output),
        JpegQuality = r.JpegQuality,
    };

    private static EditRecipe RecipeFromDto(EditRecipeDto d, string path) => At(path, () => new EditRecipe
    {
        Engine = EditEngines.Parse(d.Engine, path + ".engine", EditEngineKind.Photoshop),
        Name = Required(d.Name, path + ".name"),
        ActionSet = d.ActionSet,
        Action = d.Action,
        Executable = d.Executable,
        Arguments = [.. d.Arguments ?? []],
        Output = EditOutputs.Parse(d.Output, path + ".output", EditOutputFormat.Jpeg),
        JpegQuality = d.JpegQuality ?? 12,
    });

    private static PhotoEditDto EditToDto(PhotoEdit e) => new()
    {
        Path = e.Path,
        Width = e.PixelWidth,
        Height = e.PixelHeight,
        Orientation = e.ExifOrientation,
        Tool = e.Tool,
        Recipe = e.Recipe,
        CreatedAt = e.CreatedAt,
        Missing = e.Missing ? true : null,
    };

    private static PhotoEdit EditFromDto(PhotoEditDto d, string path) => At(path, () => new PhotoEdit
    {
        Path = Required(d.Path, path + ".path"),
        PixelWidth = RequiredValue(d.Width, path + ".width"),
        PixelHeight = RequiredValue(d.Height, path + ".height"),
        ExifOrientation = d.Orientation,
        Tool = d.Tool ?? "Harici düzenleyici",
        Recipe = d.Recipe,
        CreatedAt = d.CreatedAt ?? default,
        Missing = d.Missing ?? false,
    });

    private static PageDto PageToDto(AlbumPage page, int index) => new()
    {
        Id = page.Id,
        Index = index,
        Type = PageTypes.Format(page.Type),
        SpreadPosition = SpreadPositions.FormatOrNull(page.SpreadPosition),
        Locked = page.Locked,
        Notes = page.Notes,
        Background = FillToPageDto(page.Background),
        Elements = [.. page.Elements.Select(ElementToDto)],
        TemplateId = page.TemplateId,
        CreatedAt = page.CreatedAt,
        UpdatedAt = page.UpdatedAt,
    };

    private static AlbumPage PageFromDto(PageDto d, string path) => At(path, () => new AlbumPage
    {
        Id = Required(d.Id, path + ".id"),
        Type = PageTypes.Parse(d.Type, path + ".type", PageType.Content),
        SpreadPosition = SpreadPositions.ParseOrNull(d.SpreadPosition, path + ".spreadPosition"),
        Locked = d.Locked ?? false,
        Notes = d.Notes,
        Background = FillFromPageDto(d.Background, path + ".background"),
        Elements = [.. (d.Elements ?? []).Select((e, i) => ElementFromDto(e, $"{path}.elements[{i}]"))],
        TemplateId = d.TemplateId,
        CreatedAt = d.CreatedAt ?? default,
        UpdatedAt = d.UpdatedAt ?? d.CreatedAt ?? default,
    });

    private static ElementDto ElementToDto(PageElement element) => element switch
    {
        Slot s => SlotToDto(s),
        TextElement t => TextToDto(t),
        BackgroundElement b => BackgroundToDto(b),
        _ => throw new NotSupportedException($"Bilinmeyen öğe tipi: {element.GetType().Name}"),
    };

    private static PageElement ElementFromDto(ElementDto? d, string path) => At<PageElement>(path, () => d switch
    {
        SlotDto s => SlotFromDto(s, path),
        TextDto t => TextFromDto(t, path),
        BackgroundElementDto b => BackgroundFromDto(b, path),
        null => throw new FormatException($"'{path}' boş."),
        _ => throw new FormatException($"'{path}': bilinmeyen öğe tipi."),
    });

    private static SlotDto SlotToDto(Slot s) => new()
    {
        Id = s.Id,
        X = s.Bounds.X,
        Y = s.Bounds.Y,
        Width = s.Bounds.Width,
        Height = s.Bounds.Height,
        Unit = "mm",
        CropMode = CropModes.Format(s.CropMode),
        MinDpi = s.MinDpi,
        TargetDpi = s.TargetDpi,
        Placement = s.Placement is { } p ? PlacementToDto(p) : null,
        Shape = Shapes.Format(s.Shape),
        Polygon = PolygonToDto(s.Polygon),
        CornerRadius = s.CornerRadiusMm,
        Border = s.Border is { } b ? new BorderDto { Width = b.WidthMm, Color = b.Color.ToHex() } : null,
        Shadow = s.Shadow is { } sh ? new ShadowDto { OffsetX = sh.OffsetXMm, OffsetY = sh.OffsetYMm, Blur = sh.BlurMm, Color = sh.Color.ToHex() } : null,
        Mat = s.Mat is { } m ? new MatDto { Top = m.Edges.Top, Right = m.Edges.Right, Bottom = m.Edges.Bottom, Left = m.Edges.Left, Color = m.Color.ToHex() } : null,
        StyleId = s.StyleId,
        ZIndex = s.ZIndex,
    };

    private static Slot SlotFromDto(SlotDto d, string path) => new()
    {
        Id = Required(d.Id, path + ".id"),
        Bounds = RectFrom(d.X, d.Y, d.Width, d.Height, d.Unit, path),
        CropMode = CropModes.Parse(d.CropMode, path + ".cropMode", CropMode.Cover),
        MinDpi = d.MinDpi,
        TargetDpi = d.TargetDpi,
        Placement = d.Placement is { } p ? PlacementFromDto(p, path + ".placement") : null,
        Shape = Shapes.Parse(d.Shape, path + ".shape", SlotShape.Rectangle),
        Polygon = PolygonFromDto(d.Polygon, path + ".polygon"),
        CornerRadiusMm = d.CornerRadius ?? 0,
        Border = d.Border is { } b ? new BorderSpec(b.Width ?? 0, Color(b.Color, path + ".border.color") ?? RgbaColor.Black) : null,
        Shadow = d.Shadow is { } sh
            ? new ShadowSpec(sh.OffsetX ?? 0, sh.OffsetY ?? 0, sh.Blur ?? 0, Color(sh.Color, path + ".shadow.color") ?? RgbaColor.Black)
            : null,
        Mat = d.Mat is { } m
            ? new MatSpec(
                new EdgesMm(m.Top ?? 0, m.Right ?? 0, m.Bottom ?? 0, m.Left ?? 0),
                Color(m.Color, path + ".mat.color") ?? RgbaColor.White)
            : null,
        StyleId = d.StyleId,
        ZIndex = d.ZIndex ?? 0,
    };

    internal static List<PointDto>? PolygonToDto(IReadOnlyList<UnitPoint>? polygon) =>
        polygon is null ? null : [.. polygon.Select(p => new PointDto { X = p.X, Y = p.Y })];

    internal static List<UnitPoint>? PolygonFromDto(List<PointDto>? polygon, string path)
    {
        if (polygon is null)
        {
            return null;
        }

        if (polygon.Count < 3)
        {
            throw new FormatException($"'{path}' en az üç köşe içermeli.");
        }

        return [.. polygon.Select((p, i) => p.X is { } x && p.Y is { } y && x is >= 0 and <= 1 && y is >= 0 and <= 1
            ? new UnitPoint(x, y)
            : throw new FormatException($"'{path}[{i}]' 0–1 aralığında x ve y içermeli."))];
    }

    private static PlacementDto PlacementToDto(PhotoPlacement p) => new()
    {
        AssetId = p.AssetId,
        OffsetX = p.OffsetX,
        OffsetY = p.OffsetY,
        Scale = p.Scale,
        Rotation = p.Rotation,
        CropRect = new RectDto { X = p.Crop.X, Y = p.Crop.Y, Width = p.Crop.Width, Height = p.Crop.Height },
        Filters = new FiltersDto { Brightness = p.Adjustments.Brightness, Contrast = p.Adjustments.Contrast, Saturation = p.Adjustments.Saturation },
    };

    private static PhotoPlacement PlacementFromDto(PlacementDto d, string path) => new()
    {
        AssetId = Required(d.AssetId, path + ".assetId"),
        OffsetX = d.OffsetX ?? 0,
        OffsetY = d.OffsetY ?? 0,
        Scale = d.Scale ?? 1,
        Rotation = d.Rotation ?? 0,
        Crop = d.CropRect is { } c
            ? new NormalizedRect(c.X ?? 0, c.Y ?? 0, c.Width ?? 1, c.Height ?? 1)
            : NormalizedRect.Full,
        Adjustments = d.Filters is { } f
            ? new ImageAdjustments { Brightness = f.Brightness ?? 0, Contrast = f.Contrast ?? 0, Saturation = f.Saturation ?? 0 }
            : ImageAdjustments.None,
    };

    private static TextDto TextToDto(TextElement t) => new()
    {
        Id = t.Id,
        X = t.Bounds.X,
        Y = t.Bounds.Y,
        Width = t.Bounds.Width,
        Height = t.Bounds.Height,
        Unit = "mm",
        Content = t.Content,
        Font = new FontDto { Family = t.Font.Family, Size = t.Font.Size, Weight = t.Font.Weight, Italic = t.Font.Italic, Color = t.Font.Color.ToHex() },
        Alignment = Alignments.Format(t.Alignment),
        VerticalAlignment = VerticalAlignments.Format(t.VerticalAlignment),
        LineHeight = t.LineHeight,
        LetterSpacing = t.LetterSpacing,
        Effect = t.Effect.IsNone ? null : new TextEffectDto { Kind = EffectKinds.Format(t.Effect.Kind), Color = t.Effect.Color.ToHex(), Strength = t.Effect.Strength },
        ZIndex = t.ZIndex,
    };

    private static TextElement TextFromDto(TextDto d, string path)
    {
        var font = Required(d.Font, path + ".font");
        return new TextElement
        {
            Id = Required(d.Id, path + ".id"),
            Bounds = RectFrom(d.X, d.Y, d.Width, d.Height, d.Unit, path),
            Content = Required(d.Content, path + ".content"),
            Font = new FontSpec
            {
                Family = Required(font.Family, path + ".font.family"),
                Size = font.Size ?? 12,
                Weight = font.Weight ?? 400,
                Italic = font.Italic ?? false,
                Color = Color(font.Color, path + ".font.color") ?? RgbaColor.Black,
            },
            Alignment = Alignments.Parse(d.Alignment, path + ".alignment", TextAlignment.Left),
            VerticalAlignment = VerticalAlignments.Parse(d.VerticalAlignment, path + ".verticalAlignment", VerticalAlignment.Top),
            LineHeight = d.LineHeight,
            LetterSpacing = d.LetterSpacing ?? 0,
            Effect = d.Effect is { } e
                ? new TextEffect
                {
                    Kind = EffectKinds.Parse(e.Kind, path + ".effect.kind", TextEffectKind.None),
                    Color = Color(e.Color, path + ".effect.color") ?? RgbaColor.Black,
                    Strength = e.Strength ?? 0.6,
                }
                : TextEffect.None,
            ZIndex = d.ZIndex ?? 0,
        };
    }

    private static BackgroundElementDto BackgroundToDto(BackgroundElement b) => new()
    {
        Id = b.Id,
        Mode = BackgroundModes.Format(b.Fill.Mode),
        Color = b.Fill.Color?.ToHex(),
        Gradient = b.Fill.Gradient is { } g ? GradientToDto(g) : null,
        Image = b.Fill.ImageAssetId is { } id ? new ImageRefDto { AssetId = id } : null,
        Opacity = b.Fill.Opacity,
        ZIndex = b.ZIndex,
    };

    private static BackgroundElement BackgroundFromDto(BackgroundElementDto d, string path) => new()
    {
        Id = Required(d.Id, path + ".id"),
        Fill = FillFrom(d.Mode, d.Color, d.Gradient, d.Image, d.Opacity, path),
        ZIndex = d.ZIndex ?? -100,
    };

    private static BackgroundFill FillFrom(string? mode, string? color, GradientDto? gradient, ImageRefDto? image, double? opacity, string path) => new()
    {
        Mode = BackgroundModes.Parse(mode, path + ".mode", BackgroundMode.Solid),
        Color = Color(color, path + ".color"),
        Gradient = gradient is null
            ? null
            : new GradientSpec(
                Color(Required(gradient.From, path + ".gradient.from"), path + ".gradient.from")!.Value,
                Color(Required(gradient.To, path + ".gradient.to"), path + ".gradient.to")!.Value,
                gradient.Angle ?? 0),
        ImageAssetId = image?.AssetId,
        Opacity = opacity ?? 1,
    };

    private static GradientDto GradientToDto(GradientSpec g) => new() { From = g.From.ToHex(), To = g.To.ToHex(), Angle = g.AngleDegrees };

    private static AssetDto AssetToDto(PhotoAsset a, IReadOnlyList<string> usedInPages) => new()
    {
        Id = a.Id,
        SourcePath = a.SourcePath,
        SourceType = SourceTypes.Format(a.SourceType),
        Role = a.Role == AssetRole.Photo ? null : Roles.Format(a.Role),
        FileName = a.FileName,
        FileSize = a.FileSize,
        FileHash = a.FileHash,
        Width = a.PixelWidth,
        Height = a.PixelHeight,
        Orientation = a.ExifOrientation,
        CapturedAt = a.CapturedAt,
        ImportedAt = a.ImportedAt,
        CameraModel = a.CameraModel,
        Lens = a.Lens,
        Iso = a.Iso,
        FocalLength = a.FocalLength,
        Thumbnail = a.Thumbnail is { } t ? new ThumbnailDto { Path = t.Path, Width = t.Width, Height = t.Height } : null,
        Favorite = a.Favorite,
        Tags = [.. a.Tags],
        UsedInPages = [.. usedInPages],
        Missing = a.Missing,
        Edit = a.Edit is { } edit ? EditToDto(edit) : null,
        UseEdit = a.Edit is not null && !a.UseEdit ? false : null,
    };

    private static PhotoAsset AssetFromDto(AssetDto d, string path) => At(path, () => new PhotoAsset
    {
        Id = Required(d.Id, path + ".id"),
        SourcePath = Required(d.SourcePath, path + ".sourcePath"),
        SourceType = SourceTypes.Parse(d.SourceType, path + ".sourceType", AssetSourceType.Reference),
        Role = Roles.Parse(d.Role, path + ".role", AssetRole.Photo),
        FileName = d.FileName ?? Path.GetFileName(Required(d.SourcePath, path + ".sourcePath")),
        FileSize = d.FileSize,
        FileHash = d.FileHash,
        PixelWidth = RequiredValue(d.Width, path + ".width"),
        PixelHeight = RequiredValue(d.Height, path + ".height"),
        ExifOrientation = d.Orientation,
        CapturedAt = d.CapturedAt,
        ImportedAt = d.ImportedAt ?? default,
        CameraModel = d.CameraModel,
        Lens = d.Lens,
        Iso = d.Iso,
        FocalLength = d.FocalLength,
        Thumbnail = d.Thumbnail is { Path: { } tp } t
            ? new ThumbnailInfo(tp, RequiredValue(t.Width, path + ".thumbnail.width"), RequiredValue(t.Height, path + ".thumbnail.height"))
            : null,
        Favorite = d.Favorite ?? false,
        Tags = [.. d.Tags ?? []],
        Missing = d.Missing ?? false,
        Edit = d.Edit is { } edit ? EditFromDto(edit, path + ".edit") : null,
        UseEdit = d.UseEdit ?? true,
    });

    private static RectMm RectFrom(double? x, double? y, double? width, double? height, string? unit, string path) => new(
        ToMm(RequiredValue(x, path + ".x"), unit, path),
        ToMm(RequiredValue(y, path + ".y"), unit, path),
        ToMm(RequiredValue(width, path + ".width"), unit, path),
        ToMm(RequiredValue(height, path + ".height"), unit, path));

    private static RgbaColor? Color(string? hex, string path)
    {
        if (hex is null)
        {
            return null;
        }

        return RgbaColor.TryParse(hex, out var color) ? color : throw new FormatException($"'{path}' geçersiz renk: '{hex}'.");
    }
}
