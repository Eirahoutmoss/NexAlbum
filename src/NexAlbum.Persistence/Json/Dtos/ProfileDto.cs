using System.Text.Json.Serialization;

namespace NexAlbum.Persistence.Json.Dtos;

/// <summary>Karar 03 §3.1 profil şeması.</summary>
internal sealed class ProfileDto
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    public int? SchemaVersion { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Source { get; set; }

    public string? SourceNote { get; set; }

    public bool? Verified { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }

    public string? VerifiedBy { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? Version { get; set; }

    public SizeDto? Page { get; set; }

    public SpreadDto? Spread { get; set; }

    public EdgesDto? Bleed { get; set; }

    public EdgesDto? SafeArea { get; set; }

    public GutterDto? Gutter { get; set; }

    public ResolutionDto? Resolution { get; set; }

    public ColorDto? Color { get; set; }

    public OutputDto? Output { get; set; }

    public CoverDto? Cover { get; set; }

    public PaperDto? Paper { get; set; }

    public BindingDto? Binding { get; set; }

    public string? Notes { get; set; }

    public List<string>? Tags { get; set; }

    public List<ProfileWarningDto>? Warnings { get; set; }
}

internal sealed class SpreadDto
{
    public bool? Enabled { get; set; }

    public string? Note { get; set; }
}

internal sealed class GutterDto
{
    public double? Width { get; set; }

    public string? Unit { get; set; }

    public string? Note { get; set; }

    public bool? Verified { get; set; }
}

internal sealed class ResolutionDto
{
    [JsonPropertyName("targetDPI")]
    public int? TargetDpi { get; set; }

    [JsonPropertyName("minDPI")]
    public int? MinDpi { get; set; }

    public string? Note { get; set; }
}

internal sealed class ColorDto
{
    public string? Mode { get; set; }

    public string? Profile { get; set; }

    public bool? ProfileEmbedded { get; set; }

    public string? Note { get; set; }
}

internal sealed class OutputDto
{
    public bool? CropMarks { get; set; }

    public double? CropMarkOffset { get; set; }

    public double? CropMarkLength { get; set; }

    public bool? RegistrationMarks { get; set; }

    public bool? ColorBars { get; set; }

    public bool? PageInfo { get; set; }
}

internal sealed class CoverDto
{
    public string? Type { get; set; }

    public SizeDto? Front { get; set; }

    public SizeDto? Back { get; set; }

    public SpineDto? Spine { get; set; }
}

internal sealed class SpineDto
{
    public double? Width { get; set; }

    public string? Unit { get; set; }

    public string? CalculationMethod { get; set; }

    public string? Note { get; set; }

    public bool? Verified { get; set; }
}

internal sealed class PaperDto
{
    public string? Type { get; set; }

    public double? Weight { get; set; }

    public string? WeightUnit { get; set; }

    public string? Note { get; set; }
}

internal sealed class BindingDto
{
    public string? Type { get; set; }

    public string? Note { get; set; }
}

internal sealed class ProfileWarningDto
{
    public string? Level { get; set; }

    public string? Message { get; set; }
}
