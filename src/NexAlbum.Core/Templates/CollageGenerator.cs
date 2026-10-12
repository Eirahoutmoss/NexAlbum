using System.Globalization;
using NexAlbum.Core.Geometry;

namespace NexAlbum.Core.Templates;

/// <summary>
/// Kolaj düzeni üreticisi. Sayfayı oranlı satır/sütun bölmeleriyle keser (giyotin kesim) ve
/// elle tasarlanmış özel düzenleri (mozaik, merkez + çevre, dergi tarzı) ekler.
/// Çıktı deterministiktir; kimlikler parametrelerden türetilir ve sürümler arasında değişmez
/// (proje dosyalarına yazılır).
/// </summary>
public static class CollageGenerator
{
    /// <summary>Üretilen düzenlerin varsayılan kenar payı ve boşluğu (yüzde).</summary>
    public const double Margin = 5;
    public const double Gap = 2;

    private const int MaxPhotos = 12;
    private const int MaxPerLine = 4;

    public static IReadOnlyList<TemplateDefinition> Generate()
    {
        var result = new List<TemplateDefinition>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string id, string name, TemplateCategory category, IReadOnlyList<Cell> cells, double margin = Margin, double gap = Gap)
        {
            if (seen.Add(Signature(cells, margin, gap)))
            {
                result.Add(Build(id, name, category, cells, margin, gap));
            }
        }

        AddSingles(Add);
        for (var n = 2; n <= MaxPhotos; n++)
        {
            foreach (var parts in Compositions(n))
            {
                var uniform = parts.Distinct().Count() == 1;
                var weighted = !uniform && parts.Contains(1);
                var key = string.Join("-", parts);
                var label = string.Join("+", parts);
                var category = uniform ? TemplateCategory.Grid : weighted ? TemplateCategory.Featured : TemplateCategory.Collage;
                var rows = Lines(parts, weighted);
                Add("gen-rows-" + key, "Satır " + label, category, rows);
                Add("gen-cols-" + key, "Sütun " + label, category, Transpose(rows));
                if (uniform)
                {
                    Add("gen-rows-" + key + "-edge", "Kenarsız satır " + label, TemplateCategory.FullBleed, rows, 0, 0.8);
                    Add("gen-cols-" + key + "-edge", "Kenarsız sütun " + label, TemplateCategory.FullBleed, Transpose(rows), 0, 0.8);
                }
            }

            AddFeatured(n, Add);
        }

