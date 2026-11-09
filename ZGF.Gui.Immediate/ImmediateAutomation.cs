using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

/// <summary>What <see cref="ImmediateAutomation"/> drives a host in automation mode through, on its UI thread.</summary>
internal interface IAutomatedHost
{
    Ui Ui { get; }

    /// <summary>The input the next frame takes.</summary>
    UiInput Input { get; }

    /// <summary>The cursor the last frame asked for, which no window shows in automation mode.</summary>
    MouseCursor Cursor { get; }

    /// <summary>Draws a frame at <see cref="ImmediateAutomation"/>'s clock, offscreen, then answers what it asked of the OS.</summary>
    void RunFrame();

    /// <summary>What <paramref name="surface"/>, shown in <paramref name="snapshot"/>, drew.</summary>
    SurfaceImage Capture(SemanticSnapshot snapshot, SemanticSurface surface);

    void RunPostedWork();

    /// <summary>Drops the input queued since the last frame: the next frame starts from what the last one saw.</summary>
    void DiscardPendingInput();
    void Close();
}

/// <summary>
/// Drives an <see cref="ImmediateGuiAppHost{TUiState}"/> in automation mode: input comes only from here, frames run
/// only when asked, at a virtual clock that moves only when asked, and the app's OS services are fakes scripted here.
/// Every call runs on the UI thread and returns once it has; it is safe from any thread but the UI thread, where it
/// throws, since waiting there for the UI thread would never end. Calls made before the host runs wait until it does,
/// except the waits that take a timeout, which bound that and everything else they wait for. Once the app accepts
/// closing, by <see cref="Close"/>, <see cref="Ui.Quit"/> or a close it is asked for, calls fail with an
/// <see cref="ObjectDisposedException"/>, and calls running frames stop before the next one.
/// </summary>
public sealed class ImmediateAutomation
{
    /// <summary>The seconds apart <see cref="Advance"/> and <see cref="Settle"/> run frames.</summary>
    public const double FrameTime = 1.0 / 60.0;

    /// <summary>The most virtual seconds one <see cref="Advance"/> or <see cref="Settle"/> may cover.</summary>
    public const double MaxSeconds = 600.0;

    private const long NoDeadline = long.MaxValue;

    private readonly IAutomatedHost _host;
    private readonly int _uiThread;
    private readonly Action _wakeLoop;
    private readonly FrameReadiness _readiness;
    private readonly ConcurrentQueue<IQueuedCall> _calls = new();
    private readonly object _closeGate = new();
    private readonly List<AutomationEffect> _effects = new();
    private bool _closed;

    internal ImmediateAutomation(AutomationOptions options, IAutomatedHost host, int uiThread, Action wakeLoop)
    {
        Options = options;
        _host = host;
        _uiThread = uiThread;
        _wakeLoop = wakeLoop;
        _readiness = new FrameReadiness(BackgroundWork);
    }

    public AutomationOptions Options { get; }

    /// <summary>Work off the UI thread the app is not ready without, which <see cref="WaitUntilReady"/> waits for.
    /// The host tracks its own font loading here; an app tracks its own with <see cref="BackgroundWork.Track"/>.</summary>
    public BackgroundWork BackgroundWork { get; } = new();

    internal AutomationClipboard Clipboard { get; } = new();
    internal AutomationFilePicker FilePicker { get; } = new();
    internal AutomationMenuBar MenuBar { get; } = new();

    /// <summary>The virtual seconds since the app started, as frames see them in <see cref="Ui.RealTime"/>. UI thread only.</summary>
    internal double Now { get; private set; }

    /// <summary>The seconds the frame being drawn advances by. UI thread only.</summary>
    internal float FrameStep { get; private set; }

    public void MovePointer(float x, float y) => MovePointer(new PointF(x, y));

    /// <summary>Puts the pointer at <paramref name="at"/>, in the main window's UI points, from the next frame.</summary>
    public void MovePointer(PointF at, CancellationToken cancel = default) => Feed(input => input.MovePointer(at), cancel);

