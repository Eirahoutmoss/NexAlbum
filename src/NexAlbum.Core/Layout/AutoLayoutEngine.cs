using System.Globalization;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;
using NexAlbum.Core.Validation;

namespace NexAlbum.Core.Layout;

/// <summary>Otomatik yerleşim girdisi (Karar 10 §5.1).</summary>
public sealed record LayoutRequest
{
    /// <summary>Yerleştirilecek fotoğraflar (seçili/sıralı).</summary>
    public required IReadOnlyList<PhotoAsset> Photos { get; init; }

    public required SizeMm PageSize { get; init; }

    /// <summary>Taşma payı ve güvenli alan için profil.</summary>
    public required AlbumProfile Profile { get; init; }

    public required IReadOnlyList<TemplateDefinition> Templates { get; init; }

    /// <summary>Mevcut sayfalar; kilitli olanlar korunur, diğerleri yeniden üretilir.</summary>
    public IReadOnlyList<AlbumPage> ExistingPages { get; init; } = [];

    public LayoutOptions Options { get; init; } = new();

    /// <summary>Tohum verilmezse kullanılan varsayılan (genellikle proje kimliğinden).</summary>
    public ulong DefaultSeed { get; init; }

    /// <summary>Oluşturulan sayfaların zaman damgası.</summary>
    public DateTimeOffset Now { get; init; }

    /// <summary>Proje için istek: fotoğraflar projedeki varlıklar, tohum proje kimliğinden.</summary>
    public static LayoutRequest ForProject(AlbumProject project, IReadOnlyList<TemplateDefinition> templates, LayoutOptions options, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        return new LayoutRequest
        {
            Photos = project.Photos,
            PageSize = project.PageSize,
            Profile = project.Profile,
            Templates = templates,
            ExistingPages = project.Pages,
            Options = options with { Design = project.Settings.Design, Story = options.Story ?? project.Story },
            DefaultSeed = LayoutSeed.FromProjectId(project.Id),
            Now = now,
        };
    }
}

/// <summary>Otomatik yerleşim çıktısı.</summary>
public sealed record LayoutResult(
    IReadOnlyList<AlbumPage> Pages,
    ValidationResult Issues,
    ulong Seed,
    IReadOnlyList<string> Explanations);

