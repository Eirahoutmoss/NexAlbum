using System.Globalization;
using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;

namespace NexAlbum.Core.Layout;

/// <summary>
/// Kapak yaprağı (taşma payı hariç, mm): soldan sağa arka kapak, sırt, ön kapak. Kapak tek parça basılır; sırt genişliği
/// profilden gelir (Karar 03 §8), uygulama hesaplamaz.
/// </summary>
public sealed record CoverGeometry(SizeMm Sheet, RectMm Back, RectMm Spine, RectMm Front)
{
    /// <summary>Katlama çizgileri (x, mm): arka|sırt ve sırt|ön.</summary>
    public IReadOnlyList<double> Folds => [Spine.X, Spine.Right];
}

/// <summary>
/// Kapak (Karar 03 Rev. 1, Karar 09 Rev. 7): profildeki kapak ölçülerinden yaprak geometrisi ve otomatik kapak tasarımı.
/// Değer uydurulmaz: kapak tipi "yok" ise, ön kapak ölçüsü ya da sırt genişliği girilmemişse kapak kurulmaz ve neden bildirilir.
/// Arka kapak ölçüsü girilmemişse ön kapakla aynı kabul edilir (bilgi notu verilir). Saftır ve deterministiktir.
/// </summary>
public static class CoverLayout
{
    /// <summary>Kapak sayfasının kalıcı kimliği.</summary>
    public const string CoverPageId = "cover";

    /// <summary>Profilden kapak geometrisi; kurulamıyorsa null ve nedenler.</summary>
    public static CoverGeometry? Geometry(AlbumProfile profile, out IReadOnlyList<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var list = new List<ValidationIssue>();
        issues = list;
        if (profile.Cover is not { Type: not CoverType.None } cover)
        {
            list.Add(new("cover.none", ValidationSeverity.Error, "Profilde kapak yok. Profil düzenleyicide kapak tipini ve ölçülerini girin."));
            return null;
        }

        if (cover.Front is null)
        {
            list.Add(new("cover.front.missing", ValidationSeverity.Error, "Ön kapak ölçüsü girilmemiş. Baskı evinizden kapak ölçülerini isteyin."));
        }

        if (cover.Spine?.WidthMm is null)
        {
            list.Add(new("cover.spine.missing", ValidationSeverity.Error, "Sırt genişliği girilmemiş. Baskı evinizden sırt genişliğini isteyin (sayfa sayısına göre değişir)."));
        }

        if (cover.Front is not { } f || cover.Spine?.WidthMm is not { } spine)
        {
            return null;
        }

        var back = cover.Back ?? f;
        if (cover.Back is null)
        {
            list.Add(new("cover.back.assumed", ValidationSeverity.Info, "Arka kapak ölçüsü girilmemiş; ön kapakla aynı alındı."));
        }

        var height = Math.Max(f.Height, back.Height);
        var sheet = new SizeMm(back.Width + spine + f.Width, height);
        return new CoverGeometry(
            sheet,
            new RectMm(0, 0, back.Width, height),
            new RectMm(back.Width, 0, spine, height),
            new RectMm(back.Width + spine, 0, f.Width, height));
    }

