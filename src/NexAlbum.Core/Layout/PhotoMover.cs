using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Layout;

/// <summary>Sayfalar arası fotoğraf yerleştirmenin sonucu.</summary>
public enum PhotoMoveResult
{
    /// <summary>Fotoğraf hedef sayfaya yerleşti.</summary>
    Done,

    /// <summary>Hedef sayfada boş yuva yok.</summary>
    NoEmptySlot,

    /// <summary>Kaynak ya da hedef sayfa kilitli.</summary>
    Locked,

    /// <summary>Fotoğraf zaten hedef sayfada (ya da kaynak ile hedef aynı).</summary>
    AlreadyThere,

    /// <summary>Sayfa, yuva ya da fotoğraf bulunamadı.</summary>
    NotFound,
}

/// <summary>
/// Fotoğrafı başka bir sayfaya taşır ya da koyar: hedefteki boş yuvalardan fotoğrafın oranına en yakın olanı seçilir
/// (eşitlikte şablon sırası; deterministik). Kaydırma ve yakınlaştırma sıfırlanır, renk ayarları korunur. Saftır.
/// </summary>
public static class PhotoMover
{
    /// <summary>Fotoğrafı (galeriden) hedef sayfanın en uygun boş yuvasına koyar.</summary>
    public static (AlbumProject Project, PhotoMoveResult Result) PlaceOnPage(AlbumProject project, string targetPageId, string assetId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.FindPage(targetPageId) is not { } target || project.FindAsset(assetId) is not { } asset)
        {
            return (project, PhotoMoveResult.NotFound);
        }

        if (target.Locked)
        {
            return (project, PhotoMoveResult.Locked);
        }

        if (target.UsesAsset(assetId))
        {
            return (project, PhotoMoveResult.AlreadyThere);
        }

        return BestEmptySlot(target, asset.DisplayAspectRatio) is { } slot
            ? (project.ReplacePage(target.ReplaceElement(slot with { Placement = new PhotoPlacement { AssetId = assetId } }) with { UpdatedAt = now }).Touch(now), PhotoMoveResult.Done)
            : (project, PhotoMoveResult.NoEmptySlot);
    }

    /// <summary>Yuvadaki fotoğrafı başka bir sayfaya taşır; kaynak yuva boşalır.</summary>
    public static (AlbumProject Project, PhotoMoveResult Result) MoveToPage(AlbumProject project, string sourcePageId, string slotId, string targetPageId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.FindPage(sourcePageId) is not { } source || project.FindPage(targetPageId) is not { } target
            || source.FindElement(slotId) is not Slot { Placement: { } placement } origin
            || project.FindAsset(placement.AssetId) is not { } asset)
        {
            return (project, PhotoMoveResult.NotFound);
        }

        if (string.Equals(source.Id, target.Id, StringComparison.Ordinal) || target.UsesAsset(asset.Id))
        {
            return (project, PhotoMoveResult.AlreadyThere);
        }

        if (source.Locked || target.Locked)
        {
            return (project, PhotoMoveResult.Locked);
        }

        if (BestEmptySlot(target, asset.DisplayAspectRatio) is not { } slot)
        {
            return (project, PhotoMoveResult.NoEmptySlot);
        }

        var moved = Reset(placement);
        var updated = project
            .ReplacePage(source.ReplaceElement(origin with { Placement = null }) with { UpdatedAt = now })
            .ReplacePage(target.ReplaceElement(slot with { Placement = moved }) with { UpdatedAt = now })
            .Touch(now);
        return (updated, PhotoMoveResult.Done);
    }

    /// <summary>
    /// Yuvadaki fotoğrafı başka bir sayfanın belirli yuvasına taşır (kitap görünümünde karşı sayfaya bırakma). Hedef doluysa
    /// iki fotoğraf yer değiştirir.
    /// </summary>
    public static (AlbumProject Project, PhotoMoveResult Result) MoveToSlot(
        AlbumProject project, string sourcePageId, string slotId, string targetPageId, string targetSlotId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.FindPage(sourcePageId) is not { } source || project.FindPage(targetPageId) is not { } target
            || source.FindElement(slotId) is not Slot { Placement: { } placement } origin
            || target.FindElement(targetSlotId) is not Slot to)
        {
            return (project, PhotoMoveResult.NotFound);
        }

        var incoming = to.Placement;
        if (string.Equals(source.Id, target.Id, StringComparison.Ordinal)
            || target.Slots.Any(s => s.Id != to.Id && s.Placement?.AssetId == placement.AssetId)
            || (incoming is not null && source.Slots.Any(s => s.Id != origin.Id && s.Placement?.AssetId == incoming.AssetId)))
        {
            return (project, PhotoMoveResult.AlreadyThere);
        }

        if (source.Locked || target.Locked)
        {
            return (project, PhotoMoveResult.Locked);
        }

        var updated = project
            .ReplacePage(source.ReplaceElement(origin with { Placement = Reset(incoming) }) with { UpdatedAt = now })
            .ReplacePage(target.ReplaceElement(to with { Placement = Reset(placement) }) with { UpdatedAt = now })
            .Touch(now);
        return (updated, PhotoMoveResult.Done);
    }

    /// <summary>Boş yuvalardan oranı fotoğrafa en yakın olanı (logaritmik oran farkı); yoksa null.</summary>
    public static Slot? BestEmptySlot(AlbumPage page, double photoAspect)
    {
        ArgumentNullException.ThrowIfNull(page);
        Slot? best = null;
        var bestCost = double.PositiveInfinity;
        foreach (var slot in page.Slots.Where(s => s.Placement is null))
        {
            var aspect = slot.Bounds.Width / slot.Bounds.Height;
            var cost = photoAspect > 0 ? Math.Abs(Math.Log(aspect / photoAspect)) : 0;
            if (cost < bestCost - 1e-12)
            {
                best = slot;
                bestCost = cost;
            }
        }

        return best;
    }

    private static PhotoPlacement? Reset(PhotoPlacement? placement) =>
        placement is null ? null : placement with { OffsetX = 0, OffsetY = 0, Scale = 1 };
}