    /// <summary>Takes the pointer off every window of the app, from the next frame.</summary>
    public void LeaveWindow() => Feed(input => input.MovePointer(UiInput.Offscreen));

    public void Press(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None, CancellationToken cancel = default) =>
        Feed(input => input.Button((int)button, InputAction.Press, modifiers), cancel);

    public void Release(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None, CancellationToken cancel = default) =>
        Feed(input => input.Button((int)button, InputAction.Release, modifiers), cancel);

    public void Scroll(double x, double y, CancellationToken cancel = default) => Feed(input => input.Scroll(x, y), cancel);

    public void KeyDown(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None, CancellationToken cancel = default) =>
        Feed(input => input.Key(key, InputAction.Press, modifiers), cancel);

    public void KeyUp(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None, CancellationToken cancel = default) =>
        Feed(input => input.Key(key, InputAction.Release, modifiers), cancel);

    /// <summary>Presses and releases <paramref name="key"/> in one call, so nothing can come between the two.</summary>
    internal void PressKey(KeyboardKey key, KeyModifiers modifiers, CancellationToken cancel) => Feed(input =>
    {
        input.Key(key, InputAction.Press, modifiers);
        input.Key(key, InputAction.Release, modifiers);
    }, cancel);

    /// <summary>Cleans up after a command cancelled partway: drops the input it queued that no frame has seen, lets go
    /// of the pointer <paramref name="buttons"/> and <paramref name="keys"/> it holds, and runs one frame so the app
    /// sees them go before the next command. The buttons come up as a cancelled release, which ends a drag and clicks
    /// nothing, as when the app loses focus. Not itself cancellable: it runs once the UI thread takes it, within
    /// <paramref name="timeout"/>.</summary>
    internal void AbandonInput(IReadOnlyCollection<PointerButton> buttons, IReadOnlyCollection<(KeyboardKey Key, KeyModifiers Modifiers)> keys,
        TimeSpan timeout) => Invoke(() =>
    {
        _host.DiscardPendingInput();
        if (buttons.Count == 0 && keys.Count == 0)
            return;
        foreach (var button in buttons)
            _host.Input.CancelButton(button);
        foreach (var (key, modifiers) in keys)
            _host.Input.Key(key, InputAction.Release, modifiers);
        _readiness.RequestFrame();
        Frame(0.0);
    }, FrameReadiness.Deadline(timeout));

    /// <summary>Commits <paramref name="text"/> as typed characters.</summary>
    public void Type(string text, CancellationToken cancel = default) => Feed(input =>
    {
        foreach (var rune in text.EnumerateRunes())
            input.Character((uint)rune.Value);
    }, cancel);

    /// <summary>The IME replaces its composition with <paramref name="text"/>, caret at <paramref name="caret"/>,
    /// the end by default; an empty text ends it.</summary>
    public void Compose(string text, int? caret = null, CancellationToken cancel = default) => Feed(input =>
        input.Compose(text.Length == 0 ? PreeditText.Empty : new PreeditText(text, caret ?? text.Length, [], -1)), cancel);

    /// <summary>Files dragged from the OS file manager are dropped where the pointer is.</summary>
    public void DropFiles(params string[] paths) => Feed(input => input.DropFiles(paths));

    /// <summary>The app comes to the front, or another app takes it: held buttons then come up and popups close.</summary>
    public void SetForeground(bool foreground) => Feed(input =>
    {
        input.SetForeground(foreground);
        if (foreground) return;
        input.ReleaseAll();
        input.DismissPopups();
    });

    /// <summary>The cursor the last frame asked for; no window shows it.</summary>
    public MouseCursor Cursor => Invoke(() => _host.Cursor);

