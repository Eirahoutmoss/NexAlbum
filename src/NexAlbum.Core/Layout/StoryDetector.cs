using NexAlbum.Core.Common;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>Bölüm bulma ayarları.</summary>
public sealed record StoryDetectionOptions
{
    /// <summary>İki fotoğraf arasında bu kadar boşluk varsa yeni bölüm başlar.</summary>
    public TimeSpan Gap { get; init => field = value <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(Gap)) : value; } = TimeSpan.FromMinutes(40);

    /// <summary>Bundan az fotoğraflı bölüm, aradaki boşluğu daha kısa olan komşusuna katılır.</summary>
    public int MinPhotos { get; init => field = Guard.Positive(value, nameof(MinPhotos)); } = 4;
}

/// <summary>
/// Düğün akışını çekim saatlerinden çıkarır (Karar 10 Rev. 2): fotoğraflar zamana göre dizilir; uzun ara ya da gün
/// değişimi yeni bölüm başlatır, çok küçük bölümler komşusuna katılır. Ad, bölümün başladığı saate ve düğün gününe göre
/// önerilir (Türkiye'deki yaygın akış: hazırlık → gelin alma → dış çekim → nikâh → düğün; önceki akşam kına).
/// Öneridir; kullanıcı her adı değiştirebilir. Saftır ve deterministiktir.
/// </summary>
public static class StoryDetector
{
    /// <summary>Panelde seçilebilen bölüm adları (yaygın evreler).</summary>
    public static IReadOnlyList<string> PhaseNames { get; } =
    [
        "Kına Gecesi", "Hazırlık", "Gelin Alma", "Dış Çekim", "Nikâh", "Düğün", "İlk Dans", "Pasta", "Aile ve Dostlar", "Ertesi Gün",
    ];

    public static StoryPlan Detect(IEnumerable<PhotoAsset> photos, StoryDetectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(photos);
        options ??= new StoryDetectionOptions();
        var times = photos.Where(p => p.IsPhoto && p.CapturedAt is not null).Select(p => p.CapturedAt!.Value).Order().ToList();
        if (times.Count == 0)
        {
            return StoryPlan.Empty;
        }

        // 1. Ara ve gün değişiminde böl.
        var groups = new List<List<DateTimeOffset>> { new() { times[0] } };
        for (var i = 1; i < times.Count; i++)
        {
            var newDay = times[i].DateTime.Date != times[i - 1].DateTime.Date;
            if (newDay || times[i] - times[i - 1] >= options.Gap)
            {
                groups.Add([]);
            }

            groups[^1].Add(times[i]);
        }

        // 2. Küçük bölümleri, aradaki boşluğu daha kısa olan komşusuna kat (aynı günde kalarak).
        bool merged;
        do
        {
            merged = false;
            for (var i = 0; i < groups.Count && groups.Count > 1; i++)
            {
                if (groups[i].Count >= options.MinPhotos)
                {
                    continue;
                }

                var before = i > 0 && SameDay(groups[i - 1], groups[i]) ? groups[i][0] - groups[i - 1][^1] : TimeSpan.MaxValue;
                var after = i < groups.Count - 1 && SameDay(groups[i], groups[i + 1]) ? groups[i + 1][0] - groups[i][^1] : TimeSpan.MaxValue;
                if (before == TimeSpan.MaxValue && after == TimeSpan.MaxValue)
                {
                    continue;
                }

                var target = before <= after ? i - 1 : i + 1;
                groups[target].AddRange(groups[i]);
                groups[target].Sort();
                groups.RemoveAt(i);
                merged = true;
                break;
            }
        }
        while (merged);

        // 3. Adlandır: düğün günü = en çok fotoğrafın çekildiği gün.
        var mainDay = times.GroupBy(t => t.DateTime.Date).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        var chapters = new List<StoryChapter>(groups.Count);
        var used = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < groups.Count; i++)
        {
            var start = groups[i][0];
            var name = Suggest(start, mainDay);
            used[name] = used.TryGetValue(name, out var n) ? n + 1 : 1;
            var shown = used[name] == 1 ? name : name + " " + Roman(used[name]);
            chapters.Add(new StoryChapter("chapter-" + (i + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture), shown, start));
        }

        return new StoryPlan { Chapters = chapters };
    }

    /// <summary>Başlangıç saatine göre evre adı (çekim saati, fotoğrafın kendi saatiyle).</summary>
    public static string Suggest(DateTimeOffset start, DateTime mainDay)
    {
        var day = start.DateTime.Date;
        var hour = start.DateTime.TimeOfDay;
        if (day < mainDay)
        {
            return hour >= TimeSpan.FromHours(16) ? "Kına Gecesi" : "Hazırlık";
        }

        if (day > mainDay)
        {
            return "Ertesi Gün";
        }

        return hour.TotalHours switch
        {
            < 12 => "Hazırlık",
            < 15 => "Gelin Alma",
            < 18 => "Dış Çekim",
            < 20 => "Nikâh",
            _ => "Düğün",
        };
    }

    private static bool SameDay(List<DateTimeOffset> a, List<DateTimeOffset> b) => a[^1].DateTime.Date == b[0].DateTime.Date;

    private static string Roman(int n) => n switch
    {
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        _ => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}
