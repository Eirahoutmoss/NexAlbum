using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NexAlbum.UI.Services;

namespace NexAlbum.UI;

/// <summary>
/// Uygulama giriş noktası. Açılış ekranı (Assets/splash.png) derleme eylemiyle otomatik gösterilir (Karar 17 §9).
/// </summary>
public partial class App : Application
{
    /// <summary>Gömülü Inter; yoksa Segoe UI (Karar 17 §5).</summary>
    public static readonly FontFamily UiFont = CreateUiFont();

    static App()
    {
        // Tüm pencereler (türetilmiş olanlar dahil) aynı yazı tipi ve boyutla başlar; pencereler yalnızca renkleri verir.
        Control.FontFamilyProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(UiFont, FrameworkPropertyMetadataOptions.Inherits));
        Control.FontSizeProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata(13.0, FrameworkPropertyMetadataOptions.Inherits));
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Arayüz Türkçe öncelikli (Karar 12). Dosya formatları ayrıca InvariantCulture kullanır.
        var turkish = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentUICulture = turkish;
        CultureInfo.DefaultThreadCurrentCulture = turkish;

        DispatcherUnhandledException += OnUnhandledException;
        WpfFontResolver.Register();
        ThemeService.Apply(ThemeService.Parse(new JsonAppSettingsStore().Load().Theme), this);

        // Eklenti paketleri (Karar 22): okunamayan paket atlanır, uygulama açılır.
        try
        {
            ExtensionService.Reload();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Klasör okunamadı: yalnızca yerleşik stiller.
        }

        base.OnStartup(e);
    }

    private static FontFamily CreateUiFont()
    {
        // "pack:" şemasının kayıtlı olduğundan emin ol (Application statik kurucusundan önce çalışabiliriz).
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
        return new FontFamily(new Uri("pack://application:,,,/"), "/NexAlbum;component/Assets/Fonts/#Inter, Segoe UI");
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            ThemeService.ApplyTitleBar(window);
        }
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
