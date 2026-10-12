namespace NexAlbum.Export.Pdf;

/// <summary>PDF çıktı seçenekleri (Karar 11 §3).</summary>
public sealed record PdfExportSettings
{
    /// <summary>Taşma payı (profildeki değer) dahil edilsin mi?</summary>
    public bool IncludeBleed { get; init; } = true;

    /// <summary>Kesim işaretleri çizilsin mi? Varsayılan profilden gelir.</summary>
    public bool IncludeCropMarks { get; init; } = true;

    /// <summary>İşaretin taşma payından uzaklığı (Karar 11 §3.5 standardı: 3 mm).</summary>
    public double CropMarkOffsetMm { get; init => field = double.IsFinite(value) && value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(CropMarkOffsetMm)); } = 3;

    /// <summary>İşaret uzunluğu (Karar 11 §3.5 standardı: 5 mm).</summary>
    public double CropMarkLengthMm { get; init => field = double.IsFinite(value) && value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(CropMarkLengthMm)); } = 5;

    /// <summary>"all" ya da "1-5,8".</summary>
    public string Pages { get; init => field = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Sayfa seçimi boş olamaz.", nameof(Pages)) : value; } = "all";

    /// <summary>Her sayfa ayrı dosya (dosya_001.pdf, …).</summary>
    public bool SeparateFiles { get; init; }

    /// <summary>PDF bilgisine yazılacak üretici adı.</summary>
    public string Creator { get; init; } = "NexAlbum";

    /// <summary>Bir projenin profilinden ayarlar (kesim işareti tercihleri profilde varsa onlar kullanılır).</summary>
    public static PdfExportSettings FromProfile(Core.Profiles.AlbumProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var output = profile.Output;
        return new PdfExportSettings
        {
            IncludeCropMarks = output.CropMarks,
            CropMarkOffsetMm = output.CropMarkOffsetMm ?? 3,
            CropMarkLengthMm = output.CropMarkLengthMm ?? 5,
        };
    }
}
