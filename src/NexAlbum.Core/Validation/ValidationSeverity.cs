namespace NexAlbum.Core.Validation;

/// <summary>
/// Karar 03 §7.2: Hata dışa aktarmayı engeller, uyarı onayla geçilir, bilgi yalnızca bilgilendirir.
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
}
