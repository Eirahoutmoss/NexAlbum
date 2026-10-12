namespace NexAlbum.Core.Common;

/// <summary>
/// Domain değişmezleri için ortak argüman denetimleri.
/// </summary>
internal static class Guard
{
    public static double Finite(double value, string paramName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Değer sonlu bir sayı olmalı.");
        }

        return value;
    }

    public static double NonNegative(double value, string paramName)
    {
        Finite(value, paramName);
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Değer negatif olamaz.");
        }

        return value;
    }

    public static double Positive(double value, string paramName)
    {
        Finite(value, paramName);
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Değer sıfırdan büyük olmalı.");
        }

        return value;
    }

    public static double? PositiveOrNull(double? value, string paramName)
        => value is null ? null : Positive(value.Value, paramName);

    public static double? NonNegativeOrNull(double? value, string paramName)
        => value is null ? null : NonNegative(value.Value, paramName);

    public static int Positive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Değer sıfırdan büyük olmalı.");
        }

        return value;
    }

    public static int? PositiveOrNull(int? value, string paramName)
        => value is null ? null : Positive(value.Value, paramName);

    public static double InRange(double value, double min, double max, string paramName)
    {
        Finite(value, paramName);
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Değer {min} ile {max} arasında olmalı.");
        }

        return value;
    }

    public static int InRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, value, $"Değer {min} ile {max} arasında olmalı.");
        }

        return value;
    }

    public static string NotBlank(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Değer boş olamaz.", paramName);
        }

        return value;
    }

    public static IReadOnlyList<T> NotNullItems<T>(IReadOnlyList<T>? items, string paramName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(items, paramName);
        foreach (var item in items)
        {
            if (item is null)
            {
                throw new ArgumentException("Liste null öğe içeremez.", paramName);
            }
        }

        return items;
    }
}
