using NexAlbum.Core.Common;

namespace NexAlbum.Core.Profiles;

/// <summary>
/// Kullanıcı tanımlı baskı profili (Karar 03 §3). Baskı evi entegrasyonu yoktur; değerleri kullanıcı girer.
/// Bilinmeyen değerler null kalır ve preflight bunları raporlar.
/// </summary>
public sealed record AlbumProfile
{
    public required string Id { get; init => field = Guard.NotBlank(value, nameof(Id)); }

    public required string Name { get; init => field = Guard.NotBlank(value, nameof(Name)); }

    public string? Description { get; init; }

    public ProfileSource Source { get; init; } = ProfileSource.User;

    public string? SourceNote { get; init; }

    /// <summary>Kullanıcı değerlerin doğruluğunu onayladı mı?</summary>
    public bool Verified { get; init; }

    public DateTimeOffset? VerifiedAt { get; init; }

    public string? VerifiedBy { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Profil sürümü (SemVer, Karar 03 §12).</summary>
    public string Version { get; init => field = Guard.NotBlank(value, nameof(Version)); } = "1.0.0";

    public PageSpec Page { get; init => field = value ?? throw new ArgumentNullException(nameof(Page)); } = new();

    public SpreadSpec Spread { get; init => field = value ?? throw new ArgumentNullException(nameof(Spread)); } = new();

    public EdgeSpec Bleed { get; init => field = value ?? throw new ArgumentNullException(nameof(Bleed)); } = EdgeSpec.Unknown;

    public EdgeSpec SafeArea { get; init => field = value ?? throw new ArgumentNullException(nameof(SafeArea)); } = EdgeSpec.Unknown;

    public GutterSpec Gutter { get; init => field = value ?? throw new ArgumentNullException(nameof(Gutter)); } = GutterSpec.Unknown;

    public ResolutionSpec Resolution { get; init => field = value ?? throw new ArgumentNullException(nameof(Resolution)); } = ResolutionSpec.Unknown;

    public ColorSpec Color { get; init => field = value ?? throw new ArgumentNullException(nameof(Color)); } = ColorSpec.Unknown;

    public OutputSpec Output { get; init => field = value ?? throw new ArgumentNullException(nameof(Output)); } = OutputSpec.None;

    public CoverSpec? Cover { get; init; }

    public PaperSpec? Paper { get; init; }

    public BindingSpec? Binding { get; init; }

    public string? Notes { get; init; }

    public IReadOnlyList<string> Tags { get; init => field = value ?? throw new ArgumentNullException(nameof(Tags)); } = [];

    public IReadOnlyList<ProfileNote> Warnings { get; init => field = value ?? throw new ArgumentNullException(nameof(Warnings)); } = [];
}
