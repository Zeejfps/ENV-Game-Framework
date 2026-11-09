using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.Gui.Hosting;
using ZGF.Gui.Immediate.Widgets;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>Non-parallel, as <see cref="GpuCollection"/> is: xUnit runs such collections one at a time and disposes each
/// one's fixtures before the next starts, so the shared GPU device and these hosts never hold GL at once.</summary>
[CollectionDefinition(nameof(AutomatedHostCollection), DisableParallelization = true)]
public sealed class AutomatedHostCollection;

/// <summary>A test that runs a real host with real GLFW windows, skipped where this test thread cannot.</summary>
public sealed class AutomatedHostFactAttribute : FactAttribute
{
    public AutomatedHostFactAttribute()
    {
        if (OperatingSystem.IsMacOS())
            Skip = "GLFW runs only on the process's main thread on macOS, which a test does not have.";
        else if (OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("DISPLAY") == null
                 && Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") == null)
            Skip = "GLFW needs a display server on Linux.";
    }
}

/// <summary>A test that asks Win32 about the host's windows.</summary>
public sealed class AutomatedWindowsHostFactAttribute : FactAttribute
{
    public AutomatedWindowsHostFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "It reads and posts to the host's windows through Win32.";
    }
}

/// <summary>An <see cref="AutomatedHostFactAttribute"/> test that also opens an OpenGL <see cref="GpuDevice"/>.</summary>
public sealed class AutomatedOpenGlHostFactAttribute : FactAttribute
{
    public AutomatedOpenGlHostFactAttribute()
    {
        Skip = new AutomatedHostFactAttribute().Skip;
        if (Skip == null && GpuDevice.PlatformBackend is not (GpuBackend.Wgl or GpuBackend.Egl))
            Skip = "The platform has no OpenGL GpuDevice.";
    }
}

