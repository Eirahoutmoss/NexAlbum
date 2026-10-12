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

    void ShowAbout();

    void Info(string message);

    void Warn(string message);

    bool Confirm(string message);

    SaveDecision AskSaveChanges(string projectName);
}
