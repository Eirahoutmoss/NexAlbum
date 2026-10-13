namespace NexAlbum.Persistence.Json.Dtos;

/// <summary>Karar 09 §4 project.json.</summary>
internal sealed class ProjectDto
{
    public int? SchemaVersion { get; set; }

    public Guid? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? AppVersion { get; set; }

    public string? ProfileId { get; set; }

    public ProfileDto? ProfileSnapshot { get; set; }

    public SizeDto? Page { get; set; }

    public List<PageDto>? Pages { get; set; }

    public List<AssetDto>? Assets { get; set; }

    public SettingsDto? Settings { get; set; }

    public HistoryDto? History { get; set; }

    /// <summary>Şema v4: hikâye akışı (bölümler).</summary>
    public StoryDto? Story { get; set; }

    /// <summary>Şema v5: hikâye filmi ayarları.</summary>
    public VideoDto? Video { get; set; }
}

internal sealed class VideoDto
{
    public string? Format { get; set; }

    public string? Style { get; set; }

    public int? Fps { get; set; }

    public double? TargetSeconds { get; set; }

    public string? Music { get; set; }

    public bool? ReduceMotion { get; set; }

    public bool? ChapterTitles { get; set; }

    public List<VideoOverrideDto>? Overrides { get; set; }
}

internal sealed class VideoOverrideDto
{
    public string? Chapter { get; set; }

    public string? Transition { get; set; }

    public string? Look { get; set; }
}

internal sealed class StoryDto
{
    public bool? Enabled { get; set; }

    public bool? Openers { get; set; }

    public List<ChapterDto>? Chapters { get; set; }
}

internal sealed class ChapterDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? Start { get; set; }
}

internal sealed class PageDto
{
    public string? Id { get; set; }

    /// <summary>Yazılırken konumdan üretilir; okurken sıralama dizideki konuma göredir.</summary>
    public int? Index { get; set; }

    public string? Type { get; set; }

    public string? SpreadPosition { get; set; }

    public bool? Locked { get; set; }

    public string? Notes { get; set; }

    public PageBackgroundDto? Background { get; set; }

    public List<ElementDto>? Elements { get; set; }

