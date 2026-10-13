namespace NexAlbum.Core.Geometry;

/// <summary>
/// Dışbükey (konveks) çokgen işlemleri: normalize köşeleri milimetreye çevirme, sınır kutusu ve kenar başına
/// içe/dışa öteleme. Ekran ve PDF aynı hesabı kullanır; böylece çapraz kenarlardaki boşluk her yerde aynıdır.
/// </summary>
public static class Polygon
{
    private const double Epsilon = 1e-9;

    /// <summary>Komşu kenarlar arasındaki açının sinüsü bundan küçükse (≈ 8°) kesişim yerine ortalama kaydırma kullanılır.</summary>
    private const double NearlyParallel = 0.14;

    /// <summary>Normalize köşeleri dikdörtgenin içine yerleştirir.</summary>
    public static IReadOnlyList<PointMm> Map(IReadOnlyList<UnitPoint> points, RectMm rect)
    {
        ArgumentNullException.ThrowIfNull(points);
        return [.. points.Select(p => new PointMm(rect.X + (p.X * rect.Width), rect.Y + (p.Y * rect.Height)))];
    }

    /// <summary>Köşeleri verilen dikdörtgene göre normalize eder (sınır dışı küçük hatalar kırpılır).</summary>
    public static IReadOnlyList<UnitPoint> Normalize(IReadOnlyList<PointMm> points, RectMm rect)
    {
        ArgumentNullException.ThrowIfNull(points);
        var w = rect.Width <= 0 ? 1 : rect.Width;
        var h = rect.Height <= 0 ? 1 : rect.Height;
        return [.. points.Select(p => UnitPoint.Clamped((p.X - rect.X) / w, (p.Y - rect.Y) / h))];
    }

    /// <summary>Köşeleri kapsayan en küçük dikdörtgen.</summary>
    public static RectMm Bounds(IReadOnlyList<PointMm> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            return default;
        }

