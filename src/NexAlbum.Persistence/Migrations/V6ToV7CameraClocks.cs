using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Şema v6 → v7 (Karar 09 Rev. 6): fotoğraflara isteğe bağlı makine saat düzeltmesi (<c>clockOffsetSeconds</c>) eklendi. v6
/// dosyalarında düzeltme yoktur (0); yapı değişmez, yalnızca sürüm yükselir.
/// </summary>
public sealed class V6ToV7CameraClocks : IMigration
{
    public int FromVersion => 6;

    public int ToVersion => 7;

    public JsonNode Migrate(JsonNode project)
    {
        ArgumentNullException.ThrowIfNull(project);
        project["schemaVersion"] = ToVersion;
        return project;
    }
}