    /// <summary>Runs <paramref name="count"/> frames without moving the clock. Once <paramref name="cancel"/> fires, a
    /// call not yet started is dropped, and one running stops before its next frame.</summary>
    public void Step(int count = 1, CancellationToken cancel = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Invoke(() =>
        {
            for (var i = 0; i < count; i++)
                Frame(0.0, cancel);
        }, NoDeadline, cancel);
    }

    /// <summary>Moves the clock on by <paramref name="seconds"/>, from 0 to <see cref="MaxSeconds"/>, a frame every
    /// <see cref="FrameTime"/> and one at the end for what is left.</summary>
    public void Advance(double seconds, CancellationToken cancel = default)
    {
        RequireDuration(seconds, nameof(seconds));
        var frames = (long)Math.Ceiling(seconds / FrameTime);
        Invoke(() =>
        {
            var end = Now + seconds;
            for (var i = 1; i < frames; i++)
                Frame(FrameTime, cancel);
            if (frames > 0)
                Frame(Math.Max(0.0, end - Now), cancel);
        }, NoDeadline, cancel);
    }

    /// <summary>Runs frames <see cref="FrameTime"/> apart until one ends with nothing but background work pending, or
    /// <paramref name="maxSeconds"/>, from 0 to <see cref="MaxSeconds"/>, of virtual time have passed: animation and
    /// scheduled updates run out, as ambient ones need not.</summary>
    public SettleResult Settle(double maxSeconds, CancellationToken cancel = default)
    {
        RequireDuration(maxSeconds, nameof(maxSeconds));
        return Invoke(() => FrameReadiness.Settle(() =>
        {
            Frame(FrameTime, cancel);
            return _readiness.Pending(_host.Ui);
        }, () => Now, maxSeconds), NoDeadline, cancel);
    }

    /// <summary>Runs the work posted to the UI thread so far, through <see cref="Ui.Post"/> or the host's
    /// <see cref="ImmediateGuiAppHost{TUiState}.Dispatcher"/>, without drawing a frame.</summary>
    public void RunPostedWork() => Invoke(_host.RunPostedWork);

    /// <summary>What still wants a frame.</summary>
    public FramePending Pending => Invoke(() => _readiness.Pending(_host.Ui));

    /// <summary>The virtual seconds since the app started, as frames see them in <see cref="Ui.RealTime"/>.</summary>
    public double Time => Invoke(() => Now);

    /// <summary>The number of the last frame drawn, as <see cref="SemanticSnapshot.Frame"/> numbers it.</summary>
    public long FrameNumber => LastFrame(CancellationToken.None);

    internal long LastFrame(CancellationToken cancel) => Invoke(() => _host.Ui.FrameNumber, NoDeadline, cancel);

    internal FramePending PendingNow(CancellationToken cancel) => Invoke(() => _readiness.Pending(_host.Ui), NoDeadline, cancel);

    /// <summary>Whether the calling thread is the app's UI thread, where every other call throws.</summary>
    public bool OnUiThread => Environment.CurrentManagedThreadId == _uiThread;

    /// <summary>Runs a frame, then more without moving the clock for as long as one asks for the next with
    /// <see cref="Ui.RequestUpdate"/>, work is posted or input waits, until one ends with none of these and no
    /// <see cref="BackgroundWork"/> running; animation frames are not waited for. Throws a <see cref="TimeoutException"/>
    /// once <paramref name="timeout"/> of real time has passed, the UI thread busy or not yet running included, an
    /// <see cref="AggregateException"/> when background work failed, and an <see cref="OperationCanceledException"/> once
    /// <paramref name="cancel"/> fires.</summary>
    public void WaitUntilReady(TimeSpan timeout, CancellationToken cancel = default) =>
        WaitUntil(static () => true, true, timeout, buildSemantics: false, cancel);

