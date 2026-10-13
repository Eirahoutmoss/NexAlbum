using System.Globalization;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Validation;

namespace NexAlbum.UI.ViewModels;

/// <summary>Renk modu seçeneği; null = bilinmiyor.</summary>
public sealed record ColorModeOption(ColorMode? Mode, string Name);

/// <summary>Kapak tipi seçeneği (formda).</summary>
public sealed record CoverTypeOption(CoverType Type, string Name);

/// <summary>
/// Profil düzenleme formu (Karar 03 §6). Boş alan "bilinmiyor" demektir; değer uydurulmaz.
/// Ondalık ayırıcı olarak hem virgül hem nokta kabul edilir.
/// </summary>
public sealed class ProfileForm
{
    public ProfileForm(AlbumProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Original = profile;
        Name = profile.Name;
        PageSize = profile.Page.Size is { } s ? Text(s.Width) + " × " + Text(s.Height) + " mm" : "girilmedi";
        var bleed = Edges(profile.Bleed.Value);
        BleedTop = bleed[0];
        BleedRight = bleed[1];
        BleedBottom = bleed[2];
        BleedLeft = bleed[3];
        var safe = Edges(profile.SafeArea.Value);
        SafeTop = safe[0];
        SafeRight = safe[1];
        SafeBottom = safe[2];
        SafeLeft = safe[3];
        Spread = profile.Spread.Enabled;
        Gutter = Text(profile.Gutter.WidthMm);
        TargetDpi = profile.Resolution.TargetDpi?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        MinDpi = profile.Resolution.MinDpi?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        ColorMode = ColorModes.First(o => o.Mode == profile.Color.Mode);
        IccProfile = profile.Color.IccProfile ?? string.Empty;
        CropMarks = profile.Output.CropMarks;
        Verified = profile.Verified;
        CoverType = CoverTypes.First(o => o.Type == (profile.Cover?.Type ?? Core.Profiles.CoverType.None));
        FrontWidth = Text(profile.Cover?.Front?.Width);
        FrontHeight = Text(profile.Cover?.Front?.Height);
        BackWidth = Text(profile.Cover?.Back?.Width);
        BackHeight = Text(profile.Cover?.Back?.Height);
        SpineWidth = Text(profile.Cover?.Spine?.WidthMm);
    }

    public static IReadOnlyList<CoverTypeOption> CoverTypes { get; } =
    [
        new(Core.Profiles.CoverType.None, "Kapak yok"),
        new(Core.Profiles.CoverType.Softcover, "Yumuşak kapak"),
        new(Core.Profiles.CoverType.Hardcover, "Sert kapak"),
    ];

    public CoverTypeOption CoverType { get; set; }

    public string FrontWidth { get; set; }

    public string FrontHeight { get; set; }

    public string BackWidth { get; set; }

    public string BackHeight { get; set; }

    /// <summary>Sırt genişliği (mm): baskı evinden alınır, sayfa sayısına göre değişir (Karar 03 §8).</summary>
    public string SpineWidth { get; set; }

    public static IReadOnlyList<ColorModeOption> ColorModes { get; } =
    [
        new(null, "Bilinmiyor"),
        new(Core.Profiles.ColorMode.Rgb, "RGB"),
        new(Core.Profiles.ColorMode.Cmyk, "CMYK"),
        new(Core.Profiles.ColorMode.Grayscale, "Gri tonlama"),
    ];

    public AlbumProfile Original { get; }

    public string Name { get; set; }

    public string PageSize { get; }

    public string BleedTop { get; set; }

    public string BleedRight { get; set; }

    public string BleedBottom { get; set; }

    public string BleedLeft { get; set; }

    public string SafeTop { get; set; }

    public string SafeRight { get; set; }

    public string SafeBottom { get; set; }

    public string SafeLeft { get; set; }

    public bool Spread { get; set; }

    public string Gutter { get; set; }

    public string TargetDpi { get; set; }

    public string MinDpi { get; set; }

    public ColorModeOption ColorMode { get; set; }

    public string IccProfile { get; set; }

    public bool CropMarks { get; set; }

    /// <summary>"Bu değerleri baskı evimden aldım" onayı (Karar 03 §5.4).</summary>
    public bool Verified { get; set; }

