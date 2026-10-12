using NexAlbum.Core.Common;

namespace NexAlbum.Core.Projects;

/// <summary>
/// Albüm sayfası (Karar 09 §5). Sırası, projedeki konumundan türetilir; ayrıca saklanmaz.
/// </summary>
public sealed record AlbumPage
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public PageType Type { get; init; } = PageType.Content;

    public SpreadPosition? SpreadPosition { get; init; }

    /// <summary>Kilitli sayfa otomatik yerleşimde değiştirilmez (Karar 10).</summary>
    public bool Locked { get; init; }

    public string? Notes { get; init; }

    public BackgroundFill Background { get; init => field = value ?? throw new ArgumentNullException(nameof(Background)); } = BackgroundFill.White;

    public IReadOnlyList<PageElement> Elements { get; init => field = ValidateElements(value); } = [];

    public string? TemplateId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public IEnumerable<Slot> Slots => Elements.OfType<Slot>();

    /// <summary>Öğeler katman sırasına göre (alttan üste); eşitlikte ekleme sırası korunur.</summary>
    public IEnumerable<PageElement> ElementsInDrawOrder => Elements.OrderBy(e => e.ZIndex);

    public PageElement? FindElement(string id) => Elements.FirstOrDefault(e => e.Id == id);

    public AlbumPage AddElement(PageElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return this with { Elements = [.. Elements, element] };
    }

    public AlbumPage ReplaceElement(PageElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var index = IndexOf(element.Id);
        var list = Elements.ToList();
        list[index] = element;
        return this with { Elements = list };
    }

    public AlbumPage RemoveElement(string id)
    {
        IndexOf(id);
        return this with { Elements = [.. Elements.Where(e => e.Id != id)] };
    }

    /// <summary>Bir varlığa ait tüm yerleşimleri yuvalardan kaldırır.</summary>
    public AlbumPage ClearPlacementsOf(string assetId) => this with
    {
        Elements = [.. Elements.Select(e => e is Slot { Placement: { } p } s && p.AssetId == assetId ? s with { Placement = null } : e)],
    };

    public bool UsesAsset(string assetId) => Slots.Any(s => s.Placement?.AssetId == assetId)
        || Background.ImageAssetId == assetId
        || Elements.OfType<BackgroundElement>().Any(b => b.Fill.ImageAssetId == assetId);

    private int IndexOf(string id)
    {
        for (var i = 0; i < Elements.Count; i++)
        {
            if (Elements[i].Id == id)
            {
                return i;
            }
        }

        throw new KeyNotFoundException($"Öğe bulunamadı: {id}");
    }

    private static IReadOnlyList<PageElement> ValidateElements(IReadOnlyList<PageElement>? elements)
    {
        var list = Guard.NotNullItems(elements, nameof(Elements));
        var duplicate = list.GroupBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Aynı kimliğe sahip birden fazla öğe: {duplicate.Key}", nameof(Elements));
        }

        return list;
    }
}
