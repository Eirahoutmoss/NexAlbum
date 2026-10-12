namespace NexAlbum.Core.Profiles;

/// <summary>Profilin kaynağı (Karar 03 §4.1).</summary>
public enum ProfileSource
{
    User,
    Builtin,
    Imported,
    Community,
}

/// <summary>Renk modu (Karar 03 §3.2).</summary>
public enum ColorMode
{
    Rgb,
    Cmyk,
    Grayscale,
}

public enum CoverType
{
    Hardcover,
    Softcover,
    None,
}

public enum SpineCalculationMethod
{
    Manual,
}

public enum ProfileNoteLevel
{
    Info,
    Warning,
}
