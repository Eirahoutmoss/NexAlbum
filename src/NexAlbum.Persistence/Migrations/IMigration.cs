using System.Text.Json.Nodes;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// Bir şema sürümünden bir sonrakine JSON dönüşümü (Karar 09 §14.4). Domain'e değil, ham JSON'a uygulanır.
/// </summary>
public interface IMigration
{
    int FromVersion { get; }

    int ToVersion { get; }

    /// <summary>project.json kökünü dönüştürür. Dönen düğümde schemaVersion = ToVersion olmalıdır.</summary>
    JsonNode Migrate(JsonNode project);
}
