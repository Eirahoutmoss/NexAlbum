namespace NexAlbum.Core.Projects;

/// <summary>Projedeki bir makine: fotoğraf sayısı ve uygulanan saat düzeltmesi.</summary>
/// <param name="Key">Makine anahtarı (EXIF modeli; yoksa <see cref="CameraClocks.UnknownCamera"/>).</param>
/// <param name="Count">Çekim zamanı olan fotoğraf sayısı.</param>
/// <param name="Offset">Bu makinenin fotoğraflarına uygulanan kaydırma.</param>
public sealed record CameraClock(string Key, int Count, TimeSpan Offset);

/// <summary>
/// Makine saat farkı düzeltmesi (Karar 09 Rev. 6): iki fotoğrafçı ya da iki makine aynı anı farklı saatle kaydedebilir. Düzeltme
/// makine modeline göre uygulanır; orijinal EXIF zamanı kaybolmaz (<see cref="PhotoAsset.OriginalCapturedAt"/>). Hikâye
/// bölümleri, zaman sıralaması ve film düzeltilmiş zamanı kullanır. Saftır ve deterministiktir.
/// </summary>
public static class CameraClocks
{
    /// <summary>EXIF'te makine modeli olmayan fotoğrafların ortak anahtarı.</summary>
    public const string UnknownCamera = "Bilinmeyen makine";

    /// <summary>Otomatik eşitlemede aranan en büyük fark (iki yön).</summary>
    public static readonly TimeSpan SearchRange = TimeSpan.FromHours(14);

    /// <summary>Fotoğrafın makine anahtarı.</summary>
    public static string KeyOf(PhotoAsset photo)
    {
        ArgumentNullException.ThrowIfNull(photo);
        return string.IsNullOrWhiteSpace(photo.CameraModel) ? UnknownCamera : photo.CameraModel.Trim();
    }

