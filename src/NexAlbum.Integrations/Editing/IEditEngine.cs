using NexAlbum.Core.Projects;

namespace NexAlbum.Integrations.Editing;

/// <summary>Bir motorun kullanılabilirliği; değilse kullanıcıya gösterilecek sebep.</summary>
public sealed record EngineStatus(bool Available, string? Reason = null)
{
    public static EngineStatus Ready { get; } = new(true);

    public static EngineStatus Unavailable(string reason) => new(false, reason);
}

/// <summary>Tek bir dosyanın düzenleme isteği. Girdi her zaman orijinaldir; çıktı geçici bir yola yazılır.</summary>
public sealed record EditRequest(string InputPath, string OutputPath, EditRecipe Recipe);

/// <summary>Motorun bildirdiği hata (kullanıcıya aynen gösterilir).</summary>
public sealed class EditEngineException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Toplu düzenleme motoru (Karar 20 §3). Uygulamalar sıralı çağrılır; motor aynı anda tek dosya işler.
/// </summary>
public interface IEditEngine
{
    EditEngineKind Kind { get; }

    /// <summary>Görünen ad; düzenlenmiş sürümde "araç" olarak saklanır.</summary>
    string Name { get; }

    EngineStatus CheckStatus();

    /// <summary>Girdiyi tarife göre işler ve çıktıyı yazar. Hata durumunda <see cref="EditEngineException"/> fırlatır.</summary>
    Task ProcessAsync(EditRequest request, CancellationToken cancellationToken);
}
