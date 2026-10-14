namespace NexAlbum.Core.Effects;

/// <summary>Şeritteki bir kartın netlik hâli.</summary>
public readonly record struct FocusState(double Scale, double Blur, double Opacity, double Saturation);

/// <summary>
/// Odak şeridi (Karar 23): yatay şeritte ortadaki kart tam boy ve net; komşuları ortaya uzaklıklarıyla orantılı küçülür,
/// bulanıklaşır, soluklaşır ve rengi azalır. Video galerisi, sunumdaki «Göz at» ve HTML prova aynı hesabı kullanır.
/// </summary>
public static class FocusPull
{
    /// <summary>Kartın hâli; <paramref name="distance"/> ortaya uzaklık (kart genişliği cinsinden), <paramref name="blur"/> bir kart uzaktaki bulanıklık.</summary>
    public static FocusState Of(double distance, double blur)
    {
        var a = Math.Min(Math.Abs(distance), 1);
        return new FocusState(1 - (0.14 * a), Math.Min(Math.Abs(distance), 2) * blur, 1 - (0.5 * a), 1 - (0.45 * a));
    }

    /// <summary>
    /// Galeride u (0–1) anında şeridin konumu (kart sırası, kesirli): her kartta durur, aralarda yumuşakça kayar. Son kartta
    /// biter. <paramref name="dwell"/> durma payı (0–1).
    /// </summary>
    public static double Position(double u, int count, double dwell = 0.62)
    {
        if (count <= 1)
        {
            return 0;
        }

        u = Math.Clamp(u, 0, 1);

        // count kart, count-1 kayma: her kart bir pay; son kartta tam durur.
        var slot = 1.0 / count;
        var index = Math.Min(count - 1, (int)Math.Floor(u / slot));
        var local = (u - (index * slot)) / slot;
        if (index == count - 1 || local < dwell)
        {
            return index;
        }

        return index + TransitionEffects.InOutCubic((local - dwell) / (1 - dwell));
    }

    /// <summary>Pozisyondan en yakın kart.</summary>
    public static int Nearest(double position, int count) => count <= 0 ? 0 : Math.Clamp((int)Math.Round(position), 0, count - 1);
}
