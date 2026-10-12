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
            Photos = project.Assets,
            PageSize = project.PageSize,
            Profile = project.Profile,
            Templates = templates,
            ExistingPages = project.Pages,
            Options = options,
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
        var ordered = request.Photos.Where(p => !used.Contains(p.Id) && seen.Add(p.Id));
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

        // Aşama 2 — gruplama.
        var groups = PageGrouper.Group(photos.Count, selector.SupportedCounts(), options, random);
        var emptySlots = groups.Sum(g => g.EmptySlots);
        if (emptySlots > 0)
        {
            issues.Add(new("layout.photos.insufficient", ValidationSeverity.Warning,
                F("Fotoğraf sayısı şablonları doldurmaya yetmiyor; {0} boş yuva kaldı.", emptySlots)));
        }

        var targetUnlocked = options.TargetPageCount is { } target ? target - locked.Count : (int?)null;
        if (targetUnlocked is { } wantedPages && groups.Count != wantedPages)
        {
            issues.Add(new("layout.page-count.mismatch", ValidationSeverity.Warning,
                F("{0} fotoğraf, hedeflenen {1} sayfa için uygun değil; {2} sayfa üretildi.", photos.Count, options.TargetPageCount!.Value, groups.Count + locked.Count)));
        }

        // Aşama 3–5 — şablon seçimi, eşleştirme, çeşitlilik.
        var reservedIds = locked.Select(x => x.page.Id).ToHashSet(StringComparer.Ordinal);
        var nextId = 1;
        var generated = new List<AlbumPage>(groups.Count);
        var offset = 0;
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var groupPhotos = photos.GetRange(offset, group.PhotoCount);
            offset += group.PhotoCount;

            var choice = selector.Select(group, groupPhotos);
            var pageId = NextPageId(reservedIds, ref nextId);
            generated.Add(BuildPage(pageId, choice, request.Now));
            explanations.Add(F("Sayfa {0}: {1}", generated.Count, choice.Reason));
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

    private static AlbumPage BuildPage(string pageId, TemplateChoice choice, DateTimeOffset now)
    {
        var elements = new List<PageElement>(choice.Assignments.Count);
        foreach (var a in choice.Assignments)
        {
            var template = a.Slot.Template;
            elements.Add(new Slot
            {
                Id = pageId + "-" + template.Id,
                Bounds = a.Slot.Bounds,

                // Otomatik yerleşim fotoğrafı asla esnetmez (Karar 10 §9.6): "fill" yuvası kaplama olarak kullanılır.
                CropMode = template.CropMode == CropMode.Fill ? CropMode.Cover : template.CropMode,
                Placement = a.Photo is { } photo ? new PhotoPlacement { AssetId = photo.Asset.Id } : null,
                Shape = template.Shape,
                CornerRadiusMm = template.CornerRadiusMm,
            });
        }

        return new AlbumPage
        {
            Id = pageId,
            TemplateId = choice.Template.Id,
            Background = choice.Template.Background ?? BackgroundFill.White,
            Elements = elements,
            CreatedAt = now,
            UpdatedAt = now,
        };
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
