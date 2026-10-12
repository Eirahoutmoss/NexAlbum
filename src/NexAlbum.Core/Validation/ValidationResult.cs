namespace NexAlbum.Core.Validation;

/// <summary>
/// Doğrulama ya da preflight sonucu (Karar 09 §3.1 PreflightResult).
/// </summary>
public sealed class ValidationResult
{
    public ValidationResult(IEnumerable<ValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Issues = [.. issues];
    }

    public static ValidationResult Empty { get; } = new([]);

    public IReadOnlyList<ValidationIssue> Issues { get; }

    public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

    public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);

    public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == ValidationSeverity.Error);

    public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == ValidationSeverity.Warning);

    public bool Contains(string code) => Issues.Any(i => i.Code == code);

    public ValidationResult Merge(ValidationResult other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new ValidationResult(Issues.Concat(other.Issues));
    }
}