/// <summary>Otomatik yerleşim motoru arayüzü (Karar 10 §1: UI'dan bağımsız).</summary>
public interface ILayoutEngine
{
    LayoutResult Layout(LayoutRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// MVP otomatik yerleşim: sınıflandır → grupla → şablon seç → eşleştir → çeşitlilik → kilit → doğrula.
/// Saf ve deterministiktir: aynı istek ve tohum her zaman aynı sayfaları üretir.
/// </summary>
public sealed class AutoLayoutEngine : ILayoutEngine
{
    public LayoutResult Layout(LayoutRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Templates.Count == 0)
        {
            throw new ArgumentException("En az bir şablon gerekli.", nameof(request));
        }

        var options = request.Options;
        var seed = options.Seed ?? request.DefaultSeed;
        var random = new DeterministicRandom(seed);
        var pageRect = RectMm.FromSize(request.PageSize);
        var bleedRect = request.Profile.Bleed.Value is { } bleed ? pageRect.Inflate(bleed) : pageRect;
        var issues = new List<ValidationIssue>();
        var explanations = new List<string>();

        // Aşama 6 (hazırlık) — kilitli sayfalar ve kullanılmış fotoğraflar.
        var locked = options.IgnoreLocks
            ? []
            : request.ExistingPages.Select((page, index) => (page, index)).Where(x => x.page.Locked).ToList();
        var used = options.AllowPhotoReuse
            ? new HashSet<string>(StringComparer.Ordinal)
            : locked.SelectMany(x => x.page.Slots).Select(s => s.Placement?.AssetId).OfType<string>().ToHashSet(StringComparer.Ordinal);

        // Aşama 1 — sınıflandırma (yinelenen girdi ve kilitli sayfadaki fotoğraflar çıkarılır).
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = request.Photos.Where(p => p.IsPhoto && !used.Contains(p.Id) && seen.Add(p.Id));
        if (options.Order == PhotoOrder.CaptureTime)
        {
            ordered = ordered.OrderBy(p => p.CapturedAt ?? DateTimeOffset.MaxValue);
        }

        var photos = ordered.Select((p, i) => PhotoClassifier.Classify(p, i)).ToList();

        var selector = new TemplateSelector(
            request.Templates,
            options,
            random,
            request.PageSize.Orientation,
            template => template.Category == TemplateCategory.FullBleed ? bleedRect : pageRect);
        foreach (var lockedTemplate in locked.Select(x => x.page.TemplateId).OfType<string>()
            .Select(id => request.Templates.FirstOrDefault(candidate => candidate.Id == id)).OfType<TemplateDefinition>())
        {
            selector.Remember(lockedTemplate);
        }

        // Aşama 2 — bölümler (hikâye akışı) ve gruplama. Bölüm yoksa tüm fotoğraflar tek akıştır.
        var story = options.Story is { Enabled: true, HasChapters: true } plan ? plan : null;
        var byId = photos.ToDictionary(p => p.Asset.Id, StringComparer.Ordinal);
        List<Segment> segments = story is null
            ? [new Segment(null, photos)]
            : [.. story.Distribute(photos.Select(p => p.Asset)).Select(cp => new Segment(cp.Chapter, [.. cp.Photos.Select(a => byId[a.Id])]))];
        if (story is not null)
        {
            explanations.Add(F("Hikâye akışı: {0} bölüm; her bölüm yeni sayfada başlar.", segments.Count(s => s.Chapter is not null)));
        }

        var groupOptions = story is null ? options : options with { TargetPageCount = null };
        var plannedGroups = new List<(Segment Segment, ClassifiedPhoto? Hero, IReadOnlyList<PageGroup> Groups, List<ClassifiedPhoto> Rest)>();
        foreach (var segment in segments)
        {
            ClassifiedPhoto? hero = null;
            var rest = segment.Photos.ToList();
            if (story is { Openers: true } && segment.Chapter is not null && rest.Count > 0)
            {
                // Açılış fotoğrafı: bölümdeki ilk favori, yoksa ilk yatay, yoksa ilk fotoğraf.
                hero = rest.FirstOrDefault(p => p.Favorite) ?? rest.FirstOrDefault(p => p.Orientation == PageOrientation.Landscape) ?? rest[0];
                rest.Remove(hero);
            }

            var share = options.TargetPageCount is { } t && story is not null && photos.Count > 0
                ? groupOptions with { TargetPageCount = Math.Max(1, (int)Math.Round((double)t * rest.Count / photos.Count)) }
                : groupOptions;
            var segmentGroups = rest.Count == 0 ? [] : PageGrouper.Group(rest.Count, selector.SupportedCounts(), share, random);
            plannedGroups.Add((segment, hero, segmentGroups, rest));
        }

        var groupCount = plannedGroups.Sum(g => g.Groups.Count + (g.Hero is null ? 0 : 1));
        var emptySlots = plannedGroups.Sum(g => g.Groups.Sum(x => x.EmptySlots));
        if (emptySlots > 0)
        {
            issues.Add(new("layout.photos.insufficient", ValidationSeverity.Warning,
                F("Fotoğraf sayısı şablonları doldurmaya yetmiyor; {0} boş yuva kaldı.", emptySlots)));
        }

        var targetUnlocked = options.TargetPageCount is { } target ? target - locked.Count : (int?)null;
        if (targetUnlocked is { } wantedPages && groupCount != wantedPages)
        {
            issues.Add(new("layout.page-count.mismatch", ValidationSeverity.Warning,
                F("{0} fotoğraf, hedeflenen {1} sayfa için uygun değil; {2} sayfa üretildi.", photos.Count, options.TargetPageCount!.Value, groupCount + locked.Count)));
        }

        // Aşama 3–5 — şablon seçimi, eşleştirme, çeşitlilik.
        var reservedIds = locked.Select(x => x.page.Id).ToHashSet(StringComparer.Ordinal);
        var nextId = 1;
        var generated = new List<AlbumPage>(groupCount);
        foreach (var (segment, hero, segmentGroups, rest) in plannedGroups)
        {
            if (hero is not null && segment.Chapter is { } chapter)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var opener = selector.SelectOpener(hero);
                var pageId = NextPageId(reservedIds, ref nextId);
                var page = PageDesigner.Build(pageId, opener.Template, opener.Assignments, options.Design, request.Now);
                generated.Add(page.AddElement(OpenerTitle(chapter.Name, pageRect)));
                explanations.Add(F("Sayfa {0}: «{1}» — {2}", generated.Count, chapter.Name, opener.Reason));
            }

            var offset = 0;
            foreach (var group in segmentGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var groupPhotos = rest.GetRange(offset, group.PhotoCount);
                offset += group.PhotoCount;

                var choice = selector.Select(group, groupPhotos);
                var pageId = NextPageId(reservedIds, ref nextId);
                generated.Add(PageDesigner.Build(pageId, choice.Template, choice.Assignments, options.Design, request.Now));
                explanations.Add(F("Sayfa {0}: {1}", generated.Count, choice.Reason));
            }
        }

        // Aşama 6 — kilitli sayfaları özgün konumlarına geri koy.
        var pages = generated;
        foreach (var (page, index) in locked)
        {
            pages.Insert(Math.Min(index, pages.Count), page);
        }

        // Aşama 7 — doğrulama.
        var assetMap = request.Photos.GroupBy(p => p.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var validation = LayoutValidator.Validate(pages, assetMap, bleedRect, options.AllowPhotoReuse);
        return new LayoutResult(pages, new ValidationResult(issues).Merge(validation), seed, explanations);
    }

    /// <summary>Açılış sayfasının başlığı: alt üçte birde, beyaz el yazısı, her fotoğrafta okunsun diye gölgeli.</summary>
    private static TextElement OpenerTitle(string name, RectMm page)
    {
        var preset = TextPresets.Find("title")!;
        var white = preset with { Font = preset.Font with { Color = Common.RgbaColor.White }, CenterYPercent = 80 };
        return white.Create("text-001", name, page, PageTextEditor.TextLayer) with { Effect = TextEffect.Readable(Common.RgbaColor.White) };
    }

    private static string NextPageId(HashSet<string> reserved, ref int next)
    {
        string id;
        do
        {
            id = "page-" + next.ToString("000", CultureInfo.InvariantCulture);
            next++;
        }
        while (!reserved.Add(id));

        return id;
    }

    private static string F(string format, params object[] args) => string.Format(CultureInfo.CurrentCulture, format, args);
}

/// <summary>Otomatik yerleşimde bir bölümün (ya da bölümsüz akışın) fotoğrafları.</summary>
internal sealed record Segment(StoryChapter? Chapter, List<ClassifiedPhoto> Photos);
