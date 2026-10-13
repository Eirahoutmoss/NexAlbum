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

    /// <summary>Çekim zamanı; makine saat düzeltmesi uygulanmış haliyle (bkz. <see cref="ClockOffset"/>).</summary>
    public DateTimeOffset? CapturedAt { get; init; }

    /// <summary>Makine saatine uygulanan düzeltme (Karar 09 Rev. 6); <see cref="CapturedAt"/> bu kadar kaydırılmıştır.</summary>
    public TimeSpan ClockOffset { get; init; }

    /// <summary>Makinenin kaydettiği (düzeltilmemiş) çekim zamanı.</summary>
    public DateTimeOffset? OriginalCapturedAt => CapturedAt - ClockOffset;

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

    /// <summary>Harici araçla üretilmiş düzenlenmiş sürüm (Karar 20); yoksa null.</summary>
    public PhotoEdit? Edit { get; init; }

    /// <summary>Düzenlenmiş sürüm kullanılsın mı? false iken orijinal gösterilir (önce/sonra).</summary>
    public bool UseEdit { get; init; } = true;

    /// <summary>Ekran, ön kontrol ve PDF düzenlenmiş dosyayı mı kullanıyor?</summary>
    public bool IsEditActive => UseEdit && Edit is { Missing: false };

    /// <summary>Saat düzeltmesini değiştirir; orijinal çekim zamanı korunur.</summary>
    public PhotoAsset WithClockOffset(TimeSpan offset) => this with { CapturedAt = OriginalCapturedAt + offset, ClockOffset = offset };

    /// <summary>
    /// Etkin kaynak: düzenleme etkinse yol, boyut ve yön düzenlenmiş dosyadan gelir (orijinal kayıp olsa bile kullanılabilir);
    /// değilse varlığın kendisi. Kimlik ve diğer bilgiler aynıdır.
    /// </summary>
    public PhotoAsset Effective() => IsEditActive && Edit is { } e
        ? this with { SourcePath = e.Path, SourceType = AssetSourceType.Reference, PixelWidth = e.PixelWidth, PixelHeight = e.PixelHeight, ExifOrientation = e.ExifOrientation, Missing = false }
        : this;

    /// <summary>EXIF 5–8 yönlerinde görüntü 90° döndürülmüş gösterilir; genişlik/yükseklik yer değiştirir.</summary>
    public bool IsRotated90 => ExifOrientation is >= 5 and <= 8;

    /// <summary>Görüntülendiği haliyle genişlik (piksel).</summary>
    public int DisplayWidth => IsRotated90 ? PixelHeight : PixelWidth;

    /// <summary>Görüntülendiği haliyle yükseklik (piksel).</summary>
    public int DisplayHeight => IsRotated90 ? PixelWidth : PixelHeight;

    public double DisplayAspectRatio => (double)DisplayWidth / DisplayHeight;
}
