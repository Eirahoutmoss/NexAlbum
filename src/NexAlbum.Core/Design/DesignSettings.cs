using NexAlbum.Core.Common;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Design;

/// <summary>
/// Albümün varsayılan tasarımı: otomatik oluşturma ve "tüm sayfalara uygula" bunu kullanır.
/// Null değer "şablonun kendi değeri" demektir.
/// </summary>
public sealed record DesignSettings
{
    public string FrameStyleId { get; init => field = Guard.NotBlank(value, nameof(FrameStyleId)); } = FramePresets.NoneId;

    /// <summary>Fotoğraflar arası boşluk (sayfanın kısa kenarının yüzdesi).</summary>
    public double? GapPercent { get; init => field = value is null ? null : Guard.InRange(value.Value, 0, 20, nameof(GapPercent)); }

    /// <summary>Sayfa kenar payı (sayfanın kısa kenarının yüzdesi).</summary>
    public double? MarginPercent { get; init => field = value is null ? null : Guard.InRange(value.Value, 0, 30, nameof(MarginPercent)); }

    /// <summary>Sayfa fonu; null ise şablonun fonu ya da beyaz.</summary>
    public BackgroundFill? Background { get; init; }

    public static DesignSettings Default { get; } = new();

    public FrameStyle Frame => FramePresets.Find(FrameStyleId) ?? FramePresets.None;
}
