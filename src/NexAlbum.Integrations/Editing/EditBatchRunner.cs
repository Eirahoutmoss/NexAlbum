using NexAlbum.Assets.Metadata;
using NexAlbum.Core.Projects;

namespace NexAlbum.Integrations.Editing;

/// <summary>Toplu işin ilerlemesi.</summary>
public sealed record EditProgress(int Done, int Total, string? CurrentFile);

/// <summary>Bir dosyanın başarısızlığı.</summary>
public sealed record EditFailure(string AssetId, string FileName, string Reason);

/// <summary>Toplu düzenleme sonucu; <see cref="ApplyTo"/> başarılı olanları projeye bağlar.</summary>
public sealed record EditBatchResult(
    EditRecipe Recipe,
    IReadOnlyDictionary<string, PhotoEdit> Edits,
    IReadOnlyList<EditFailure> Failures,
    bool Cancelled)
{
    /// <summary>Başarılı düzenlemeleri bağlar ve tarifi projeye yazar (hiçbiri başarılı değilse proje aynen döner).</summary>
    public AlbumProject ApplyTo(AlbumProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (Edits.Count == 0)
        {
            return project;
        }

        var updated = PhotoEdits.RememberRecipe(project, Recipe);
        foreach (var (assetId, edit) in Edits)
        {
            if (updated.FindAsset(assetId) is not null)
            {
                updated = PhotoEdits.Apply(updated, assetId, edit);
            }
        }

        return updated;
    }
}

/// <summary>
/// Toplu düzenleme kuyruğu (Karar 20 §2): dosyalar sırayla işlenir; girdi her zaman orijinaldir (tarif yeniden uygulanınca
/// sonuç aynı olur); çıktı önce geçici ada yazılır, okunabilirse kalıcı ada taşınır. Bir dosyanın hatası işi durdurmaz;
/// zaman aşımı ise durdurur (motor meşgul kalmış olabilir).
/// </summary>
public sealed class EditBatchRunner(IEditEngine engine, IPhotoMetadataReader reader, TimeProvider? clock = null)
{
    private readonly TimeProvider clock = clock ?? TimeProvider.System;

    /// <summary>Dosya başına süre sınırı.</summary>
    public TimeSpan PerFileTimeout { get; init; } = TimeSpan.FromMinutes(5);

    public async Task<EditBatchResult> RunAsync(
        IReadOnlyList<PhotoAsset> assets,
        EditRecipe recipe,
        string outputFolder,
        IProgress<EditProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFolder);
        if (recipe.Engine != engine.Kind)
        {
            throw new ArgumentException("Tarif bu motor için değil.", nameof(recipe));
        }

        if (recipe.Problem is { } problem)
        {
            throw new ArgumentException(problem, nameof(recipe));
        }

        Directory.CreateDirectory(outputFolder);
        var edits = new Dictionary<string, PhotoEdit>(StringComparer.Ordinal);
        var failures = new List<EditFailure>();
        var cancelled = false;

        for (var i = 0; i < assets.Count; i++)
        {
            var asset = assets[i];
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            progress?.Report(new EditProgress(i, assets.Count, asset.FileName));
            var outcome = await ProcessOneAsync(asset, recipe, outputFolder, cancellationToken).ConfigureAwait(false);
            if (outcome.Edit is { } edit)
            {
                edits[asset.Id] = edit;
            }
            else
            {
                failures.Add(new EditFailure(asset.Id, asset.FileName, outcome.Reason!));
            }

            if (outcome.Stop)
            {
                cancelled = true;
                break;
            }
        }

        progress?.Report(new EditProgress(edits.Count + failures.Count, assets.Count, null));
        return new EditBatchResult(recipe, edits, failures, cancelled);
    }

    private async Task<(PhotoEdit? Edit, string? Reason, bool Stop)> ProcessOneAsync(PhotoAsset asset, EditRecipe recipe, string folder, CancellationToken cancellationToken)
    {
        if (asset.SourceType != AssetSourceType.Reference || !File.Exists(asset.SourcePath))
        {
            return (null, "orijinal dosya bulunamadı", false);
        }

        var final = Path.Combine(folder, recipe.OutputFileName(asset.Id));
        var partial = Path.Combine(folder, Path.GetFileNameWithoutExtension(final) + ".yarim" + recipe.Extension);
        TryDelete(partial);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PerFileTimeout);
        try
        {
            await engine.ProcessAsync(new EditRequest(asset.SourcePath, partial, recipe), timeout.Token)
                .WaitAsync(timeout.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDelete(partial);
            return (null, "iptal edildi", true);
        }
        catch (OperationCanceledException)
        {
            TryDelete(partial);
            return (null, $"zaman aşımı ({PerFileTimeout.TotalMinutes:0} dk); iş durduruldu", true);
        }
        catch (EditEngineException ex)
        {
            TryDelete(partial);
            return (null, ex.Message, false);
        }

        if (!File.Exists(partial))
        {
            return (null, "araç çıktı dosyası üretmedi", false);
        }

        PhotoMetadata? metadata;
        using (var stream = File.OpenRead(partial))
        {
            metadata = reader.Read(stream);
        }

        if (metadata is null)
        {
            TryDelete(partial);
            return (null, "çıktı dosyası okunamadı", false);
        }

        File.Move(partial, final, overwrite: true);
        return (new PhotoEdit
        {
            Path = Path.GetFullPath(final),
            PixelWidth = metadata.Width,
            PixelHeight = metadata.Height,
            ExifOrientation = metadata.Orientation is >= 1 and <= 8 ? metadata.Orientation : null,
            Tool = engine.Name,
            Recipe = recipe.Name,
            CreatedAt = clock.GetUtcNow(),
        }, null, false);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
