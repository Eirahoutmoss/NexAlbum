using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Geri al / yinele (Karar 12 §10.2). Proje değişmez bir kayıt olduğundan her adım bir anlık görüntüdür; değişmeyen
/// parçalar (sayfalar, fotoğraflar) önceki sürümle paylaşıldığı için bellek maliyeti küçüktür.
/// Kısa aralıkla gelen değişiklikler (kaydırıcı sürükleme, tekerlekle yakınlaştırma) tek adımda birleştirilir.
/// </summary>
public sealed class ProjectHistory
{
    /// <summary>Saklanan en fazla geri alma adımı.</summary>
    public const int Capacity = 100;

    private readonly LinkedList<Entry> undo = new();
    private readonly Stack<Entry> redo = new();
    private readonly TimeProvider clock;
    private DateTimeOffset lastPush = DateTimeOffset.MinValue;

    public ProjectHistory(TimeProvider? clock = null)
    {
        this.clock = clock ?? TimeProvider.System;
    }

    /// <summary>Bu süreden kısa aralıkla gelen değişiklikler tek adım sayılır (0 = hiç birleştirme).</summary>
    public TimeSpan MergeWindow { get; set; } = TimeSpan.FromMilliseconds(600);

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public int UndoCount => undo.Count;

    /// <summary>Geri alınacak adımın açıklaması (ör. "Şablon uygulandı").</summary>
    public string? UndoLabel => undo.Last?.Value.Label;

    public string? RedoLabel => redo.Count > 0 ? redo.Peek().Label : null;

    /// <summary>Değişiklikten önceki hâli kaydeder; yinele yığını temizlenir.</summary>
    /// <param name="before">Değişiklikten önceki proje.</param>
    /// <param name="label">Adımın kısa açıklaması (durum çubuğunda gösterilir).</param>
    public void Record(AlbumProject before, string? label)
    {
        ArgumentNullException.ThrowIfNull(before);
        var now = clock.GetUtcNow();
        redo.Clear();
        if (undo.Count > 0 && MergeWindow > TimeSpan.Zero && now - lastPush < MergeWindow)
        {
            // Birleştir: en eski "önceki hâl" korunur, yalnızca açıklama güncellenir.
            var last = undo.Last!.Value;
            undo.Last.Value = last with { Label = label ?? last.Label };
        }
        else
        {
            undo.AddLast(new Entry(before, label));
            if (undo.Count > Capacity)
            {
                undo.RemoveFirst();
            }
        }

        lastPush = now;
    }

    /// <summary>Son adımı geri alır: dönülecek proje; geçmiş boşsa null.</summary>
    public AlbumProject? Undo(AlbumProject current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (undo.Last is not { } node)
        {
            return null;
        }

        undo.RemoveLast();
        redo.Push(new Entry(current, node.Value.Label));
        lastPush = DateTimeOffset.MinValue;
        return node.Value.Project;
    }

    /// <summary>Geri alınan adımı yeniden uygular; yoksa null.</summary>
    public AlbumProject? Redo(AlbumProject current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (redo.Count == 0)
        {
            return null;
        }

        var entry = redo.Pop();
        undo.AddLast(new Entry(current, entry.Label));
        lastPush = DateTimeOffset.MinValue;
        return entry.Project;
    }

    /// <summary>Yeni ya da açılan projede geçmiş sıfırlanır.</summary>
    public void Clear()
    {
        undo.Clear();
        redo.Clear();
        lastPush = DateTimeOffset.MinValue;
    }

    private sealed record Entry(AlbumProject Project, string? Label);
}
