using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexAlbum.Assets.Metadata;
using NexAlbum.Core.Projects;
using NexAlbum.Integrations.CommandLine;
using NexAlbum.Integrations.Editing;
using NexAlbum.Integrations.Photoshop;
using NexAlbum.UI.Services;

namespace NexAlbum.UI.ViewModels;

/// <summary>Toplu düzenlemenin kapsamı.</summary>
public enum EditScope
{
    Selected,
    Page,
    Album,
    All,
}

/// <summary>Kapsam seçeneği ve içerdiği (orijinal) fotoğraflar.</summary>
public sealed record EditScopeOption(EditScope Scope, string Name, IReadOnlyList<PhotoAsset> Assets)
{
    public string Label => string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Name, Assets.Count);
}

/// <summary>Toplu düzenleme penceresinin dış dünyası: motorlar, ayarlar, sonuçların projeye bağlanması.</summary>
public interface IBatchEditHost
{
    PhotoshopEngine Photoshop { get; }

    TimeProvider Clock { get; }

    /// <summary>Düzenlenmiş dosyaların klasörü.</summary>
    string OutputFolder { get; }

    string? ToolPath(string presetId);

    void RememberToolPath(string presetId, string path);

    string? AskExecutable();

    /// <summary>Sonucu projeye bağlar (UI iş parçacığında).</summary>
    void Apply(EditBatchResult result);
}

/// <summary>
/// Toplu düzenleme (Karar 20 §1): Photoshop action'ı ya da komut satırı aracı; önce tek fotoğrafta dene, önce/sonra
/// karşılaştır, sonra kapsamdaki tüm fotoğraflara uygula. Orijinaller değişmez; her sonuç hemen projeye bağlanır.
/// </summary>
public sealed class BatchEditViewModel : ObservableObject
{
    private readonly IBatchEditHost host;
    private CancellationTokenSource? cancellation;
    private bool usePhotoshop = true;
    private PhotoshopAction? selectedAction;
    private CommandLinePreset selectedPreset;
    private string executable = string.Empty;
    private string argumentsText = string.Empty;
    private string recipeName = string.Empty;
    private bool outputPng;
    private int jpegQuality = 12;
    private EditScopeOption selectedScope;
    private bool isRunning;
    private double progress;
    private string progressText = string.Empty;
    private string photoshopStatus = string.Empty;
    private string resultText = string.Empty;
    private ImageSource? beforeImage;
    private ImageSource? afterImage;
    private bool nameEdited;

    public BatchEditViewModel(IReadOnlyList<EditScopeOption> scopes, EditRecipe? lastRecipe, IBatchEditHost host)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        Scopes = scopes;
        selectedScope = scopes.FirstOrDefault(s => s.Assets.Count > 0) ?? scopes[0];
        selectedPreset = CommandLinePresets.All[0];

        LoadActionsCommand = new AsyncRelayCommand(LoadActionsAsync, () => !IsRunning);
        BrowseCommand = new RelayCommand(Browse, () => !IsRunning);
        TryCommand = new AsyncRelayCommand(() => RunAsync(previewOnly: true), CanRun);
        RunCommand = new AsyncRelayCommand(() => RunAsync(previewOnly: false), CanRun);
        CancelCommand = new RelayCommand(() => cancellation?.Cancel(), () => IsRunning);

        if (lastRecipe is not null)
        {
            Restore(lastRecipe);
        }
        else
        {
            ApplyPreset(selectedPreset);
        }

