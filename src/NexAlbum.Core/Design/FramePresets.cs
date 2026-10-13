using NexAlbum.Core.Common;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Design;

/// <summary>Hazır çerçeve stilleri. Kimlikler kalıcıdır (proje dosyalarına yazılır); değiştirilmemelidir.</summary>
public static class FramePresets
{
    public const string NoneId = "none";

    public static FrameStyle None { get; } = new() { Id = NoneId, Name = "Çerçevesiz" };

    public static IReadOnlyList<FrameStyle> All { get; } =
    [
        None,
        new() { Id = "hairline-black", Name = "İnce siyah çizgi", StrokeWidthMm = 0.35, StrokeColor = RgbaColor.Black },
        new() { Id = "line-white", Name = "Beyaz kenar", StrokeWidthMm = 1.2, StrokeColor = RgbaColor.White },
        new() { Id = "gold", Name = "Altın çizgi", StrokeWidthMm = 1.0, StrokeColor = RgbaColor.Parse("#C9A227") },
        new() { Id = "silver", Name = "Gümüş çizgi", StrokeWidthMm = 0.8, StrokeColor = RgbaColor.Parse("#A8A9AD") },
        new() { Id = "mat-white", Name = "Beyaz paspartu", MatPercent = EdgePercents.Uniform(4), StrokeWidthMm = 0.2, StrokeColor = RgbaColor.Parse("#C8C8C8") },
        new() { Id = "mat-cream", Name = "Krem paspartu", MatPercent = EdgePercents.Uniform(5), MatColor = RgbaColor.Parse("#F3EAD8") },
        new() { Id = "mat-black", Name = "Siyah paspartu", MatPercent = EdgePercents.Uniform(4), MatColor = RgbaColor.Parse("#1E1E1E") },
        new() { Id = "polaroid", Name = "Polaroid", MatPercent = new EdgePercents(5, 5, 18, 5), Shadow = new ShadowStyle(0.8, 2.5, 0.30) },
        new() { Id = "print-shadow", Name = "Beyaz kenar + gölge", MatPercent = EdgePercents.Uniform(2.5), Shadow = new ShadowStyle(1.0, 3.0, 0.32) },
        new() { Id = "soft-shadow", Name = "Yumuşak gölge", Shadow = new ShadowStyle(1.2, 4.0, 0.38) },
        new() { Id = "rounded", Name = "Yuvarlak köşe", CornerRadiusPercent = 6 },
        new() { Id = "rounded-shadow", Name = "Yuvarlak köşe + gölge", CornerRadiusPercent = 6, Shadow = new ShadowStyle(1.0, 3.5, 0.32) },
        new() { Id = "oval", Name = "Oval", Shape = SlotShape.Circle, StrokeWidthMm = 0.6, StrokeColor = RgbaColor.White, Shadow = new ShadowStyle(0.8, 3.0, 0.28) },

        // Biçimli çerçeveler (Karar 10 Rev. 1): yuvayı keser; kaldırılınca şablon biçimi geri gelir.
        new() { Id = "arch", Name = "Kemer", Shape = SlotShape.Arch, Shadow = new ShadowStyle(0.8, 3.0, 0.26) },
        new() { Id = "arch-gold", Name = "Kemer + altın", Shape = SlotShape.Arch, StrokeWidthMm = 0.8, StrokeColor = RgbaColor.Parse("#C9A227") },
        new() { Id = "arch-mat", Name = "Kemer paspartu", Shape = SlotShape.Arch, MatPercent = EdgePercents.Uniform(4), MatColor = RgbaColor.Parse("#F3EAD8") },
        new() { Id = "hexagon", Name = "Altıgen", Shape = SlotShape.Hexagon, StrokeWidthMm = 0.6, StrokeColor = RgbaColor.White },
        new() { Id = "hexagon-gold", Name = "Altıgen + altın", Shape = SlotShape.Hexagon, StrokeWidthMm = 0.8, StrokeColor = RgbaColor.Parse("#C9A227") },
        new() { Id = "diamond", Name = "Elmas", Shape = SlotShape.Diamond, Shadow = new ShadowStyle(0.8, 3.0, 0.26) },
        new() { Id = "octagon", Name = "Kesik köşe", Shape = SlotShape.Octagon, StrokeWidthMm = 0.35, StrokeColor = RgbaColor.Black },
        new() { Id = "octagon-mat", Name = "Kesik köşe paspartu", Shape = SlotShape.Octagon, MatPercent = EdgePercents.Uniform(4), StrokeWidthMm = 0.2, StrokeColor = RgbaColor.Parse("#C8C8C8") },
        new() { Id = "triangle", Name = "Üçgen", Shape = SlotShape.Triangle },
        new() { Id = "slant", Name = "Eğik", Shape = SlotShape.Slant, Shadow = new ShadowStyle(0.8, 3.0, 0.26) },
    ];

    public static FrameStyle? Find(string? id) => id is null ? null : All.FirstOrDefault(f => f.Id == id);
}