    /// <summary>As <see cref="WaitUntilReady"/>, but without waiting for background work, until exactly one node
    /// matches <paramref name="selector"/>, and returns it.</summary>
    public SemanticNode WaitFor(Selector selector, TimeSpan timeout, CancellationToken cancel = default)
    {
        SemanticNode? node = null;
        WaitUntil(() =>
        {
            node = _host.Ui.Snapshot?.Resolve(selector) is TargetResult.Found found ? found.Node : null;
            return node != null;
        }, false, timeout, buildSemantics: true, cancel);
        return node ?? throw new InvalidOperationException($"Waited for {selector} without finding it.");
    }

    /// <summary>The last frame as a test sees it. Until one is asked for, frames build none; the first call runs a
    /// frame, without moving the clock, to build it, and every frame after builds one too.</summary>
    public SemanticSnapshot Snapshot(CancellationToken cancel = default) => Invoke(SnapshotNow, NoDeadline, cancel);

    /// <summary>As <see cref="Snapshot()"/>, but throws a <see cref="TimeoutException"/> once <paramref name="timeout"/> of
    /// real time has passed without the UI thread starting it.</summary>
    internal SemanticSnapshot Snapshot(TimeSpan timeout) => Invoke(SnapshotNow, FrameReadiness.Deadline(timeout));

    /// <summary>What <paramref name="surface"/>, an index into <see cref="Snapshot"/>'s surfaces, drew in the last
    /// frame: 0 is the main window, then each popup and window of its own.</summary>
    public SurfaceImage Capture(int surface = 0) => Invoke(() =>
    {
        var snapshot = SnapshotNow();
        if (surface < 0 || surface >= snapshot.Surfaces.Count)
            throw new ArgumentOutOfRangeException(nameof(surface), surface, $"The last frame drew {snapshot.Surfaces.Count} surfaces.");
        return _host.Capture(snapshot, snapshot.Surfaces[surface]);
    });

    /// <summary>What <paramref name="surface"/>, from this or an earlier snapshot, drew in the last frame, if it is
    /// still shown.</summary>
    public SurfaceImage Capture(SemanticSurface surface, CancellationToken cancel = default) =>
        Invoke(() => _host.Capture(SnapshotNow(), surface), NoDeadline, cancel);

    /// <summary>Answers the next file picker the app opens with <paramref name="path"/>. Pickers with no answer
    /// queued cancel.</summary>
    public void QueueFilePickerAnswer(string path, CancellationToken cancel = default) => Invoke(() => FilePicker.Queue(path), NoDeadline, cancel);

    /// <summary>Cancels the next file picker the app opens, before the answers queued after.</summary>
    public void QueueFilePickerCancel(CancellationToken cancel = default) => Invoke(() => FilePicker.Queue(null), NoDeadline, cancel);

    /// <summary>Every file picker the app opened, in order.</summary>
    public IReadOnlyList<FilePickerRequest> FilePickerRequests(CancellationToken cancel = default) =>
        Invoke(() => FilePicker.Requests.ToArray(), NoDeadline, cancel);

    public string? GetClipboardText(CancellationToken cancel = default) => Invoke(() => Clipboard.Text, NoDeadline, cancel);

    /// <summary>The HTML last put on the clipboard; null after plain text.</summary>
    public string? GetClipboardHtml(CancellationToken cancel = default) => Invoke(() => Clipboard.Html, NoDeadline, cancel);

    public void SetClipboardText(string text, CancellationToken cancel = default) => Invoke(() => Clipboard.SetText(text), NoDeadline, cancel);

    /// <summary>The items of the system menu bar the app's <c>MenuBar</c> records into, where there is one: macOS's.
    /// Empty elsewhere, where menu bars are drawn in the window.</summary>
    public IReadOnlyList<SystemMenuItem> SystemMenuItems(CancellationToken cancel = default) => Invoke(MenuBar.Items, NoDeadline, cancel);

    /// <summary>Picks the system menu bar's item <paramref name="path"/> names, menu titles then its own, as clicking
    /// it would; the app sees it on its next frame.</summary>
    public void PickSystemMenuItem(params string[] path) => PickSystemMenuItem(path, CancellationToken.None);