    /// <summary>
    /// Formdan profil üretir. Okunamayan alanlar ve mantık hataları (Karar 03 §13.2) <paramref name="errors"/>'a yazılır.
    /// Uyarılar (§13.3) profili engellemez.
    /// </summary>
    public AlbumProfile? TryBuild(DateTimeOffset now, out IReadOnlyList<string> errors, out IReadOnlyList<string> warnings)
    {
        var problems = new List<string>();
        var bleed = ParseEdges("Taşma payı", BleedTop, BleedRight, BleedBottom, BleedLeft, problems);
        var safe = ParseEdges("Güvenli alan", SafeTop, SafeRight, SafeBottom, SafeLeft, problems);
        var gutter = ParseNumber("Cilt payı", Gutter, problems);
        var target = ParseInt("Hedef DPI", TargetDpi, problems);
        var min = ParseInt("En düşük DPI", MinDpi, problems);
        var front = ParseSize("Ön kapak", FrontWidth, FrontHeight, problems);
        var back = ParseSize("Arka kapak", BackWidth, BackHeight, problems);
        var spine = ParseNumber("Sırt genişliği", SpineWidth, problems);
        if (spine is 0d)
        {
            problems.Add("Sırt genişliği sıfır olamaz; bilmiyorsanız boş bırakın.");
        }
        if (string.IsNullOrWhiteSpace(Name))
        {
            problems.Add("Profil adı boş olamaz.");
        }

        warnings = [];
        if (problems.Count > 0)
        {
            errors = problems;
            return null;
        }

        AlbumProfile profile;
        try
        {
            var original = Original;
            profile = original with
            {
                Name = Name.Trim(),
                Source = original.Source == ProfileSource.Builtin ? ProfileSource.User : original.Source,
                Id = original.Source == ProfileSource.Builtin ? "kullanici-" + original.Id.Replace("builtin-", string.Empty, StringComparison.Ordinal) : original.Id,
                Bleed = original.Bleed with { Value = bleed },
                SafeArea = original.SafeArea with { Value = safe },
                Spread = original.Spread with { Enabled = Spread },
                Gutter = new GutterSpec(gutter, original.Gutter.Verified, original.Gutter.Note),
                Resolution = new ResolutionSpec(target, min, original.Resolution.Note),
                Color = original.Color with { Mode = ColorMode.Mode, IccProfile = string.IsNullOrWhiteSpace(IccProfile) ? null : IccProfile.Trim() },
                Cover = CoverType.Type == Core.Profiles.CoverType.None && front is null && back is null && spine is null
                    ? (original.Cover is null ? null : new CoverSpec(Core.Profiles.CoverType.None))
                    : new CoverSpec(CoverType.Type, front, back, spine is null ? null : new SpineSpec(spine, SpineCalculationMethod.Manual, original.Cover?.Spine?.Verified ?? false, original.Cover?.Spine?.Note)),
                Output = new OutputSpec(CropMarks, original.Output.CropMarkOffsetMm, original.Output.CropMarkLengthMm, original.Output.RegistrationMarks, original.Output.ColorBars, original.Output.PageInfo),
                Verified = Verified,
                VerifiedAt = Verified ? (original.Verified ? original.VerifiedAt : now) : null,
                VerifiedBy = Verified ? original.VerifiedBy ?? "Kullanıcı beyanı" : null,
                UpdatedAt = now,
                Version = NextVersion(original.Version),
                Warnings = original.Source == ProfileSource.Builtin ? [] : original.Warnings,
            };
        }
        catch (ArgumentException ex)
        {
            errors = [ex.Message];
            return null;
        }

        var validation = ProfileValidator.Validate(profile);
        errors = [.. validation.Errors.Select(e => e.Message)];
        warnings = [.. validation.Warnings.Select(w => w.Message)];
        return validation.HasErrors ? null : profile;
    }

    /// <summary>SemVer küçük sürüm artışı: düzenleme sürümü değiştirir, kimlik aynı kalır (Karar 03 §12.1).</summary>
    public static string NextVersion(string version)
    {
        var parts = version.Split('.');
        return parts.Length == 3
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor)
            ? string.Create(CultureInfo.InvariantCulture, $"{major}.{minor + 1}.0")
            : "1.1.0";
    }

    /// <summary>"3,5" ve "3.5" aynı kabul edilir.</summary>
    public static bool TryParseNumber(string? text, out double value)
    {
        value = 0;
        var normalized = text?.Trim().Replace(',', '.');
        return !string.IsNullOrEmpty(normalized)
            && double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value);
    }

    private static EdgesMm? ParseEdges(string label, string top, string right, string bottom, string left, List<string> problems)
    {
        var texts = new[] { top, right, bottom, left };
        if (texts.All(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        if (texts.Any(string.IsNullOrWhiteSpace))
        {
            problems.Add(label + ": dört kenarın hepsi girilmeli ya da hepsi boş bırakılmalı (eksik kenar tahmin edilmez).");
            return null;
        }

        var values = new double[4];
        for (var i = 0; i < 4; i++)
        {
            if (!TryParseNumber(texts[i], out values[i]) || values[i] < 0)
            {
                problems.Add(label + ": geçersiz değer '" + texts[i] + "'.");
                return null;
            }
        }

        return new EdgesMm(values[0], values[1], values[2], values[3]);
    }

    private static double? ParseNumber(string label, string text, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (TryParseNumber(text, out var value) && value >= 0)
        {
            return value;
        }

        problems.Add(label + ": geçersiz değer '" + text + "'.");
        return null;
    }

    private static SizeMm? ParseSize(string label, string width, string height, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(width) && string.IsNullOrWhiteSpace(height))
        {
            return null;
        }

        var w = ParseNumber(label + " genişliği", width, problems);
        var h = ParseNumber(label + " yüksekliği", height, problems);
        if (w is not > 0 || h is not > 0)
        {
            problems.Add(label + ": genişlik ve yükseklik birlikte girilmeli (eksik değer tahmin edilmez).");
            return null;
        }

        return new SizeMm(w.Value, h.Value);
    }

    private static int? ParseInt(string label, string text, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            return value;
        }

        problems.Add(label + ": pozitif tam sayı olmalı ('" + text + "').");
        return null;
    }

    private static string[] Edges(EdgesMm? e) => e is { } v
        ? [Text(v.Top), Text(v.Right), Text(v.Bottom), Text(v.Left)]
        : [string.Empty, string.Empty, string.Empty, string.Empty];

    private static string Text(double? value) => value?.ToString("0.###", CultureInfo.CurrentCulture) ?? string.Empty;
}
