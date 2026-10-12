using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Bmp;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Gif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Formats.WebP;
using MetaDirectory = MetadataExtractor.Directory;

namespace NexAlbum.Assets.Metadata;

/// <summary>
/// MetadataExtractor tabanlı okuyucu (Karar 08 §7.2). Pikselleri çözmez; büyük dosyalarda da hızlıdır.
/// </summary>
public sealed class MetadataExtractorReader : IPhotoMetadataReader
{
    // EXIF etiketleri (TIFF/EXIF 2.32 standardı).
    private const int TagImageWidth = 0x0100;
    private const int TagImageLength = 0x0101;
    private const int TagModel = 0x0110;
    private const int TagOrientation = 0x0112;
    private const int TagIso = 0x8827;
    private const int TagDateTimeOriginal = 0x9003;
    private const int TagOffsetTimeOriginal = 0x9011;
    private const int TagFocalLength = 0x920A;
    private const int TagExifImageWidth = 0xA002;
    private const int TagExifImageHeight = 0xA003;
    private const int TagLensModel = 0xA434;

    public PhotoMetadata? Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        IReadOnlyList<MetaDirectory> directories;
        try
        {
            directories = ImageMetadataReader.ReadMetadata(stream);
        }
        catch (ImageProcessingException)
        {
            return null;
        }

        var size = FindSize(directories);
        if (size is not { } s)
        {
            return null;
        }

        var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        var sub = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

        return new PhotoMetadata
        {
            Width = s.Width,
            Height = s.Height,
            Orientation = Int(ifd0, TagOrientation) is { } o and >= 1 and <= 8 ? o : null,
            CapturedAt = CaptureTime(sub),
            CameraModel = Text(ifd0, TagModel),
            Lens = Text(sub, TagLensModel),
            Iso = Int(sub, TagIso) is { } iso and > 0 ? iso : null,
            FocalLength = sub is not null && sub.TryGetRational(TagFocalLength, out var f) && f.Denominator != 0 && f.ToDouble() > 0 ? f.ToDouble() : null,
        };
    }

    private static (int Width, int Height)? FindSize(IReadOnlyList<MetaDirectory> directories)
    {
        foreach (var d in directories)
        {
            var size = d switch
            {
                JpegDirectory => Pair(d, JpegDirectory.TagImageWidth, JpegDirectory.TagImageHeight),
                PngDirectory => Pair(d, PngDirectory.TagImageWidth, PngDirectory.TagImageHeight),
                BmpHeaderDirectory => Pair(d, BmpHeaderDirectory.TagImageWidth, BmpHeaderDirectory.TagImageHeight),
                GifHeaderDirectory => Pair(d, GifHeaderDirectory.TagImageWidth, GifHeaderDirectory.TagImageHeight),
                WebPDirectory => Pair(d, WebPDirectory.TagImageWidth, WebPDirectory.TagImageHeight),
                _ => null,
            };

            if (size is not null)
            {
                return size;
            }
        }

        // TIFF: boyut IFD0'da, bazı dosyalarda yalnızca EXIF alt dizininde.
        return Pair(directories.OfType<ExifIfd0Directory>().FirstOrDefault(), TagImageWidth, TagImageLength)
            ?? Pair(directories.OfType<ExifSubIfdDirectory>().FirstOrDefault(), TagExifImageWidth, TagExifImageHeight);
    }

    private static (int Width, int Height)? Pair(MetaDirectory? d, int widthTag, int heightTag) =>
        Int(d, widthTag) is { } w and > 0 && Int(d, heightTag) is { } h and > 0 ? (w, h) : null;

    private static int? Int(MetaDirectory? d, int tag) =>
        d is not null && d.TryGetInt32(tag, out var value) ? value : null;

    private static string? Text(MetaDirectory? d, int tag)
    {
        var text = d?.GetString(tag)?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>EXIF "yyyy:MM:dd HH:mm:ss" ve varsa "+03:00" gibi saat dilimi.</summary>
    private static DateTimeOffset? CaptureTime(MetaDirectory? sub)
    {
        var raw = Text(sub, TagDateTimeOriginal);
        if (raw is null
            || !DateTime.TryParseExact(raw, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return null;
        }

        var offset = TimeSpan.Zero;
        var rawOffset = Text(sub, TagOffsetTimeOriginal);
        if (rawOffset is not null && TimeSpan.TryParseExact(rawOffset.TrimStart('+', '-'), @"hh\:mm", CultureInfo.InvariantCulture, out var parsed))
        {
            offset = rawOffset.StartsWith('-') ? -parsed : parsed;
        }

        return new DateTimeOffset(local, offset);
    }
}