    public void PickSystemMenuItem(IReadOnlyList<string> path, CancellationToken cancel) => Invoke(() => MenuBar.Pick(path), NoDeadline, cancel);

    /// <summary>Every platform effect the app asked for, in order, recorded instead of performed.</summary>
    public IReadOnlyList<AutomationEffect> Effects(CancellationToken cancel = default) => Invoke(() => _effects.ToArray(), NoDeadline, cancel);

    /// <summary>Closes the app as <see cref="ImmediateGuiAppHost{TUiState}.CloseImmediately"/> does. Calls queued behind
    /// this one, and every call after, fail.</summary>
    public void Close(CancellationToken cancel = default) => Invoke(_host.Close, NoDeadline, cancel);

    internal void Record(AutomationEffect effect) => _effects.Add(effect);

    /// <summary>Notes that something asked for a frame, from any thread, and wakes whoever waits for that.</summary>
    internal void NoteFrameRequested() => _readiness.RequestFrame();

    /// <summary>Runs the calls queued so far, until the app accepts closing. UI thread only.</summary>
    internal void RunCalls()
    {
        for (var count = _calls.Count; count > 0 && !IsClosed() && _calls.TryDequeue(out var call); count--)
            call.Run();
    }

    /// <summary>The app accepted closing: fails every call still queued, and every call after, and stops the frames
    /// of the call running, if any, before the next. Safe to repeat, from any thread.</summary>
    internal void Shutdown()
    {
        lock (_closeGate)
            _closed = true;
        _readiness.Stop();
        while (_calls.TryDequeue(out var call))
            call.Fail(Closed());
    }

    private bool IsClosed()
    {
        lock (_closeGate)
            return _closed;
    }

    private void Feed(Action<UiInput> enqueue, CancellationToken cancel = default) => Invoke(() =>
    {
        enqueue(_host.Input);
        _readiness.RequestFrame();
    }, NoDeadline, cancel);

    private void Frame(double step, CancellationToken cancel = default)
    {
        if (IsClosed())
            throw Closed();
        cancel.ThrowIfCancellationRequested();
        _readiness.BeginFrame();
        Now += step;
        FrameStep = (float)step;
        _readiness.AdvanceTo(Now);
        _host.RunFrame();
        _readiness.EndFrame(_host.Ui);
    }

    private SemanticSnapshot SnapshotNow()
    {
        var ui = _host.Ui;
        var snapshot = ui.Snapshot;
        if (snapshot != null)
            return snapshot;
        ui.BuildsSemantics = true;
        Frame(0.0);
        return ui.Snapshot ?? throw new InvalidOperationException("The frame built no snapshot.");
    }

