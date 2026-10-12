using System.Globalization;
using System.Text;
using NexAlbum.Core.Geometry;
using NexAlbum.Core.Profiles;

namespace NexAlbum.UI.Services;

/// <summary>Profil özeti metni (Karar 03 §10.4). Bilinmeyen değerler açıkça "girilmedi" yazılır.</summary>
public static class ProfileText
{
    private const string Missing = "girilmedi";

    public static string Summary(AlbumProfile profile, SizeMm pageSize)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var c = CultureInfo.CurrentCulture;
        var sb = new StringBuilder();
        sb.Append(c, $"Profil: {profile.Name} ({Source(profile.Source)})").AppendLine();
        sb.Append(c, $"Doğrulama: {(profile.Verified ? "✅ Kullanıcı doğruladı" : "⚠️ Doğrulanmamış")}").AppendLine();
        sb.Append(c, $"Sayfa: {pageSize.Width:0.#}×{pageSize.Height:0.#} mm").AppendLine();
        sb.Append(c, $"Bleed: {Edges(profile.Bleed.Value)}").AppendLine();
        sb.Append(c, $"Güvenli alan: {Edges(profile.SafeArea.Value)}").AppendLine();
        if (profile.Spread.Enabled)
        {
            sb.Append(c, $"Gutter: {(profile.Gutter.WidthMm is { } g ? g.ToString("0.#", c) + " mm" : Missing)}").AppendLine();
        }

        sb.Append(c, $"DPI: {(profile.Resolution.TargetDpi is { } dpi ? dpi.ToString(c) : Missing)}").AppendLine();
        sb.Append(c, $"Renk: {(profile.Color.Mode is { } mode ? Mode(mode) : Missing)}{(profile.Color.IccProfile is { } icc ? " / " + icc : string.Empty)}");
        return sb.ToString();
    }

    private static string Edges(EdgesMm? edges)
    {
        if (edges is not { } e)
        {
            return Missing;
        }

        var c = CultureInfo.CurrentCulture;
        return e.IsUniform
            ? string.Format(c, "{0:0.#} mm (her kenar)", e.Top)
            : string.Format(c, "üst {0:0.#}, sağ {1:0.#}, alt {2:0.#}, sol {3:0.#} mm", e.Top, e.Right, e.Bottom, e.Left);
    }

    private static string Source(ProfileSource source) => source switch
    {
        ProfileSource.Builtin => "Örnek",
        ProfileSource.Imported => "İçe aktarılmış",
        ProfileSource.Community => "Topluluk",
        _ => "Kullanıcı",
    };

    private static string Mode(ColorMode mode) => mode switch
    {
        ColorMode.Cmyk => "CMYK",
        ColorMode.Grayscale => "Gri tonlama",
        _ => "RGB",
    };
}
