using System.Reflection;
using System.Runtime.InteropServices;
using NexAlbum.Core.Projects;
using NexAlbum.Integrations.Editing;

namespace NexAlbum.Integrations.Photoshop;

/// <summary>Photoshop'a betik gönderen kapı; testte sahtesi kullanılır.</summary>
public interface IPhotoshopHost
{
    EngineStatus CheckStatus();

    /// <summary>ExtendScript çalıştırır ve dönen metni verir. Erişim hatasında <see cref="EditEngineException"/>.</summary>
    string RunScript(string script);
}

/// <summary>
/// Photoshop action'larıyla düzenleme (Karar 20 §3). Photoshop aynı anda tek iş yapar; çağrılar sıraya alınır.
/// </summary>
public sealed class PhotoshopEngine(IPhotoshopHost host) : IEditEngine
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public EditEngineKind Kind => EditEngineKind.Photoshop;

    public string Name => "Photoshop";

    public EngineStatus CheckStatus() => host.CheckStatus();

    /// <summary>Photoshop'taki action setleri ve action'lar (Photoshop'u gerekiyorsa başlatır).</summary>
    public async Task<IReadOnlyList<PhotoshopAction>> ListActionsAsync(CancellationToken cancellationToken = default)
    {
        var text = await RunAsync(PhotoshopScripts.ListActions(), cancellationToken).ConfigureAwait(false);
        return PhotoshopScripts.ParseActions(text);
    }

    public async Task ProcessAsync(EditRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Recipe.Problem is { } problem)
        {
            throw new EditEngineException(problem);
        }

        var result = await RunAsync(PhotoshopScripts.Process(request.InputPath, request.OutputPath, request.Recipe), cancellationToken).ConfigureAwait(false);
        if (result.StartsWith(PhotoshopScripts.ErrorPrefix, StringComparison.Ordinal))
        {
            throw new EditEngineException("Photoshop: " + result[PhotoshopScripts.ErrorPrefix.Length..].Trim());
        }

        if (!string.Equals(result, PhotoshopScripts.Ok, StringComparison.Ordinal))
        {
            throw new EditEngineException("Photoshop beklenmeyen yanıt verdi: " + result);
        }
    }

    private async Task<string> RunAsync(string script, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => host.RunScript(script), cancellationToken).ConfigureAwait(false) ?? string.Empty;
        }
        finally
        {
            gate.Release();
        }
    }
}

/// <summary>
/// Windows'ta COM üzerinden Photoshop (<c>Photoshop.Application</c>, <c>DoJavaScript</c>). Kullanıcının kendi kurulumu
/// kullanılır; Adobe kodu dağıtılmaz (Karar 20 §6). Diğer işletim sistemlerinde kullanılamaz durumdadır.
/// </summary>
public sealed class ComPhotoshopHost : IPhotoshopHost, IDisposable
{
    public const string ProgId = "Photoshop.Application";

    private readonly Lock gate = new();
    private object? application;

    public EngineStatus CheckStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            return EngineStatus.Unavailable("Photoshop otomasyonu yalnızca Windows'ta kullanılabilir.");
        }

        return Type.GetTypeFromProgID(ProgId) is null
            ? EngineStatus.Unavailable("Photoshop bulunamadı (kurulu değil ya da otomasyon kaydı yok).")
            : EngineStatus.Ready;
    }

    public string RunScript(string script)
    {
        ArgumentNullException.ThrowIfNull(script);
        if (!OperatingSystem.IsWindows())
        {
            throw new EditEngineException("Photoshop otomasyonu yalnızca Windows'ta kullanılabilir.");
        }

        lock (gate)
        {
            try
            {
                application ??= Activator.CreateInstance(Type.GetTypeFromProgID(ProgId)
                    ?? throw new EditEngineException("Photoshop bulunamadı."))
                    ?? throw new EditEngineException("Photoshop başlatılamadı.");
                var result = application.GetType().InvokeMember("DoJavaScript", BindingFlags.InvokeMethod, binder: null, application, [script], System.Globalization.CultureInfo.InvariantCulture);
                return result as string ?? string.Empty;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is COMException com)
            {
                Release();
                throw new EditEngineException("Photoshop hatası: " + com.Message, com);
            }
            catch (COMException ex)
            {
                Release();
                throw new EditEngineException("Photoshop'a ulaşılamadı: " + ex.Message, ex);
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            Release();
        }
    }

    private void Release()
    {
        if (application is not null && OperatingSystem.IsWindows())
        {
            Marshal.FinalReleaseComObject(application);
        }

        application = null;
    }
}
