using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;

namespace NexAlbum.Core.Tests;

/// <summary>Testler için deterministik örnek veriler.</summary>
internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    public static readonly Guid ProjectId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    /// <summary>Karar 03 §3.1 örneğine karşılık gelen, eksiksiz ve doğrulanmış 30×30 profil.</summary>
    public static AlbumProfile CompleteProfile() => new()
    {
        Id = "kullanici-profil-01",
        Name = "30×30 Layflat",
        Verified = true,
        CreatedAt = Now,
        UpdatedAt = Now,
        Page = new PageSpec(new SizeMm(300, 300)),
        Bleed = new EdgeSpec(EdgesMm.Uniform(3), Verified: true),
        SafeArea = new EdgeSpec(EdgesMm.Uniform(5), Verified: true),
        Gutter = new GutterSpec(0, verified: true),
        Resolution = new ResolutionSpec(300, 200),
        Color = new ColorSpec(ColorMode.Cmyk, "CoatedFOGRA39.icc"),
        Output = new OutputSpec(cropMarks: true, cropMarkOffsetMm: 3, cropMarkLengthMm: 5),
    };

    public static AlbumProject Project(AlbumProfile? profile = null) =>
        AlbumProject.Create("Ahmet & Zeynep", profile ?? CompleteProfile(), Now, "0.1.0", ProjectId);

    /// <summary>6000×4000 piksel fotoğraf.</summary>
    public static PhotoAsset Asset(string id = "asset-001", int width = 6000, int height = 4000, int? orientation = null, bool missing = false) => new()
    {
        Id = id,
        SourcePath = $@"C:\Photos\{id}.jpg",
        FileName = $"{id}.jpg",
        PixelWidth = width,
        PixelHeight = height,
        ExifOrientation = orientation,
        ImportedAt = Now,
        Missing = missing,
    };

    public static Slot Slot(string id = "slot-001", double x = 20, double y = 20, double width = 260, double height = 260, string? assetId = null) => new()
    {
        Id = id,
        Bounds = new RectMm(x, y, width, height),
        Placement = assetId is null ? null : new PhotoPlacement { AssetId = assetId },
    };

    public static AlbumPage Page(string id = "page-001", params PageElement[] elements) => new()
    {
        Id = id,
        Elements = elements,
        CreatedAt = Now,
        UpdatedAt = Now,
    };

    public static TextElement Text(string id = "text-001", double x = 20, double y = 270, double width = 260, double height = 15) => new()
    {
        Id = id,
        Bounds = new RectMm(x, y, width, height),
        Content = "15 Eylül 2024",
        Font = new FontSpec { Family = "Inter" },
    };
}
