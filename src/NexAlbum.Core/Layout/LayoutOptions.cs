using NexAlbum.Core.Common;
using NexAlbum.Core.Design;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;

namespace NexAlbum.Core.Layout;

/// <summary>Üretim modu (Karar 10 §13).</summary>
public enum LayoutMode
{
    /// <summary>En kısa yoldan albüm; dengeli dağıtım, en az çeşitlilik denetimi.</summary>
    Quick,

    /// <summary>Çeşitlilik ve kalite: değişken sayfa yoğunluğu, kategori ritmi (varsayılan).</summary>
    Balanced,

    /// <summary>Kullanıcı parametreleri kesin: sabit sayfa yoğunluğu, seçili şablonlar.</summary>
    Controlled,
}

/// <summary>Fotoğrafların sayfalara diziliş sırası.</summary>
public enum PhotoOrder
{
    /// <summary>EXIF çekim zamanına göre (düğün akışını korur); zamanı olmayanlar sona, kendi sıralarıyla.</summary>
    CaptureTime,

    /// <summary>Verildiği sırayla.</summary>
    AsGiven,
}

/// <summary>Otomatik yerleşim parametreleri (Karar 10 §7.2, §13).</summary>
public sealed record LayoutOptions
{
    public LayoutMode Mode { get; init; } = LayoutMode.Balanced;

    public int MinPhotosPerPage { get; init => field = Guard.Positive(value, nameof(MinPhotosPerPage)); } = 1;

    public int MaxPhotosPerPage { get; init => field = Guard.Positive(value, nameof(MaxPhotosPerPage)); } = 6;

    /// <summary>Kontrollü modda her sayfadaki fotoğraf sayısı.</summary>
    public int? FixedPhotosPerPage { get; init => field = Guard.PositiveOrNull(value, nameof(FixedPhotosPerPage)); }

    /// <summary>Hedef sayfa sayısı; verilmezse otomatik.</summary>
    public int? TargetPageCount { get; init => field = Guard.PositiveOrNull(value, nameof(TargetPageCount)); }

    /// <summary>Tohum; aynı tohum aynı sonucu verir. Null ise proje kimliğinden türetilir.</summary>
    public ulong? Seed { get; init; }

    public PhotoOrder Order { get; init; } = PhotoOrder.CaptureTime;

    /// <summary>Bir fotoğrafın birden fazla sayfada kullanılmasına izin (Karar 10 §11.5; varsayılan hayır).</summary>
    public bool AllowPhotoReuse { get; init; }

    /// <summary>"Sıfırdan oluştur": kilitler yok sayılır (Karar 10 §11.4; arayüz onay istemeli).</summary>
    public bool IgnoreLocks { get; init; }

    /// <summary>Tercih edilen kategoriler (puanlamada bonus; kontrollü modda filtre).</summary>
    public IReadOnlyList<TemplateCategory> PreferredCategories { get; init => field = value ?? throw new ArgumentNullException(nameof(PreferredCategories)); } = [];

    /// <summary>Çerçeve, boşluk ve fon (proje tasarım ayarlarından gelir).</summary>
    public DesignSettings Design { get; init => field = value ?? throw new ArgumentNullException(nameof(Design)); } = DesignSettings.Default;

    /// <summary>
    /// Hikâye akışı (Karar 10 Rev. 2): bölümler etkinse her bölüm yeni sayfada başlar, istenirse açılış sayfasıyla.
    /// Null ya da bölümsüz plan: fotoğraflar tek akış halinde dizilir.
    /// </summary>
    public StoryPlan? Story { get; init; }

    /// <summary>Yalnızca bu şablonlar kullanılsın (boşsa hepsi).</summary>
    public IReadOnlyList<string> AllowedTemplateIds { get; init => field = value ?? throw new ArgumentNullException(nameof(AllowedTemplateIds)); } = [];
}
