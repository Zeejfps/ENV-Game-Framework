using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>An inspection of a host running as usual, its window kept hidden so nothing shows: captures that cannot
/// complete leave no file behind, and closing the app ends every wait at once.</summary>
[Collection(nameof(AutomatedHostCollection))]
public sealed class ImmediateInspectionTests
{
    private sealed class App() : ImmediateGuiApp<DefaultUiState>(new DefaultUiState())
    {
        protected override void OnUpdate(Ui<DefaultUiState> ui)
        {
            using (ui.Column(new Box { Padding = 8f }))
                ui.Label("Inspected");
        }
    }

    [AutomatedHostFact]
    public void Captures_leave_no_file_and_closing_ends_every_wait()
    {
        var started = new TaskCompletionSource<ImmediateGuiAppHost<DefaultUiState>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var host = new ImmediateGuiAppHost<DefaultUiState>(new App(),
                    new StartupConfig { WindowWidth = 200, WindowHeight = 120, WindowTitle = "Inspection test", StartHidden = true });
                started.SetResult(host);
                host.Run();
            }
            catch (Exception e)
            {
                started.TrySetException(e);
            }
        }) { IsBackground = true, Name = "Inspected UI thread" };
        thread.Start();
        var host = started.Task.Result;
        var inspection = host.Inspection;
        Assert.NotNull(inspection);
        var before = InspectFiles();

        Outcome(() => inspection.Capture(TimeSpan.FromMilliseconds(300)));
        using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        Outcome(() => inspection.Capture(TimeSpan.FromSeconds(10), cancelled.Token));
        var pending = Task.Run(() => Outcome(() => inspection.Capture(TimeSpan.FromSeconds(10))));
        Thread.Sleep(200);
        var closing = Stopwatch.StartNew();
        host.Dispatcher.Post(host.CloseImmediately);

        Assert.True(pending.Wait(TimeSpan.FromSeconds(5)), "A capture still waited after the app closed.");
        Assert.True(closing.Elapsed < TimeSpan.FromSeconds(5));
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The host did not close.");
        Assert.Throws<ObjectDisposedException>(() => inspection.Snapshot(TimeSpan.FromSeconds(1)));
        Thread.Sleep(200);
        Assert.Equal(before, InspectFiles());
    }

    /// <summary>Runs <paramref name="capture"/>, which either captures or fails as an inspection may: no frame in time,
    /// cancelled, or the app closed.</summary>
    private static string Outcome(Func<InspectedFrame> capture)
    {
        try
        {
            var frame = capture();
            Assert.Equal(0x89, frame.Png[0]);
            return "captured";
        }
        catch (Exception e) when (e is TimeoutException or OperationCanceledException or ObjectDisposedException)
        {
            return e.GetType().Name;
        }
    }

    private static string[] InspectFiles() =>
        Directory.GetFiles(Path.GetTempPath(), "zgf-inspect-*").Order().ToArray();
}
