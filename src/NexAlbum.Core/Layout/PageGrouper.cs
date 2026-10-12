namespace NexAlbum.Core.Layout;

/// <summary>Bir sayfaya düşen fotoğraf sayısı ve kullanılacak şablonun yuva sayısı.</summary>
/// <param name="PhotoCount">Sayfaya yerleşecek fotoğraf sayısı.</param>
/// <param name="SlotCount">Şablonun yuva sayısı; fotoğraf yetmezse PhotoCount'tan büyüktür (boş yuva).</param>
public sealed record PageGroup(int PhotoCount, int SlotCount)
{
    public int EmptySlots => SlotCount - PhotoCount;
}

/// <summary>
/// Aşama 2 — fotoğrafları sayfalara dağıtma (Karar 10 §7). Yalnızca şablonu bulunan sayfa yoğunlukları kullanılır;
/// dağılım dengelidir ve tek fotoğraf artığı bırakılmaz.
/// </summary>
public static class PageGrouper
{
    /// <param name="photoCount">Yerleştirilecek fotoğraf sayısı.</param>
    /// <param name="supportedCounts">Şablonu bulunan sayfa başı fotoğraf sayıları.</param>
    /// <param name="options">Yerleşim parametreleri.</param>
    /// <param name="random">Dengeli modda yoğunluk çeşitlemesi için.</param>
    public static IReadOnlyList<PageGroup> Group(int photoCount, IReadOnlyCollection<int> supportedCounts, LayoutOptions options, DeterministicRandom random)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(photoCount);
        ArgumentNullException.ThrowIfNull(supportedCounts);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(random);

        if (photoCount == 0)
        {
            return [];
        }

        var allowed = AllowedCounts(supportedCounts, options);
        if (allowed.Length == 0)
        {
            throw new InvalidOperationException("İstenen sayfa yoğunluğu için uygun şablon yok.");
        }

        var reachable = Reachability(photoCount, allowed);
        var perfect = photoCount;
        while (!reachable[perfect])
        {
            perfect--;
        }

        var leftover = photoCount - perfect;
        var desired = perfect > 0 ? DesiredSizes(perfect, allowed, options) : [];
        var groups = new List<PageGroup>();
        var remaining = perfect;
        var i = 0;
        while (remaining > 0)
        {
            var wish = i < desired.Count ? desired[i] : desired[^1];
            if (options.Mode == LayoutMode.Balanced && options.FixedPhotosPerPage is null && options.TargetPageCount is null)
            {
                wish += random.Next(3) - 1;
            }

            var size = Choose(remaining, wish, allowed, reachable);
            groups.Add(new PageGroup(size, size));
            remaining -= size;
            i++;
        }

        if (leftover > 0)
        {
            // Hiçbir yoğunluk kalan fotoğrafları tam karşılamıyor: en küçük uygun şablonla, boş yuvalı son sayfa.
            var slots = allowed.FirstOrDefault(a => a >= leftover);
            groups.Add(new PageGroup(leftover, slots > 0 ? slots : allowed[^1]));
        }

        return groups;
    }

    private static int[] AllowedCounts(IReadOnlyCollection<int> supported, LayoutOptions options)
    {
        if (options.FixedPhotosPerPage is { } fixedCount)
        {
            return supported.Contains(fixedCount) ? [fixedCount] : [];
        }

        return [.. supported.Where(c => c >= options.MinPhotosPerPage && c <= options.MaxPhotosPerPage).Distinct().Order()];
    }

    /// <summary>reachable[k]: k fotoğraf izin verilen yoğunluklarla tam bölünebilir mi?</summary>
    private static bool[] Reachability(int n, int[] allowed)
    {
        var reachable = new bool[n + 1];
        reachable[0] = true;
        for (var k = 1; k <= n; k++)
        {
            foreach (var a in allowed)
            {
                if (a <= k && reachable[k - a])
                {
                    reachable[k] = true;
                    break;
                }
            }
        }

        return reachable;
    }

    /// <summary>Hedef sayfa sayısı varsa eşit dağılım; yoksa (min+max)/2 yoğunlukta dengeli dağılım.</summary>
    private static List<int> DesiredSizes(int n, int[] allowed, LayoutOptions options)
    {
        int pages;
        if (options.TargetPageCount is { } target)
        {
            pages = target;
        }
        else
        {
            var per = options.FixedPhotosPerPage ?? Math.Clamp((options.MinPhotosPerPage + options.MaxPhotosPerPage) / 2, allowed[0], allowed[^1]);
            pages = Math.Max(1, (int)Math.Ceiling((double)n / per));
        }

        pages = Math.Min(pages, n);
        var sizes = new List<int>(pages);
        var q = n / pages;
        var r = n % pages;
        for (var p = 0; p < pages; p++)
        {
            sizes.Add(q + (p < r ? 1 : 0));
        }

        return sizes;
    }

    /// <summary>Kalanı hâlâ tam bölünebilir tutan, isteğe en yakın yoğunluk (eşitlikte küçük olan).</summary>
    private static int Choose(int remaining, int wish, int[] allowed, bool[] reachable)
    {
        var best = -1;
        foreach (var a in allowed)
        {
            if (a > remaining || !reachable[remaining - a])
            {
                continue;
            }

            if (best < 0 || Math.Abs(a - wish) < Math.Abs(best - wish))
            {
                best = a;
            }
        }

        return best;
    }
}
