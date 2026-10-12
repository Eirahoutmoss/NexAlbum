using NexAlbum.Core.Common;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Design;

/// <summary>Hazır fon.</summary>
public sealed record BackgroundPreset(string Id, string Name, BackgroundFill Fill);

/// <summary>Düğün albümlerine uygun düz renk ve gradyan fonlar.</summary>
public static class BackgroundPresets
{
    public static IReadOnlyList<BackgroundPreset> Colors { get; } =
    [
        Solid("white", "Beyaz", "#FFFFFF"),
        Solid("off-white", "Kırık beyaz", "#FAF7F2"),
        Solid("ivory", "Fildişi", "#F6F1E7"),
        Solid("cream", "Krem", "#EFE6D5"),
        Solid("champagne", "Şampanya", "#E8D8B8"),
        Solid("beige", "Bej", "#E3D5C0"),
        Solid("blush", "Pudra", "#F2DCDB"),
        Solid("dusty-rose", "Gül kurusu", "#C9A0A0"),
        Solid("sage", "Adaçayı", "#B7C4B1"),
        Solid("olive", "Zeytin", "#7D8471"),
        Solid("dusty-blue", "Toz mavi", "#A9BCD0"),
        Solid("navy", "Gece mavisi", "#1F2A44"),
        Solid("burgundy", "Bordo", "#6D1F2B"),
        Solid("light-gray", "Açık gri", "#E6E6E6"),
        Solid("charcoal", "Antrasit", "#3A3A3A"),
        Solid("black", "Siyah", "#111111"),
    ];

    public static IReadOnlyList<BackgroundPreset> Gradients { get; } =
    [
        Gradient("dawn", "Şafak", "#FAF7F2", "#F2DCDB", 90),
        Gradient("champagne-glow", "Şampanya ışığı", "#FFFFFF", "#E8D8B8", 90),
        Gradient("golden-hour", "Altın saat", "#FFF8E7", "#E3C77A", 45),
        Gradient("rose", "Gül", "#F2DCDB", "#C9A0A0", 90),
        Gradient("sage-mist", "Adaçayı sisi", "#F6F1E7", "#B7C4B1", 90),
        Gradient("mist", "Sis", "#FFFFFF", "#D9D9D9", 90),
        Gradient("ocean", "Okyanus", "#A9BCD0", "#1F2A44", 90),
        Gradient("night", "Gece", "#1F2A44", "#111111", 90),
        Gradient("velvet", "Kadife", "#6D1F2B", "#2B0C12", 135),
    ];

    public static IReadOnlyList<BackgroundPreset> All { get; } = [.. Colors, .. Gradients];

    public static BackgroundPreset? Find(string id) => All.FirstOrDefault(p => p.Id == id);

    private static BackgroundPreset Solid(string id, string name, string hex) =>
        new(id, name, BackgroundFill.Solid(RgbaColor.Parse(hex)));

    private static BackgroundPreset Gradient(string id, string name, string from, string to, double angle) =>
        new(id, name, new BackgroundFill
        {
            Mode = BackgroundMode.Gradient,
            Color = null,
            Gradient = new GradientSpec(RgbaColor.Parse(from), RgbaColor.Parse(to), angle),
        });
}
