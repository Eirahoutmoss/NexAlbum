namespace NexAlbum.Assets.Metadata;

/// <summary>
/// Dosyadan okunan meta veri. Okunamayan alanlar null kalır; değer uydurulmaz.
/// </summary>
public sealed record PhotoMetadata
{
    /// <summary>Dosyadaki ham piksel genişliği (EXIF yönü uygulanmadan).</summary>
    public required int Width { get; init; }

    /// <summary>Dosyadaki ham piksel yüksekliği (EXIF yönü uygulanmadan).</summary>
    public required int Height { get; init; }

    /// <summary>EXIF yönü (1–8).</summary>
    public int? Orientation { get; init; }

    /// <summary>Çekim zamanı. EXIF saat dilimi yoksa duvar saati UTC farkı 0 ile tutulur.</summary>
    public DateTimeOffset? CapturedAt { get; init; }

    public string? CameraModel { get; init; }

    public string? Lens { get; init; }

    public int? Iso { get; init; }

    public double? FocalLength { get; init; }
}

/// <summary>Meta veri okuyucu. Test edilebilirlik ve ileride RAW desteği (Karar 08: LibRaw, v1.2) için soyutlanmıştır.</summary>
public interface IPhotoMetadataReader
{
    /// <summary>Meta veriyi okur; görüntü boyutu bulunamazsa null döner.</summary>
    PhotoMetadata? Read(Stream stream);
}
