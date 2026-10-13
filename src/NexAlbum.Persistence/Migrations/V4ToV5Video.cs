using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Şema v4 → v5 (Karar 09 Rev. 4): projeye isteğe bağlı "video" alanı (hikâye filmi ayarları, Karar 21) eklendi. v4 dosyalarında
/// bu alan yoktur ve varsayılan ayarlar kullanılır; yapı değişmez, yalnızca sürüm yükselir.
/// </summary>
public sealed class V4ToV5Video : IMigration
{
    public int FromVersion => 4;

    public int ToVersion => 5;

    public JsonNode Migrate(JsonNode project)
    {
        ArgumentNullException.ThrowIfNull(project);
        project["schemaVersion"] = ToVersion;
        return project;
    }
}
