namespace NexAlbum.Export.Proof;

/// <summary>HTML provaya gömülen kaynaklar.</summary>
public static class ProofAssets
{
    /// <summary>Pro efektlerin tarayıcı karşılığı (Core <c>TransitionEffects</c> ile aynı hesap; Karar 23).</summary>
    public static string EffectsScript { get; } = Read("NexAlbum.Export.Proof.effects.js");

    /// <summary>Prova sayfası şablonu (yer tutucular: TITLE, COUNT, DATE, META, EFFECTS, PAGES).</summary>
    public static string ProofTemplate { get; } = Read("NexAlbum.Export.Proof.proof.html");

    private static string Read(string name)
    {
        using var stream = typeof(ProofAssets).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Gömülü kaynak bulunamadı: " + name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
