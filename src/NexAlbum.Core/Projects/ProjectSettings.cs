using NexAlbum.Core.Common;
using NexAlbum.Core.Design;

namespace NexAlbum.Core.Projects;

public sealed record ExportSettings
{
    public int JpegQuality { get; init => field = Guard.InRange(value, 1, 100, nameof(JpegQuality)); } = 95;
}

public sealed record UiSettings
{
    public string Theme { get; init => field = Guard.NotBlank(value, nameof(Theme)); } = "light";
}

public sealed record SaveSettings
{
    public int AutosaveIntervalMinutes { get; init => field = Guard.Positive(value, nameof(AutosaveIntervalMinutes)); } = 5;
}

/// <summary>Proje ayarları (Karar 09 §4).</summary>
public sealed record ProjectSettings
{
    public ExportSettings Export { get; init => field = value ?? throw new ArgumentNullException(nameof(Export)); } = new();

    public UiSettings Ui { get; init => field = value ?? throw new ArgumentNullException(nameof(Ui)); } = new();

    public SaveSettings Save { get; init => field = value ?? throw new ArgumentNullException(nameof(Save)); } = new();

    /// <summary>Albümün varsayılan tasarımı (çerçeve, boşluk, fon).</summary>
    public DesignSettings Design { get; init => field = value ?? throw new ArgumentNullException(nameof(Design)); } = DesignSettings.Default;

    /// <summary>Harici düzenleme (Karar 20).</summary>
    public EditingSettings Editing { get; init => field = value ?? throw new ArgumentNullException(nameof(Editing)); } = EditingSettings.Default;

    public static ProjectSettings Default { get; } = new();
}

/// <summary>Geri al/yinele ayarı (Karar 09 §19). Geçmiş dosyaya yazılmaz.</summary>
public sealed record HistorySettings
{
    public bool Enabled { get; init; } = true;

    public int MaxSteps { get; init => field = Guard.Positive(value, nameof(MaxSteps)); } = 100;
}
