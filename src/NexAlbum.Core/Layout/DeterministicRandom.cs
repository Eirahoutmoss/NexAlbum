namespace NexAlbum.Core.Layout;

/// <summary>
/// SplitMix64 tabanlı rastgele sayı üreteci. <see cref="Random"/>'ın tohumlu dizisi .NET sürümleri arasında
/// garanti edilmediğinden kendi üretecimizi kullanırız: aynı tohum her sürümde aynı albümü verir (Karar 10 §15).
/// </summary>
public sealed class DeterministicRandom(ulong seed)
{
    private ulong state = seed;

    public ulong NextUInt64()
    {
        state += 0x9E3779B97F4A7C15UL;
        var z = state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>[0, maxExclusive) aralığında sapmasız tamsayı.</summary>
    public int Next(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        var bound = (ulong)maxExclusive;
        var limit = ulong.MaxValue - (ulong.MaxValue % bound);
        ulong value;
        do
        {
            value = NextUInt64();
        }
        while (value >= limit);

        return (int)(value % bound);
    }

    /// <summary>[0, 1) aralığında ondalık sayı.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));
}

/// <summary>Tohum türetme yardımcıları.</summary>
public static class LayoutSeed
{
    /// <summary>Proje kimliğinden kararlı tohum (Karar 10 §15.2: varsayılan proje ID'sinden türetilir).</summary>
    public static ulong FromProjectId(Guid projectId)
    {
        Span<byte> bytes = stackalloc byte[16];
        projectId.TryWriteBytes(bytes);
        return BitConverter.ToUInt64(bytes[..8]) ^ BitConverter.ToUInt64(bytes[8..]);
    }
}