    /// <summary>
    /// Otomatik kapak: ön kapakta seçilen fotoğraf (taşma payına kadar) ve albüm adı; arka kapakta ikinci fotoğraf ortada,
    /// paspartulu; sırt boş (fon rengi). Fotoğraf seçimi: favoriler önce, sonra yatay/dikey uyumu, sonra çekim sırası.
    /// </summary>
    public static AlbumPage Create(AlbumProject project, CoverGeometry geometry, DateTimeOffset now, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(geometry);
        var bleed = project.Profile.Bleed.Value ?? EdgesMm.Uniform(0);
        var front = geometry.Front;
        var back = geometry.Back;

        // Ön kapak fotoğrafı yaprağın sağ kenarındaki, üst ve alttaki taşma payına uzanır; sırt tarafında katlama çizgisinde biter.
        var frontSlot = new RectMm(front.X, -bleed.Top, front.Width + bleed.Right, front.Height + bleed.Top + bleed.Bottom);
        var backWidth = back.Width * 0.62;
        var backHeight = back.Height * 0.46;
        var backSlot = new RectMm(back.X + ((back.Width - backWidth) / 2), back.Y + (back.Height * 0.2), backWidth, backHeight);

        var photos = project.Photos.Where(p => !p.Missing).ToList();
        var frontPhoto = Pick(photos, frontSlot.Width / frontSlot.Height, null);
        var backPhoto = Pick(photos, backSlot.Width / backSlot.Height, frontPhoto?.Id);

        var elements = new List<PageElement>
        {
            new Slot
            {
                Id = "cover-front",
                Bounds = frontSlot,
                Placement = frontPhoto is null ? null : new PhotoPlacement { AssetId = frontPhoto.Id },
                ZIndex = 1,
            },
            new Slot
            {
                Id = "cover-back",
                Bounds = backSlot,
                Placement = backPhoto is null ? null : new PhotoPlacement { AssetId = backPhoto.Id },
                ZIndex = 1,
            },
        };

        var preset = TextPresets.Find("title")!;
        var white = preset with { Font = preset.Font with { Color = RgbaColor.White }, CenterYPercent = 76, WidthPercent = 80 };
        var text = white.Create("text-001", string.IsNullOrWhiteSpace(title) ? project.Name : title.Trim(), front, PageTextEditor.TextLayer);
        elements.Add(text with { Effect = TextEffect.Readable(RgbaColor.White) });

        return new AlbumPage
        {
            Id = CoverPageId,
            Type = PageType.FrontCover,
            Elements = elements,
            Background = BackgroundFill.Solid(RgbaColor.Parse("#F4F0E8")),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Kapak yaprağı ölçüsü kapak sayfasından okunur (sırt genişliği değişti mi denetimi için).</summary>
    public static string Describe(CoverGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        return string.Format(
            CultureInfo.CurrentCulture,
            "{0:0.#} × {1:0.#} mm · arka {2:0.#} + sırt {3:0.#} + ön {4:0.#}",
            geometry.Sheet.Width,
            geometry.Sheet.Height,
            geometry.Back.Width,
            geometry.Spine.Width,
            geometry.Front.Width);
    }

    /// <summary>
    /// Kapak ön kontrolü: profil artık kapak kuramıyorsa hata; ön kapak yuvası katlama çizgisinde değilse (sırt genişliği ya da
    /// kapak ölçüsü sonradan değişmiş) uyarı; sırt genişliği her zaman baskı evinizle teyit edilmeli (bilgi).
    /// </summary>
    public static IReadOnlyList<ValidationIssue> Check(AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.Cover is not { } cover)
        {
            return [];
        }

        var geometry = Geometry(project.Profile, out var problems);
        var issues = new List<ValidationIssue>();
        if (geometry is null)
        {
            issues.AddRange(problems.Where(p => p.Severity == ValidationSeverity.Error)
                .Select(p => p with { Code = "cover.profile." + p.Code, Severity = ValidationSeverity.Warning, Message = "Kapak: " + p.Message }));
            return issues;
        }

        if (cover.Slots.FirstOrDefault(s => s.Id == "cover-front") is { } front && !Length.NearlyEqual(front.Bounds.X, geometry.Front.X))
        {
            issues.Add(new("cover.outdated", ValidationSeverity.Warning,
                "Kapak, profildeki güncel sırt genişliği/kapak ölçüsünden önce oluşturulmuş.", Suggestion: "«Kapağı yeniden oluştur» ile güncelleyin."));
        }

        issues.Add(new("cover.spine.confirm", ValidationSeverity.Info,
            string.Format(CultureInfo.CurrentCulture, "Kapak sırtı {0:0.#} mm ({1} sayfa). Sayfa sayısı değiştiyse sırt genişliğini baskı evinizle teyit edin.", geometry.Spine.Width, project.Pages.Count)));
        return issues;
    }

    private static PhotoAsset? Pick(List<PhotoAsset> photos, double aspect, string? exclude)
    {
        var landscapeSlot = aspect >= 1;
        return photos
            .Where(p => !string.Equals(p.Id, exclude, StringComparison.Ordinal))
            .OrderByDescending(p => p.Favorite)
            .ThenByDescending(p => (p.DisplayAspectRatio >= 1) == landscapeSlot)
            .ThenBy(p => p.CapturedAt ?? DateTimeOffset.MaxValue)
            .ThenBy(p => p.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
