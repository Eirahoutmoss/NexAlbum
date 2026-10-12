using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Ana pencerenin görünüm modeli.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        // "+commit" son ekini göstermeyelim.
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        VersionText = string.Format(CultureInfo.CurrentCulture, "Sürüm {0}", plus >= 0 ? version[..plus] : version);
    }

    public string Title { get; } = "NexAlbum";

    public string VersionText { get; }
}
