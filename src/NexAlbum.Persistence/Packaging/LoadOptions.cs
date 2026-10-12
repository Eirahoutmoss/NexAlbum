using NexAlbum.Persistence.Migrations;

namespace NexAlbum.Persistence.Packaging;

/// <summary>Açma seçenekleri ve güvenlik sınırları (Karar 09 §21).</summary>
public sealed record LoadOptions
{
    /// <summary>Bütünlük denetimi; büyük projelerde kapatılabilir (Karar 09 §21.4).</summary>
    public bool VerifyChecksums { get; init; } = true;

    /// <summary>Göç gerekiyorsa önce dosyanın yedeğini al (proje.albumproj.bak-vN).</summary>
    public bool BackupBeforeMigration { get; init; } = true;

    /// <summary>Tek bir JSON dosyasının en büyük açık boyutu.</summary>
    public long MaxJsonBytes { get; init; } = 64L * 1024 * 1024;

    /// <summary>Tüm paketin en büyük açık boyutu (taşınabilir projeler GB'larca olabilir).</summary>
    public long MaxTotalBytes { get; init; } = 32L * 1024 * 1024 * 1024;

    /// <summary>Zip bombası eşiği: bu orandan fazla sıkıştırılmış büyük girdiler reddedilir.</summary>
    public double MaxCompressionRatio { get; init; } = 200;

    public MigrationChain Migrations { get; init; } = MigrationChain.Default;

    public static LoadOptions Default { get; } = new();
}
