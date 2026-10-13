using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Şema v2 → v3 (Karar 09 Rev. 2): metinlere isteğe bağlı "effect" alanı eklendi. v2 dosyalarında bu alan yoktur ve
/// metinler etkisizdir; yapı değişmez, yalnızca sürüm yükselir (eski uygulama v3 dosyayı "daha yeni sürüm" diye reddeder).
/// </summary>
public sealed class V2ToV3TextEffects : IMigration
{
    public int FromVersion => 2;

    public int ToVersion => 3;

    public JsonNode Migrate(JsonNode project)
    {
        ArgumentNullException.ThrowIfNull(project);
        project["schemaVersion"] = ToVersion;
        return project;
    }
}
