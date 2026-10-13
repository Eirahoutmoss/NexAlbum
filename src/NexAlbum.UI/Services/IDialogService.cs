using NexAlbum.Core.Profiles;

namespace NexAlbum.UI.Services;

/// <summary>Yeni proje penceresinin sonucu.</summary>
public sealed record NewProjectChoice(string Name, AlbumProfile Profile);

/// <summary>Kaydedilmemiş değişiklik sorusunun cevabı.</summary>
public enum SaveDecision
{
    Save,
    Discard,
    Cancel,
}

/// <summary>
/// Pencere ve diyaloglar. ViewModel'ler bu arayüzle konuşur; böylece WPF olmadan test edilebilir.
/// </summary>
public interface IDialogService
{
    NewProjectChoice? AskNewProject();

    string? AskOpenProjectPath();

    string? AskSaveProjectPath(string suggestedName);

    string? AskPhotoFolder();

    /// <summary>Fotoğraf dosyalarını seçtirir (çoklu seçim); iptalde null.</summary>
    IReadOnlyList<string>? AskPhotoFiles();

    /// <summary>Fon olarak kullanılacak görselleri seçtirir (çoklu seçim); iptalde null.</summary>
    IReadOnlyList<string>? AskBackgroundImages();

    /// <summary>Bir program (.exe) seçtirir; iptalde null.</summary>
    string? AskExecutable();

    /// <summary>Dosyayı harici düzenleyicide açar (program null ise Windows'un "düzenle" eylemi).</summary>
    void LaunchEditor(string file, string? executable);

    /// <summary>Uygulama ayarları penceresi; iptalde null.</summary>
    AppSettings? EditAppSettings(AppSettings settings);

    /// <summary>Toplu düzenleme penceresi (kapanana kadar bekler).</summary>
    void ShowBatchEdit(ViewModels.BatchEditViewModel viewModel);

    /// <summary>Profil düzenleyiciyi açar; iptalde null.</summary>
    AlbumProfile? EditProfile(AlbumProfile profile);

    string? AskPdfPath(string suggestedName);

    /// <summary>Çıktı klasörünü dosya gezgininde gösterir.</summary>
    void Reveal(string path);

    void ShowAbout();

    void Info(string message);

    void Warn(string message);

    bool Confirm(string message);

    SaveDecision AskSaveChanges(string projectName);
}
