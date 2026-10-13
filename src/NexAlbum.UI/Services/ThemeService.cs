using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NexAlbum.UI.Services;

/// <summary>Uygulama teması (Karar 17 §1).</summary>
public enum AppTheme
{
    /// <summary>Koyu antrasit + altın (varsayılan).</summary>
    Gece,

    /// <summary>Açık krem + altın.</summary>
    Fildisi,
}

/// <summary>
/// Temayı çalışırken değiştirir: uygulama kaynaklarındaki ilk sözlük (renk paleti) değiştirilir; tüm stiller renkleri
/// DynamicResource ile aldığından açık pencereler anında güncellenir. Windows başlık çubukları da temaya uyar.
/// </summary>
public static class ThemeService
{
    private const string Folder = "/NexAlbum;component/Themes/";

    /// <summary>Etkin tema.</summary>
    public static AppTheme Current { get; private set; } = AppTheme.Gece;

    /// <summary>Ayar dosyasındaki değer; tanınmayan ya da boş değer varsayılan temadır (Gece).</summary>
    public static AppTheme Parse(string? value) =>
        string.Equals(value, nameof(AppTheme.Fildisi), StringComparison.Ordinal) ? AppTheme.Fildisi : AppTheme.Gece;

    /// <summary>Ayar dosyasına yazılacak değer.</summary>
    public static string ToSetting(AppTheme theme) => theme.ToString();

    /// <summary>Kullanıcıya gösterilen ad.</summary>
    public static string DisplayName(AppTheme theme) => theme == AppTheme.Fildisi ? "Fildişi" : "Gece";

    public static void Apply(AppTheme theme, Application? application = null)
    {
        Current = theme;
        var app = application ?? Application.Current;
        if (app is null)
        {
            return;
        }

        var palette = new ResourceDictionary { Source = new Uri(Folder + ToSetting(theme) + ".xaml", UriKind.Relative) };
        var dictionaries = app.Resources.MergedDictionaries;
        var index = -1;
        for (var i = 0; i < dictionaries.Count; i++)
        {
            if (IsPalette(dictionaries[i]))
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
        {
            dictionaries[index] = palette;
        }
        else
        {
            dictionaries.Insert(0, palette);
        }

        foreach (Window window in app.Windows)
        {
            ApplyTitleBar(window);
        }
    }

    /// <summary>Gece temasında Windows başlık çubuğunu koyu çizer (Windows 10 20H1+); daha eski sürümlerde sessizce yok sayılır.</summary>
    public static void ApplyTitleBar(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18985))
        {
            return;
        }

        var dark = Current == AppTheme.Gece ? 1 : 0;
        _ = NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmUseImmersiveDarkMode, ref dark, sizeof(int));
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            var round = NativeMethods.DwmCornerRound;
            _ = NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmWindowCornerPreference, ref round, sizeof(int));
        }
    }

    private static bool IsPalette(ResourceDictionary dictionary)
    {
        var source = dictionary.Source?.OriginalString;
        return source is not null
            && (source.EndsWith("/Gece.xaml", StringComparison.Ordinal) || source.EndsWith("/Fildisi.xaml", StringComparison.Ordinal));
    }
}

/// <summary>Windows masaüstü pencere yöneticisi (DWM) çağrıları.</summary>
internal static class NativeMethods
{
    internal const int DwmUseImmersiveDarkMode = 20;
    internal const int DwmWindowCornerPreference = 33;
    internal const int DwmCornerRound = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
