using System.Globalization;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Validation;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Dış kaynak denetimleri. IO Core dışında olduğundan işlev olarak verilir; null olan kontrol atlanır.
/// </summary>
public sealed record PreflightEnvironment
{
    public Func<string, bool>? IccProfileExists { get; init; }

    public Func<string, bool>? FontAvailable { get; init; }

    public static PreflightEnvironment None { get; } = new();
}

/// <summary>
/// Baskı öncesi kontrol (Karar 11 §9.2, Karar 03 §7). Hata dışa aktarmayı engeller.
/// Metin taşması (§9.2 #9) yazı tipi ölçümü gerektirdiğinden Rendering katmanında denetlenir.
/// </summary>
public static class ProjectPreflight
{
    /// <summary>Bu DPI'ın altı baskı için kabul edilemez (Karar 11 §9.2 #6).</summary>
    public const double CriticalDpi = 150;

    public static ValidationResult Check(AlbumProject project, PreflightEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        var env = environment ?? PreflightEnvironment.None;
        var profile = project.Profile;
        var issues = new List<ValidationIssue>();

        if (profile.Page.Size is { } profileSize
            && (!Length.NearlyEqual(profileSize.Width, project.PageSize.Width) || !Length.NearlyEqual(profileSize.Height, project.PageSize.Height)))
        {
            issues.Add(new("project.page-size.profile-mismatch", ValidationSeverity.Warning,
                "Proje sayfa ölçüsü profildekinden farklı.", Suggestion: "Profili veya proje ölçüsünü kontrol edin."));
        }

        if (profile.Color.Mode is not null && string.IsNullOrWhiteSpace(profile.Color.IccProfile))
        {
            issues.Add(new("project.icc.missing", ValidationSeverity.Warning,
                "Renk (ICC) profili belirtilmemiş.", Suggestion: "Baskı evinizden ICC dosyasını isteyin."));
        }

        var pageRect = project.PageRect;
        var bleedRect = profile.Bleed.Value is { } bleed ? pageRect.Inflate(bleed) : pageRect;
        RectMm? safeRect = profile.SafeArea.Value is { } safe ? pageRect.Deflate(safe) : null;
        // Etkin kaynak: düzenlenmiş sürüm varsa çözünürlük ve varlık denetimi ona göre yapılır (Karar 20 §4).
        var assets = project.Assets.ToDictionary(a => a.Id, a => a.Effective(), StringComparer.Ordinal);
        foreach (var asset in project.Assets.Where(a => a.UseEdit && a.Edit is { Missing: true }))
        {
            issues.Add(new("project.photo.edit-missing", ValidationSeverity.Warning,
                Format("{0}: düzenlenmiş sürüm bulunamadı; orijinal kullanılacak.", asset.FileName), ElementId: asset.Id,
                Suggestion: "Düzenlemeyi yeniden uygulayın ya da kaldırın."));
        }

        var assetUse = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        for (var i = 0; i < project.Pages.Count; i++)
        {
            var page = project.Pages[i];
            var pageNo = i + 1;
            CheckBackground(page, page.Background, assets, issues);

            foreach (var element in page.Elements)
            {
                switch (element)
                {
                    case Slot slot:
                        CheckSlot(project, page, pageNo, slot, assets, assetUse, bleedRect, safeRect, issues);
                        break;
                    case TextElement text:
                        CheckText(project, page, pageNo, text, bleedRect, safeRect, env, issues);
                        break;
                    case BackgroundElement background:
                        CheckBackground(page, background.Fill, assets, issues);
                        break;
                }
            }
        }

        foreach (var (assetId, slots) in assetUse.Where(kv => kv.Value.Count > 1))
        {
            issues.Add(new("project.photo.duplicate", ValidationSeverity.Warning,
                Format("Aynı fotoğraf {0} yuvada kullanılmış.", slots.Count), ElementId: assetId));
        }

        var locked = project.Pages.Count(p => p.Locked);
        if (locked > 0)
        {
            issues.Add(new("project.pages.locked", ValidationSeverity.Info, Format("{0} kilitli sayfa var.", locked)));
        }

        return ProfilePreflight.Check(profile, env.IccProfileExists).Merge(new ValidationResult(issues));
    }