    private static void RequireDuration(double seconds, string name)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > MaxSeconds)
            throw new ArgumentOutOfRangeException(name, seconds, $"A duration is a finite number of seconds from 0 to {MaxSeconds}.");
    }

    /// <summary>Readiness by the shared rules, each step carrying the deadline across to the UI thread: one not started
    /// by then never runs, and one still running then is left to finish while the wait gives up. <paramref name="cancel"/>
    /// wakes the wait, which then throws before running another step.</summary>
    private void WaitUntil(Func<bool> done, bool untilBackgroundDone, TimeSpan timeout, bool buildSemantics, CancellationToken cancel)
    {
        var deadline = FrameReadiness.Deadline(timeout);
        using var wake = cancel.Register(_readiness.RequestFrame);
        _readiness.WaitUntil(runFrame => Invoke(() =>
        {
            if (buildSemantics)
                _host.Ui.BuildsSemantics = true;
            if (runFrame)
                Frame(0.0, cancel);
            return _readiness.Check(_host.Ui, done);
        }, deadline, cancel), FrameReadiness.Blocking(untilBackgroundDone), timeout, deadline);
    }

    private void Invoke(Action action, long deadline = NoDeadline, CancellationToken cancel = default) => Invoke(() =>
    {
        action();
        return true;
    }, deadline, cancel);

    private T Invoke<T>(Func<T> work, long deadline = NoDeadline, CancellationToken cancel = default)
    {
        if (OnUiThread)
            throw new InvalidOperationException(
                "ImmediateAutomation was called on the app's UI thread, which would wait for itself; call it from another thread.");
        cancel.ThrowIfCancellationRequested();
        var call = new QueuedCall<T>(work, deadline, cancel);
        lock (_closeGate)
        {
            if (_closed)
                throw Closed();
            _calls.Enqueue(call);
        }
        _wakeLoop();
        return call.Await();
    }

    private static ObjectDisposedException Closed() => new(nameof(ImmediateAutomation), "The app has closed.");

    private interface IQueuedCall
    {
        void Run();
        void Fail(Exception error);
    }

    /// <summary>A call waiting for the UI thread. Whichever of the UI thread starting it, the caller giving up at its
    /// deadline or on cancellation, and the app closing comes first decides whether it runs; a caller that gives up on a
    /// call already running leaves it to finish and drops what it returns. A cancelled call never starts.</summary>
    private sealed class QueuedCall<T>(Func<T> work, long deadline, CancellationToken cancel) : IQueuedCall
    {
        private const int Queued = 0;
        private const int Taken = 1;
        private static readonly TimeSpan MaxWaitSlice = TimeSpan.FromHours(1);

        private sealed class Returned(T value)
        {
            public readonly T Value = value;
        }

        private readonly ManualResetEventSlim _done = new();
        private int _state = Queued;
        private Returned? _result;
        private ExceptionDispatchInfo? _error;

        public void Run()
        {
            if (Interlocked.CompareExchange(ref _state, Taken, Queued) != Queued)
                return;
            try
            {
                cancel.ThrowIfCancellationRequested();
                if (deadline != NoDeadline && FrameReadiness.Remaining(deadline) <= TimeSpan.Zero)
                    throw new OwnerBusyException("the UI thread did not start a call before its deadline");
                _result = new Returned(work());
            }
            catch (Exception e)
            {
                _error = ExceptionDispatchInfo.Capture(e);
            }
            finally
            {
                _done.Set();
            }
        }

        public void Fail(Exception error)
        {
            if (Interlocked.CompareExchange(ref _state, Taken, Queued) != Queued)
                return;
            _error = ExceptionDispatchInfo.Capture(error);
            _done.Set();
        }

        public T Await()
        {
            while (!WaitOnce())
            {
                if (cancel.IsCancellationRequested)
                {
                    if (Interlocked.CompareExchange(ref _state, Taken, Queued) == Queued)
                        throw new OperationCanceledException("The call was cancelled before the UI thread started it.", cancel);
                    _done.Wait();
                    break;
                }
                if (deadline == NoDeadline || FrameReadiness.Remaining(deadline) > TimeSpan.Zero)
                    continue;
                if (Interlocked.CompareExchange(ref _state, Taken, Queued) == Queued)
                    throw new OwnerBusyException("the UI thread did not start a call before its deadline, being busy or not yet running");
                throw new OwnerBusyException("a call the UI thread started had not finished by the deadline; it finishes on its own");
            }
            _error?.Throw();
            if (_result == null)
                throw new InvalidOperationException("A call finished without a result or an error.");
            return _result.Value;
        }

        /// <summary>Waits until the call is done, its deadline passes or it is cancelled; true when done.</summary>
        private bool WaitOnce()
        {
            try
            {
                return _done.Wait(WaitSlice(), cancel);
            }
            catch (OperationCanceledException)
            {
                return _done.IsSet;
            }
        }

        private TimeSpan WaitSlice()
        {
            if (deadline == NoDeadline)
                return MaxWaitSlice;
            var remaining = FrameReadiness.Remaining(deadline);
            if (remaining < TimeSpan.Zero)
                return TimeSpan.Zero;
            return remaining < MaxWaitSlice ? remaining : MaxWaitSlice;
        }
    }
}
