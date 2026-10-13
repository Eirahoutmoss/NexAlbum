using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Şema v5 → v6 (Karar 09 Rev. 5): fotoğraf ayarlarına isteğe bağlı pozlama, sıcaklık ve siyah-beyaz eklendi. v5 dosyalarında
/// bu alanlar yoktur (0 / kapalı); yapı değişmez, yalnızca sürüm yükselir.
/// </summary>
public sealed class V5ToV6PhotoAdjustments : IMigration
{
    public int FromVersion => 5;

    public int ToVersion => 6;

    public JsonNode Migrate(JsonNode project)
    {
        ArgumentNullException.ThrowIfNull(project);
        project["schemaVersion"] = ToVersion;
        return project;
    }
}