    private static void CheckSlot(
        AlbumProject project,
        AlbumPage page,
        int pageNo,
        Slot slot,
        Dictionary<string, PhotoAsset> assets,
        Dictionary<string, List<string>> assetUse,
        RectMm bleedRect,
        RectMm? safeRect,
        List<ValidationIssue> issues)
    {
        if (!bleedRect.Contains(slot.Bounds))
        {
            issues.Add(new("project.element.outside-page", ValidationSeverity.Warning,
                Format("Sayfa {0}: yuva sayfa (taşma payı dahil) dışına çıkıyor.", pageNo), page.Id, slot.Id));
        }

        if (safeRect is { } safe && !safe.Contains(slot.Bounds) && !IsFullBleed(slot.Bounds, project.PageRect))
        {
            issues.Add(new("project.photo.outside-safe-area", ValidationSeverity.Warning,
                Format("Sayfa {0}: fotoğraf güvenli alan dışına taşıyor; kenardaki içerik kesilebilir.", pageNo), page.Id, slot.Id));
        }

        if (slot.Placement is not { } placement)
        {
            issues.Add(new("project.slot.empty", ValidationSeverity.Warning,
                Format("Sayfa {0}: boş fotoğraf yuvası.", pageNo), page.Id, slot.Id, "Fotoğraf yerleştirin veya yuvayı kaldırın."));
            return;
        }

        if (!assets.TryGetValue(placement.AssetId, out var asset))
        {
            issues.Add(new("project.placement.asset-unknown", ValidationSeverity.Error,
                Format("Sayfa {0}: yuva projede olmayan bir fotoğrafa bağlı.", pageNo), page.Id, slot.Id));
            return;
        }

        if (!assetUse.TryGetValue(asset.Id, out var uses))
        {
            uses = [];
            assetUse[asset.Id] = uses;
        }

        uses.Add(slot.Id);

        if (asset.Missing)
        {
            issues.Add(new("project.photo.source-missing", ValidationSeverity.Error,
                Format("Sayfa {0}: kaynak fotoğraf bulunamadı ({1}).", pageNo, asset.FileName), page.Id, slot.Id, "Yeniden bağla"));
        }

        var dpi = PlacementDpi.Calculate(asset, slot, placement);
        var target = slot.TargetDpi ?? project.Profile.Resolution.TargetDpi;
        if (dpi < CriticalDpi)
        {
            issues.Add(new("project.photo.dpi-critical", ValidationSeverity.Error,
                Format("Sayfa {0}: fotoğraf çözünürlüğü çok düşük ({1:0} DPI < {2:0}).", pageNo, dpi, CriticalDpi), page.Id, slot.Id,
                "Daha büyük bir fotoğraf seçin veya yuvayı küçültün."));
        }
        else if (target is { } t && dpi < t)
        {
            issues.Add(new("project.photo.dpi-low", ValidationSeverity.Warning,
                Format("Sayfa {0}: DPI {1:0} (hedef {2}).", pageNo, dpi, t), page.Id, slot.Id));
        }
    }

    private static void CheckText(
        AlbumProject project,
        AlbumPage page,
        int pageNo,
        TextElement text,
        RectMm bleedRect,
        RectMm? safeRect,
        PreflightEnvironment env,
        List<ValidationIssue> issues)
    {
        if (!bleedRect.Contains(text.Bounds))
        {
            issues.Add(new("project.element.outside-page", ValidationSeverity.Warning,
                Format("Sayfa {0}: metin sayfa dışına çıkıyor.", pageNo), page.Id, text.Id));
        }

        if (safeRect is { } safe && !safe.Contains(text.Bounds))
        {
            issues.Add(new("project.text.outside-safe-area", ValidationSeverity.Warning,
                Format("Sayfa {0}: metin güvenli alan dışında.", pageNo), page.Id, text.Id));
        }

        if (GutterZone(project, page) is { } gutter && gutter.Intersects(text.Bounds))
        {
            issues.Add(new("project.text.in-gutter", ValidationSeverity.Warning,
                Format("Sayfa {0}: metin orta cilt alanında.", pageNo), page.Id, text.Id));
        }

        if (env.FontAvailable is { } fontAvailable && !fontAvailable(text.Font.Family))
        {
            issues.Add(new("project.font.missing", ValidationSeverity.Warning,
                Format("Sayfa {0}: yazı tipi bulunamadı ({1}).", pageNo, text.Font.Family), page.Id, text.Id));
        }
    }

    private static void CheckBackground(AlbumPage page, BackgroundFill fill, Dictionary<string, PhotoAsset> assets, List<ValidationIssue> issues)
    {
        if (fill is not { Mode: BackgroundMode.Image } || fill.ImageAssetId is not { } assetId)
        {
            return;
        }

        if (!assets.TryGetValue(assetId, out var asset))
        {
            issues.Add(new("project.placement.asset-unknown", ValidationSeverity.Error,
                "Arka plan projede olmayan bir fotoğrafa bağlı.", page.Id, assetId));
        }
        else if (asset.Missing)
        {
            issues.Add(new("project.photo.source-missing", ValidationSeverity.Error,
                Format("Arka plan fotoğrafı bulunamadı ({0}).", asset.FileName), page.Id, assetId, "Yeniden bağla"));
        }
    }

    /// <summary>Çift sayfa albümde, sayfanın cilde bakan kenarındaki cilt payı şeridi.</summary>
    private static RectMm? GutterZone(AlbumProject project, AlbumPage page)
    {
        var profile = project.Profile;
        if (!profile.Spread.Enabled || profile.Gutter.WidthMm is not { } g || g <= 0)
        {
            return null;
        }

        var size = project.PageSize;
        return page.SpreadPosition switch
        {
            SpreadPosition.Left => new RectMm(size.Width - g, 0, g, size.Height),
            SpreadPosition.Right => new RectMm(0, 0, g, size.Height),
            _ => null,
        };
    }

    /// <summary>Sayfayı tamamen kaplayan (kenardan kenara) fotoğraf bilinçli bir tasarımdır; güvenli alan uyarısı verilmez.</summary>
    private static bool IsFullBleed(RectMm bounds, RectMm page) => bounds.Contains(page);

    private static string Format(string format, params object[] args) => string.Format(CultureInfo.CurrentCulture, format, args);
}
