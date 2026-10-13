using System.Globalization;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Extensions;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Eklenti paketleri (Karar 22): yalnızca veri taşıyan stil paketleri (.nexpaket). Yüklenince film stilleri listesinde paket adıyla
/// görünür; paketi kaldırılmış projede stil, ilk yerleşik stile düşer (proje dosyasındaki kimlik korunur).
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Paket klasörü (testte geçici klasör verilebilir).</summary>
    public string ExtensionsFolder { get; init; } = ExtensionService.Folder;

    public RelayCommand InstallExtensionCommand { get; private set; } = null!;

    public RelayCommand ShowExtensionsFolderCommand { get; private set; } = null!;

    /// <summary>"2 eklenti paketi: Pro Sinema 1.0.0 · 3 film stili; …" ya da "Eklenti paketi yok".</summary>
    public string ExtensionsSummary
    {
        get
        {
            var packages = ExtensionCatalog.Current.Packages;
            return packages.Count == 0
                ? "Eklenti paketi yok."
                : string.Format(CultureInfo.CurrentCulture, "{0} eklenti paketi: {1}", packages.Count, string.Join("; ", packages.Select(p => p.Summary)));
        }
    }

    public void InstallExtension()
    {
        var file = dialogs.AskExtensionPackagePath();
        if (file is null)
        {
            return;
        }

        try
        {
            var package = ExtensionService.Install(file, ExtensionsFolder);
            Status = string.Format(CultureInfo.CurrentCulture, "Eklenti yüklendi: {0}. Yeni stiller «Hikâye filmi» penceresinde.", package.Summary);
            if (ExtensionService.Problems.Count > 0)
            {
                dialogs.Warn("Bazı öğeler atlandı:\n" + string.Join("\n", ExtensionService.Problems.Select(p => "• " + p.Message)));
            }

            OnPropertyChanged(nameof(ExtensionsSummary));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            dialogs.Warn("Eklenti yüklenemedi: " + ex.Message);
        }
    }

    private void InitializeExtensions()
    {
        InstallExtensionCommand = new RelayCommand(InstallExtension, () => !IsBusy);
        ShowExtensionsFolderCommand = new RelayCommand(() =>
        {
            Directory.CreateDirectory(ExtensionsFolder);
            dialogs.Reveal(ExtensionsFolder);
        });
    }
}
