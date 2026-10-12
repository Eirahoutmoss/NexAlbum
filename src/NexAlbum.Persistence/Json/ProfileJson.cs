using System.Text.Json;
using NexAlbum.Core.Profiles;
using NexAlbum.Core.Templates;
using NexAlbum.Persistence.Format;
using NexAlbum.Persistence.Json.Dtos;

namespace NexAlbum.Persistence.Json;

/// <summary>
/// Tek başına profil dosyaları (%APPDATA%/NexAlbum/profiles/*.json, Karar 03 §9).
/// </summary>
public static class ProfileJson
{
    public static string Serialize(AlbumProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(ProfileMapper.ToDto(profile), JsonDefaults.Options);
    }

    /// <summary>Profili okur. Geçersiz profil yüklenmez; hata mesajı eksik alanı söyler (Karar 03 §13.1).</summary>
    public static AlbumProfile Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var dto = Parse<ProfileDto>(json);
        if (dto.SchemaVersion is > 1)
        {
            throw new ProjectFormatException(ProjectLoadError.TooNew, "Profil daha yeni bir NexAlbum sürümüyle oluşturulmuş.");
        }

        return Wrap(() => ProfileMapper.FromDto(dto, "profile"));
    }

    internal static T Parse<T>(string json)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonDefaults.Options)
                ?? throw new ProjectFormatException(ProjectLoadError.InvalidData, "Dosya boş.");
        }
        catch (JsonException ex)
        {
            throw new ProjectFormatException(ProjectLoadError.InvalidData, "JSON okunamadı: " + ex.Message, ex);
        }
    }

    internal static T Wrap<T>(Func<T> map)
    {
        try
        {
            return map();
        }
        catch (FormatException ex)
        {
            throw new ProjectFormatException(ProjectLoadError.InvalidData, ex.Message, ex);
        }
    }
}

/// <summary>Tek başına şablon dosyaları (yerleşik ve kullanıcı şablonları).</summary>
public static class TemplateJson
{
    public static string Serialize(TemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return JsonSerializer.Serialize(TemplateMapper.ToDto(template), JsonDefaults.Options);
    }

    public static TemplateDefinition Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var dto = ProfileJson.Parse<TemplateDto>(json);
        if (dto.SchemaVersion is > 1)
        {
            throw new ProjectFormatException(ProjectLoadError.TooNew, "Şablon daha yeni bir NexAlbum sürümüyle oluşturulmuş.");
        }

        return ProfileJson.Wrap(() => TemplateMapper.FromDto(dto, "template"));
    }
}