[Collection(nameof(AutomatedHostCollection))]
public sealed class AutomationModeTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan Short = TimeSpan.FromMilliseconds(300);
    private const uint Background = 0xFF2060A0;

    private sealed class TestApp() : ImmediateGuiApp<DefaultUiState>(new DefaultUiState())
    {
        private WindowBounds? _bounds;
        public int Clicks;
        public int Frames;
        public float Scale = 1f;
        public bool PopupOpen;
        public bool DialogOpen;
        public string? Picked;
        public string Name = "";
        public ImmediateAutomation? Automation;
        public Exception? CalledFromUiThread;
        public AutomationRunner? Runner;
        public AutomationResult? RunnerFromUiThread;
        public volatile CancellationTokenSource? CancelOnPress;
        public volatile bool LeftDown;
        public volatile ManualResetEventSlim? Block;
        public readonly ManualResetEventSlim Blocked = new();
        public bool CallDuringShutdown;
        public Exception? CalledDuringShutdown;
        public bool ShutdownCallReturned;
        public int Round;
        private int _startedRound;
        public int SeenRound;
        public bool AskOnSemantics;
        private bool _askedOnSemantics;
        public bool FollowUpRan;

        protected override void OnUpdate(Ui<DefaultUiState> ui)
        {
            Frames++;
            if (CancelOnPress != null && ui.Input.WasPressed(PointerButton.Left))
                CancelOnPress.Cancel();
            LeftDown = ui.Input.IsDown(PointerButton.Left);
            if (AskOnSemantics)
            {
                if (_askedOnSemantics)
                    FollowUpRan = true;
                else if (ui.WantsSemantics)
                {
                    _askedOnSemantics = true;
                    ui.RequestUpdate();
                }
            }
            var round = Round;
            if (round != _startedRound && Automation != null)
            {
                _startedRound = round;
                Automation.BackgroundWork.Track($"round {round}", Task.Run(() => ui.Post(() => SeenRound = round)));
            }
            var block = Block;
            if (block != null)
            {
                Blocked.Set();
                block.Wait();
            }
            ui.Scale = Scale;
            ui.WindowBackground = Background;
            using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Padding = 8f, Gap = 8f }))
            {
                if (ui.Button("Count", id: "count"))
                    Clicks++;
                ui.Label($"Clicks {Clicks}");
                if (ui.Button("Open file", id: "open"))
                    ui.PickFile("Open a note", null, null, path => Picked = path);
                if (ui.Button("Help", id: "help"))
                    ui.OpenUrl("https://example.com/help");
                if (ui.Button("Popup", id: "popup"))
                    PopupOpen = !PopupOpen;
                if (ui.Button("Dialog", id: "dialog"))
                    DialogOpen = true;
                if (ui.Button("Reenter", id: "reenter"))
                    CallFromUiThread();
                if (ui.Button("Zoom", id: "zoom"))
                    Scale = 1.5f;
                if (ui.Button("Quit", id: "quit"))
                    ui.Quit();
                ui.TextField("name", ref Name, width: Sizing.Fixed(120f));
            }
            if (PopupOpen)
            {
                using (ui.Popup(ui.GetId("menu"), new UiRect(150f, 20f, 10f, 10f)))
                using (ui.Column(new Box { Gap = 0f }))
                {
                    using (ui.Box(new Box { Width = Sizing.Fixed(120f), Height = Sizing.Fixed(30f), Background = 0x80FF0000 }))
                    {
                    }
                    using (ui.Box(new Box { Width = Sizing.Fixed(120f), Height = Sizing.Fixed(30f), Background = 0xFF0000FF }))
                    {
                    }
                    ui.Label("In popup");
                }
            }
            using var dialog = ui.WindowDialog("export", "Export", ref DialogOpen, 300f, 200f, ref _bounds);
            if (dialog.IsOpen)
                ui.Label("In dialog");
        }

        protected override void OnShutdown(ImmediateGuiAppHost<DefaultUiState> host)
        {
            var automation = Automation;
            if (!CallDuringShutdown || automation == null)
                return;
            var call = Task.Run(() =>
            {
                try
                {
                    automation.Step();
                }
                catch (Exception e)
                {
                    CalledDuringShutdown = e;
                }
            });
            ShutdownCallReturned = call.Wait(TimeSpan.FromSeconds(5));
        }

        private void CallFromUiThread()
        {
            RunnerFromUiThread = Runner?.Run(new AutomationCommand.Step());
            try
            {
                Automation?.Step();
            }
            catch (Exception e)
            {
                CalledFromUiThread = e;
            }
        }
    }

    /// <summary>A host in automation mode running on a thread of its own, as its UI thread, from once
    /// <paramref name="runGate"/> is set when there is one.</summary>
    private sealed class RunningHost : IDisposable
    {
        private readonly Thread _thread;
        private Exception? _runFailure;

        private RunningHost(TestApp app, ManualResetEventSlim? runGate)
        {
            var started = new TaskCompletionSource<(ImmediateAutomation Automation, uint ThreadId)>(TaskCreationOptions.RunContinuationsAsynchronously);
            _thread = new Thread(() =>
            {
                try
                {
                    var host = new ImmediateGuiAppHost<DefaultUiState>(app,
                        new StartupConfig { WindowWidth = 480, WindowHeight = 600, WindowTitle = "Automation test" },
                        automation: AutomationOptions.InProcess());
                    var automation = host.Automation;
                    if (automation == null)
                        throw new InvalidOperationException("A host given automation options has no automation.");
                    app.Automation = automation;
                    started.SetResult((automation, OperatingSystem.IsWindows() ? Win32.GetCurrentThreadId() : 0));
                    runGate?.Wait();
                    host.Run();
                }
                catch (Exception e)
                {
                    if (!started.TrySetException(e))
                        _runFailure = e;
                }
            }) { IsBackground = true, Name = "Automated UI thread" };
            _thread.Start();
            if (!started.Task.Wait(Timeout))
                throw new TimeoutException("The host did not start.");
            (Automation, UiThreadId) = started.Task.Result;
        }

        public ImmediateAutomation Automation { get; }
        public uint UiThreadId { get; }

        public static RunningHost Start(TestApp app, ManualResetEventSlim? runGate = null) => new(app, runGate);

        public void Dispose()
        {
            try
            {
                Automation.Close();
            }
            catch (ObjectDisposedException)
            {
            }
            Assert.True(_thread.Join(Timeout), "The host did not close.");
            Assert.Null(_runFailure);
        }
    }

    [AutomatedHostFact]
    public void A_click_queued_by_the_driver_reaches_the_app_and_shows_in_the_snapshot_and_the_pixels()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        using var watch = NativeWindowWatch.Start(running);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        var labelBefore = Assert.IsType<TargetResult.Found>(automation.Snapshot().Resolve(Selector.WithText("Clicks 0"))).Node;
        var before = automation.Capture();

        Click(automation, "count");
        automation.Step();

        Assert.Equal(1, app.Clicks);
        var snapshot = automation.Snapshot();
        var label = Assert.IsType<TargetResult.Found>(snapshot.Resolve(Selector.WithText("Clicks 1"))).Node;
        var main = snapshot.Surfaces[0];
        var after = automation.Capture();
        Assert.Equal((int)MathF.Round(main.Rect.Width * main.DpiScale), after.Width);
        Assert.Equal((int)MathF.Round(main.Rect.Height * main.DpiScale), after.Height);
        Assert.Equal(((byte)0x20, (byte)0x60, (byte)0xA0, (byte)0xFF), after[after.Width - 2, after.Height - 2]);
        Assert.True(PixelsDiffer(before, after, Around(labelBefore.Bounds, label.Bounds), main.DpiScale), "The label's pixels did not change with its text.");
        watch.AssertNothingShownOrActivated();
    }

    [AutomatedHostFact]
    public void A_popup_and_a_window_dialog_draw_offscreen_and_no_native_window_is_shown_or_activated()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        using var watch = NativeWindowWatch.Start(running);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);

        Click(automation, "popup");
        automation.Step();
        var withPopup = automation.Snapshot();
        Assert.IsType<TargetResult.Found>(withPopup.Resolve(Selector.WithText("In popup")));
        var popup = Assert.Single(withPopup.Surfaces, s => s.Kind == SurfaceKind.Popup);
        var popupImage = automation.Capture(popup.Index);
        var translucent = popupImage[(int)(60 * popup.DpiScale), (int)(15 * popup.DpiScale)];
        Assert.InRange(translucent.A, 120, 136);
        Assert.InRange(translucent.R, translucent.A - 2, translucent.A + 2);
        Assert.Equal((0, 0), (translucent.G, translucent.B));
        Assert.Equal(((byte)0, (byte)0, (byte)0xFF, (byte)0xFF), popupImage[(int)(60 * popup.DpiScale), (int)(45 * popup.DpiScale)]);

        Click(automation, "popup");
        Click(automation, "dialog");
        automation.Step(2);
        var withDialog = automation.Snapshot();
        Assert.IsType<TargetResult.Found>(withDialog.Resolve(Selector.WithText("In dialog")));
        var dialog = Assert.Single(withDialog.Surfaces, s => s.Kind == SurfaceKind.Window);
        var dialogImage = automation.Capture(dialog.Index);
        Assert.Equal((int)MathF.Round(dialog.Rect.Width * dialog.DpiScale), dialogImage.Width);
        Assert.NotEqual(0, dialogImage[dialogImage.Width / 2, dialogImage.Height / 2].A);
        Assert.Empty(automation.Effects());
        watch.AssertNothingShownOrActivated();
    }

    [AutomatedHostFact]
    public void A_scale_change_settles_into_a_snapshot_and_pixels_of_the_same_size()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        var before = automation.Snapshot().Surfaces[0].DpiScale;

        Click(automation, "zoom");
        automation.WaitUntilReady(Timeout);

        var main = automation.Snapshot().Surfaces[0];
        var image = automation.Capture();
        Assert.Equal(before * 1.5f, main.DpiScale, 3);
        Assert.Equal((int)MathF.Round(main.Rect.Width * main.DpiScale), image.Width);
        Assert.Equal((int)MathF.Round(main.Rect.Height * main.DpiScale), image.Height);
    }

    [AutomatedHostFact]
    public void A_queued_file_picker_answer_reaches_the_app_and_an_unanswered_picker_cancels()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);

        automation.QueueFilePickerAnswer(@"C:\notes\today.md");
        Click(automation, "open");
        automation.WaitUntilReady(Timeout);
        Assert.Equal(@"C:\notes\today.md", app.Picked);

        Click(automation, "open");
        automation.WaitUntilReady(Timeout);
        Assert.Equal(@"C:\notes\today.md", app.Picked);

        var requests = automation.FilePickerRequests();
        Assert.Equal(2, requests.Count);
        Assert.Equal(new FilePickerRequest(FilePickerKind.File, "Open a note", null, null, null, @"C:\notes\today.md"), requests[0]);
        Assert.Null(requests[1].Answer);

        Click(automation, "help");
        Assert.Equal(new AutomationEffect[] { new AutomationEffect.UrlOpened("https://example.com/help") }, automation.Effects());
    }

    [AutomatedWindowsHostFact]
    public void Input_from_the_OS_is_ignored()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        Click(automation, "name");
        automation.Type("a");
        automation.Step();
        Assert.Equal("a", app.Name);

        var count = Assert.IsType<TargetResult.Found>(automation.Snapshot().Resolve(Selector.AtPath("count"))).Node;
        var main = automation.Snapshot().Surfaces[0];
        var x = (int)((count.Bounds.X + count.Bounds.Width / 2) * main.DpiScale);
        var y = (int)((count.Bounds.Y + count.Bounds.Height / 2) * main.DpiScale);
        automation.LeaveWindow();
        foreach (var window in Win32.ThreadWindows(running.UiThreadId))
        {
            Win32.PostMessage(window, Win32.WmChar, 'x', 0);
            Win32.PostMessage(window, Win32.WmMouseMove, 0, Win32.Point(x, y));
            Win32.PostMessage(window, Win32.WmLButtonDown, 1, Win32.Point(x, y));
            Win32.PostMessage(window, Win32.WmLButtonUp, 0, Win32.Point(x, y));
        }
        automation.Step(3);

        Assert.Equal("a", app.Name);
        Assert.Equal(0, app.Clicks);
        var after = Assert.IsType<TargetResult.Found>(automation.Snapshot().Resolve(Selector.AtPath("count"))).Node;
        Assert.False(after.Has(SemanticStates.Hovered));
    }

    [AutomatedHostFact]
    public void The_cursor_a_frame_asks_for_is_readable_but_shown_nowhere()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);

        Click(running.Automation, "name");

        Assert.Equal(MouseCursor.Text, running.Automation.Cursor);
    }

    [AutomatedHostFact]
    public void A_call_on_the_UI_thread_throws_instead_of_waiting_for_itself()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);

        Click(running.Automation, "reenter");

        Assert.IsType<InvalidOperationException>(app.CalledFromUiThread);
    }

    [AutomatedHostFact]
    public void A_runner_called_on_the_UI_thread_rejects_at_once_while_another_command_holds_its_turn()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);
        var runner = AutomationRunner.Driving(running.Automation);
        app.Runner = runner;
        var holding = Task.Run(() => runner.Run(new AutomationCommand.WaitFor(Selector.WithText("Never shown"), TimeSpan.FromSeconds(2))));
        Thread.Sleep(200);

        Click(running.Automation, "reenter");

        var rejected = Assert.IsType<AutomationResult.Rejected>(app.RunnerFromUiThread);
        Assert.Contains("UI thread", rejected.Message);
        Assert.IsType<AutomationResult.Timeout>(holding.Result);
    }

    [AutomatedHostFact]
    public void A_command_cancelled_while_it_waits_its_turn_is_dropped_before_it_injects_anything()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);
        var runner = AutomationRunner.Driving(running.Automation);
        var holding = Task.Run(() => runner.Run(new AutomationCommand.WaitFor(Selector.WithText("Never shown"), TimeSpan.FromSeconds(1.5))));
        Thread.Sleep(200);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        var click = runner.Run(new AutomationCommand.ClickOn(Selector.AtPath("count")), cancel.Token);

        Assert.Contains("cancelled", Assert.IsType<AutomationResult.Timeout>(click).Message);
        Assert.IsType<AutomationResult.Timeout>(holding.Result);
        running.Automation.Step(3);
        Assert.Equal(0, app.Clicks);
    }

    [AutomatedHostFact]
    public void A_wait_cancelled_while_it_runs_stops_at_once()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);
        var runner = AutomationRunner.Driving(running.Automation);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var waited = System.Diagnostics.Stopwatch.StartNew();

        var result = runner.Run(new AutomationCommand.WaitFor(Selector.WithText("Never shown"), TimeSpan.FromSeconds(10)), cancel.Token);

        Assert.Contains("cancelled", Assert.IsType<AutomationResult.Timeout>(result).Message);
        Assert.True(waited.Elapsed < TimeSpan.FromSeconds(3), $"The wait ran {waited.Elapsed} after it was cancelled at 0.2s.");
        Assert.IsType<AutomationResult.Ok>(runner.Run(new AutomationCommand.Step()));
    }

    [AutomatedHostFact]
    public void A_click_cancelled_after_its_press_frame_clicks_nothing_holds_nothing_and_leaves_the_next_command_alone()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);
        var runner = AutomationRunner.Driving(running.Automation);
        using var cancel = new CancellationTokenSource();
        app.CancelOnPress = cancel;

        var cancelled = runner.Run(new AutomationCommand.ClickOn(Selector.AtPath("count")), cancel.Token);

        app.CancelOnPress = null;
        Assert.Contains("cancelled", Assert.IsType<AutomationResult.Timeout>(cancelled).Message);
        running.Automation.Step(2);
        Assert.Equal(0, app.Clicks);
        Assert.False(app.LeftDown);
        Assert.IsType<AutomationResult.Ok>(runner.Run(new AutomationCommand.ClickOn(Selector.AtPath("count"))));
        Assert.Equal(1, app.Clicks);
    }

    [AutomatedHostFact]
    public void Input_a_cancelled_command_queued_that_no_frame_saw_is_dropped()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        Click(automation, "name");
        automation.Step();

        automation.Type("lost");
        automation.Press();
        automation.AbandonInput([], [], TimeSpan.FromSeconds(5));
        automation.Step(2);

        Assert.Equal("", app.Name);
        Assert.False(app.LeftDown);
        automation.Type("kept");
        automation.Step();
        Assert.Equal("kept", app.Name);
    }

    [AutomatedHostFact]
    public void A_clipboard_change_cancelled_while_the_UI_thread_is_busy_never_happens_and_frees_the_turn()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        automation.SetClipboardText("before");
        var runner = AutomationRunner.Driving(automation);
        using var unblock = new ManualResetEventSlim();
        app.Block = unblock;
        var busy = Task.Run(() => automation.Step());
        Assert.True(app.Blocked.Wait(Timeout), "The UI thread did not block.");
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var waited = System.Diagnostics.Stopwatch.StartNew();

        var set = runner.Run(new AutomationCommand.SetClipboard("after"), cancel.Token);
        var held = waited.Elapsed;
        app.Block = null;
        unblock.Set();

        Assert.True(held < TimeSpan.FromSeconds(3), $"The cancelled command held the turn for {held}.");
        Assert.IsType<AutomationResult.Timeout>(set);
        Assert.True(busy.Wait(Timeout));
        automation.Step();
        var read = Assert.IsType<AutomationResult.Ok>(runner.Run(new AutomationCommand.GetClipboard()));
        Assert.Equal("before", Assert.IsType<AutomationPayload.Clipboard>(read.Payload).Text);
    }

    [AutomatedHostFact]
    public void Time_moves_only_when_the_driver_advances_it()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        var start = automation.Time;

        Thread.Sleep(50);
        automation.Step(5);
        Assert.Equal(start, automation.Time);

        automation.Advance(double.Epsilon);
        Assert.Equal(start + double.Epsilon, automation.Time);

        automation.Advance(0.5);
        Assert.Equal(0.5, automation.Time - start, 9);
    }

    [AutomatedHostFact]
    public void Durations_that_are_not_finite_negative_or_too_long_are_refused()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        var frames = app.Frames;

        foreach (var bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1.0, 1e20, ImmediateAutomation.MaxSeconds + 1 })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => automation.Advance(bad));
            Assert.Throws<ArgumentOutOfRangeException>(() => automation.Settle(bad));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => automation.WaitUntilReady(TimeSpan.FromSeconds(-1)));
        Assert.Equal(frames, app.Frames);
    }

    [AutomatedHostFact]
    public void A_readiness_wait_times_out_while_the_UI_thread_is_busy_and_its_expired_call_never_runs()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        _ = automation.Time;
        var frames = app.Frames;

        using var release = new ManualResetEventSlim();
        app.Block = release;
        var busy = Task.Run(() => automation.Step());
        Assert.True(app.Blocked.Wait(Timeout));
        app.Block = null;

        var clock = System.Diagnostics.Stopwatch.StartNew();
        Assert.Throws<TimeoutException>(() => automation.WaitUntilReady(Short));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"The wait took {clock.Elapsed}.");

        release.Set();
        Assert.True(busy.Wait(Timeout));
        _ = automation.Time;
        Assert.Equal(frames + 1, app.Frames);
    }

    [AutomatedHostFact]
    public void A_readiness_wait_times_out_before_the_host_runs_and_its_expired_call_never_runs()
    {
        var app = new TestApp();
        using var runGate = new ManualResetEventSlim();
        using var running = RunningHost.Start(app, runGate);

        Assert.Throws<TimeoutException>(() => running.Automation.WaitUntilReady(Short));

        runGate.Set();
        _ = running.Automation.Time;
        Assert.Equal(0, app.Frames);
        running.Automation.Step();
        Assert.Equal(1, app.Frames);
    }

    [AutomatedHostFact]
    public void Calls_made_while_the_app_shuts_down_fail_instead_of_waiting_forever()
    {
        var app = new TestApp { CallDuringShutdown = true };
        var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);

        Click(running.Automation, "quit");
        running.Dispose();

        Assert.True(app.ShutdownCallReturned);
        Assert.IsType<ObjectDisposedException>(app.CalledDuringShutdown);
    }

    [AutomatedHostFact]
    public void Calls_after_the_app_closed_fail()
    {
        var running = RunningHost.Start(new TestApp());
        running.Dispose();

        Assert.Throws<ObjectDisposedException>(() => running.Automation.Step());
    }

    [AutomatedHostFact]
    public void A_readiness_wait_gives_up_on_a_frame_it_started_that_does_not_finish()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);

        using var release = new ManualResetEventSlim();
        app.Block = release;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var timedOut = Assert.Throws<TimeoutException>(() => automation.WaitUntilReady(Short));
        var waited = clock.Elapsed;
        Assert.True(app.Blocked.IsSet, "The wait never started its frame.");
        Assert.InRange(waited, Short, Short + TimeSpan.FromSeconds(2));
        Assert.Contains("had not finished", timedOut.Message);

        app.Block = null;
        release.Set();
        automation.Step();
    }

    [AutomatedHostFact]
    public void WaitFor_returns_the_node_it_waited_for()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);

        var node = running.Automation.WaitFor(Selector.WithText("Clicks 0"), Timeout);

        Assert.Equal("Clicks 0", node.Label);
    }

    [AutomatedHostFact]
    public void Frames_stop_once_a_frame_quits_and_later_calls_fail()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);
        var quit = Assert.IsType<TargetResult.Hittable>(automation.Snapshot().Target(Selector.AtPath("quit")));
        automation.MovePointer(quit.At);
        automation.Step();
        automation.Press();
        automation.Step();
        automation.Release();
        _ = automation.Time;
        var frames = app.Frames;

        Assert.Throws<ObjectDisposedException>(() => automation.Step(5));

        Assert.Equal(frames + 1, app.Frames);
        Assert.Throws<ObjectDisposedException>(() => automation.Step());
    }

    [AutomatedHostFact]
    public void Readiness_sees_work_that_posts_then_finishes_between_its_checks()
    {
        var app = new TestApp();
        using var running = RunningHost.Start(app);
        var automation = running.Automation;
        automation.WaitUntilReady(Timeout);

        for (var round = 1; round <= 200; round++)
        {
            app.Round = round;
            automation.WaitUntilReady(Timeout);
            Assert.Equal(round, app.SeenRound);
        }
    }

    [AutomatedHostFact]
    public void WaitFor_runs_out_an_update_the_snapshot_building_frame_asked_for()
    {
        var app = new TestApp { AskOnSemantics = true };
        using var running = RunningHost.Start(app);
        running.Automation.WaitUntilReady(Timeout);

        running.Automation.WaitFor(Selector.WithText("Clicks 0"), Timeout);

        Assert.True(app.FollowUpRan, "WaitFor returned before the update the snapshot frame asked for ran.");
        Assert.Equal(FramePending.None, running.Automation.Pending & FramePending.Update);
    }

    [AutomatedOpenGlHostFact]
    public void A_host_cannot_open_while_a_GPU_device_holds_the_GL_entry_points_and_can_once_it_is_gone()
    {
        var device = GpuDevice.Create();
        try
        {
            var refused = Assert.ThrowsAny<Exception>(() => RunningHost.Start(new TestApp()));
            var message = refused.InnerException?.Message ?? refused.Message;
            Assert.Contains("an OpenGL GpuDevice is already live", message);
        }
        finally
        {
            device.Dispose();
        }

        using var running = RunningHost.Start(new TestApp());
        running.Automation.WaitUntilReady(Timeout);
    }

    [AutomatedOpenGlHostFact]
    public void A_GPU_device_cannot_open_while_a_host_holds_the_GL_entry_points_and_can_once_it_is_gone()
    {
        var running = RunningHost.Start(new TestApp());
        try
        {
            running.Automation.WaitUntilReady(Timeout);
            var refused = Assert.Throws<InvalidOperationException>(GpuDevice.Create);
            Assert.Contains("an OpenGL app window is already live", refused.Message);
        }
        finally
        {
            running.Dispose();
        }

        using var device = GpuDevice.Create();
        Assert.True(device.Run(() => device.IsRenderThread));
    }

    private static void Click(ImmediateAutomation automation, string path)
    {
        var target = automation.Snapshot().Target(Selector.AtPath(path));
        var hittable = Assert.IsType<TargetResult.Hittable>(target);
        automation.MovePointer(hittable.At);
        automation.Step();
        automation.Press();
        automation.Step();
        automation.Release();
        automation.Step();
    }


    private static UiRect Around(UiRect a, UiRect b)
    {
        var left = MathF.Min(a.X, b.X);
        var top = MathF.Min(a.Y, b.Y);
        return new UiRect(left, top, MathF.Max(a.Right, b.Right) - left, MathF.Max(a.Bottom, b.Bottom) - top);
    }
    private static bool PixelsDiffer(SurfaceImage before, SurfaceImage after, UiRect rect, float dpiScale)
    {
        var left = (int)(rect.X * dpiScale);
        var top = (int)(rect.Y * dpiScale);
        var right = Math.Min(after.Width, (int)MathF.Ceiling(rect.Right * dpiScale));
        var bottom = Math.Min(after.Height, (int)MathF.Ceiling(rect.Bottom * dpiScale));
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (before[x, y] != after[x, y])
                    return true;
            }
        }
        return false;
    }

    /// <summary>Records, for as long as it runs, every time one of the UI thread's windows is shown, is the
    /// foreground window, or is its thread's active or focused window: through WinEvents as they happen, and by
    /// polling every millisecond. Does nothing off Windows.</summary>
    private sealed class NativeWindowWatch : IDisposable
    {
        private const uint EventSystemForeground = 0x0003;
        private const uint EventObjectShow = 0x8002;
        private const uint EventObjectFocus = 0x8005;
        private const uint OutOfContext = 0x0000;

        private readonly uint _uiThread;
        private readonly List<string> _violations = new();
        private readonly Thread? _thread;
        private volatile bool _stop;
        private Win32.WinEventProc? _hook;

        private NativeWindowWatch(uint uiThread)
        {
            _uiThread = uiThread;
            if (!OperatingSystem.IsWindows())
                return;
            using var hooked = new ManualResetEventSlim();
            _thread = new Thread(() => Watch(hooked)) { IsBackground = true, Name = "Native window watch" };
            _thread.Start();
            hooked.Wait();
        }

        public static NativeWindowWatch Start(RunningHost running) => new(running.UiThreadId);

        public void AssertNothingShownOrActivated()
        {
            Thread.Sleep(50);
            lock (_violations)
                Assert.Empty(_violations);
        }

        private void Watch(ManualResetEventSlim hooked)
        {
            _hook = OnEvent;
            var process = (uint)Environment.ProcessId;
            var hooks = new[]
            {
                Win32.SetWinEventHook(EventSystemForeground, EventSystemForeground, 0, _hook, process, 0, OutOfContext),
                Win32.SetWinEventHook(EventObjectShow, EventObjectShow, 0, _hook, process, 0, OutOfContext),
                Win32.SetWinEventHook(EventObjectFocus, EventObjectFocus, 0, _hook, process, 0, OutOfContext),
            };
            hooked.Set();
            try
            {
                while (!_stop)
                {
                    while (Win32.PeekMessage(out var message, 0, 0, 0, 1))
                    {
                        Win32.TranslateMessage(ref message);
                        Win32.DispatchMessage(ref message);
                    }
                    Poll();
                    Thread.Sleep(1);
                }
            }
            finally
            {
                foreach (var hook in hooks)
                    Win32.UnhookWinEvent(hook);
            }
        }

        private void OnEvent(nint hook, uint eventType, nint window, int objectId, int childId, uint thread, uint time)
        {
            if (window == 0 || Win32.GetWindowThreadProcessId(window, out _) != _uiThread)
                return;
            if (eventType == EventObjectShow && objectId != 0)
                return;
            Note($"event 0x{eventType:X} on window {window}");
        }

        private void Poll()
        {
            var windows = Win32.ThreadWindows(_uiThread);
            foreach (var window in windows)
            {
                if (Win32.IsWindowVisible(window))
                    Note($"window {window} visible");
            }
            if (windows.Contains(Win32.GetForegroundWindow()))
                Note("a window of the UI thread is in the foreground");
            var info = new Win32.GuiThreadInfo { Size = Marshal.SizeOf<Win32.GuiThreadInfo>() };
            if (Win32.GetGUIThreadInfo(_uiThread, ref info) && (windows.Contains(info.Active) || windows.Contains(info.Focus)))
                Note("a window of the UI thread is active or focused");
        }

        private void Note(string violation)
        {
            lock (_violations)
            {
                if (!_violations.Contains(violation))
                    _violations.Add(violation);
            }
        }

        public void Dispose()
        {
            _stop = true;
            _thread?.Join();
        }
    }

    private static class Win32
    {
        public const uint WmMouseMove = 0x0200;
        public const uint WmLButtonDown = 0x0201;
        public const uint WmLButtonUp = 0x0202;
        public const uint WmChar = 0x0102;

        [StructLayout(LayoutKind.Sequential)]
        public struct GuiThreadInfo
        {
            public int Size;
            public int Flags;
            public nint Active;
            public nint Focus;
            public nint Capture;
            public nint MenuOwner;
            public nint MoveSize;
            public nint Caret;
            public int CaretLeft, CaretTop, CaretRight, CaretBottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Msg
        {
            public nint Window;
            public uint Message;
            public nint WParam;
            public nint LParam;
            public uint Time;
            public int X;
            public int Y;
            public uint Private;
        }

        public delegate void WinEventProc(nint hook, uint eventType, nint window, int objectId, int childId, uint thread, uint time);

        private delegate bool EnumWindowsProc(nint window, nint parameter);

        public static nint Point(int x, int y) => (y << 16) | (x & 0xFFFF);

        public static List<nint> ThreadWindows(uint threadId)
        {
            var windows = new List<nint>();
            EnumThreadWindows(threadId, (window, _) =>
            {
                windows.Add(window);
                return true;
            }, 0);
            return windows;
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, nint parameter);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(nint window);

        [DllImport("user32.dll")]
        public static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(nint window, out uint processId);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

        [DllImport("user32.dll")]
        public static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint module, WinEventProc callback, uint processId, uint threadId, uint flags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(nint hook);

        [DllImport("user32.dll")]
        public static extern bool PeekMessage(out Msg message, nint window, uint filterMin, uint filterMax, uint remove);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref Msg message);

        [DllImport("user32.dll")]
        public static extern nint DispatchMessage(ref Msg message);
    }
}
