using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NexAlbum.UI.Converters;

/// <summary>Anahtar adını (ör. "Icon.Export") uygulama kaynağına çevirir; görünüm modelleri WPF kaynaklarını bilmez.</summary>
public sealed class ResourceKeyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key ? Application.Current?.TryFindResource(key) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
