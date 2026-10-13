using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NexAlbum.UI.ViewModels;

/// <summary>Komut paletindeki bir eylem (Karar 12 §11).</summary>
/// <param name="Title">Görünen ad, ör. "PDF çıktı al".</param>
/// <param name="Category">Kategori, ör. "Albüm".</param>
/// <param name="IconKey">İkon kaynağı anahtarı (Themes/Icons.xaml), ör. "Icon.Export".</param>
/// <param name="Shortcut">Kısayol metni; yoksa null.</param>
/// <param name="Command">Çalıştırılacak komut.</param>
/// <param name="Keywords">Aramada ek olarak eşleşen sözcükler (eş anlamlılar).</param>
public sealed record PaletteCommand(string Title, string Category, string IconKey, string? Shortcut, ICommand Command, string? Keywords = null)
{
    public bool HasShortcut => !string.IsNullOrEmpty(Shortcut);
}

/// <summary>Palette gösterilen satır: komut + o anki etkinlik durumu.</summary>
public sealed class PaletteResult(PaletteCommand command, bool isEnabled) : ObservableObject
{
    private bool isSelected;

    public PaletteCommand Command { get; } = command;

    public bool IsEnabled { get; } = isEnabled;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}

/// <summary>
/// Komut paleti (Ctrl+K): bulanık arama, kısayol gösterimi, son kullanılanlar üstte, ↑↓ + Enter ile gezinme (Karar 12 §11.4).
/// Arama Türkçe karakterlerden bağımsızdır: "on kontrol" → "Ön kontrol", "cikti" → "PDF çıktı al".
/// </summary>
public sealed class CommandPaletteViewModel : ObservableObject
{
    private readonly IReadOnlyList<PaletteCommand> commands;
    private readonly List<PaletteCommand> recent = [];
    private string query = string.Empty;
    private bool isOpen;
    private PaletteResult? selected;

    public CommandPaletteViewModel(IReadOnlyList<PaletteCommand> commands)
    {
        this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public ObservableCollection<PaletteResult> Results { get; } = [];

    public IReadOnlyList<PaletteCommand> Commands => commands;

    public bool IsOpen
    {
        get => isOpen;
        private set => SetProperty(ref isOpen, value);
    }

    public string Query
    {
        get => query;
        set
        {
            if (SetProperty(ref query, value ?? string.Empty))
            {
                Refresh();
            }
        }
    }

    public PaletteResult? Selected
    {
        get => selected;
        set
        {
            var previous = selected;
            if (SetProperty(ref selected, value))
            {
                previous?.IsSelected = false;
                value?.IsSelected = true;
            }
        }
    }

    public bool HasResults => Results.Count > 0;

    public void Open()
    {
        query = string.Empty;
        OnPropertyChanged(nameof(Query));
        Refresh();
        IsOpen = true;
    }

    public void Close() => IsOpen = false;

    /// <summary>Seçimi aşağı (+1) ya da yukarı (−1) taşır; uçlarda başa/sona döner.</summary>
    public void MoveSelection(int delta)
    {
        if (Results.Count == 0)
        {
            return;
        }

        var index = Selected is null ? -1 : Results.IndexOf(Selected);
        index = ((index + delta) % Results.Count + Results.Count) % Results.Count;
        Selected = Results[index];
    }

    /// <summary>Seçili komutu çalıştırır; devre dışıysa palet açık kalır. Çalıştıysa true.</summary>
    public bool ExecuteSelected() => Selected is { } result && Execute(result);

    public bool Execute(PaletteResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var command = result.Command.Command;
        if (!command.CanExecute(null))
        {
            return false;
        }

        recent.Remove(result.Command);
        recent.Insert(0, result.Command);
        Close();
        command.Execute(null);
        return true;
    }

    /// <summary>Türkçe harfleri ve büyük/küçük harfi yok sayan karşılaştırma biçimi (kültürden bağımsız).</summary>
    public static string Fold(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(c switch
            {
                'ı' or 'i' or 'İ' or 'I' => 'I',
                'ç' or 'Ç' => 'C',
                'ğ' or 'Ğ' => 'G',
                'ö' or 'Ö' => 'O',
                'ş' or 'Ş' => 'S',
                'ü' or 'Ü' => 'U',
                'â' or 'Â' => 'A',
                'î' or 'Î' => 'I',
                'û' or 'Û' => 'U',
                _ => char.ToUpperInvariant(c),
            });
        }

        return sb.ToString();
    }

    private void Refresh()
    {
        var tokens = Fold(query).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var ranked = new List<(PaletteCommand Command, int Score, int Order)>();
        for (var i = 0; i < commands.Count; i++)
        {
            if (Score(commands[i], tokens) is { } score)
            {
                ranked.Add((commands[i], score, i));
            }
        }

        var results = ranked
            .Select(r => (r.Command, r.Score, r.Order, Recent: recent.IndexOf(r.Command), Enabled: r.Command.Command.CanExecute(null)))
            .OrderByDescending(r => r.Enabled)
            .ThenBy(r => r.Score)
            .ThenBy(r => r.Recent < 0 ? int.MaxValue : r.Recent)
            .ThenBy(r => r.Order)
            .Select(r => new PaletteResult(r.Command, r.Enabled))
            .ToList();

        Results.Clear();
        foreach (var result in results)
        {
            Results.Add(result);
        }

        Selected = Results.FirstOrDefault();
        OnPropertyChanged(nameof(HasResults));
    }

    /// <summary>Düşük puan daha iyi: 0 başlık başı, 1 sözcük başı, 2 içinde, 3 kategori/anahtar sözcük; eşleşmezse null.</summary>
    private static int? Score(PaletteCommand command, string[] tokens)
    {
        if (tokens.Length == 0)
        {
            return 0;
        }

        var title = Fold(command.Title);
        var extra = Fold(command.Category + " " + command.Keywords);
        var worst = 0;
        foreach (var token in tokens)
        {
            int score;
            var at = title.IndexOf(token, StringComparison.Ordinal);
            if (at == 0)
            {
                score = 0;
            }
            else if (at > 0 && title[at - 1] == ' ')
            {
                score = 1;
            }
            else if (at > 0)
            {
                score = 2;
            }
            else if (extra.Contains(token, StringComparison.Ordinal))
            {
                score = 3;
            }
            else
            {
                return null;
            }

            worst = Math.Max(worst, score);
        }

        return worst;
    }
}