        var status = host.Photoshop.CheckStatus();
        photoshopStatus = status.Available ? "Photoshop hazır. Action listesini yükleyin." : status.Reason ?? "Photoshop kullanılamıyor.";
        if (!status.Available && lastRecipe is null)
        {
            usePhotoshop = false;
        }
    }

    public IReadOnlyList<EditScopeOption> Scopes { get; }

    public IReadOnlyList<CommandLinePreset> Presets => CommandLinePresets.All;

    public ObservableCollection<PhotoshopAction> Actions { get; } = [];

    public ObservableCollection<EditFailure> Failures { get; } = [];

    public AsyncRelayCommand LoadActionsCommand { get; }

    public RelayCommand BrowseCommand { get; }

    public AsyncRelayCommand TryCommand { get; }

    public AsyncRelayCommand RunCommand { get; }

    public RelayCommand CancelCommand { get; }

    public bool UsePhotoshop
    {
        get => usePhotoshop;
        set
        {
            if (SetProperty(ref usePhotoshop, value))
            {
                OnPropertyChanged(nameof(UseCommandLine));
                SuggestName();
                RefreshCommands();
            }
        }
    }

    public bool UseCommandLine
    {
        get => !usePhotoshop;
        set => UsePhotoshop = !value;
    }

    public string PhotoshopStatus
    {
        get => photoshopStatus;
        private set => SetProperty(ref photoshopStatus, value);
    }

    public PhotoshopAction? SelectedAction
    {
        get => selectedAction;
        set
        {
            if (SetProperty(ref selectedAction, value))
            {
                SuggestName();
                RefreshCommands();
            }
        }
    }

    public CommandLinePreset SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (value is not null && SetProperty(ref selectedPreset, value))
            {
                ApplyPreset(value);
            }
        }
    }

    public string PresetHint => SelectedPreset.Hint;

    public string Executable
    {
        get => executable;
        set
        {
            if (SetProperty(ref executable, value ?? string.Empty))
            {
                RefreshCommands();
            }
        }
    }

    /// <summary>Bağımsız değişkenler, her satıra bir tane (boşluk içerenler tırnaksız yazılır).</summary>
    public string ArgumentsText
    {
        get => argumentsText;
        set
        {
            if (SetProperty(ref argumentsText, value ?? string.Empty))
            {
                RefreshCommands();
            }
        }
    }

    /// <summary>Tarifin adı; dosya adları ve "düzenlendi" bilgisi bundan türer.</summary>
    public string RecipeName
    {
        get => recipeName;
        set
        {
            if (SetProperty(ref recipeName, value ?? string.Empty))
            {
                nameEdited = true;
                RefreshCommands();
            }
        }
    }

    public bool OutputPng
    {
        get => outputPng;
        set
        {
            if (SetProperty(ref outputPng, value))
            {
                OnPropertyChanged(nameof(OutputJpeg));
            }
        }
    }

    public bool OutputJpeg
    {
        get => !outputPng;
        set => OutputPng = !value;
    }

    public int JpegQuality
    {
        get => jpegQuality;
        set => SetProperty(ref jpegQuality, Math.Clamp(value, 1, 12));
    }

    public EditScopeOption SelectedScope
    {
        get => selectedScope;
        set
        {
            if (value is not null && SetProperty(ref selectedScope, value))
            {
                RefreshCommands();
            }
        }
    }

    public bool IsRunning
    {
        get => isRunning;
        private set
        {
            if (SetProperty(ref isRunning, value))
            {
                RefreshCommands();
            }
        }
    }

    public double Progress
    {
        get => progress;
        private set => SetProperty(ref progress, value);
    }

    public string ProgressText
    {
        get => progressText;
        private set => SetProperty(ref progressText, value);
    }

    public string ResultText
    {
        get => resultText;
        private set => SetProperty(ref resultText, value);
    }

    public ImageSource? BeforeImage
    {
        get => beforeImage;
        private set => SetProperty(ref beforeImage, value);
    }

    public ImageSource? AfterImage
    {
        get => afterImage;
        private set
        {
            if (SetProperty(ref afterImage, value))
            {
                OnPropertyChanged(nameof(HasPreview));
            }
        }
    }

    public bool HasPreview => AfterImage is not null;

    /// <summary>Tarif kurulamıyorsa sebebi; kurulabiliyorsa null.</summary>
    public string? Problem => BuildRecipe().Problem;

    /// <summary>Formdaki seçimlerden tarif.</summary>
    public (EditRecipe? Recipe, string? Problem) BuildRecipe()
    {
        var name = RecipeName.Trim();
        if (name.Length == 0)
        {
            return (null, "Tarif adı girin.");
        }

        var recipe = UsePhotoshop
            ? new EditRecipe { Engine = EditEngineKind.Photoshop, Name = name, ActionSet = SelectedAction?.Set, Action = SelectedAction?.Name }
            : new EditRecipe
            {
                Engine = EditEngineKind.CommandLine,
                Name = name,
                Executable = string.IsNullOrWhiteSpace(Executable) ? null : Executable.Trim(),
                Arguments = [.. ArgumentsText.Split('\n').Select(a => a.Trim('\r', ' ')).Where(a => a.Length > 0)],
            };
        recipe = recipe with { Output = OutputPng ? EditOutputFormat.Png : EditOutputFormat.Jpeg, JpegQuality = JpegQuality };
        return recipe.Problem is { } problem ? (null, problem) : (recipe, null);
    }

    public async Task LoadActionsAsync()
    {
        IsRunning = true;
        PhotoshopStatus = "Photoshop'tan action listesi alınıyor… (Photoshop kapalıysa açılır)";
        try
        {
            var actions = await host.Photoshop.ListActionsAsync().ConfigureAwait(true);
            var previous = SelectedAction;
            Actions.Clear();
            foreach (var action in actions)
            {
                Actions.Add(action);
            }

            SelectedAction = Actions.FirstOrDefault(a => a == previous) ?? Actions.FirstOrDefault();
            PhotoshopStatus = Actions.Count == 0
                ? "Photoshop'ta hiç action bulunamadı."
                : string.Format(CultureInfo.CurrentCulture, "{0} action bulundu.", Actions.Count);
        }
        catch (EditEngineException ex)
        {
            PhotoshopStatus = ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>Önce dene: kapsamdaki ilk fotoğraf; değilse kapsamın tamamı. Her iki durumda da sonuç projeye bağlanır.</summary>
    public async Task RunAsync(bool previewOnly)
    {
        var (recipe, problem) = BuildRecipe();
        if (recipe is null)
        {
            ResultText = problem ?? string.Empty;
            return;
        }

        var assets = previewOnly ? SelectedScope.Assets.Take(1).ToList() : [.. SelectedScope.Assets];
        if (assets.Count == 0)
        {
            ResultText = "Bu kapsamda fotoğraf yok.";
            return;
        }

        IEditEngine engine = recipe.Engine == EditEngineKind.Photoshop ? host.Photoshop : new CommandLineEngine(recipe.Executable);
        if (engine.CheckStatus() is { Available: false, Reason: var reason })
        {
            ResultText = reason ?? "Araç kullanılamıyor.";
            return;
        }

        if (!UsePhotoshop && recipe.Executable is { } exe)
        {
            host.RememberToolPath(SelectedPreset.Id, exe);
        }

        IsRunning = true;
        Failures.Clear();
        ResultText = string.Empty;
        Progress = 0;
        cancellation = new CancellationTokenSource();
        var report = new System.Progress<EditProgress>(p =>
        {
            Progress = p.Total == 0 ? 0 : (double)p.Done / p.Total;
            ProgressText = p.CurrentFile is null
                ? string.Format(CultureInfo.CurrentCulture, "{0} / {1}", p.Done, p.Total)
                : string.Format(CultureInfo.CurrentCulture, "{0} / {1} · {2}", p.Done + 1, p.Total, p.CurrentFile);
        });

        try
        {
            var runner = new EditBatchRunner(engine, new MetadataExtractorReader(), host.Clock);
            var result = await runner.RunAsync(assets, recipe, host.OutputFolder, report, cancellation.Token).ConfigureAwait(true);
            foreach (var failure in result.Failures)
            {
                Failures.Add(failure);
            }

            foreach (var path in result.Edits.Values.Select(e => e.Path))
            {
                ImageLoader.Shared.Invalidate(path);
            }

            host.Apply(result);
            Progress = 1;
            ResultText = Summary(result);
            if (previewOnly && result.Edits.Count == 1)
            {
                var original = assets[0];
                BeforeImage = await Task.Run(() => ImageLoader.Shared.Load(original, ImageLoader.DetailWidth / 2)).ConfigureAwait(true);
                var edited = original with { Edit = result.Edits.Values.Single(), UseEdit = true };
                AfterImage = await Task.Run(() => ImageLoader.Shared.Load(edited.Effective(), ImageLoader.DetailWidth / 2)).ConfigureAwait(true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ResultText = "Çıktı klasörüne yazılamadı: " + ex.Message;
        }
        finally
        {
            cancellation.Dispose();
            cancellation = null;
            IsRunning = false;
        }
    }

    private static string Summary(EditBatchResult result)
    {
        var text = string.Format(CultureInfo.CurrentCulture, "{0} fotoğraf düzenlendi", result.Edits.Count);
        if (result.Failures.Count > 0)
        {
            text += string.Format(CultureInfo.CurrentCulture, ", {0} başarısız", result.Failures.Count);
        }

        if (result.Cancelled)
        {
            text += " — iş durduruldu";
        }

        return text + ". Orijinaller değişmedi; her fotoğrafta «Orijinali göster» ile karşılaştırabilirsiniz.";
    }

    private bool CanRun() => !IsRunning && SelectedScope.Assets.Count > 0 && BuildRecipe().Recipe is not null;

    private void Restore(EditRecipe recipe)
    {
        usePhotoshop = recipe.Engine == EditEngineKind.Photoshop;
        if (recipe is { ActionSet: { } set, Action: { } action })
        {
            selectedAction = new PhotoshopAction(set, action);
            Actions.Add(selectedAction);
        }

        executable = recipe.Executable ?? string.Empty;
        argumentsText = string.Join("\n", recipe.Arguments);
        outputPng = recipe.Output == EditOutputFormat.Png;
        jpegQuality = recipe.JpegQuality;
        recipeName = recipe.Name;
        nameEdited = true;
    }

    private void ApplyPreset(CommandLinePreset preset)
    {
        Executable = host.ToolPath(preset.Id) ?? preset.Locate() ?? string.Empty;
        ArgumentsText = string.Join("\n", preset.Arguments);
        OnPropertyChanged(nameof(PresetHint));
        SuggestName();
    }

    private void SuggestName()
    {
        if (nameEdited && recipeName.Length > 0)
        {
            return;
        }

        recipeName = UsePhotoshop ? SelectedAction?.DisplayName ?? string.Empty : SelectedPreset.Name;
        OnPropertyChanged(nameof(RecipeName));
    }

    private void Browse()
    {
        if (host.AskExecutable() is { } path)
        {
            Executable = path;
        }
    }

    private void RefreshCommands()
    {
        OnPropertyChanged(nameof(Problem));
        LoadActionsCommand.NotifyCanExecuteChanged();
        BrowseCommand.NotifyCanExecuteChanged();
        TryCommand.NotifyCanExecuteChanged();
        RunCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }
}
