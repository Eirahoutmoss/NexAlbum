using System.Globalization;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Templates;

namespace NexAlbum.Core.Layout;

/// <summary>Seçilen şablon ve gerekçesi.</summary>
public sealed record TemplateChoice(TemplateDefinition Template, IReadOnlyList<SlotAssignment> Assignments, double Score, string Reason);

/// <summary>
/// Aşama 3 ve 5 — şablon seçimi ve çeşitlilik (Karar 10 §8, §10).
/// </summary>
public sealed class TemplateSelector
{
    /// <summary>Aynı şablon son bu kadar sayfada tekrar etmez (Karar 10 §10.3).</summary>
    public const int TemplateWindow = 3;

    /// <summary>Dengeli modda aynı kategori son bu kadar sayfada tekrar etmez (Karar 10 §10.2).</summary>
    public const int CategoryWindow = 2;

    /// <summary>Dengeli modda bu kadar sayfada bir tam sayfa (single/fullBleed) teşvik edilir (Karar 10 §10.7).</summary>
    public const int FeatureRhythm = 5;

    /// <summary>
    /// Geometrik şablonlar vurgudur: tercih edilmedikçe en fazla bu kadar sayfada bir seçilir (Karar 10 Rev. 1).
    /// Üçgen ve çapraz kesimlerle dolu bir albüm yorar; ritim, klasik sayfaların arasında sürpriz etkisini korur.
    /// </summary>
    public const int GeometricRhythm = 5;

    private readonly IReadOnlyList<TemplateDefinition> templates;
    private readonly LayoutOptions options;
    private readonly DeterministicRandom random;
    private readonly Func<TemplateDefinition, RectMm> areaFor;
    private readonly List<TemplateDefinition> history = [];

    public TemplateSelector(
        IReadOnlyList<TemplateDefinition> templates,
        LayoutOptions options,
        DeterministicRandom random,
        PageOrientation pageOrientation,
        Func<TemplateDefinition, RectMm> areaFor)
    {
        ArgumentNullException.ThrowIfNull(templates);
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.random = random ?? throw new ArgumentNullException(nameof(random));
        this.areaFor = areaFor ?? throw new ArgumentNullException(nameof(areaFor));

        var allowed = options.AllowedTemplateIds;
        this.templates = [.. templates
            .Where(t => t.Fits(pageOrientation))
            .Where(t => allowed.Count == 0 || allowed.Contains(t.Id))
            .OrderBy(t => t.Id, StringComparer.Ordinal)];
    }

    /// <summary>Kullanılabilir şablonların desteklediği sayfa başı fotoğraf sayıları.</summary>
    public IReadOnlyCollection<int> SupportedCounts() =>
        [.. templates.SelectMany(CountsOf).Distinct().Order()];

    /// <summary>Daha önce seçilmiş (ör. kilitli) bir sayfanın şablonunu çeşitlilik geçmişine ekler.</summary>
    public void Remember(TemplateDefinition template) => history.Add(template ?? throw new ArgumentNullException(nameof(template)));

