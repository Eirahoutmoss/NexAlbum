using System.Windows;
using System.Windows.Media;

namespace NexAlbum.UI.Controls;

/// <summary>
/// Stillerin kullandığı ekli özellikler. Örnek: <c>&lt;Button Style="{StaticResource CommandButton}" ui:Ui.Icon="{StaticResource Icon.Save}" Content="Kaydet" /&gt;</c>.
/// </summary>
public static class Ui
{
    /// <summary>Düğme, sekme ya da seçenek üzerinde gösterilecek ikon (<c>Themes/Icons.xaml</c>).</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached(
        "Icon", typeof(Geometry), typeof(Ui), new FrameworkPropertyMetadata(null));

    /// <summary>Komut düğmelerinde kısayol ipucu (ör. "Ctrl+S"); komut paletinde ve ipuçlarında gösterilir.</summary>
    public static readonly DependencyProperty ShortcutProperty = DependencyProperty.RegisterAttached(
        "Shortcut", typeof(string), typeof(Ui), new FrameworkPropertyMetadata(null));

    public static Geometry? GetIcon(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (Geometry?)element.GetValue(IconProperty);
    }

    public static void SetIcon(DependencyObject element, Geometry? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IconProperty, value);
    }

    public static string? GetShortcut(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (string?)element.GetValue(ShortcutProperty);
    }

    public static void SetShortcut(DependencyObject element, string? value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ShortcutProperty, value);
    }
}
