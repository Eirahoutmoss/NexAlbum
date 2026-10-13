using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Şema v7 → v8 (Karar 09 Rev. 7): projeye isteğe bağlı kapak yaprağı (<c>cover</c>) eklendi. v7
/// dosyalarında kapak yoktur; yapı değişmez, yalnızca sürüm yükselir.
/// </summary>
public sealed class V7ToV8Cover : IMigration
{
    public int FromVersion => 7;

    public int ToVersion => 8;

    public JsonNode Migrate(JsonNode project)
    {
        ArgumentNullException.ThrowIfNull(project);
        project["schemaVersion"] = ToVersion;
        return project;
    }
}
