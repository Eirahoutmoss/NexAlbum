using NexAlbum.Core.Design;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;

namespace NexAlbum.Core.Layout;

/// <summary>Şablon değiştirme sonucu; yeni şablona sığmayan fotoğraflar sayfadan çıkar.</summary>
public sealed record TemplateApplication(AlbumPage Page, IReadOnlyList<string> RemovedAssetIds);

/// <summary>
/// Sayfa tasarım işlemleri: şablondan sayfa kurma, şablon değiştirme, çerçeve ve fon uygulama, boşluk değişince
/// yeniden yerleşim. Otomatik yerleşim ve elle düzenleme aynı kurucuyu kullanır. Toplu işlemler kilitli sayfalara dokunmaz.
/// </summary>
public static class PageDesigner
{
    /// <summary>Şablon yuvasından türetilen kalıcı yuva kimliği.</summary>
    public static string SlotId(string pageId, string templateSlotId) => pageId + "-" + templateSlotId;

    /// <summary>Şablon ve atamalardan sayfa kurar; tasarımdaki çerçeve ve fon uygulanır.</summary>
    public static AlbumPage Build(string pageId, TemplateDefinition template, IReadOnlyList<SlotAssignment> assignments, DesignSettings design, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(design);
        var frame = design.Frame;
        var elements = new List<PageElement>(assignments.Count);
        foreach (var a in assignments)
        {
            var ts = a.Slot.Template;
            var slot = new Slot
            {
                Id = SlotId(pageId, ts.Id),
                Bounds = a.Slot.Bounds,

                // Otomatik yerleşim fotoğrafı asla esnetmez (Karar 10 §9.6): "fill" kaplama olarak kullanılır.
                CropMode = ts.CropMode == CropMode.Fill ? CropMode.Cover : ts.CropMode,
                Placement = a.Photo is { } photo ? new PhotoPlacement { AssetId = photo.Asset.Id } : null,
                Shape = ts.EffectiveShape,
                Polygon = a.Slot.Polygon,
                CornerRadiusMm = ts.CornerRadiusMm,
            };
            elements.Add(frame.Id == FramePresets.NoneId ? slot : frame.ApplyTo(slot));
        }

        return new AlbumPage
        {
            Id = pageId,
            TemplateId = template.Id,
            Background = design.Background ?? template.Background ?? BackgroundFill.White,
            Elements = elements,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Sayfadaki tüm yuvalara çerçeve stilini uygular ("Çerçevesiz" stilleri temizler).</summary>
    public static AlbumPage ApplyFrame(AlbumPage page, FrameStyle frame, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(frame);
        return page with
        {
            Elements = [.. page.Elements.Select(e => e is Slot s ? frame.ApplyTo(s) : e)],
            UpdatedAt = now,
        };
    }

    public static AlbumPage ApplyBackground(AlbumPage page, BackgroundFill fill, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(fill);
        return page with { Background = fill, UpdatedAt = now };
    }

    /// <summary>
    /// Sayfaya başka bir şablon uygular. Mevcut fotoğraflar okuma sırasıyla (yukarıdan aşağı, soldan sağa) alınır ve
    /// yeni yuvalara en uygun biçimde eşlenir. Sayfanın çerçeve stili, fonu, metinleri ve kilidi korunur.
    /// </summary>
    public static TemplateApplication ApplyTemplate(AlbumPage page, TemplateDefinition template, AlbumProject project, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(project);

        var design = project.Settings.Design;
        var styleId = page.Slots.Select(s => s.StyleId).FirstOrDefault(id => id is not null);
        if (styleId is not null && FramePresets.Find(styleId) is not null)
        {
            design = design with { FrameStyleId = styleId };
        }

        var assets = project.Assets.ToDictionary(a => a.Id, StringComparer.Ordinal);
        var photos = page.Slots
            .Where(s => s.Placement is not null)
            .OrderBy(s => s.Bounds.Y).ThenBy(s => s.Bounds.X)
            .Select(s => assets.GetValueOrDefault(s.Placement!.AssetId))
            .OfType<PhotoAsset>()
            .Select((a, i) => PhotoClassifier.Classify(a, i))
            .ToList();

        var slots = LayoutSlots(template, project, design);
        var kept = photos.Take(slots.Count).ToList();
        var removed = photos.Skip(slots.Count).Select(p => p.Asset.Id).ToList();
        var built = Build(page.Id, template, SlotMatcher.Match(kept, slots), design, now);

        var others = page.Elements.Where(e => e is not Slot);
        var result = built with
        {
            Type = page.Type,
            SpreadPosition = page.SpreadPosition,
            Locked = page.Locked,
            Notes = page.Notes,
            Background = page.Background,
            Elements = [.. built.Elements, .. others],
            CreatedAt = page.CreatedAt,
        };
        return new TemplateApplication(result, removed);
    }

    /// <summary>
    /// Boşluk/kenar payı değişince sayfayı kendi şablonuyla yeniden ölçülendirir; fotoğraflar ve çerçeveler korunur.
    /// Şablonu bilinmeyen sayfa olduğu gibi döner.
    /// </summary>
    public static AlbumPage Relayout(AlbumPage page, AlbumProject project, IReadOnlyList<TemplateDefinition> templates, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(templates);
        var template = templates.FirstOrDefault(t => t.Id == page.TemplateId);
        if (template is null)
        {
            return page;
        }

        var design = project.Settings.Design;
        var resolved = LayoutSlots(template, project, design);
        var elements = new List<PageElement>();
        foreach (var ls in resolved)
        {
            var id = SlotId(page.Id, ls.Template.Id);
            var existing = page.FindElement(id) as Slot;
            var slot = existing is null
                ? new Slot
                {
                    Id = id,
                    Bounds = ls.Bounds,
                    Polygon = ls.Polygon,
                    Shape = ls.Template.EffectiveShape,
                    CornerRadiusMm = ls.Template.CornerRadiusMm,
                    CropMode = ls.Template.CropMode == CropMode.Fill ? CropMode.Cover : ls.Template.CropMode,
                }
                : existing with { Bounds = ls.Bounds, Polygon = ls.Polygon };
            var frame = FramePresets.Find(slot.StyleId) ?? (existing is null ? design.Frame : null);
            elements.Add(frame is null ? slot : frame.ApplyTo(slot));
        }

        elements.AddRange(page.Elements.Where(e => e is not Slot));
        return page with { Elements = elements, UpdatedAt = now };
    }

    /// <summary>Kilitsiz tüm sayfalara bir değişiklik uygular; değişen sayfa sayısını döndürür.</summary>
    public static (AlbumProject Project, int Changed) ApplyToUnlocked(AlbumProject project, Func<AlbumPage, AlbumPage> change)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(change);
        var changed = 0;
        var pages = project.Pages.Select(p =>
        {
            if (p.Locked)
            {
                return p;
            }

            changed++;
            return change(p);
        }).ToList();
        return (project.WithPages(pages), changed);
    }

    /// <summary>Proje tasarım ayarını değiştirir (sayfalara dokunmaz).</summary>
    public static AlbumProject WithDesign(AlbumProject project, DesignSettings design)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(design);
        return project with { Settings = project.Settings with { Design = design } };
    }

    /// <summary>Kenarsız (fullBleed) şablonlar taşma payı alanını, diğerleri bitmiş sayfayı kullanır.</summary>
    public static RectMm AreaFor(TemplateDefinition template, AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(project);
        var page = project.PageRect;
        return template.Category == TemplateCategory.FullBleed && project.Profile.Bleed.Value is { } bleed ? page.Inflate(bleed) : page;
    }

    private static List<LayoutSlot> LayoutSlots(TemplateDefinition template, AlbumProject project, DesignSettings design) =>
        [.. template.Resolve(AreaFor(template, project), design.GapPercent, design.MarginPercent)
            .Zip(template.Slots, (r, s) => new LayoutSlot(s, r.Bounds, r.Polygon))];
}
