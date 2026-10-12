namespace NexAlbum.Persistence.Format;

/// <summary>Proje dosyası açılamadığında hatanın türü (Karar 09 §16.2).</summary>
public enum ProjectLoadError
{
    /// <summary>ZIP bozuk ya da okunamıyor.</summary>
    CorruptArchive,

    /// <summary>manifest yok ya da bu bir NexAlbum projesi değil.</summary>
    NotAProject,

    /// <summary>Dosya bu uygulamadan daha yeni bir sürümle kaydedilmiş.</summary>
    TooNew,

    /// <summary>Eski sürüm dönüştürülemedi; orijinal dosya korunur.</summary>
    MigrationFailed,

    /// <summary>project.json (veya başka bir JSON) okunamadı ya da geçersiz değer içeriyor.</summary>
    InvalidData,

    /// <summary>Paket güvenlik denetimini geçemedi (yol gezinme, zip bombası, boyut).</summary>
    UnsafeArchive,
}

/// <summary>
/// Proje dosyası okunurken ya da yazılırken oluşan, kullanıcıya gösterilebilir hata.
/// </summary>
public sealed class ProjectFormatException : Exception
{
    public ProjectFormatException()
        : this(ProjectLoadError.InvalidData, "Proje dosyası geçersiz.")
    {
    }

    public ProjectFormatException(string message)
        : this(ProjectLoadError.InvalidData, message)
    {
    }

    public ProjectFormatException(string message, Exception innerException)
        : this(ProjectLoadError.InvalidData, message, innerException)
    {
    }

    public ProjectFormatException(ProjectLoadError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
    }

    public ProjectLoadError Error { get; }
}
