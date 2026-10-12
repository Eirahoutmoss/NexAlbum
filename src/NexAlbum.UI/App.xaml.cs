using System.Globalization;
using System.Windows;

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
        base.OnStartup(e);
    }
}
