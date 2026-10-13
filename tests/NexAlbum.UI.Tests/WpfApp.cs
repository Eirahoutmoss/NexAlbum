using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace NexAlbum.UI.Tests;

/// <summary>
/// Testler için tek WPF uygulaması: bir STA iş parçacığında çalışan dağıtıcı (Dispatcher) ve üzerinde <see cref="App"/>.
/// WPF bir süreçte yalnızca bir Application'a izin verir ve nesneleri onu oluşturan iş parçacığına bağlıdır; bu yüzden
/// pencere açan tüm testler işlerini <see cref="Run"/> ile aynı iş parçacığında yürütür (sırayla).
/// </summary>
internal static class WpfApp
{
    private static readonly Lazy<Dispatcher> Host = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Exception? error = null;
        Host.Value.Invoke(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    /// <summary>Kuyruktaki işleri (bağlamalar, düzen, arka plandan dönen küçük resimler) verilen süre boyunca işletir.</summary>
    public static void Pump(TimeSpan duration)
    {
        var end = DateTime.UtcNow + duration;
        do
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
            Thread.Sleep(15);
        }
        while (DateTime.UtcNow < end);
    }

    private static Dispatcher Start()
    {
        Dispatcher? dispatcher = null;
        Exception? error = null;
        using var ready = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                dispatcher = Dispatcher.CurrentDispatcher;
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                var app = Application.Current as App ?? new App();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.InitializeComponent();
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                ready.Set();
            }

            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "WPF test",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }

        return dispatcher!;
    }
}
