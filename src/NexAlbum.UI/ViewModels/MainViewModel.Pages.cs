using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Core.Projects;

namespace NexAlbum.UI.ViewModels;

/// <summary>
/// Geri al / yinele (Karar 12 §10.2) ve elle sayfa yönetimi: ekle, çoğalt, sil, taşı (şeritte sürükle-bırak dahil).
/// Silme onay sormaz; her işlem tek adımda geri alınabilir.
/// </summary>
public sealed partial class MainViewModel
{
    private ProjectHistory history = new();
    private AlbumProject? savedProject;

    public RelayCommand UndoCommand { get; private set; } = null!;

    public RelayCommand RedoCommand { get; private set; } = null!;

    public RelayCommand AddPageCommand { get; private set; } = null!;

    public RelayCommand DuplicatePageCommand { get; private set; } = null!;

    public RelayCommand DeletePageCommand { get; private set; } = null!;

    public RelayCommand MovePageLeftCommand { get; private set; } = null!;

    public RelayCommand MovePageRightCommand { get; private set; } = null!;

    /// <summary>Geri alma geçmişi (testler birleştirme penceresini kapatabilir).</summary>
    public ProjectHistory History => history;

    public string UndoToolTip => history.UndoLabel is { } label ? "Geri al: " + label + " (Ctrl+Z)" : "Geri al (Ctrl+Z)";

    public string RedoToolTip => history.RedoLabel is { } label ? "Yinele: " + label + " (Ctrl+Y)" : "Yinele (Ctrl+Y)";

    public void Undo()
    {
        if (project is null)
        {
            return;
        }

        var label = history.UndoLabel;
        if (history.Undo(project) is { } previous)
        {
            RestoreFromHistory(previous, "Geri alındı: " + (label ?? "düzenleme") + ".");
        }
    }

    public void Redo()
    {
        if (project is null)
        {
            return;
        }

        var label = history.RedoLabel;
        if (history.Redo(project) is { } next)
        {
            RestoreFromHistory(next, "Yinelendi: " + (label ?? "düzenleme") + ".");
        }
    }

    /// <summary>Seçili sayfanın arkasına boş sayfa ekler (sayfa yoksa ilk sayfa).</summary>
    public void AddPage()
    {
        if (project is null)
        {
            return;
        }

        var (updated, index) = PageOperations.AddBlank(project, SelectedPage is { IsCover: false } current ? current.Index : Pages.Count - 1, clock.GetUtcNow());
        UpdateProject(updated, "Sayfa ekle");
        SelectedPage = Pages.ElementAtOrDefault(index);
        Status = F("Sayfa {0} eklendi. «Şablonlar» sekmesinden bir düzen seçin ya da galeriden fotoğraf sürükleyin.", index + 1);
    }

    public void DuplicatePage()
    {
        if (project is null || SelectedPage is null or { IsCover: true })
        {
            return;
        }

        var (updated, index) = PageOperations.Duplicate(project, SelectedPage.Page.Id, clock.GetUtcNow());
        UpdateProject(updated, "Sayfayı çoğalt");
        SelectedPage = Pages.ElementAtOrDefault(index);
        Status = F("Sayfa {0}, sayfa {1} olarak çoğaltıldı.", index, index + 1);
    }

    public void DeletePage()
    {
        if (project is null || SelectedPage is null or { IsCover: true })
        {
            return;
        }

        var index = SelectedPage.Index;
        var photos = SelectedPage.PhotoCount;
        if (PageOperations.Delete(project, SelectedPage.Page.Id) is not { } updated)
        {
            Status = "Sayfa kilitli; silmek için önce kilidi açın.";
            return;
        }

        UpdateProject(updated, "Sayfayı sil");
        SelectedPage = Pages.ElementAtOrDefault(Math.Min(index, Pages.Count - 1));
        Status = photos > 0
            ? F("Sayfa {0} silindi; {1} fotoğraf galeride «kullanılmamış». Geri almak için Ctrl+Z.", index + 1, photos)
            : F("Sayfa {0} silindi. Geri almak için Ctrl+Z.", index + 1);
    }

    /// <summary>Seçili sayfayı bir sola (−1) ya da sağa (+1) taşır.</summary>
    public void ShiftPage(int delta)
    {
        if (SelectedPage is { } page)
        {
            MovePageTo(page, page.Index + delta);
        }
    }

    /// <summary>Sayfayı yeni sırasına taşır (sayfa şeridinde sürükle-bırak).</summary>
    public bool MovePageTo(PageItemViewModel page, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (project is null)
        {
            return false;
        }

        var updated = PageOperations.Move(project, page.Page.Id, newIndex);
        if (updated is null)
        {
            Status = "Kilitli sayfalar yerinden oynamaz; taşımak için önce kilidi açın.";
            return false;
        }

        if (ReferenceEquals(updated, project))
        {
            return false;
        }

        var target = updated.Pages.ToList().FindIndex(p => p.Id == page.Page.Id);
        UpdateProject(updated, "Sayfayı taşı");
        SelectedPage = Pages.ElementAtOrDefault(target);
        Status = F("Sayfa {0}. sıraya taşındı.", target + 1);
        return true;
    }

    private static string F(string format, params object[] args) => string.Format(CultureInfo.CurrentCulture, format, args);

    private void InitializePages()
    {
        history = new ProjectHistory(clock);
        UndoCommand = new RelayCommand(Undo, () => HasProject && history.CanUndo && !IsBusy);
        RedoCommand = new RelayCommand(Redo, () => HasProject && history.CanRedo && !IsBusy);
        AddPageCommand = new RelayCommand(AddPage, () => HasProject && !IsBusy);
        DuplicatePageCommand = new RelayCommand(DuplicatePage, () => SelectedPage is { IsCover: false } && !IsBusy);
        DeletePageCommand = new RelayCommand(DeletePage, () => SelectedPage is { IsLocked: false, IsCover: false } && !IsBusy);
        MovePageLeftCommand = new RelayCommand(() => ShiftPage(-1), () => SelectedPage is { IsLocked: false, Index: > 0 } && !IsBusy);
        MovePageRightCommand = new RelayCommand(() => ShiftPage(1), () => SelectedPage is { IsLocked: false, IsCover: false } page && page.Index < Pages.Count - 1 && !IsBusy);
    }

    private void RefreshPageCommands()
    {
        UndoCommand?.NotifyCanExecuteChanged();
        RedoCommand?.NotifyCanExecuteChanged();
        AddPageCommand?.NotifyCanExecuteChanged();
        DuplicatePageCommand?.NotifyCanExecuteChanged();
        DeletePageCommand?.NotifyCanExecuteChanged();
        MovePageLeftCommand?.NotifyCanExecuteChanged();
        MovePageRightCommand?.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(UndoToolTip));
        OnPropertyChanged(nameof(RedoToolTip));
    }

    /// <summary>Geçmişten gelen hâli uygular: geçmişe yeniden yazılmaz; kaydedilmiş hâle dönüldüyse "kaydedildi" görünür.</summary>
    private void RestoreFromHistory(AlbumProject value, string status)
    {
        UpdateProject(value, label: null, record: false);
        Status = status;
    }
}
