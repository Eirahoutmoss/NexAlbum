using NexAlbum.Core.Common;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Projects;
using NexAlbum.Core.Templates;

namespace NexAlbum.Persistence.Tests;

/// <summary>Tüm alanları dolu, gerçekçi örnek proje (round-trip testleri için).</summary>
internal static class Samples
{
    public static readonly DateTimeOffset Now = new(2026, 10, 12, 10, 0, 0, TimeSpan.Zero);

    public static AlbumProfile FullProfile() => new()
    {
        Id = "kullanici-profil-01",
        Name = "Fotoğrafçı Ahmet — 30×30 Layflat",
        Description = "Kendi stüdyom için",
        Source = ProfileSource.User,
        SourceNote = "XYZ Matbaa",
        Verified = true,
        VerifiedAt = Now,
        VerifiedBy = "Kullanıcı beyanı",
        CreatedAt = Now.AddDays(-5),
        UpdatedAt = Now,
        Version = "1.2.0",
        Page = new PageSpec(new SizeMm(300, 300)),
        Spread = new SpreadSpec(true, "Çift sayfa"),
        Bleed = new EdgeSpec(new EdgesMm(3, 3, 3, 3), Verified: true, Note: "matbaa"),
        SafeArea = new EdgeSpec(new EdgesMm(5, 6, 7, 8), Verified: true),
        Gutter = new GutterSpec(4, verified: true, note: "layflat"),
        Resolution = new ResolutionSpec(300, 200, "300 öneriliyor"),
        Color = new ColorSpec(ColorMode.Cmyk, "CoatedFOGRA39.icc", ProfileEmbedded: false, Note: "matbaadan"),
        Output = new OutputSpec(cropMarks: true, cropMarkOffsetMm: 3, cropMarkLengthMm: 5, pageInfo: true),
        Cover = new CoverSpec(CoverType.Hardcover, new SizeMm(310, 310), new SizeMm(310, 310), new SpineSpec(12, verified: true, note: "40 sayfa")),
        Paper = new PaperSpec("matte", 250, "standart"),
        Binding = new BindingSpec("layflat", "Layflat cilt"),
        Notes = "Kişisel kullanım.",
        Tags = ["layflat", "30x30", "kişisel"],
        Warnings = [new ProfileNote(ProfileNoteLevel.Info, "Kullanıcı tanımlı profil.")],
    };

