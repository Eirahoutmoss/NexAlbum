using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Design;

/// <summary>Kenar bazında yüzde (yuvanın kısa kenarına göre).</summary>
public readonly record struct EdgePercents(double Top, double Right, double Bottom, double Left)
{
    public static EdgePercents Uniform(double value) => new(value, value, value, value);

    public bool IsZero => Top == 0 && Right == 0 && Bottom == 0 && Left == 0;
}

/// <summary>Yumuşak gölge: kaydırma, bulanıklık (mm) ve saydamlık (0–1).</summary>
public sealed record ShadowStyle(double OffsetMm, double BlurMm, double Opacity);

/// <summary>
/// Fotoğraf çerçevesi stili: kontur, paspartu, köşe yuvarlama, biçim ve gölge.
/// Paspartu ve köşe yuvarlama yuvanın kısa kenarına oranlıdır; böylece küçük ve büyük yuvada aynı görünür.
/// Kontur kalınlığı ise baskıda sabit kalması için mm cinsindendir.
/// </summary>
public sealed record FrameStyle
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required string Name { get; init => field = Guard.NotBlank(value, nameof(Name)); }

    public double StrokeWidthMm { get; init => field = Guard.InRange(value, 0, 20, nameof(StrokeWidthMm)); }

    public RgbaColor StrokeColor { get; init; } = RgbaColor.Black;

    public EdgePercents MatPercent { get; init; }

    public RgbaColor MatColor { get; init; } = RgbaColor.White;

    public double CornerRadiusPercent { get; init => field = Guard.InRange(value, 0, 50, nameof(CornerRadiusPercent)); }

    /// <summary>Dikdörtgen, yuvarlak köşeli ya da oval (yuvaya içten teğet elips).</summary>
    public SlotShape Shape { get; init; } = SlotShape.Rectangle;

    public ShadowStyle? Shadow { get; init; }

    /// <summary>Stili yuvaya uygular; yuvanın ölçüsüne göre mm değerlerini hesaplar. Fotoğraf yerleşimi korunur.</summary>
    public Slot ApplyTo(Slot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        var unit = Math.Min(slot.Bounds.Width, slot.Bounds.Height) / 100;
        var m = MatPercent;
        MatSpec? mat = m.IsZero ? null : new MatSpec(new EdgesMm(m.Top * unit, m.Right * unit, m.Bottom * unit, m.Left * unit), MatColor);
        var radius = Shape == SlotShape.Circle ? 0 : CornerRadiusPercent * unit;
        return slot with
        {
            StyleId = Id,
            Border = StrokeWidthMm > 0 ? new BorderSpec(StrokeWidthMm, StrokeColor) : null,
            Mat = mat,
            Shape = Shape == SlotShape.Circle ? SlotShape.Circle : radius > 0 ? SlotShape.Rounded : SlotShape.Rectangle,
            CornerRadiusMm = radius,
            Shadow = Shadow is { } s
                ? new ShadowSpec(s.OffsetMm, s.OffsetMm, s.BlurMm, new RgbaColor(0, 0, 0, (byte)Math.Round(Math.Clamp(s.Opacity, 0, 1) * 255)))
                : null,
        };
    }
}
