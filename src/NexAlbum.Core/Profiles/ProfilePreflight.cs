using NexAlbum.Core.Validation;

namespace NexAlbum.Core.Profiles;

/// <summary>
/// Dışa aktarma öncesi profil kontrolleri (Karar 03 §7.1). Eksik değer otomatik atanmaz; raporlanır.
/// </summary>
public static class ProfilePreflight
{
    /// <param name="profile">Denetlenecek profil.</param>
    /// <param name="iccProfileExists">ICC dosyasının varlığını denetleyen işlev (IO, Core dışındadır). Null ise bu kontrol atlanır.</param>
    public static ValidationResult Check(AlbumProfile profile, Func<string, bool>? iccProfileExists = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var issues = new List<ValidationIssue>();

        if (profile.Page.Size is null)
        {
            issues.Add(Error("profile.page-size.missing", "Sayfa ölçüsü girilmemiş."));
        }

        if (profile.Bleed.Value is null)
        {
            issues.Add(Error("profile.bleed.missing", "Taşma payı (bleed) girilmemiş. Baskı evinizden öğrenin."));
        }

        if (profile.SafeArea.Value is null)
        {
            issues.Add(Error("profile.safe-area.missing", "Güvenli alan girilmemiş. Baskı evinizden öğrenin."));
        }

        if (profile.Spread.Enabled && profile.Gutter.WidthMm is null)
        {
            issues.Add(Error("profile.gutter.missing", "Çift sayfa albüm için cilt payı (gutter) girilmemiş."));
        }

        if (profile.Resolution.TargetDpi is null)
        {
            issues.Add(Warning("profile.dpi.missing", "Hedef DPI girilmemiş."));
        }

        if (profile.Color.Mode is null)
        {
            issues.Add(Warning("profile.color-mode.missing", "Renk modu girilmemiş."));
        }

        if (profile.Cover is { Type: not CoverType.None } cover)
        {
            if (cover.Front is null || cover.Back is null)
            {
                issues.Add(Warning("profile.cover-size.missing", "Kapak ölçüleri girilmemiş."));
            }

            if (cover.Type == CoverType.Hardcover && cover.Spine?.WidthMm is null)
            {
                issues.Add(Warning("profile.spine.missing", "Sırt genişliği girilmemiş. Baskı evinizden sırt genişliğini isteyin."));
            }
        }

        if (!profile.Verified)
        {
            issues.Add(Warning("profile.unverified", "Profil doğrulanmamış. Değerleri baskı evinizle teyit edin."));
        }

        if (profile.Color.IccProfile is { Length: > 0 } icc && iccProfileExists is not null && !iccProfileExists(icc))
        {
            issues.Add(Warning("profile.icc.not-found", $"ICC profil dosyası bulunamadı: {icc}"));
        }

        return new ValidationResult(issues).Merge(ProfileValidator.Validate(profile));
    }

    private static ValidationIssue Error(string code, string message) => new(code, ValidationSeverity.Error, message);

    private static ValidationIssue Warning(string code, string message) => new(code, ValidationSeverity.Warning, message);
}