    public TemplateChoice Select(PageGroup group, IReadOnlyList<ClassifiedPhoto> photos)
    {
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(photos);

        var fitting = templates.Where(t => t.PhotoCount.Accepts(group.SlotCount) && t.Slots.Count >= group.SlotCount).ToList();
        if (fitting.Count == 0)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0} fotoğraflık sayfa için şablon yok.", group.SlotCount));
        }

        if (options.Mode == LayoutMode.Controlled && options.PreferredCategories.Count > 0)
        {
            var preferred = fitting.Where(t => options.PreferredCategories.Contains(t.Category)).ToList();
            if (preferred.Count > 0)
            {
                fitting = preferred;
            }
        }

        fitting = ApplyGeometricRhythm(fitting);
        var candidates = ApplyDiversityRules(fitting);
        var scored = candidates
            .Select(t => Evaluate(t, group, photos))
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Template.Id, StringComparer.Ordinal)
            .ToList();

        var top = scored[0].Score;
        var ties = scored.TakeWhile(c => top - c.Score < 1e-9).ToList();
        var choice = ties.Count == 1 ? ties[0] : ties[random.Next(ties.Count)];
        history.Add(choice.Template);
        return choice;
    }

    /// <summary>
    /// Bölüm açılışı için tek fotoğraflık şablon: tam sayfa (taşma paylı) olan tercih edilir; yoksa tek fotoğraflık herhangi biri.
    /// </summary>
    public TemplateChoice SelectOpener(ClassifiedPhoto hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        var single = templates.Where(t => t.PhotoCount.Accepts(1) && t.Slots.Count >= 1).ToList();
        if (single.Count == 0)
        {
            throw new InvalidOperationException("Bölüm açılışı için tek fotoğraflık şablon yok.");
        }

        var bleed = single.Where(t => t.Category == TemplateCategory.FullBleed).ToList();
        var choice = (bleed.Count > 0 ? bleed : single)
            .Select(t => Evaluate(t, new PageGroup(1, 1), [hero]))
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Template.Id, StringComparer.Ordinal)
            .First();
        history.Add(choice.Template);
        return choice with { Reason = "bölüm açılışı, " + choice.Reason };
    }

    /// <summary>
    /// Tekrar kuralları (Karar 10 §10.3). Uygun aday kalmazsa kurallar kademeli gevşetilir:
    /// önce kategori kuralı, sonra şablon penceresi 3 → 2 → 1 sayfaya iner.
    /// </summary>
    private List<TemplateDefinition> ApplyDiversityRules(List<TemplateDefinition> fitting)
    {
        if (options.Mode == LayoutMode.Balanced)
        {
            var recentTemplates = RecentIds(TemplateWindow);
            var recentCategories = history.TakeLast(CategoryWindow).Select(t => t.Category).ToHashSet();
            var strict = fitting.Where(t => !recentTemplates.Contains(t.Id) && !recentCategories.Contains(t.Category)).ToList();
            if (strict.Count > 0)
            {
                return strict;
            }
        }

        for (var window = TemplateWindow; window >= 1; window--)
        {
            var recent = RecentIds(window);
            var allowed = fitting.Where(t => !recent.Contains(t.Id)).ToList();
            if (allowed.Count > 0)
            {
                return allowed;
            }
        }

        return fitting;
    }

    private List<TemplateDefinition> ApplyGeometricRhythm(List<TemplateDefinition> fitting)
    {
        if (options.PreferredCategories.Contains(TemplateCategory.Geometric))
        {
            return fitting;
        }

        var recent = history.TakeLast(GeometricRhythm - 1).Any(t => t.Category == TemplateCategory.Geometric);
        var classic = fitting.Where(t => t.Category != TemplateCategory.Geometric).ToList();
        return recent && classic.Count > 0 ? classic : fitting;
    }

    private HashSet<string> RecentIds(int window) => history.TakeLast(window).Select(t => t.Id).ToHashSet(StringComparer.Ordinal);

    private TemplateChoice Evaluate(TemplateDefinition template, PageGroup group, IReadOnlyList<ClassifiedPhoto> photos)
    {
        var area = areaFor(template);
        var slots = template.Resolve(area, options.Design.GapPercent, options.Design.MarginPercent)
            .Zip(template.Slots, (resolved, slot) => new LayoutSlot(slot, resolved.Bounds, resolved.Polygon))
            .Take(group.SlotCount)
            .ToList();
        var assignments = SlotMatcher.Match(photos, slots);
        var placed = assignments.Where(a => a.Photo is not null).ToList();

        var orientationMatch = placed.Count == 0 ? 1 : 1 - ((double)placed.Count(a => SlotMatcher.OrientationMismatch(a.Photo!, a.Slot)) / placed.Count);
        var aspectMatch = placed.Count == 0 ? 1 : 1 - Math.Min(1, placed.Average(a => Math.Abs(Math.Log(a.Photo!.AspectRatio / a.Slot.AspectRatio))));
        var diversity = Diversity(template);
        var preference = options.PreferredCategories.Contains(template.Category) ? 1.0 : 0.0;

        var (w1, w2, w3, w4) = options.Mode == LayoutMode.Balanced ? (0.25, 0.15, 0.45, 0.10) : (0.3, 0.2, 0.3, 0.1);
        var score = (w1 * orientationMatch) + (w2 * aspectMatch) + (w3 * diversity) + (w4 * preference);

        var reason = string.Format(
            CultureInfo.InvariantCulture,
            "{0}: yön uyumu {1:0.00}, oran uyumu {2:0.00}, çeşitlilik {3:0.00}, tercih {4:0}",
            template.Name,
            orientationMatch,
            aspectMatch,
            diversity,
            preference);
        return new TemplateChoice(template, assignments, score, reason);
    }

    /// <summary>Kategori değişimi ve (dengeli modda) tam sayfa ritmi için 0–1 arası bonus.</summary>
    private double Diversity(TemplateDefinition template)
    {
        if (history.Count == 0)
        {
            return 1;
        }

        var value = history[^1].Category == template.Category ? 0.5 : 1.0;
        if (options.Mode == LayoutMode.Balanced)
        {
            var sinceFeature = 0;
            for (var i = history.Count - 1; i >= 0 && !IsFeature(history[i]); i--)
            {
                sinceFeature++;
            }

            if (sinceFeature >= FeatureRhythm - 1)
            {
                value = IsFeature(template) ? 1.0 : value * 0.6;
            }
            else if (IsFeature(template) && sinceFeature < 2)
            {
                value *= 0.5;
            }
        }

        return value;
    }

    private static IEnumerable<int> CountsOf(TemplateDefinition t)
    {
        var from = Math.Max(1, t.PhotoCount.Min);
        var to = Math.Min(t.PhotoCount.Max, t.Slots.Count);
        return to < from ? [] : Enumerable.Range(from, to - from + 1);
    }

    private static bool IsFeature(TemplateDefinition t) => t.Category is TemplateCategory.Single or TemplateCategory.FullBleed;
}
