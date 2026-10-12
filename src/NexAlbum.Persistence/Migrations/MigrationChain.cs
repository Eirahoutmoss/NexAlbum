using System.Text.Json.Nodes;
using NexAlbum.Persistence.Format;

namespace NexAlbum.Persistence.Migrations;

/// <summary>
/// v1 → v2 → … → hedef sürüm zinciri. Her adım tam olarak bir sürüm ilerler; boşluk veya çakışma kabul edilmez.
/// </summary>
public sealed class MigrationChain
{
    private readonly Dictionary<int, IMigration> byFrom = [];

    public MigrationChain(IEnumerable<IMigration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        foreach (var m in migrations)
        {
            if (m.ToVersion != m.FromVersion + 1)
            {
                throw new ArgumentException($"Göç bir sürüm ilerlemeli: {m.FromVersion} → {m.ToVersion}.", nameof(migrations));
            }

            if (!byFrom.TryAdd(m.FromVersion, m))
            {
                throw new ArgumentException($"v{m.FromVersion} için birden fazla göç tanımlı.", nameof(migrations));
            }
        }
    }

    /// <summary>Uygulamanın yerleşik göçleri. Şu an şema v1; ilk göç v2 ile eklenecek.</summary>
    public static MigrationChain Default { get; } = new([]);

    /// <summary>JSON'u <paramref name="from"/> sürümünden <paramref name="to"/> sürümüne yükseltir.</summary>
    public JsonNode Run(JsonNode project, int from, int to)
    {
        ArgumentNullException.ThrowIfNull(project);
        var current = project;
        for (var v = from; v < to; v++)
        {
            if (!byFrom.TryGetValue(v, out var step))
            {
                throw new ProjectFormatException(ProjectLoadError.MigrationFailed, $"v{v} → v{v + 1} dönüşümü tanımlı değil.");
            }

            try
            {
                current = step.Migrate(current);
            }
            catch (Exception ex) when (ex is not ProjectFormatException)
            {
                throw new ProjectFormatException(ProjectLoadError.MigrationFailed, $"v{v} → v{v + 1} dönüşümü başarısız: {ex.Message}", ex);
            }

            var written = current["schemaVersion"]?.GetValue<int>();
            if (written != step.ToVersion)
            {
                throw new ProjectFormatException(ProjectLoadError.MigrationFailed, $"v{v} → v{v + 1} dönüşümü schemaVersion alanını güncellemedi.");
            }
        }

        return current;
    }
}
