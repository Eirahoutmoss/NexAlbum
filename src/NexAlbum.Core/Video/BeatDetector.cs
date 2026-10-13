namespace NexAlbum.Core.Video;

/// <summary>Müziğin vuruş ızgarası: tempo, vuruş anları (sn), güven (0–1) ve sessiz girişin sonu.</summary>
public sealed record BeatGrid(double Bpm, IReadOnlyList<double> Beats, double Confidence, double LeadIn, double Duration)
{
    public static BeatGrid None(double duration) => new(0, [], 0, 0, duration);

    public bool IsUsable => Beats.Count >= 4 && Confidence >= 0.15;

    /// <summary>Verilen ana en yakın vuruş; ±<paramref name="tolerance"/> içinde yoksa kendisi.</summary>
    public double Snap(double t, double tolerance)
    {
        if (!IsUsable)
        {
            return t;
        }

        var best = t;
        var bestDistance = tolerance;
        foreach (var beat in Beats)
        {
            var d = Math.Abs(beat - t);
            if (d <= bestDistance)
            {
                best = beat;
                bestDistance = d;
            }
            else if (beat > t + tolerance)
            {
                break;
            }
        }

        return best;
    }
}

/// <summary>
/// Basit ve deterministik vuruş bulucu (Karar 21 §4): enerji zarfının artışlarından başlangıç gücü, özilinti ile tempo
/// (60–180 BPM, 110 civarına hafif öncelik), en güçlü evre ile vuruş ızgarası. Kusursuz değildir; kesmeleri müziğe
/// "oturtmak" için yeterlidir. Güven düşükse planlayıcı sabit ritim kullanır.
/// </summary>
public static class BeatDetector
{
    /// <param name="samples">Tek kanallı örnekler (−1…1).</param>
    /// <param name="sampleRate">Örnekleme hızı (Hz).</param>
    public static BeatGrid Detect(ReadOnlySpan<float> samples, int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        var duration = (double)samples.Length / sampleRate;
        var hop = Math.Max(1, sampleRate / 100); // 10 ms
        var frames = samples.Length / hop;
        if (frames < 400)
        {
            return BeatGrid.None(duration);
        }

        // 1. Enerji zarfı (RMS, 20 ms pencere) ve sessiz giriş.
        var energy = new double[frames];
        for (var f = 0; f < frames; f++)
        {
            double sum = 0;
            var start = f * hop;
            var end = Math.Min(samples.Length, start + (2 * hop));
            for (var i = start; i < end; i++)
            {
                sum += samples[i] * samples[i];
            }

            energy[f] = Math.Sqrt(sum / Math.Max(1, end - start));
        }

        var peak = energy.Max();
        if (peak <= 1e-6)
        {
            return BeatGrid.None(duration);
        }

        var leadIn = 0.0;
        for (var f = 0; f < frames; f++)
        {
            if (energy[f] >= peak * 0.08)
            {
                leadIn = f * hop / (double)sampleRate;
                break;
            }
        }

        // 2. Başlangıç gücü: logaritmik enerjinin pozitif artışı, ortalaması çıkarılmış.
        var onset = new double[frames];
        for (var f = 1; f < frames; f++)
        {
            onset[f] = Math.Max(0, Math.Log(1e-4 + energy[f]) - Math.Log(1e-4 + energy[f - 1]));
        }

        var mean = onset.Average();
        for (var f = 0; f < frames; f++)
        {
            onset[f] = Math.Max(0, onset[f] - mean);
        }

        // 3. Tempo: özilinti (60–180 BPM), 110 BPM çevresine log-normal öncelik.
        var fps = sampleRate / (double)hop;
        var minLag = (int)Math.Floor(fps * 60 / 180);
        var maxLag = (int)Math.Ceiling(fps * 60 / 60);
        var bestLag = 0;
        var bestScore = 0.0;
        var scores = new List<double>();
        for (var lag = minLag; lag <= maxLag && lag < frames / 2; lag++)
        {
            double s = 0;
            for (var f = lag; f < frames; f++)
            {
                s += onset[f] * onset[f - lag];
            }

            s /= frames - lag;
            var bpm = 60 * fps / lag;
            var prior = Math.Exp(-0.5 * Math.Pow(Math.Log2(bpm / 110) / 0.9, 2));
            var score = s * prior;
            scores.Add(score);
            if (score > bestScore)
            {
                bestScore = score;
                bestLag = lag;
            }
        }

        if (bestLag == 0 || bestScore <= 0)
        {
            return BeatGrid.None(duration) with { LeadIn = leadIn };
        }

        var confidence = Math.Clamp((bestScore - scores.Average()) / bestScore, 0, 1);

        // 4. Evre: vuruş konumlarındaki toplam başlangıç gücü en yüksek kayma.
        var bestPhase = 0;
        var bestPhaseScore = -1.0;
        for (var phase = 0; phase < bestLag; phase++)
        {
            double s = 0;
            for (var f = phase; f < frames; f += bestLag)
            {
                s += onset[f];
            }

            if (s > bestPhaseScore)
            {
                bestPhaseScore = s;
                bestPhase = phase;
            }
        }

        var beats = new List<double>();
        for (var f = bestPhase; f < frames; f += bestLag)
        {
            beats.Add(f / fps);
        }

        return new BeatGrid(60 * fps / bestLag, beats, confidence, leadIn, duration);
    }
}
