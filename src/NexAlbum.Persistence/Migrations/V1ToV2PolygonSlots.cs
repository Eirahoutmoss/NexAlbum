using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Şema v1 → v2 (Karar 10 Rev. 1, Karar 09 §14): yuvalara isteğe bağlı "polygon" köşeleri ve yeni biçimler eklendi.
/// v1 dosyalarında bu alanlar yoktur ve tüm yuvalar dikdörtgen/yuvarlak/elipstir; yapı değişmez, yalnızca sürüm yükselir.
/// Sürümün yükselmesi önemlidir: eski uygulama v2 dosyayı "bilinmeyen biçim" hatasıyla değil, "daha yeni sürüm"
/// uyarısıyla reddeder.
/// </summary>
public sealed class V1ToV2PolygonSlots : IMigration
{
    public int FromVersion => 1;

    public int ToVersion => 2;

    public JsonNode Migrate(JsonNode project)
    {
        ArgumentNullException.ThrowIfNull(project);
        project["schemaVersion"] = ToVersion;
        return project;
    }
}
