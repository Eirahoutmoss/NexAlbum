using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;
using NexAlbum.Persistence.Json.Dtos;
using static NexAlbum.Persistence.Json.MapContext;

namespace NexAlbum.Persistence.Json;

/// <summary>Profil ↔ JSON (Karar 03 §3.1).</summary>
internal static class ProfileMapper
{
    public const string SchemaUri = "https://nexalbum.dev/schemas/profile-v1.json";

    private static readonly TextEnum<ProfileSource> Sources = new();
    private static readonly TextEnum<ColorMode> ColorModes = new((ColorMode.Rgb, "RGB"), (ColorMode.Cmyk, "CMYK"), (ColorMode.Grayscale, "Grayscale"));
    private static readonly TextEnum<CoverType> CoverTypes = new();
    private static readonly TextEnum<SpineCalculationMethod> SpineMethods = new();
    private static readonly TextEnum<ProfileNoteLevel> NoteLevels = new();
    private static readonly TextEnum<PageOrientation> Orientations = new();

    public static ProfileDto ToDto(AlbumProfile p) => new()
    {
        Schema = SchemaUri,
        SchemaVersion = 1,
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Source = Sources.Format(p.Source),
        SourceNote = p.SourceNote,
        Verified = p.Verified,
        VerifiedAt = p.VerifiedAt,
        VerifiedBy = p.VerifiedBy,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Version = p.Version,
        Page = p.Page.Size is { } size ? SizeToDto(size) : new SizeDto(),
        Spread = new SpreadDto { Enabled = p.Spread.Enabled, Note = p.Spread.Note },
        Bleed = EdgesToDto(p.Bleed),
        SafeArea = EdgesToDto(p.SafeArea),
        Gutter = new GutterDto { Width = p.Gutter.WidthMm, Unit = "mm", Note = p.Gutter.Note, Verified = p.Gutter.Verified },
        Resolution = new ResolutionDto { TargetDpi = p.Resolution.TargetDpi, MinDpi = p.Resolution.MinDpi, Note = p.Resolution.Note },
        Color = ColorToDto(p.Color),
        Output = OutputToDto(p.Output),
        Cover = p.Cover is { } cover ? CoverToDto(cover) : null,
        Paper = p.Paper is { } paper ? new PaperDto { Type = paper.Type, Weight = paper.WeightGsm, WeightUnit = "gsm", Note = paper.Note } : null,
        Binding = p.Binding is { } binding ? new BindingDto { Type = binding.Type, Note = binding.Note } : null,
        Notes = p.Notes,
        Tags = [.. p.Tags],
        Warnings = [.. p.Warnings.Select(w => new ProfileWarningDto { Level = NoteLevels.Format(w.Level), Message = w.Message })],
    };

    public static AlbumProfile FromDto(ProfileDto d, string path) => At(path, () => Map(d, path));

    internal static SizeDto SizeToDto(SizeMm size) => new()
    {
        Width = size.Width,
        Height = size.Height,
        Unit = "mm",
        Orientation = Orientations.Format(size.Orientation),
    };

    /// <summary>Genişlik ve yükseklik birlikte yoksa "bilinmiyor" (null). Yalnızca biri varsa hata.</summary>
    internal static SizeMm? SizeFromDtoOrNull(SizeDto? d, string path)
    {
        if (d is null || (d.Width is null && d.Height is null))
        {
            return null;
        }

        return new SizeMm(
            ToMm(RequiredValue(d.Width, path + ".width"), d.Unit, path),
            ToMm(RequiredValue(d.Height, path + ".height"), d.Unit, path));
    }

    private static AlbumProfile Map(ProfileDto d, string path) => new()
    {
        Id = Required(d.Id, path + ".id"),
        Name = Required(d.Name, path + ".name"),
        Description = d.Description,
        Source = Sources.Parse(d.Source, path + ".source", ProfileSource.User),
        SourceNote = d.SourceNote,
        Verified = d.Verified ?? false,
        VerifiedAt = d.VerifiedAt,
        VerifiedBy = d.VerifiedBy,
        CreatedAt = d.CreatedAt ?? default,
        UpdatedAt = d.UpdatedAt ?? d.CreatedAt ?? default,
        Version = d.Version ?? "1.0.0",
        Page = new PageSpec(SizeFromDtoOrNull(d.Page, path + ".page")),
        Spread = new SpreadSpec(d.Spread?.Enabled ?? false, d.Spread?.Note),
        Bleed = EdgesFromDto(d.Bleed, path + ".bleed"),
        SafeArea = EdgesFromDto(d.SafeArea, path + ".safeArea"),
        Gutter = new GutterSpec(ToMmOrNull(d.Gutter?.Width, d.Gutter?.Unit, path + ".gutter"), d.Gutter?.Verified ?? false, d.Gutter?.Note),
        Resolution = new ResolutionSpec(d.Resolution?.TargetDpi, d.Resolution?.MinDpi, d.Resolution?.Note),
        Color = ColorFromDto(d.Color, path + ".color"),
        Output = OutputFromDto(d.Output),
        Cover = d.Cover is { } cover ? CoverFromDto(cover, path + ".cover") : null,
        Paper = d.Paper is { } paper ? new PaperSpec(paper.Type, paper.Weight, paper.Note) : null,
        Binding = d.Binding is { Type: { } bindingType } binding ? new BindingSpec(bindingType, binding.Note) : null,
        Notes = d.Notes,
        Tags = [.. d.Tags ?? []],
        Warnings = [.. (d.Warnings ?? []).Select((w, i) => WarningFromDto(w, $"{path}.warnings[{i}]"))],
    };

