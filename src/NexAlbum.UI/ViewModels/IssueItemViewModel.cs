using System.Windows.Media;
using NexAlbum.Core.Validation;

namespace NexAlbum.UI.ViewModels;

/// <summary>Ön kontrol listesindeki satır (Karar 11 §9.5).</summary>
public sealed class IssueItemViewModel(ValidationIssue issue, int? pageNumber)
{
    private static readonly Brush ErrorBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28)));
    private static readonly Brush WarningBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xF5, 0x7C, 0x00)));
    private static readonly Brush InfoBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xCC)));

    public ValidationIssue Issue { get; } = issue;

    public int? PageNumber { get; } = pageNumber;

    public string Message => Issue.Message;

    public string? Suggestion => Issue.Suggestion;

    public bool HasSuggestion => !string.IsNullOrEmpty(Issue.Suggestion);

    public string Symbol => Issue.Severity switch
    {
        ValidationSeverity.Error => "✖",
        ValidationSeverity.Warning => "▲",
        _ => "ℹ",
    };

    public Brush SeverityBrush => Issue.Severity switch
    {
        ValidationSeverity.Error => ErrorBrush,
        ValidationSeverity.Warning => WarningBrush,
        _ => InfoBrush,
    };

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
