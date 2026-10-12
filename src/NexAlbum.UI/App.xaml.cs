using System.Globalization;
using System.Windows;
using System.Windows.Threading;

namespace NexAlbum.UI;

/// <summary>
/// Uygulama giriş noktası.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Arayüz Türkçe öncelikli (Karar 12). Dosya formatları ayrıca InvariantCulture kullanır.
        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentUICulture = turkish;
        CultureInfo.DefaultThreadCurrentCulture = turkish;

        DispatcherUnhandledException += OnUnhandledException;
        base.OnStartup(e);
    }

    /// <summary>Beklenmeyen hata uygulamayı kapatmasın; kullanıcı çalışmasını kaydedebilsin.</summary>
    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Beklenmeyen bir hata oluştu. Çalışmanızı kaydetmeniz önerilir.\n\n" + e.Exception.Message,
            AppInfo.Name,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