    /// <summary>Çekim zamanı olan fotoğrafların makineleri; en çok fotoğraflı önce (eşitlikte ada göre).</summary>
    public static IReadOnlyList<CameraClock> Cameras(AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return
        [
            .. project.Photos
                .Where(p => p.CapturedAt is not null)
                .GroupBy(KeyOf, StringComparer.Ordinal)
                .Select(g => new CameraClock(g.Key, g.Count(), g.First().ClockOffset))
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Key, StringComparer.Ordinal),
        ];
    }

    /// <summary>Bu makinenin tüm fotoğraflarına (fon görselleri dahil) kaydırma uygular; sonradan eklenenler de bunu alır.</summary>
    public static AlbumProject WithOffset(this AlbumProject project, string cameraKey, TimeSpan offset)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(cameraKey);
        offset = TimeSpan.FromSeconds(Math.Round(offset.TotalSeconds));
        if (project.Assets.All(a => !string.Equals(KeyOf(a), cameraKey, StringComparison.Ordinal) || a.ClockOffset == offset))
        {
            return project;
        }

        return project with
        {
            Assets = [.. project.Assets.Select(a => string.Equals(KeyOf(a), cameraKey, StringComparison.Ordinal) ? a.WithClockOffset(offset) : a)],
        };
    }

    /// <summary>Projede bu makine için kayıtlı kaydırma (yoksa sıfır).</summary>
    public static TimeSpan OffsetOf(AlbumProject project, string cameraKey)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.Assets.FirstOrDefault(a => string.Equals(KeyOf(a), cameraKey, StringComparison.Ordinal))?.ClockOffset ?? TimeSpan.Zero;
    }

    /// <summary>"Bu iki fotoğraf aynı anda çekildi": hedefin makinesine verilecek kaydırma.</summary>
    public static TimeSpan OffsetToMatch(PhotoAsset reference, PhotoAsset target)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(target);
        if (reference.CapturedAt is not { } r || target.OriginalCapturedAt is not { } t)
        {
            throw new ArgumentException("İki fotoğrafın da çekim zamanı olmalı.");
        }

        return TimeSpan.FromSeconds(Math.Round((r - t).TotalSeconds));
    }

    /// <summary>
    /// Otomatik eşitleme önerisi: hedef makinenin (orijinal) çekim anlarını, başvuru makinesinin (düzeltilmiş) anlarıyla en
    /// çok örtüşecek biçimde kaydırır. Aynı olayda iki makine aynı anlarda yoğunlaşır (ör. pasta, sahne, toplu fotoğraf);
    /// dakikalık adımlarla ±14 saat aranır, eşitlikte sıfıra yakın olan seçilir. Belirgin bir örtüşme yoksa null.
    /// </summary>
    /// <param name="window">İki çekimin "aynı an" sayılacağı en büyük fark (varsayılan 90 sn).</param>
    public static TimeSpan? SuggestOffset(IEnumerable<DateTimeOffset> reference, IEnumerable<DateTimeOffset> target, TimeSpan? window = null)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(target);
        var refTicks = reference.Select(t => t.UtcTicks).Order().ToArray();
        var targetTicks = target.Select(t => t.UtcTicks).Order().ToArray();
        if (refTicks.Length < 3 || targetTicks.Length < 3)
        {
            return null;
        }

        var tolerance = (window ?? TimeSpan.FromSeconds(90)).Ticks;
        var steps = (int)(SearchRange.Ticks / TimeSpan.TicksPerMinute);
        var bestScore = 0;
        var bestDistance = long.MaxValue;
        var bestStep = 0;
        var zeroScore = 0;
        for (var step = -steps; step <= steps; step++)
        {
            var shift = step * TimeSpan.TicksPerMinute;
            var score = 0;
            var distance = 0L;
            foreach (var t in targetTicks)
            {
                var nearest = Nearest(refTicks, t + shift);
                if (nearest <= tolerance)
                {
                    score++;
                    distance += nearest;
                }
            }

            if (step == 0)
            {
                zeroScore = score;
            }

            // Önce örtüşen çekim sayısı, sonra toplam yakınlık, en son sıfıra yakınlık.
            var better = score > bestScore
                || (score == bestScore && distance < bestDistance)
                || (score == bestScore && distance == bestDistance && Math.Abs(step) < Math.Abs(bestStep));
            if (better)
            {
                bestScore = score;
                bestDistance = distance;
                bestStep = step;
            }
        }

        // Belirgin değilse önerme: hedefin en az dörtte biri örtüşmeli ve sıfır kaydırmadan açıkça iyi olmalı.
        var meaningful = bestScore >= Math.Max(3, targetTicks.Length / 4) && (bestStep == 0 || bestScore >= zeroScore + Math.Max(2, zeroScore / 5));
        return meaningful ? TimeSpan.FromMinutes(bestStep) : null;
    }

    /// <summary>Başvuru makinesine (en çok fotoğraflı) göre diğer makineler için öneriler.</summary>
    public static IReadOnlyDictionary<string, TimeSpan> SuggestAll(AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var cameras = Cameras(project);
        var result = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);
        if (cameras.Count < 2)
        {
            return result;
        }

        var byCamera = project.Photos.Where(p => p.CapturedAt is not null).ToLookup(KeyOf, StringComparer.Ordinal);
        var reference = byCamera[cameras[0].Key].Select(p => p.CapturedAt!.Value).ToList();
        foreach (var camera in cameras.Skip(1))
        {
            if (SuggestOffset(reference, byCamera[camera.Key].Select(p => p.OriginalCapturedAt!.Value)) is { } offset)
            {
                result[camera.Key] = offset;
            }
        }

        return result;
    }

    /// <summary>"+1 sa 05 dk", "−12 dk", "0" biçiminde kısa gösterim.</summary>
    public static string Format(TimeSpan offset)
    {
        if (offset == TimeSpan.Zero)
        {
            return "0";
        }

        var sign = offset < TimeSpan.Zero ? "−" : "+";
        var a = offset.Duration();
        var parts = new List<string>();
        if (a.Days > 0)
        {
            parts.Add(a.Days.ToString(System.Globalization.CultureInfo.InvariantCulture) + " g");
        }

        if (a.Hours > 0)
        {
            parts.Add(a.Hours.ToString(System.Globalization.CultureInfo.InvariantCulture) + " sa");
        }

        if (a.Minutes > 0)
        {
            parts.Add(a.Minutes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " dk");
        }

        if (a.Seconds > 0)
        {
            parts.Add(a.Seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) + " sn");
        }

        return sign + string.Join(' ', parts);
    }

    /// <summary>
    /// Kullanıcının yazdığı kaydırmayı okur: "+1:07", "-0:12", "1:07:30", "67" (dakika), "+1 sa 7 dk", "−12 dk", "2 g", "0".
    /// </summary>
    public static bool TryParse(string? text, out TimeSpan offset)
    {
        offset = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var s = text.Trim().Replace('−', '-').Replace(',', '.');
        var sign = 1;
        if (s[0] is '+' or '-')
        {
            sign = s[0] == '-' ? -1 : 1;
            s = s[1..].Trim();
        }

        if (s.Length == 0)
        {
            return false;
        }

        var inv = System.Globalization.CultureInfo.InvariantCulture;
        TimeSpan value;
        if (s.Contains(':', StringComparison.Ordinal))
        {
            var parts = s.Split(':');
            if (parts.Length > 3 || !parts.All(p => int.TryParse(p, System.Globalization.NumberStyles.None, inv, out _)))
            {
                return false;
            }

            var n = parts.Select(p => int.Parse(p, inv)).ToArray();
            if (n.Skip(1).Any(v => v >= 60))
            {
                return false;
            }

            value = new TimeSpan(n[0], n[1], n.Length > 2 ? n[2] : 0);
        }
        else if (double.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint, inv, out var minutes))
        {
            value = TimeSpan.FromMinutes(minutes);
        }
        else
        {
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length % 2 != 0)
            {
                return false;
            }

            value = TimeSpan.Zero;
            for (var i = 0; i < tokens.Length; i += 2)
            {
                if (!double.TryParse(tokens[i], System.Globalization.NumberStyles.AllowDecimalPoint, inv, out var amount))
                {
                    return false;
                }

                TimeSpan? unit = tokens[i + 1].ToUpperInvariant() switch
                {
                    "G" or "GÜN" => TimeSpan.FromDays(1),
                    "SA" or "SAAT" or "S" => TimeSpan.FromHours(1),
                    "DK" or "DAKIKA" => TimeSpan.FromMinutes(1),
                    "SN" or "SANIYE" => TimeSpan.FromSeconds(1),
                    _ => null,
                };
                if (unit is null)
                {
                    return false;
                }

                value += unit.Value * amount;
            }
        }

        if (value > TimeSpan.FromDays(30))
        {
            return false;
        }

        offset = TimeSpan.FromSeconds(Math.Round(value.TotalSeconds)) * sign;
        return true;
    }

    /// <summary>Sıralı dizide değere en yakın elemanın uzaklığı.</summary>
    private static long Nearest(long[] sorted, long value)
    {
        var index = Array.BinarySearch(sorted, value);
        if (index >= 0)
        {
            return 0;
        }

        index = ~index;
        var after = index < sorted.Length ? sorted[index] - value : long.MaxValue;
        var before = index > 0 ? value - sorted[index - 1] : long.MaxValue;
        return Math.Min(after, before);
    }
}
