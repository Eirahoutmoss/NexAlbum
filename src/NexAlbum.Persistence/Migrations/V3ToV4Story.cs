using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Şema v3 → v4 (Karar 09 Rev. 3): projeye isteğe bağlı "story" alanı (hikâye akışı) eklendi. v3 dosyalarında bu alan
/// yoktur ve albüm tek akıştır; yapı değişmez, yalnızca sürüm yükselir.
/// </summary>
public sealed class V3ToV4Story : IMigration
{
    public int FromVersion => 3;

    public int ToVersion => 4;

    public JsonNode Migrate(JsonNode project)
    {
        ArgumentNullException.ThrowIfNull(project);
        project["schemaVersion"] = ToVersion;
        return project;
    }
}