        AddSpecials(Add);
        return result;
    }

    /// <summary>Satır dağılımları: en çok 3 satır (9+ fotoğrafta 4), satır başı en çok 4, komşu satır farkı en çok 2.</summary>
    public static IEnumerable<int[]> Compositions(int n)
    {
        var maxLines = n <= 8 ? 3 : 4;
        var output = new List<int[]>();
        void Recurse(int remaining, List<int> current)
        {
            if (remaining == 0)
            {
                if (current.Count >= 2)
                {
                    output.Add([.. current]);
                }

                return;
            }

            if (current.Count >= maxLines)
            {
                return;
            }

            for (var p = 1; p <= Math.Min(MaxPerLine, remaining); p++)
            {
                if (current.Count > 0 && Math.Abs(current[^1] - p) > 2)
                {
                    continue;
                }

                current.Add(p);
                Recurse(remaining - p, current);
                current.RemoveAt(current.Count - 1);
            }
        }

        Recurse(n, []);
        return output.Where(parts => !(n >= 7 && parts.Length > 2 && parts.Min() == 1));
    }

    private static void AddSingles(Action<string, string, TemplateCategory, IReadOnlyList<Cell>, double, double> add)
    {
        add("gen-single-center", "Ortada büyük", TemplateCategory.Single, [new(10, 10, 80, 80)], 0, 0);
        add("gen-single-left", "Solda, geniş boşluklu", TemplateCategory.Minimal, [new(8, 12, 58, 76)], 0, 0);
        add("gen-single-right", "Sağda, geniş boşluklu", TemplateCategory.Minimal, [new(34, 12, 58, 76)], 0, 0);
        add("gen-single-top", "Üstte, altı boş", TemplateCategory.Minimal, [new(10, 8, 80, 62)], 0, 0);
        add("gen-single-small", "Küçük ve sade", TemplateCategory.Minimal, [new(30, 30, 40, 40)], 0, 0);
        add("gen-single-half-left", "Sol yarı kenarsız", TemplateCategory.Minimal, [new(0, 0, 50, 100)], 0, 0);
        add("gen-single-half-right", "Sağ yarı kenarsız", TemplateCategory.Minimal, [new(50, 0, 50, 100)], 0, 0);
    }

    /// <summary>Öne çıkan: bir kenarda büyük fotoğraf (%62), kalanlar bir ya da iki şeritte.</summary>
    private static void AddFeatured(int n, Action<string, string, TemplateCategory, IReadOnlyList<Cell>, double, double> add)
    {
        if (n > 9)
        {
            return;
        }

        const double ratio = 62;
        var rest = n - 1;
        string[] sides = ["left", "right", "top", "bottom"];
        string[] names = ["Solda", "Sağda", "Üstte", "Altta"];
        for (var s = 0; s < sides.Length; s++)
        {
            for (var strips = 1; strips <= 2; strips++)
            {
                if (rest > MaxPerLine * strips || (strips == 2 && rest < 4))
                {
                    continue;
                }

                var cells = new List<Cell> { new(0, 0, ratio, 100) };
                var stripWidth = (100 - ratio) / strips;
                for (var strip = 0; strip < strips; strip++)
                {
                    var count = (rest / strips) + (strip < rest % strips ? 1 : 0);
                    for (var i = 0; i < count; i++)
                    {
                        cells.Add(new(ratio + (strip * stripWidth), 100.0 * i / count, stripWidth, 100.0 / count));
                    }
                }

                var oriented = cells.Select(c => Orient(c, sides[s])).ToList();
                var id = string.Create(CultureInfo.InvariantCulture, $"gen-feat-{sides[s]}-{n}-{strips}");
                var name = string.Create(CultureInfo.InvariantCulture, $"{names[s]} büyük + {rest}") + (strips == 2 ? " (iki şerit)" : string.Empty);
                add(id, name, TemplateCategory.Featured, oriented, Margin, Gap);
            }
        }
    }

    private static void AddSpecials(Action<string, string, TemplateCategory, IReadOnlyList<Cell>, double, double> add)
    {
        const double t = 100.0 / 3;

        // Mozaik: 3×3 ızgarada 2×2 büyük hücre + 5 küçük (dört köşe varyasyonu).
        string[] corners = ["tl", "tr", "bl", "br"];
        string[] cornerNames = ["sol üst", "sağ üst", "sol alt", "sağ alt"];
        for (var k = 0; k < corners.Length; k++)
        {
            var bigX = k % 2 == 0 ? 0 : t;
            var bigY = k < 2 ? 0 : t;
            var cells = new List<Cell> { new(bigX, bigY, 2 * t, 2 * t) };
            for (var r = 0; r < 3; r++)
            {
                for (var c = 0; c < 3; c++)
                {
                    var x = c * t;
                    var y = r * t;
                    if (!(x >= bigX - 0.01 && x < bigX + (2 * t) - 0.01 && y >= bigY - 0.01 && y < bigY + (2 * t) - 0.01))
                    {
                        cells.Add(new(x, y, t, t));
                    }
                }
            }

            add("gen-mosaic-" + corners[k] + "-6", "Mozaik, büyük " + cornerNames[k], TemplateCategory.Collage, cells, Margin, Gap);
        }

        // Mozaik 4×4: çapraz iki büyük + sekiz küçük.
        add("gen-mosaic-diagonal-10", "Mozaik, çapraz iki büyük", TemplateCategory.Collage,
        [
            new(0, 0, 50, 50), new(50, 0, 25, 25), new(75, 0, 25, 25), new(50, 25, 25, 25), new(75, 25, 25, 25),
            new(0, 50, 25, 25), new(25, 50, 25, 25), new(0, 75, 25, 25), new(25, 75, 25, 25), new(50, 50, 50, 50),
        ], Margin, Gap);

        // Merkez + çevre: üst ve alt şerit, ortada büyük ve iki yan.
        add("gen-center-5", "Merkez + 4", TemplateCategory.Featured,
            [new(0, 0, 100, 25), new(0, 25, 25, 50), new(25, 25, 50, 50), new(75, 25, 25, 50), new(0, 75, 100, 25)], Margin, Gap);
        add("gen-center-7", "Merkez + 6", TemplateCategory.Featured,
        [
            new(0, 0, 50, 25), new(50, 0, 50, 25), new(0, 25, 25, 50), new(25, 25, 50, 50), new(75, 25, 25, 50),
            new(0, 75, 50, 25), new(50, 75, 50, 25),
        ], Margin, Gap);

        // T ve L biçimleri.
        add("gen-t-4", "T düzeni", TemplateCategory.Collage, [new(0, 0, 100, 45), new(0, 45, 30, 55), new(30, 45, 40, 55), new(70, 45, 30, 55)], Margin, Gap);
        add("gen-l-4", "L düzeni", TemplateCategory.Collage, [new(0, 0, 65, 65), new(65, 0, 35, 65), new(0, 65, 65, 35), new(65, 65, 35, 35)], Margin, Gap);

        // Dergi tarzı: bilinçli boşluk bırakan, kaydırılmış düzenler.
        add("gen-mag-2-offset", "Dergi: kaydırılmış ikili", TemplateCategory.Minimal, [new(6, 8, 52, 60), new(62, 40, 32, 52)], 0, 0);
        add("gen-mag-2-stack", "Dergi: üst geniş, alt küçük", TemplateCategory.Minimal, [new(8, 6, 84, 52), new(52, 62, 40, 32)], 0, 0);
        add("gen-mag-3-steps", "Dergi: basamak", TemplateCategory.Minimal, [new(6, 6, 28, 28), new(36, 36, 28, 28), new(66, 66, 28, 28)], 0, 0);
        add("gen-mag-3-column", "Dergi: dar sütun + büyük", TemplateCategory.Minimal, [new(6, 10, 26, 36), new(6, 54, 26, 36), new(38, 10, 56, 80)], 0, 0);
        add("gen-mag-4-strip", "Dergi: ortada şerit", TemplateCategory.Minimal,
            [new(4, 34, 22, 32), new(28, 34, 22, 32), new(52, 34, 22, 32), new(76, 34, 20, 32)], 0, 0);
        add("gen-mag-3-strip", "Dergi: ortada üçlü", TemplateCategory.Minimal, [new(6, 30, 28, 40), new(36, 30, 28, 40), new(66, 30, 28, 40)], 0, 0);
    }

    /// <summary>Yuva dikdörtgenleri ve kategoriden oluşan geometri imzası (kopya ayıklamak için).</summary>
    public static string Geometry(TemplateDefinition template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return string.Join(";", template.Slots.Select(s => string.Create(CultureInfo.InvariantCulture, $"{s.Rect.X:0.#},{s.Rect.Y:0.#},{s.Rect.Width:0.#},{s.Rect.Height:0.#}")).Order(StringComparer.Ordinal))
            + "|" + Enum.GetName(template.Category);
    }

    private static IReadOnlyList<Cell> Lines(int[] parts, bool weighted)
    {
        var weights = parts.Select(c => weighted ? 1 / Math.Sqrt(c) : 1.0).ToArray();
        var total = weights.Sum();
        var cells = new List<Cell>();
        var y = 0.0;
        for (var i = 0; i < parts.Length; i++)
        {
            var h = i == parts.Length - 1 ? 100 - y : 100 * weights[i] / total;
            for (var c = 0; c < parts[i]; c++)
            {
                cells.Add(new(100.0 * c / parts[i], y, 100.0 / parts[i], h));
            }

            y += h;
        }

        return cells;
    }

    private static IReadOnlyList<Cell> Transpose(IReadOnlyList<Cell> cells) => [.. cells.Select(c => new Cell(c.Y, c.X, c.H, c.W))];

    private static Cell Orient(Cell c, string side) => side switch
    {
        "right" => new(100 - c.X - c.W, c.Y, c.W, c.H),
        "top" => new(c.Y, c.X, c.H, c.W),
        "bottom" => new(c.Y, 100 - c.X - c.W, c.H, c.W),
        _ => c,
    };

    private static TemplateDefinition Build(string id, string name, TemplateCategory category, IReadOnlyList<Cell> cells, double margin, double gap)
    {
        var largest = cells.Max(c => c.W * c.H);
        var featured = cells.Count > 1 && cells.Count(c => Math.Abs((c.W * c.H) - largest) < 1e-6) == 1 && largest >= 1.8 * cells.Min(c => c.W * c.H);
        var slots = cells.Select((c, i) => new TemplateSlot
        {
            Id = "s" + (i + 1).ToString(CultureInfo.InvariantCulture),
            Rect = new PercentRect(Clamp(c.X), Clamp(c.Y), Fit(c.X, c.W), Fit(c.Y, c.H)),
            Priority = featured && Math.Abs((c.W * c.H) - largest) > 1e-6 ? 2 : 1,
        }).ToList();

        return new TemplateDefinition
        {
            Id = id,
            Name = name,
            Category = category,
            PhotoCount = new PhotoCountRange(cells.Count, cells.Count),
            Slots = slots,
            OuterMarginPercent = margin,
            GutterPercent = gap,
            Tags = [cells.Count.ToString(CultureInfo.InvariantCulture) + "-foto", "üretilmiş"],
        };
    }

    private static double Clamp(double v) => Math.Clamp(Math.Round(v, 6), 0, 100);

    private static double Fit(double start, double size) => Math.Min(Math.Round(size, 6), 100 - Clamp(start));

    private static string Signature(IReadOnlyList<Cell> cells, double margin, double gap) =>
        string.Join(";", cells.Select(c => string.Create(CultureInfo.InvariantCulture, $"{c.X:0.##},{c.Y:0.##},{c.W:0.##},{c.H:0.##}")).Order(StringComparer.Ordinal))
        + string.Create(CultureInfo.InvariantCulture, $"|{margin:0.##}|{gap:0.##}");

    /// <summary>Yüzde hücre.</summary>
    internal readonly record struct Cell(double X, double Y, double W, double H);
}