    public string? TemplateId { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class AssetDto
{
    public string? Id { get; set; }

    public string? SourcePath { get; set; }

    public string? SourceType { get; set; }

    public string? Role { get; set; }

    public string? FileName { get; set; }

    public long? FileSize { get; set; }

    public string? FileHash { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int? Orientation { get; set; }

    public DateTimeOffset? CapturedAt { get; set; }

    /// <summary>Makine saat düzeltmesi (saniye); capturedAt bu kadar kaydırılmıştır. Yoksa 0 (şema v7).</summary>
    public long? ClockOffsetSeconds { get; set; }

    public DateTimeOffset? ImportedAt { get; set; }

    public string? CameraModel { get; set; }

    public string? Lens { get; set; }

    public int? Iso { get; set; }

    public double? FocalLength { get; set; }

    public ThumbnailDto? Thumbnail { get; set; }

    public bool? Favorite { get; set; }

    public List<string>? Tags { get; set; }

    /// <summary>Bilgi amaçlı yazılır; okurken yok sayılır (kullanım sayfalardan türetilir).</summary>
    public List<string>? UsedInPages { get; set; }

    public bool? Missing { get; set; }

    /// <summary>Harici araçla üretilmiş düzenlenmiş sürüm (Karar 20); opsiyonel.</summary>
    public PhotoEditDto? Edit { get; set; }

    /// <summary>Yoksa true.</summary>
    public bool? UseEdit { get; set; }
}

internal sealed class PhotoEditDto
{
    public string? Path { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public int? Orientation { get; set; }

    public string? Tool { get; set; }

    public string? Recipe { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public bool? Missing { get; set; }
}

internal sealed class EditingSettingsDto
{
    public EditRecipeDto? LastRecipe { get; set; }
}

internal sealed class EditRecipeDto
{
    public string? Engine { get; set; }

    public string? Name { get; set; }

    public string? ActionSet { get; set; }

    public string? Action { get; set; }

    public string? Executable { get; set; }

    public List<string>? Arguments { get; set; }

    public string? Output { get; set; }

    public int? JpegQuality { get; set; }
}

internal sealed class ThumbnailDto
{
    public string? Path { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }
}

internal sealed class SettingsDto
{
    public ExportSettingsDto? Export { get; set; }

    public UiSettingsDto? Ui { get; set; }

    public SaveSettingsDto? Save { get; set; }

    public DesignSettingsDto? Design { get; set; }

    public EditingSettingsDto? Editing { get; set; }
}

internal sealed class DesignSettingsDto
{
    public string? FrameStyle { get; set; }

    public double? Gap { get; set; }

    public double? Margin { get; set; }

    public PageBackgroundDto? Background { get; set; }
}

internal sealed class ExportSettingsDto
{
    public int? JpegQuality { get; set; }
}

internal sealed class UiSettingsDto
{
    public string? Theme { get; set; }
}

internal sealed class SaveSettingsDto
{
    public int? AutosaveIntervalMinutes { get; set; }
}

internal sealed class HistoryDto
{
    public bool? Enabled { get; set; }

    public int? MaxSteps { get; set; }
}

/// <summary>Karar 09 §12.3 manifest.json.</summary>
internal sealed class ManifestDto
{
    public string? Format { get; set; }

    public string? FormatVersion { get; set; }

    public int? SchemaVersion { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public string? AppVersion { get; set; }

    public Guid? ProjectId { get; set; }

    public string? ProjectName { get; set; }

    public bool? Portable { get; set; }

    public string? ChecksumsFile { get; set; }
}

/// <summary>Karar 09 §12.4 checksums.json.</summary>
internal sealed class ChecksumsDto
{
    public string? Algorithm { get; set; }

    public Dictionary<string, string>? Files { get; set; }
}

/// <summary>profiles.json: projede kullanılan profiller.</summary>
internal sealed class ProfilesFileDto
{
    public int? SchemaVersion { get; set; }

    public List<ProfileDto>? Profiles { get; set; }
}

/// <summary>templates.json: projede kullanılan (gömülü) şablonlar.</summary>
internal sealed class TemplatesFileDto
{
    public int? SchemaVersion { get; set; }

    public List<TemplateDto>? Templates { get; set; }
}

/// <summary>Karar 09 §10.1 şablon.</summary>
internal sealed class TemplateDto
{
    public int? SchemaVersion { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Category { get; set; }

    public List<string>? Tags { get; set; }

    public string? Orientation { get; set; }

    public PhotoCountDto? PhotoCount { get; set; }

    public List<TemplateSlotDto>? Slots { get; set; }

    public List<TemplateSlotDto>? TextFields { get; set; }

    public PageBackgroundDto? Background { get; set; }

    public double? Gutter { get; set; }

    public double? OuterMargin { get; set; }

    public TemplateRulesDto? Rules { get; set; }

    public string? Preview { get; set; }

    public string? Author { get; set; }

    public string? Version { get; set; }
}

internal sealed class PhotoCountDto
{
    public int? Min { get; set; }

    public int? Max { get; set; }
}

internal sealed class TemplateSlotDto
{
    public string? Id { get; set; }

    public double? X { get; set; }

    public double? Y { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }

    public string? Unit { get; set; }

    public string? Placeholder { get; set; }

    public string? CropMode { get; set; }

    public int? Priority { get; set; }

    public List<string>? RecommendedOrientation { get; set; }

    public double? MinAspectRatio { get; set; }

    public double? MaxAspectRatio { get; set; }

    public string? Shape { get; set; }

    /// <summary>Çokgen yuvanın köşeleri, yuva dikdörtgeni içinde 0–1 (şema v2).</summary>
    public List<PointDto>? Polygon { get; set; }

    public double? CornerRadius { get; set; }
}

internal sealed class TemplateRulesDto
{
    public bool? PreserveAspectRatio { get; set; }

    public bool? AllowUserEdit { get; set; }
}
