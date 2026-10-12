using NexAlbum.Core.Geometry;

namespace NexAlbum.Export.Pdf;

/// <summary>Kesim işareti çizgisi (mm, sayfa sol üst köşesine göre).</summary>
public readonly record struct MarkLine(double X1, double Y1, double X2, double Y2);

/// <summary>
/// Baskı sayfasının kutuları (Karar 11 §3.2, §3.5). Tüm ölçüler mm; köken medya kutusunun sol üst köşesidir.
/// Medya = bitmiş ölçü + taşma payı + (varsa) kesim işareti alanı.
/// </summary>
public sealed record SheetGeometry
{
    private SheetGeometry(SizeMm media, RectMm trim, RectMm bleed, IReadOnlyList<MarkLine> marks)
    {
        Media = media;
        Trim = trim;
        Bleed = bleed;
        CropMarks = marks;
    }

    /// <summary>PDF sayfasının tam ölçüsü (MediaBox).</summary>
    public SizeMm Media { get; }

    /// <summary>Bitmiş (kesilmiş) sayfa (TrimBox).</summary>
    public RectMm Trim { get; }

    /// <summary>Taşma payı dahil alan (BleedBox).</summary>
    public RectMm Bleed { get; }

    public IReadOnlyList<MarkLine> CropMarks { get; }

    /// <param name="trimSize">Bitmiş sayfa ölçüsü.</param>
    /// <param name="bleed">Taşma payı; null veya dahil edilmeyecekse sıfır kabul edilir (uydurulmaz).</param>
    /// <param name="cropMarks">Kesim işaretleri çizilsin mi?</param>
    /// <param name="markOffsetMm">İşaretin taşma payı kenarından uzaklığı.</param>
    /// <param name="markLengthMm">İşaret uzunluğu.</param>
    public static SheetGeometry Create(SizeMm trimSize, EdgesMm? bleed, bool cropMarks, double markOffsetMm = 3, double markLengthMm = 5)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(markOffsetMm);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(markLengthMm);

        var b = bleed ?? default;
        var mark = cropMarks ? markOffsetMm + markLengthMm : 0;
        var trim = new RectMm(b.Left + mark, b.Top + mark, trimSize.Width, trimSize.Height);
        var bleedBox = trim.Inflate(b);
        var media = new SizeMm(bleedBox.Width + (2 * mark), bleedBox.Height + (2 * mark));

        IReadOnlyList<MarkLine> lines = cropMarks ? Marks(trim, b, markOffsetMm, markLengthMm) : [];
        return new SheetGeometry(media, trim, bleedBox, lines);
    }

    /// <summary>Her köşede, bitmiş kenarların uzantısında iki çizgi; taşma payı + uzaklıktan başlar.</summary>
    private static List<MarkLine> Marks(RectMm trim, EdgesMm b, double offset, double length)
    {
        var left = trim.X;
        var right = trim.Right;
        var top = trim.Y;
        var bottom = trim.Bottom;

        var lStart = left - b.Left - offset;
        var rStart = right + b.Right + offset;
        var tStart = top - b.Top - offset;
        var bStart = bottom + b.Bottom + offset;

        return
        [
            // Sol üst
            new(lStart - length, top, lStart, top),
            new(left, tStart - length, left, tStart),

            // Sağ üst
            new(rStart, top, rStart + length, top),
            new(right, tStart - length, right, tStart),

            // Sol alt
            new(lStart - length, bottom, lStart, bottom),
            new(left, bStart, left, bStart + length),

            // Sağ alt
            new(rStart, bottom, rStart + length, bottom),
            new(right, bStart, right, bStart + length),
        ];
    }
}
