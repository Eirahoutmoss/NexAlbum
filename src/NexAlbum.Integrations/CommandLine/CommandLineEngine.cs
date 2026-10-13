using System.Diagnostics;
using System.Text;
using NexAlbum.Core.Projects;
using NexAlbum.Integrations.Editing;

namespace NexAlbum.Integrations.CommandLine;

/// <summary>
/// Komut satırı aracıyla düzenleme (darktable-cli, rawtherapee-cli, …). Bağımsız değişkenler kabuk olmadan,
/// tek tek verilir; <c>{input}</c> ve <c>{output}</c> yer tutucuları dosya yollarıyla değiştirilir (Karar 20 §3).
/// </summary>
public sealed class CommandLineEngine : IEditEngine
{
    private readonly string? executable;

    /// <param name="executable">Durum denetimi için aracın yolu (tarifteki yol işlerken kullanılır); null ise yalnızca tarif denetlenir.</param>
    public CommandLineEngine(string? executable = null)
    {
        this.executable = executable;
    }

    public EditEngineKind Kind => EditEngineKind.CommandLine;

    public string Name => executable is null ? "Komut satırı" : Path.GetFileNameWithoutExtension(executable);

    public EngineStatus CheckStatus() => executable is null || File.Exists(executable)
        ? EngineStatus.Ready
        : EngineStatus.Unavailable("Araç bulunamadı: " + executable);

    /// <summary>Yer tutucuları doldurulmuş bağımsız değişkenler.</summary>
    public static IReadOnlyList<string> BuildArguments(EditRecipe recipe, string input, string output)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        return [.. recipe.Arguments.Select(a => a
            .Replace(EditRecipe.InputToken, input, StringComparison.Ordinal)
            .Replace(EditRecipe.OutputToken, output, StringComparison.Ordinal))];
    }

    public async Task ProcessAsync(EditRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var recipe = request.Recipe;
        if (recipe.Problem is { } problem)
        {
            throw new EditEngineException(problem);
        }

        var exe = recipe.Executable!;
        var info = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (var argument in BuildArguments(recipe, request.InputPath, request.OutputPath))
        {
            info.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(info) ?? throw new EditEngineException("Araç başlatılamadı: " + exe);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new EditEngineException("Araç başlatılamadı (" + exe + "): " + ex.Message, ex);
        }

        using (process)
        {
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                throw;
            }

            var error = (await stderr.ConfigureAwait(false)).Trim();
            var output = (await stdout.ConfigureAwait(false)).Trim();
            if (process.ExitCode != 0)
            {
                var detail = error.Length > 0 ? error : output;
                throw new EditEngineException(string.Create(
                    System.Globalization.CultureInfo.CurrentCulture,
                    $"Araç {process.ExitCode} koduyla bitti{(detail.Length > 0 ? ": " + Shorten(detail) : ".")}"));
            }
        }
    }

    private static string Shorten(string text) => text.Length <= 300 ? text : text[..300] + "…";

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }
}
