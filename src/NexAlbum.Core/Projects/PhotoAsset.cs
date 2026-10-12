using NexAlbum.Core.Common;

namespace NexAlbum.Core.Projects;

/// <summary>Varlığın küçük önizlemesi (paket içi göreceli yol).</summary>
public sealed record ThumbnailInfo
{
    public ThumbnailInfo(string path, int width, int height)
    {
        Path = Guard.NotBlank(path, nameof(path));
        Width = Guard.Positive(width, nameof(width));
        Height = Guard.Positive(height, nameof(height));
    }

    public string Path { get; }

    public int Width { get; }

    public int Height { get; }
}

/// <summary>
/// Fotoğraf varlığı: kaynak dosya referansı ve meta veri (Karar 09 §7).
/// Hangi sayfalarda kullanıldığı saklanmaz; <see cref="AlbumProject.PagesUsingAsset"/> ile hesaplanır.
/// </summary>
public sealed record PhotoAsset
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required string SourcePath { get; init => field = Guard.NotBlank(value, nameof(SourcePath)); }

    public AssetSourceType SourceType { get; init; } = AssetSourceType.Reference;

    public AssetRole Role { get; init; } = AssetRole.Photo;

    public bool IsPhoto => Role == AssetRole.Photo;

    public required string FileName { get; init => field = Guard.NotBlank(value, nameof(FileName)); }

    public long? FileSize { get; init => field = value is < 0 ? throw new ArgumentOutOfRangeException(nameof(FileSize)) : value; }

    /// <summary>Bütünlük için "sha256:..." biçiminde özet.</summary>
    public string? FileHash { get; init; }

    /// <summary>Dosyadaki ham piksel genişliği (EXIF yönü uygulanmadan).</summary>
    public required int PixelWidth { get; init => field = Guard.Positive(value, nameof(PixelWidth)); }

    /// <summary>Dosyadaki ham piksel yüksekliği (EXIF yönü uygulanmadan).</summary>
    public required int PixelHeight { get; init => field = Guard.Positive(value, nameof(PixelHeight)); }

    /// <summary>EXIF yönü (1–8). Bilinmiyorsa null (1 kabul edilir).</summary>
    public int? ExifOrientation { get; init => field = value is null ? null : Guard.InRange(value.Value, 1, 8, nameof(ExifOrientation)); }

    public DateTimeOffset? CapturedAt { get; init; }

    public DateTimeOffset ImportedAt { get; init; }

    public string? CameraModel { get; init; }

    public string? Lens { get; init; }

    public int? Iso { get; init => field = Guard.PositiveOrNull(value, nameof(Iso)); }

    public double? FocalLength { get; init => field = Guard.PositiveOrNull(value, nameof(FocalLength)); }

    public ThumbnailInfo? Thumbnail { get; init; }

    public bool Favorite { get; init; }

    public IReadOnlyList<string> Tags { get; init => field = value ?? throw new ArgumentNullException(nameof(Tags)); } = [];

    /// <summary>Kaynak dosya bulunamadı mı? Açılışta denetlenir (Karar 09 §7.4).</summary>
    public bool Missing { get; init; }

    /// <summary>EXIF 5–8 yönlerinde görüntü 90° döndürülmüş gösterilir; genişlik/yükseklik yer değiştirir.</summary>
    public bool IsRotated90 => ExifOrientation is >= 5 and <= 8;

    /// <summary>Görüntülendiği haliyle genişlik (piksel).</summary>
    public int DisplayWidth => IsRotated90 ? PixelHeight : PixelWidth;

    /// <summary>Görüntülendiği haliyle yükseklik (piksel).</summary>
    public int DisplayHeight => IsRotated90 ? PixelWidth : PixelHeight;

    public double DisplayAspectRatio => (double)DisplayWidth / DisplayHeight;
}
