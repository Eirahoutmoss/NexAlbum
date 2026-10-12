namespace NexAlbum.Core.Validation;

/// <summary>
/// Tek bir doğrulama/preflight bulgusu (Karar 11 §9.4). <see cref="Code"/> sabittir; testler ve arayüz ona göre davranır.
/// </summary>
/// <param name="Code">Kalıcı, makine tarafından okunabilir kod (ör. "profile.bleed.missing").</param>
/// <param name="Severity">Önem düzeyi.</param>
/// <param name="Message">Kullanıcıya gösterilecek Türkçe mesaj.</param>
/// <param name="PageId">İlgili sayfa, varsa.</param>
/// <param name="ElementId">İlgili öğe veya varlık, varsa.</param>
/// <param name="Suggestion">Kullanıcıya önerilen eylem, varsa.</param>
public sealed record ValidationIssue(
    string Code,
    ValidationSeverity Severity,
    string Message,
    string? PageId = null,
    string? ElementId = null,
    string? Suggestion = null);
