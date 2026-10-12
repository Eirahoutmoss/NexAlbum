using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Profiles;

/// <summary>
/// Uygulamayla gelen örnek profiller (Karar 03 §5). Gerçek baskı değeri değildir:
/// yalnızca sayfa ölçüsü vardır, taşma payı / güvenli alan / DPI kullanıcıya bırakılır.
/// </summary>
public static class BuiltInProfiles
{
    public const string SampleWarning = "Bu örnek bir profildir. Baskı evinizden aldığınız değerlerle güncelleyin.";

    private static readonly DateTimeOffset Epoch = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<AlbumProfile> All { get; } =
    [
        Create("builtin-15x13", "15×13 cm", 150, 130, "Küçük albüm"),
        Create("builtin-15x21", "15×21 cm", 150, 210, "Klasik oran"),
        Create("builtin-20x20", "20×20 cm", 200, 200, "Kare albüm"),
        Create("builtin-20x30", "20×30 cm", 200, 300, "Yaygın"),
        Create("builtin-25x25", "25×25 cm", 250, 250, "Orta boy"),
        Create("builtin-30x30", "30×30 cm", 300, 300, "Büyük kare"),
        Create("builtin-30x20", "30×20 cm", 300, 200, "Yatay"),
        Create("builtin-30x30-layflat", "30×30 cm layflat", 300, 300, "Çift sayfa", spread: true),
    ];

    public static AlbumProfile? Find(string id) => All.FirstOrDefault(p => p.Id == id);

    private static AlbumProfile Create(string id, string name, double widthMm, double heightMm, string description, bool spread = false) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        Source = ProfileSource.Builtin,
        Verified = false,
        CreatedAt = Epoch,
        UpdatedAt = Epoch,
        Page = new PageSpec(new SizeMm(widthMm, heightMm)),
        Spread = new SpreadSpec(spread),
        Binding = spread ? new BindingSpec("layflat") : null,
        Tags = ["örnek"],
        Warnings = [new ProfileNote(ProfileNoteLevel.Warning, SampleWarning)],
    };
}
