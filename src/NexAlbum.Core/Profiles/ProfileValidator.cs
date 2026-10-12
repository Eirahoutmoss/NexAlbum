using System.Globalization;
using NexAlbum.Core.Validation;

namespace NexAlbum.Core.Profiles;

/// <summary>
/// Profilin kendi içinde tutarlılığı (Karar 03 §13.2 mantıksal doğrulama, §13.3 uyarılar).
/// Hata varsa profil kaydedilmemelidir. Eksik değerler burada değil, <see cref="ProfilePreflight"/>'ta raporlanır.
/// </summary>
public static class ProfileValidator
{
    public const double SmallBleedMm = 2;
    public const double LargeSafeAreaMm = 20;
    public const int LowDpi = 150;
    public const int HighDpi = 600;

    public static ValidationResult Validate(AlbumProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var issues = new List<ValidationIssue>();
        var size = profile.Page.Size;
        var bleed = profile.Bleed.Value;
        var safe = profile.SafeArea.Value;

        if (size is { } s && bleed is { } b && (b.Max >= s.Width || b.Max >= s.Height))
        {
            issues.Add(Error("profile.bleed.too-large", "Taşma payı sayfa ölçüsünden küçük olmalı."));
        }

        if (size is { } s2 && safe is { } sa && (sa.Left + sa.Right >= s2.Width || sa.Top + sa.Bottom >= s2.Height))
        {
            issues.Add(Error("profile.safe-area.too-large", "Güvenli alan sayfayı tamamen kaplayamaz."));
        }

        if (bleed is { } b2 && safe is { } sa2
            && (sa2.Top < b2.Top || sa2.Right < b2.Right || sa2.Bottom < b2.Bottom || sa2.Left < b2.Left))
        {
            issues.Add(Error("profile.safe-area.smaller-than-bleed", "Güvenli alan her kenarda taşma payından küçük olamaz."));
        }

        if (size is { } s3 && profile.Gutter.WidthMm is { } g && g >= s3.Width)
        {
            issues.Add(Error("profile.gutter.too-large", "Cilt payı sayfa genişliğinden küçük olmalı."));
        }

        var res = profile.Resolution;
        if (res.TargetDpi is { } target && res.MinDpi is { } min && min > target)
        {
            issues.Add(Error("profile.dpi.min-above-target", "En düşük DPI hedef DPI'dan büyük olamaz."));
        }

        if (bleed is { } b3 && b3.Min < SmallBleedMm)
        {
            issues.Add(Warning("profile.bleed.small", Format("Taşma payı çok küçük (< {0} mm), kontrol edin.", SmallBleedMm)));
        }

        if (safe is { } sa3 && sa3.Max > LargeSafeAreaMm)
        {
            issues.Add(Warning("profile.safe-area.large", Format("Güvenli alan çok büyük (> {0} mm), kontrol edin.", LargeSafeAreaMm)));
        }

        foreach (var dpi in new[] { res.TargetDpi, res.MinDpi })
        {
            if (dpi is < LowDpi)
            {
                issues.Add(Warning("profile.dpi.low", Format("DPI çok düşük ({0}); baskı kalitesi düşük olabilir.", dpi.Value)));
            }
            else if (dpi is > HighDpi)
            {
                issues.Add(Warning("profile.dpi.high", Format("DPI çok yüksek ({0}); dosya gereksiz büyük olur.", dpi.Value)));
            }
        }

        return new ValidationResult(issues);
    }

    private static ValidationIssue Error(string code, string message) => new(code, ValidationSeverity.Error, message);

    private static ValidationIssue Warning(string code, string message) => new(code, ValidationSeverity.Warning, message);

    private static string Format(string format, object arg) => string.Format(CultureInfo.CurrentCulture, format, arg);
}