        var minX = points.Min(p => p.X);
        var minY = points.Min(p => p.Y);
        return new RectMm(minX, minY, points.Max(p => p.X) - minX, points.Max(p => p.Y) - minY);
    }

    /// <summary>İşaretli alan (y aşağı eksende saat yönü pozitif). Mutlak değeri alandır.</summary>
    public static double SignedArea(IReadOnlyList<PointMm> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var sum = 0.0;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            sum += (a.X * b.Y) - (b.X * a.Y);
        }

        return sum / 2;
    }

    /// <summary>Normalize köşeler dışbükey bir çokgen oluşturuyor mu (en az 3 köşe, kendini kesmeyen)?</summary>
    public static bool IsConvex(IReadOnlyList<UnitPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var p = Distinct([.. points.Select(u => new PointMm(u.X, u.Y))]);
        if (p.Count < 3)
        {
            return false;
        }

        var sign = 0;
        var turns = 0.0;
        for (var i = 0; i < p.Count; i++)
        {
            var a = p[i];
            var b = p[(i + 1) % p.Count];
            var c = p[(i + 2) % p.Count];
            var cross = ((b.X - a.X) * (c.Y - b.Y)) - ((b.Y - a.Y) * (c.X - b.X));
            if (Math.Abs(cross) < Epsilon)
            {
                continue;
            }

            var s = Math.Sign(cross);
            if (sign != 0 && s != sign)
            {
                return false;
            }

            sign = s;
            turns += Math.Atan2(cross, ((b.X - a.X) * (c.X - b.X)) + ((b.Y - a.Y) * (c.Y - b.Y)));
        }

        // Tek tur dönen (yıldız gibi kendini kesmeyen) çokgen: toplam dönüş ±2π.
        return sign != 0 && Math.Abs(Math.Abs(turns) - (2 * Math.PI)) < 1e-6;
    }

    /// <summary>Tüm kenarları aynı mesafede öteler: pozitif içe (paspartu, kontur), negatif dışa (gölge).</summary>
    public static IReadOnlyList<PointMm> Offset(IReadOnlyList<PointMm> points, double distance)
    {
        ArgumentNullException.ThrowIfNull(points);
        return Offset(points, _ => distance);
    }

    /// <summary>
    /// Her kenarı kendi mesafesiyle içe öteler (pozitif içe, negatif dışa) ve komşu kenar doğrularının kesişimlerinden
    /// yeni köşeleri bulur. Dışbükey çokgenler içindir. Çokgen çökerse (ters dönerse) boş liste döner.
    /// </summary>
    /// <param name="points">Köşeler (saat yönü ya da tersi).</param>
    /// <param name="distance">i. kenarın (i → i+1) öteleme mesafesi.</param>
    public static IReadOnlyList<PointMm> Offset(IReadOnlyList<PointMm> points, Func<int, double> distance)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(distance);
        var p = Distinct(points);
        var n = p.Count;
        if (n < 3)
        {
            return [];
        }

        var area = SignedArea(p);
        var inwardSign = area >= 0 ? 1 : -1;
        var origins = new PointMm[n];
        var directions = new PointMm[n];
        var normals = new PointMm[n];
        for (var i = 0; i < n; i++)
        {
            var a = p[i];
            var b = p[(i + 1) % n];
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var length = Math.Sqrt((dx * dx) + (dy * dy));
            var d = new PointMm(dx / length, dy / length);
            var normal = new PointMm(-d.Y * inwardSign, d.X * inwardSign);
            var shift = distance(IndexIn(points, a, i));
            directions[i] = d;
            normals[i] = normal;
            origins[i] = new PointMm(a.X + (normal.X * shift), a.Y + (normal.Y * shift));
        }

        var result = new PointMm[n];
        for (var i = 0; i < n; i++)
        {
            var prev = (i + n - 1) % n;
            var d1 = directions[prev];
            var d2 = directions[i];
            var cross = (d1.X * d2.Y) - (d1.Y * d2.X);
            if (Math.Abs(cross) < NearlyParallel)
            {
                // Neredeyse aynı doğrultudaki komşu kenarlar (ör. kemer yayının yan kenara teğet girdiği nokta):
                // kesişim kararsızdır; köşe iki kenarın ötelemelerinin ortalamasıyla kaydırılır (köşe kesme sınırı).
                var shiftPrev = distance(IndexIn(points, p[prev], prev));
                var shiftCurrent = distance(IndexIn(points, p[i], i));
                result[i] = new PointMm(
                    p[i].X + (((normals[prev].X * shiftPrev) + (normals[i].X * shiftCurrent)) / 2),
                    p[i].Y + (((normals[prev].Y * shiftPrev) + (normals[i].Y * shiftCurrent)) / 2));
                continue;
            }

            var qx = origins[i].X - origins[prev].X;
            var qy = origins[i].Y - origins[prev].Y;
            var t = ((qx * d2.Y) - (qy * d2.X)) / cross;
            result[i] = new PointMm(origins[prev].X + (d1.X * t), origins[prev].Y + (d1.Y * t));
        }

        var newArea = SignedArea(result);
        return Math.Sign(newArea) != Math.Sign(area) || Math.Abs(newArea) < Epsilon ? [] : result;
    }

    /// <summary>Art arda gelen çakışık köşeleri (sıfır uzunluklu kenarları) atar.</summary>
    private static List<PointMm> Distinct(IReadOnlyList<PointMm> points)
    {
        var list = new List<PointMm>(points.Count);
        foreach (var point in points)
        {
            if (list.Count == 0 || !Same(list[^1], point))
            {
                list.Add(point);
            }
        }

        while (list.Count > 1 && Same(list[0], list[^1]))
        {
            list.RemoveAt(list.Count - 1);
        }

        return list;
    }

    /// <summary>Çakışık köşeler atıldıktan sonra kenar sırası değişebilir; kenar mesafesi özgün sıraya göre sorulur.</summary>
    private static int IndexIn(IReadOnlyList<PointMm> original, PointMm start, int fallback)
    {
        for (var i = 0; i < original.Count; i++)
        {
            if (Same(original[i], start) && !Same(original[(i + 1) % original.Count], start))
            {
                return i;
            }
        }

        return fallback;
    }

    private static bool Same(PointMm a, PointMm b) => Math.Abs(a.X - b.X) < Epsilon && Math.Abs(a.Y - b.Y) < Epsilon;
}