    public static AlbumProject FullProject()
    {
        var assets = new[]
        {
            new PhotoAsset
            {
                Id = "asset-001",
                SourcePath = @"C:\Photos\Düğün\IMG_0001.jpg",
                FileName = "IMG_0001.jpg",
                FileSize = 5_242_880,
                FileHash = "sha256:abc123",
                PixelWidth = 6000,
                PixelHeight = 4000,
                ExifOrientation = 1,
                CapturedAt = Now.AddYears(-1),
                ImportedAt = Now,
                CameraModel = "Canon EOS R5",
                Lens = "RF 24-70mm F2.8",
                Iso = 400,
                FocalLength = 50,
                Thumbnail = new ThumbnailInfo("assets/thumbnails/asset-001.jpg", 256, 171),
                Favorite = true,
                Tags = ["düğün", "favori"],
            },
            new PhotoAsset
            {
                Id = "asset-002",
                SourcePath = "assets/originals/asset-002.jpg",
                SourceType = AssetSourceType.Embedded,
                FileName = "IMG_0002.jpg",
                PixelWidth = 4000,
                PixelHeight = 6000,
                ImportedAt = Now,
                Missing = true,
            },
        };

        var page1 = new AlbumPage
        {
            Id = "page-001",
            Type = PageType.FrontCover,
            Notes = "Kapak",
            Background = new BackgroundFill { Mode = BackgroundMode.Gradient, Color = null, Gradient = new GradientSpec(RgbaColor.White, RgbaColor.Parse("#33333380"), 45), Opacity = 0.9 },
            TemplateId = "template-4-grid",
            CreatedAt = Now,
            UpdatedAt = Now,
            Elements =
            [
                new BackgroundElement { Id = "bg-001", Fill = new BackgroundFill { Mode = BackgroundMode.Image, Color = null, ImageAssetId = "asset-002", Opacity = 0.5 } },
                new Slot
                {
                    Id = "slot-001",
                    Bounds = new RectMm(20, 20, 260, 260),
                    CropMode = CropMode.Contain,
                    MinDpi = 200,
                    TargetDpi = 300,
                    Placement = new PhotoPlacement
                    {
                        AssetId = "asset-001",
                        OffsetX = 0.1,
                        OffsetY = -0.2,
                        Scale = 1.25,
                        Rotation = 90,
                        Crop = new NormalizedRect(0.1, 0.1, 0.8, 0.7),
                        Adjustments = new ImageAdjustments { Brightness = 0.1, Contrast = -0.05, Saturation = 0.2 },
                    },
                    Shape = SlotShape.Rounded,
                    CornerRadiusMm = 4,
                    Border = new BorderSpec(0.5, RgbaColor.Black),
                    Shadow = new ShadowSpec(1, 1.5, 2, RgbaColor.Parse("#00000040")),
                    ZIndex = 1,
                },
                new TextElement
                {
                    Id = "text-001",
                    Bounds = new RectMm(20, 280, 260, 15),
                    Content = "Ahmet & Zeynep — 15 Eylül 2024 \"ışık\"",
                    Font = new FontSpec { Family = "Inter", Size = 14, Weight = 600, Italic = true, Color = RgbaColor.Parse("#333333") },
                    Alignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    LineHeight = 1.2,
                    LetterSpacing = 0.5,
                    ZIndex = 10,
                },
            ],
        };

        var page2 = new AlbumPage
        {
            Id = "page-002",
            SpreadPosition = SpreadPosition.Left,
            Locked = true,
            CreatedAt = Now,
            UpdatedAt = Now,
            Elements = [new Slot { Id = "slot-002", Bounds = new RectMm(-3, -3, 306, 306) }],
        };

        return new AlbumProject
        {
            Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Name = "Ahmet & Zeynep Düğün Albümü",
            Description = "30×30 layflat, 40 sayfa",
            CreatedAt = Now.AddDays(-1),
            UpdatedAt = Now,
            AppVersion = "0.1.0",
            Profile = FullProfile(),
            PageSize = new SizeMm(300, 300),
            Pages = [page1, page2],
            Assets = assets,
            Settings = new ProjectSettings
            {
                Export = new ExportSettings { JpegQuality = 90 },
                Ui = new UiSettings { Theme = "dark" },
                Save = new SaveSettings { AutosaveIntervalMinutes = 3 },
            },
            History = new HistorySettings { Enabled = true, MaxSteps = 50 },
        };
    }

    public static TemplateDefinition FourGrid() => new()
    {
        Id = "template-4-grid",
        Name = "4'lü Izgara",
        Category = TemplateCategory.Grid,
        Tags = ["sade", "kolaj"],
        Orientation = TemplateOrientation.Any,
        PhotoCount = new PhotoCountRange(4, 4),
        Slots =
        [
            new TemplateSlot { Id = "s1", Rect = new PercentRect(0, 0, 50, 50) },
            new TemplateSlot { Id = "s2", Rect = new PercentRect(50, 0, 50, 50) },
            new TemplateSlot { Id = "s3", Rect = new PercentRect(0, 50, 50, 50) },
            new TemplateSlot { Id = "s4", Rect = new PercentRect(50, 50, 50, 50) },
        ],
        TextFields = [new TemplateTextField { Id = "t1", Rect = new PercentRect(10, 90, 80, 8), Placeholder = "Tarih" }],
        Background = BackgroundFill.Solid(RgbaColor.White),
        Rules = new TemplateRules(PreserveAspectRatio: true, AllowUserEdit: false),
        Preview = "previews/template-4-grid.jpg",
        Author = "builtin",
        Version = "1.0.0",
    };
}
