using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Hazır metin stili: albüm sayfasına eklenen başlık, tarih, alıntı gibi yazıların başlangıç biçimi.
/// Konum ve genişlik sayfa ölçüsüne göre yüzde verilir; böylece her albüm boyutunda aynı oranda durur.
/// </summary>
/// <param name="Id">Kalıcı kimlik.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="SampleText">Eklenince yazılan örnek metin (kullanıcı hemen değiştirir).</param>
/// <param name="Font">Yazı tipi, punto, kalınlık, renk.</param>
/// <param name="Alignment">Yatay hizalama.</param>
/// <param name="WidthPercent">Metin kutusunun sayfa genişliğine oranı (%).</param>
/// <param name="CenterYPercent">Kutunun dikey ortasının sayfa yüksekliğine oranı (%).</param>
/// <param name="Lines">Kutunun başlangıçta sığdıracağı satır sayısı.</param>
/// <param name="LineHeight">Satır aralığı (punto katı).</param>
public sealed record TextPreset(
    string Id,
    string Name,
    string SampleText,
    FontSpec Font,
    TextAlignment Alignment,
    double WidthPercent,
    double CenterYPercent,
    int Lines,
    double LineHeight = 1.25)
{
    /// <summary>Punto → milimetre.</summary>
    public const double MmPerPoint = 25.4 / 72;

    /// <summary>
    /// Bu stille sayfaya yerleşecek metin öğesini oluşturur. Kutu sayfaya (taşma payı hariç) göre ortalanır ve sayfa
    /// içinde kalır; yükseklik satır sayısı ve satır aralığından hesaplanır.
    /// </summary>
    /// <param name="id">Öğe kimliği (sayfada eşsiz).</param>
    /// <param name="content">Yazı; null ise örnek metin.</param>
    /// <param name="page">Bitmiş sayfa alanı (mm).</param>
    /// <param name="zIndex">Katman (yuvaların üstünde olmalı).</param>
    public TextElement Create(string id, string? content, RectMm page, int zIndex)
    {
        var width = page.Width * WidthPercent / 100;
        var height = Math.Max(Font.Size * MmPerPoint * LineHeight * Lines, Font.Size * MmPerPoint);
        var x = page.X + ((page.Width - width) / 2);
        var y = Math.Max(page.Y, Math.Min(page.Y + (page.Height * CenterYPercent / 100) - (height / 2), page.Bottom - height));
        return new TextElement
        {
            Id = id,
            Bounds = new RectMm(x, y, width, height),
            Content = content ?? SampleText,
            Font = Font,
            Alignment = Alignment,
            VerticalAlignment = VerticalAlignment.Middle,
            LineHeight = LineHeight,
            ZIndex = zIndex,
        };
    }
}

/// <summary>Hazır metin stilleri (Karar 17: albüm yazı tipleri uygulamaya gömülüdür, kurulum gerekmez).</summary>
public static class TextPresets
{
    /// <summary>Koyu, sıcak mürekkep (saf siyah yerine; baskıda daha yumuşak).</summary>
    public static RgbaColor Ink { get; } = RgbaColor.Parse("#2B2620");

    /// <summary>Altın varak tonu (baskıda CMYK karşılığı profil ve baskı evine göre değişir).</summary>
    public static RgbaColor Gold { get; } = RgbaColor.Parse("#A9843F");

    public static IReadOnlyList<TextPreset> All { get; } =
    [
        new("title", "Başlık", "Birlikte", new FontSpec { Family = "Great Vibes", Size = 54, Color = Ink }, TextAlignment.Center, 70, 42, 1, 1.1),
        new("subtitle", "Alt başlık", "sonsuza dek", new FontSpec { Family = "Cormorant Garamond", Size = 20, Italic = true, Color = Ink }, TextAlignment.Center, 60, 54, 1),
        new("date", "Tarih", "13 EKİM 2026", new FontSpec { Family = "Montserrat", Size = 10, Color = Gold }, TextAlignment.Center, 50, 62, 1, 1.4),
        new("names", "İsimler", "Ece & Deniz", new FontSpec { Family = "Playfair Display", Size = 28, Color = Ink }, TextAlignment.Center, 70, 48, 1),
        new("quote", "Alıntı", "Bazı anlar bir ömür sürer.", new FontSpec { Family = "Cormorant Garamond", Size = 16, Italic = true, Color = Ink }, TextAlignment.Center, 62, 80, 2, 1.35),
        new("body", "Serbest metin", "Metninizi yazın", new FontSpec { Family = "Cormorant Garamond", Size = 13, Color = Ink }, TextAlignment.Left, 50, 50, 3, 1.35),
    ];

    public static TextPreset? Find(string id) => All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));
}
