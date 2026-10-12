using System.Text.Json.Serialization;

namespace NexAlbum.Persistence.Json.Dtos;

// Dosya şemasının (v1) birebir karşılıkları. Domain'den bağımsızdır; şema göçleri bu katmanda kalır.
// Tüm alanlar null olabilir: eksik alanı eşleyici yakalar ve anlamlı hata verir.

internal sealed class SizeDto
{
    public double? Width { get; set; }

    public double? Height { get; set; }

    public string? Unit { get; set; }

    public string? Orientation { get; set; }
}

internal sealed class EdgesDto
{
    public double? Top { get; set; }

    public double? Right { get; set; }

    public double? Bottom { get; set; }

    public double? Left { get; set; }

    public string? Unit { get; set; }

    public bool? Verified { get; set; }

    public string? Note { get; set; }
}

internal sealed class RectDto
{
    public double? X { get; set; }

    public double? Y { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }

    public string? Unit { get; set; }
}

internal sealed class GradientDto
{
    public string? From { get; set; }

    public string? To { get; set; }

    public double? Angle { get; set; }
}

internal sealed class ImageRefDto
{
    public string? AssetId { get; set; }
}

internal sealed class PageBackgroundDto
{
    public string? Type { get; set; }

    public string? Color { get; set; }

    public GradientDto? Gradient { get; set; }

    public ImageRefDto? Image { get; set; }

    public double? Opacity { get; set; }
}

internal sealed class BorderDto
{
    public double? Width { get; set; }

    public string? Color { get; set; }
}

internal sealed class ShadowDto
{
    public double? OffsetX { get; set; }

    public double? OffsetY { get; set; }

    public double? Blur { get; set; }

    public string? Color { get; set; }
}

internal sealed class FiltersDto
{
    public double? Brightness { get; set; }

    public double? Contrast { get; set; }

    public double? Saturation { get; set; }
}

internal sealed class FontDto
{
    public string? Family { get; set; }

    public double? Size { get; set; }

    public int? Weight { get; set; }

    public bool? Italic { get; set; }

    public string? Color { get; set; }
}

internal sealed class PlacementDto
{
    public string? AssetId { get; set; }

    public double? OffsetX { get; set; }

    public double? OffsetY { get; set; }

    public double? Scale { get; set; }

    public double? Rotation { get; set; }

    public RectDto? CropRect { get; set; }

    public FiltersDto? Filters { get; set; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SlotDto), "slot")]
[JsonDerivedType(typeof(TextDto), "text")]
[JsonDerivedType(typeof(BackgroundElementDto), "background")]
internal abstract class ElementDto
{
    public string? Id { get; set; }

    public int? ZIndex { get; set; }
}

internal sealed class SlotDto : ElementDto
{
    public double? X { get; set; }

    public double? Y { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }

    public string? Unit { get; set; }

    public string? CropMode { get; set; }

    [JsonPropertyName("minDPI")]
    public int? MinDpi { get; set; }

    [JsonPropertyName("targetDPI")]
    public int? TargetDpi { get; set; }

    public PlacementDto? Placement { get; set; }

    public string? Shape { get; set; }

    public double? CornerRadius { get; set; }

    public BorderDto? Border { get; set; }

    public ShadowDto? Shadow { get; set; }
}

internal sealed class TextDto : ElementDto
{
    public double? X { get; set; }

    public double? Y { get; set; }

    public double? Width { get; set; }

    public double? Height { get; set; }

    public string? Unit { get; set; }

    public string? Content { get; set; }

    public FontDto? Font { get; set; }

    public string? Alignment { get; set; }

    public string? VerticalAlignment { get; set; }

    public double? LineHeight { get; set; }

    public double? LetterSpacing { get; set; }
}

internal sealed class BackgroundElementDto : ElementDto
{
    public string? Mode { get; set; }

    public string? Color { get; set; }

    public GradientDto? Gradient { get; set; }

    public ImageRefDto? Image { get; set; }

    public double? Opacity { get; set; }
}
