using System.Globalization;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Validation;

namespace NexAlbum.Core.Layout;

/// <summary>
/// Aşama 7 — üretilen düzenin doğrulanması (Karar 10 §12). Esnetme ve sınır dışı hata; diğerleri uyarıdır.
/// </summary>
public static class LayoutValidator
{
    /// <summary>Bu orandan fazla kırpma kaybı uyarı üretir (Karar 10 §9.8).</summary>
    public const double HighCropLoss = 0.4;

    public static ValidationResult Validate(
        IReadOnlyList<AlbumPage> pages,
        IReadOnlyDictionary<string, PhotoAsset> assets,
        RectMm allowedArea,
        bool allowPhotoReuse)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(assets);
        var issues = new List<ValidationIssue>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var p = 0; p < pages.Count; p++)
        {
            var page = pages[p];
            var no = p + 1;
            var slots = page.Slots.ToList();
            var empty = 0;

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!allowedArea.Contains(slot.Bounds))
                {
                    issues.Add(new("layout.slot.out-of-bounds", ValidationSeverity.Error, F("Sayfa {0}: yuva sayfa dışına taşıyor.", no), page.Id, slot.Id));
                }

                for (var j = i + 1; j < slots.Count; j++)
                {
                    if (slot.Bounds.Intersects(slots[j].Bounds))
                    {
                        issues.Add(new("layout.slot.overlap", ValidationSeverity.Warning, F("Sayfa {0}: yuvalar üst üste biniyor.", no), page.Id, slot.Id));
                    }
                }

                if (slot.Placement is not { } placement)
                {
                    empty++;
                    continue;
                }

                if (slot.CropMode == CropMode.Fill)
                {
                    issues.Add(new("layout.photo.stretched", ValidationSeverity.Error, F("Sayfa {0}: fotoğraf esnetilmiş.", no), page.Id, slot.Id));
                }

                if (!allowPhotoReuse && !seen.TryAdd(placement.AssetId, page.Id) && !page.Locked)
                {
                    issues.Add(new("layout.photo.duplicate", ValidationSeverity.Warning, F("Sayfa {0}: fotoğraf tekrar kullanılmış.", no), page.Id, slot.Id));
                }

                if (assets.TryGetValue(placement.AssetId, out var asset))
                {
                    var slotRatio = slot.Bounds.Width / slot.Bounds.Height;
                    if (slot.CropMode == CropMode.Cover && SlotMatcher.CropLoss(asset.DisplayAspectRatio, slotRatio) > HighCropLoss)
                    {
                        issues.Add(new("layout.crop.high-loss", ValidationSeverity.Warning, F("Sayfa {0}: fotoğrafın büyük kısmı kırpılıyor.", no), page.Id, slot.Id));
                    }

                    if (PhotoClassifier.ClassifyDpi(PlacementDpi.Calculate(asset, slot, placement)) == ResolutionClass.Low)
                    {
                        issues.Add(new("layout.dpi.low", ValidationSeverity.Warning, F("Sayfa {0}: çözünürlük düşük.", no), page.Id, slot.Id));
                    }
                }
            }

            if (empty > 0)
            {
                issues.Add(new("layout.slot.empty", ValidationSeverity.Warning, F("Sayfa {0}: {1} boş yuva.", no, empty), page.Id));
            }
        }

        return new ValidationResult(issues);
    }

    private static string F(string format, params object[] args) => string.Format(CultureInfo.CurrentCulture, format, args);
}
