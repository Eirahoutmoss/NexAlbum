namespace NexAlbum.Core.Projects;

/// <summary>Düzenlenmiş sürüm işlemleri (Karar 20). Orijinal varlık bilgisi hiç değişmez.</summary>
public static class PhotoEdits
{
    /// <summary>Düzenlenmiş sürümü bağlar ve etkinleştirir (öncekinin yerine geçer).</summary>
    public static AlbumProject Apply(AlbumProject project, string assetId, PhotoEdit edit)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(edit);
        return project.ReplaceAsset(Find(project, assetId) with { Edit = edit, UseEdit = true });
    }

    /// <summary>Düzenlemeyi kaldırır; fotoğraf orijinaline döner. Dosya diskte kalır (kullanıcı silebilir).</summary>
    public static AlbumProject Remove(AlbumProject project, string assetId)
    {
        ArgumentNullException.ThrowIfNull(project);
        var asset = Find(project, assetId);
        return asset.Edit is null ? project : project.ReplaceAsset(asset with { Edit = null, UseEdit = true });
    }

    /// <summary>Önce/sonra: düzenlenmiş sürümü kullan ya da orijinali göster.</summary>
    public static AlbumProject SetUseEdit(AlbumProject project, string assetId, bool use)
    {
        ArgumentNullException.ThrowIfNull(project);
        var asset = Find(project, assetId);
        return asset.UseEdit == use ? project : project.ReplaceAsset(asset with { UseEdit = use });
    }

    /// <summary>Son toplu tarifi projeye yazar.</summary>
    public static AlbumProject RememberRecipe(AlbumProject project, EditRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(recipe);
        return project with { Settings = project.Settings with { Editing = project.Settings.Editing with { LastRecipe = recipe } } };
    }

    /// <summary>Düzenlenmiş dosyaların varlığını denetler (<paramref name="exists"/> test için değiştirilebilir).</summary>
    public static AlbumProject RefreshMissing(AlbumProject project, Func<string, bool> exists)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(exists);
        var updated = project;
        foreach (var asset in project.Assets.Where(a => a.Edit is not null))
        {
            var missing = !exists(asset.Edit!.Path);
            if (missing != asset.Edit.Missing)
            {
                updated = updated.ReplaceAsset(asset with { Edit = asset.Edit with { Missing = missing } });
            }
        }

        return updated;
    }

    private static PhotoAsset Find(AlbumProject project, string assetId) =>
        project.FindAsset(assetId) ?? throw new KeyNotFoundException("Varlık bulunamadı: " + assetId);
}
