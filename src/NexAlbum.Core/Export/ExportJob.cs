using NexAlbum.Core.Common;

namespace NexAlbum.Core.Export;

/// <summary>Çıktı formatları (Karar 11). PSD/CDR MVP dışındadır.</summary>
public enum ExportFormat
{
    Pdf,
    Jpg,
    Svg,
    Png,
    Tiff,
}

/// <summary>İş durumu (Karar 09 §11.2).</summary>
public enum ExportStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
}

public sealed record ExportOptions
{
    /// <summary>Sayfa seçimi: "all" ya da "1-5,8" gibi bir aralık ifadesi.</summary>
    public string Pages { get; init => field = Guard.NotBlank(value, nameof(Pages)); } = "all";

    public bool SpreadOutput { get; init; }

    public bool IncludeCropMarks { get; init; } = true;

    public bool IncludeBleed { get; init; } = true;

    public int JpegQuality { get; init => field = Guard.InRange(value, 1, 100, nameof(JpegQuality)); } = 95;

    /// <summary>ICC profil dosya adı; yoksa null (uydurulmaz).</summary>
    public string? ColorProfile { get; init; }
}

/// <summary>
/// Dışa aktarma işi (Karar 09 §11). Durum geçişleri yalnızca geçerli sırayla yapılabilir.
/// </summary>
public sealed record ExportJob
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required ExportFormat Format { get; init; }

    public required string TargetPath { get; init => field = Guard.NotBlank(value, nameof(TargetPath)); }

    public required string ProfileId { get; init => field = Guard.NotBlank(value, nameof(ProfileId)); }

    public ExportOptions Options { get; init => field = value ?? throw new ArgumentNullException(nameof(Options)); } = new();

    public ExportStatus Status { get; private init; } = ExportStatus.Pending;

    public DateTimeOffset? StartedAt { get; private init; }

    public DateTimeOffset? CompletedAt { get; private init; }

    public string? Error { get; private init; }

    public IReadOnlyList<string> OutputFiles { get; private init; } = [];

    public bool IsFinished => Status is ExportStatus.Completed or ExportStatus.Failed or ExportStatus.Cancelled;

    public ExportJob Start(DateTimeOffset now)
    {
        Require(ExportStatus.Pending);
        return this with { Status = ExportStatus.Running, StartedAt = now };
    }

    public ExportJob Complete(DateTimeOffset now, IReadOnlyList<string> outputFiles)
    {
        ArgumentNullException.ThrowIfNull(outputFiles);
        Require(ExportStatus.Running);
        return this with { Status = ExportStatus.Completed, CompletedAt = now, OutputFiles = [.. outputFiles] };
    }

    public ExportJob Fail(DateTimeOffset now, string error)
    {
        Require(ExportStatus.Running);
        return this with { Status = ExportStatus.Failed, CompletedAt = now, Error = Guard.NotBlank(error, nameof(error)) };
    }

    public ExportJob Cancel(DateTimeOffset now)
    {
        if (IsFinished)
        {
            throw new InvalidOperationException($"Tamamlanmış iş iptal edilemez (durum: {Status}).");
        }

        return this with { Status = ExportStatus.Cancelled, CompletedAt = now };
    }

    private void Require(ExportStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Geçersiz durum geçişi: beklenen {expected}, mevcut {Status}.");
        }
    }
}
