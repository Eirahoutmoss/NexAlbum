namespace NexAlbum.Persistence.Format;

/// <summary>
/// .albumproj paket sabitleri (Karar 09 §12).
/// </summary>
public static class ProjectFormat
{
    public const string FileExtension = ".albumproj";
    public const string FormatName = "nexalbum-project";
    public const string FormatVersion = "1.0";

    /// <summary>
    /// Bu uygulamanın yazdığı ve okuyabildiği en yeni şema sürümü (Karar 09 §14.1).
    /// v2 (Karar 10 Rev. 1): yuvalarda "polygon" köşeleri ve yeni biçimler (triangle, diamond, hexagon, octagon, arch, slant).
    /// v3 (Karar 09 Rev. 2): metinlerde "effect" (gölge, hale, kontur, bant, kabartma); "letterSpacing" punto katıdır.
    /// v4 (Karar 09 Rev. 3): projede "story" (hikâye akışı: bölüm adları ve başlangıç zamanları).
    /// v5 (Karar 09 Rev. 4): projede "video" (hikâye filmi ayarları, Karar 21).
    /// v6 (Karar 09 Rev. 5): yerleşim "filters" alanına "exposure", "temperature", "blackWhite".
    /// v7 (Karar 09 Rev. 6): varlıklarda "clockOffsetSeconds" (makine saat düzeltmesi; "capturedAt" düzeltilmiş zamandır).
    /// </summary>
    public const int CurrentSchemaVersion = 7;

    public const string ManifestEntry = "manifest.json";
    public const string ProjectEntry = "project.json";
    public const string ProfilesEntry = "profiles.json";
    public const string TemplatesEntry = "templates.json";
    public const string ChecksumsEntry = "checksums.json";
    public const string ReadmeEntry = "README.txt";
    public const string ThumbnailsFolder = "assets/thumbnails/";
    public const string PreviewsFolder = "assets/previews/";
    public const string OriginalsFolder = "assets/originals/";
    public const string FontsFolder = "fonts/";

    public const string ChecksumAlgorithm = "sha256";
}