    private static ColorDto ColorToDto(ColorSpec c) => new()
    {
        Mode = ColorModes.FormatOrNull(c.Mode),
        Profile = c.IccProfile,
        ProfileEmbedded = c.ProfileEmbedded,
        Note = c.Note,
    };

    private static ColorSpec ColorFromDto(ColorDto? d, string path) => d is null
        ? ColorSpec.Unknown
        : new ColorSpec(ColorModes.ParseOrNull(d.Mode, path + ".mode"), d.Profile, d.ProfileEmbedded ?? false, d.Note);

    private static OutputDto OutputToDto(OutputSpec o) => new()
    {
        CropMarks = o.CropMarks,
        CropMarkOffset = o.CropMarkOffsetMm,
        CropMarkLength = o.CropMarkLengthMm,
        RegistrationMarks = o.RegistrationMarks,
        ColorBars = o.ColorBars,
        PageInfo = o.PageInfo,
    };

    private static OutputSpec OutputFromDto(OutputDto? o) => o is null
        ? OutputSpec.None
        : new OutputSpec(o.CropMarks ?? false, o.CropMarkOffset, o.CropMarkLength, o.RegistrationMarks ?? false, o.ColorBars ?? false, o.PageInfo ?? false);

    private static CoverDto CoverToDto(CoverSpec c) => new()
    {
        Type = CoverTypes.Format(c.Type),
        Front = c.Front is { } front ? SizeToDto(front) : null,
        Back = c.Back is { } back ? SizeToDto(back) : null,
        Spine = c.Spine is { } spine ? SpineToDto(spine) : null,
    };

    private static CoverSpec CoverFromDto(CoverDto c, string path) => new(
        CoverTypes.Parse(c.Type, path + ".type"),
        SizeFromDtoOrNull(c.Front, path + ".front"),
        SizeFromDtoOrNull(c.Back, path + ".back"),
        c.Spine is { } spine ? SpineFromDto(spine, path + ".spine") : null);

    private static SpineDto SpineToDto(SpineSpec s) => new()
    {
        Width = s.WidthMm,
        Unit = "mm",
        CalculationMethod = SpineMethods.Format(s.Method),
        Note = s.Note,
        Verified = s.Verified,
    };

    private static SpineSpec SpineFromDto(SpineDto s, string path) => new(
        ToMmOrNull(s.Width, s.Unit, path),
        SpineMethods.Parse(s.CalculationMethod, path + ".calculationMethod", SpineCalculationMethod.Manual),
        s.Verified ?? false,
        s.Note);

    private static ProfileNote WarningFromDto(ProfileWarningDto w, string path) => new(
        NoteLevels.Parse(w.Level, path + ".level", ProfileNoteLevel.Info),
        Required(w.Message, path + ".message"));

    private static EdgesDto EdgesToDto(EdgeSpec spec) => new()
    {
        Top = spec.Value?.Top,
        Right = spec.Value?.Right,
        Bottom = spec.Value?.Bottom,
        Left = spec.Value?.Left,
        Unit = "mm",
        Verified = spec.Verified,
        Note = spec.Note,
    };

    /// <summary>Dört kenar da yoksa "bilinmiyor"; kısmen girilmişse hata (eksik kenar uydurulmaz).</summary>
    private static EdgeSpec EdgesFromDto(EdgesDto? d, string path)
    {
        if (d is null)
        {
            return EdgeSpec.Unknown;
        }

        EdgesMm? edges = null;
        if (d.Top is not null || d.Right is not null || d.Bottom is not null || d.Left is not null)
        {
            edges = new EdgesMm(
                ToMm(RequiredValue(d.Top, path + ".top"), d.Unit, path),
                ToMm(RequiredValue(d.Right, path + ".right"), d.Unit, path),
                ToMm(RequiredValue(d.Bottom, path + ".bottom"), d.Unit, path),
                ToMm(RequiredValue(d.Left, path + ".left"), d.Unit, path));
        }

        return new EdgeSpec(edges, d.Verified ?? false, d.Note);
    }
}
