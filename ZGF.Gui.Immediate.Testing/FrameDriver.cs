using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>What a harness's frames draw on: readied before each frame, and finished once the frame has
/// recorded and laid out everything.</summary>
internal interface IFrameTarget
{
    void BeginFrame();

    /// <summary>The frame has ended; <paramref name="background"/> is what it asked the window to be cleared to.</summary>
    void EndFrame(uint background);

    /// <summary>Runs <paramref name="work"/> on the thread that owns the canvas, waiting for it.</summary>
    T Run<T>(Func<T> work);

    /// <summary>A font the frame asked to register, or a family to unregister, once the frame has ended; true when
    /// that changes how text measures and draws.</summary>
    bool ApplyFontChange(FontChange change);
}

/// <summary>The frames of a <see cref="Ui{T}"/> as a harness drives them: input queued between frames, a synthetic
/// clock, readiness, and selector actions checked through the real hit test. Not thread-safe: one thread drives it,
/// except for <see cref="RequestFrame"/>.</summary>
internal sealed class FrameDriver<TState> where TState : class, IUiState
{
    private readonly IFrameTarget _target;
    private readonly Ui<TState> _ui;
    private readonly UiSynchronizationContext _context;
    private readonly BackgroundWork? _background;
    private readonly object _gate = new();
    private bool _frameRequested;
    private long _finishedBeforeFrame;
    private double _scheduledAt = double.PositiveInfinity;
    private UiInput _frame = new();
    private UiInput _pending = new();

    public FrameDriver(TState state, ICanvas canvas, IPopupHost? popups, IFrameTarget target, float width, float height,
        Action<Ui<TState>>? body, BackgroundWork? background)
    {
        _target = target;
        Width = width;
        Height = height;
        _ui = new Ui<TState>(state, canvas, Clipboard, popups);
        _ui.Wake = RequestFrame;
        _context = new UiSynchronizationContext(_ui.Post);
        _background = background;
        background?.Attach(RequestFrame);
        Body = body;
    }

    public Ui<TState> Ui => _ui;
    public TState State => _ui.State;
    public Action<Ui<TState>>? Body { get; set; }
    public Action? BeforeFrame { get; set; }
    public float FrameTime { get; set; }
    public TestClipboard Clipboard { get; } = new();
    public TestFilePicker FilePicker { get; } = new();
    public TestSystemFonts SystemFonts { get; } = new();
    public List<FontFace> RegisteredFonts { get; } = new();
    public List<string> UnregisteredFonts { get; } = new();
    public List<string> OpenedFolders { get; } = new();
    public List<string> RevealedFiles { get; } = new();
    public List<string> OpenedFiles { get; } = new();
    public List<string> OpenedUrls { get; } = new();
    public float Width { get; set; }
    public float Height { get; set; }
    public double RealTime { get; private set; }
    public bool UpdateRequested { get; private set; }
    public double WakeAt { get; private set; }
    public MouseCursor Cursor { get; private set; }
    public bool WantsTextInput { get; private set; }
    public UiRect? ImeCaret { get; private set; }
    public int CompositionResets { get; private set; }
    public bool QuitRequested { get; private set; }
    public WindowState? RequestedWindowState { get; private set; }
    public bool FrontRequested { get; private set; }
    public uint WindowBackground { get; private set; }
    public WindowBounds WindowBounds { get; set; }
    public bool CanDrawTitleBar { get; set; }
    public UiRect? CaptionButtons { get; set; }
    public float HiddenTop { get; set; }
    public uint? TitleBarBackground { get; private set; }
    public List<UiRect> TitleBarDragAreas { get; } = new();
    public WindowIcon? WindowIcon { get; set; }

    public const float SettleFrameTime = 1f / 60f;

    public Action<Ui<TState>> BoundBody => Body ?? throw new InvalidOperationException("No body is bound.");

    public void UseSystemMenuBar(ISystemMenuBar bar) => _ui.SystemMenus = new SystemMenus(bar, static () => { });

    public T Run<T>(float deltaTime, Func<Ui<TState>, T> body)
    {
        using var context = _context.Install();
        Begin(deltaTime);
        var value = body(_ui);
        End();
        return value;
    }

    public void Run(float deltaTime, Action<Ui<TState>> body)
    {
        using var context = _context.Install();
        Begin(deltaTime);
        body(_ui);
        End();
    }

    public void Frame(int count)
    {
        var body = BoundBody;
        for (var i = 0; i < count; i++)
            Run(FrameTime, body);
    }

    public T Frame<T>(Func<Ui<TState>, T> read)
    {
        var body = BoundBody;
        using var context = _context.Install();
        Begin(FrameTime);
        body(_ui);
        var value = read(_ui);
        End();
        return value;
    }

    public void Frame(Action<Ui<TState>> then)
    {
        var body = BoundBody;
        using var context = _context.Install();
        Begin(FrameTime);
        body(_ui);
        then(_ui);
        End();
    }

    public void Wait(double seconds) => RealTime += seconds;

    public void LockPointer(bool locked) => _pending.LockPointer(locked);

    public void MovePointer(float x, float y) => _pending.MovePointer(new PointF(x, y));

    public void Press(PointerButton button, KeyModifiers modifiers) => _pending.Button((int)button, InputAction.Press, modifiers);

    public void Release(PointerButton button, KeyModifiers modifiers) => _pending.Button((int)button, InputAction.Release, modifiers);

    public void Wheel(double y, double x) => _pending.Scroll(x, y);

    public void Swipe(double x, double y, ScrollPhase gesture, ScrollPhase momentum) => _pending.Scroll(x, y, gesture, momentum);

    public void DropFiles(string[] paths) => _pending.DropFiles(paths);

    public void DismissPopups() => _pending.DismissPopups();

    public void SetForeground(bool foreground) => _pending.SetForeground(foreground);

    public void Minimized(bool minimized) => _pending.SetMinimized(minimized);

    public void LoseFocus()
    {
        _pending.ReleaseAll();
        _pending.DismissPopups();
    }

    public void Key(KeyboardKey key, KeyModifiers modifiers) => _pending.Key(key, InputAction.Press, modifiers);

    public void ReleaseKey(KeyboardKey key, KeyModifiers modifiers) => _pending.Key(key, InputAction.Release, modifiers);

    public void Key(Shortcut shortcut)
    {
        Key(shortcut.First.Key, shortcut.First.Held);
        var second = shortcut.Second;
        if (second != null)
            Key(second.Value.Key, second.Value.Held);
    }

    public void Type(string text)
    {
        foreach (var rune in text.EnumerateRunes())
            _pending.Character((uint)rune.Value);
    }

    public void Compose(string text, int? caret, IReadOnlyList<PreeditBlock>? blocks, int focusedBlock) =>
        _pending.Compose(text.Length == 0 ? PreeditText.Empty : new PreeditText(text, caret ?? text.Length, blocks ?? [], focusedBlock));

    public UiRect RectOf(Action<Ui<TState>> body, Func<Ui<TState>, int> id) => Run(0f, ui =>
    {
        body(ui);
        return ui.TryGetRect(id(ui), out var rect) ? rect : throw new InvalidOperationException("Not laid out.");
    });

    public void Click(float x, float y, Action<Ui<TState>> body, PointerButton button, KeyModifiers modifiers)
    {
        MovePointer(x, y);
        Run(1f / 60f, body);
        Press(button, modifiers);
        Run(1f / 60f, body);
        Release(button, modifiers);
        Run(1f / 60f, body);
    }

    public void Click(UiRect rect, Action<Ui<TState>> body, PointerButton button, KeyModifiers modifiers) =>
        Click(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, body, button, modifiers);

    public void DoubleClick(float x, float y, PointerButton button, KeyModifiers modifiers)
    {
        var body = BoundBody;
        Click(x, y, body, button, modifiers);
        Press(button, modifiers);
        Run(1f / 60f, body);
        Release(button, modifiers);
        Run(1f / 60f, body);
    }

    public void Drag(KeyModifiers modifiers, PointF from, ReadOnlySpan<PointF> path)
    {
        MovePointer(from.X, from.Y);
        Frame(1);
        Press(PointerButton.Left, modifiers);
        Frame(1);
        foreach (var point in path)
        {
            MovePointer(point.X, point.Y);
            Frame(1);
        }
    }

    public SemanticSnapshot Snapshot()
    {
        var snapshot = _ui.Snapshot;
        if (snapshot != null)
            return snapshot;
        _ui.BuildsSemantics = true;
        Run(0f, BoundBody);
        return _ui.Snapshot ?? throw new InvalidOperationException("The frame built no snapshot.");
    }

    public SemanticNode Find(Selector selector)
    {
        var snapshot = Snapshot();
        var resolved = snapshot.Resolve(selector);
        if (resolved is TargetResult.Found found)
            return found.Node;
        throw UiTargetException.For(resolved, snapshot);
    }

    public TargetResult Target(Selector selector) => Snapshot().Target(selector);

    public void MoveTo(Selector selector)
    {
        var body = BoundBody;
        var at = Reachable(selector);
        MovePointer(at.X, at.Y);
        Run(1f / 60f, body);
        Recheck(selector, at);
    }

    public void ClickOn(Selector selector, PointerButton button, KeyModifiers modifiers)
    {
        var body = BoundBody;
        MoveTo(selector);
        Press(button, modifiers);
        Run(1f / 60f, body);
        Release(button, modifiers);
        Run(1f / 60f, body);
    }

    public SemanticNode WaitFor(Selector selector, TimeSpan timeout)
    {
        _target.Run(() => _ui.BuildsSemantics = true);
        SemanticNode? node = null;
        try
        {
            WaitUntil(() =>
            {
                node = _ui.Snapshot?.Resolve(selector) is TargetResult.Found found ? found.Node : null;
                return node != null;
            }, timeout);
        }
        catch (TimeoutException timedOut)
        {
            var detail = _target.Run(() => TimedOutDetail(selector));
            if (detail == null)
                throw;
            throw new TimeoutException(timedOut.Message + "\n" + detail, timedOut);
        }
        return node ?? throw new InvalidOperationException($"Waited for {selector} without finding it.");
    }

    private string? TimedOutDetail(Selector selector)
    {
        var snapshot = _ui.Snapshot;
        if (snapshot == null)
            return null;
        var resolved = snapshot.Resolve(selector);
        return resolved is TargetResult.Found found
            ? $"{found.Node} is there, but the frames never became ready.\n\n{snapshot.ToText()}"
            : UiTargetException.Describe(resolved, snapshot);
    }

    private PointF Reachable(Selector selector)
    {
        var snapshot = Snapshot();
        var target = snapshot.Target(selector);
        if (target is TargetResult.Hittable hittable)
            return hittable.At;
        throw UiTargetException.For(target, snapshot);
    }

    private void Recheck(Selector selector, PointF at)
    {
        var snapshot = Snapshot();
        var resolved = snapshot.Resolve(selector);
        if (resolved is not TargetResult.Found found)
            throw UiTargetException.For(resolved, snapshot);
        var reach = snapshot.ReachAt(found.Node, at);
        if (reach is not TargetResult.Hittable)
            throw UiTargetException.For(reach, snapshot);
    }

    public SettleResult Settle(double maxSeconds, float frameTime)
    {
        var body = BoundBody;
        var start = RealTime;
        while (true)
        {
            Run(frameTime, body);
            var pending = Pending() & ~FramePending.BackgroundWork;
            var seconds = RealTime - start;
            if (pending == FramePending.None || seconds >= maxSeconds)
                return new SettleResult(pending, seconds);
        }
    }

    public void WaitUntil(Func<bool> predicate, TimeSpan timeout) => WaitFor(predicate, false, timeout);

    public void WaitUntilReady(TimeSpan timeout) => WaitFor(static () => true, true, timeout);

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

    /// <summary>What still moves after the last frame, which readiness does not wait for and settling runs out:
    /// an animation frame asked for, or an update scheduled.</summary>
    public FramePending Animating => _target.Run(() => Pending() & (FramePending.Animation | FramePending.ScheduledUpdate));

    /// <summary>Runs frames without advancing time while work is posted, frames are requested or a frame asks for the
    /// next to finish a change of state, then waits, on the calling thread, until none of that is pending, nor
    /// tracked background work when <paramref name="untilBackgroundDone"/>, and <paramref name="done"/> holds.
    /// Animation frames are not waited for. Each frame and check runs on the thread that owns the canvas.</summary>
    private void WaitFor(Func<bool> done, bool untilBackgroundDone, TimeSpan timeout)
    {
        var body = BoundBody;
        var clock = Stopwatch.StartNew();
        var state = _target.Run(() =>
        {
            Run(0f, body);
            return Check(done);
        });
        while (true)
        {
            var remaining = timeout - clock.Elapsed;
            var pending = state.Pending & (FramePending.Update | FramePending.FrameRequest |
                                           (untilBackgroundDone ? FramePending.BackgroundWork : FramePending.None));
            var runFrame = (pending & (FramePending.FrameRequest | FramePending.Update)) != 0;
            if (!runFrame && pending == FramePending.None && state.Done)
                return;
            if (remaining <= TimeSpan.Zero)
                throw NotReady(pending, timeout);
            if (runFrame)
            {
                state = _target.Run(() =>
                {
                    Run(0f, body);
                    return Check(done);
                });
                continue;
            }
            lock (_gate)
            {
                if (!_frameRequested)
                    Monitor.Wait(_gate, remaining < PollInterval ? remaining : PollInterval);
            }
            state = _target.Run(() => Check(done));
        }
    }

    private (FramePending Pending, bool Done) Check(Func<bool> done)
    {
        ThrowBackgroundFailures();
        return (Pending(), done());
    }

    private FramePending Pending()
    {
        var pending = FramePending.None;
        if (_ui.RequiredUpdateRequested)
            pending |= FramePending.Update;
        if (_ui.AnimationFrameRequested)
            pending |= FramePending.Animation;
        if (double.IsFinite(_scheduledAt))
            pending |= FramePending.ScheduledUpdate;
        if (_background != null)
        {
            var (outstanding, finished) = _background.Progress();
            if (outstanding > 0)
                pending |= FramePending.BackgroundWork;
            if (finished != _finishedBeforeFrame)
                pending |= FramePending.FrameRequest;
        }
        lock (_gate)
        {
            if (_frameRequested)
                pending |= FramePending.FrameRequest;
        }
        return pending;
    }

    /// <summary>Asks for another frame, as posted work does. Safe from any thread.</summary>
    public void RequestFrame()
    {
        lock (_gate)
        {
            _frameRequested = true;
            Monitor.PulseAll(_gate);
        }
    }

    private void ThrowBackgroundFailures()
    {
        if (_background == null) return;
        var failures = _background.TakeFailures();
        if (failures.Length == 0) return;
        var names = string.Join(", ", failures.Select(f => f.Name));
        throw new AggregateException($"Background work failed: {names}.", failures.Select(f => f.Error));
    }

    private TimeoutException NotReady(FramePending pending, TimeSpan timeout)
    {
        var reasons = new List<string>();
        if ((pending & FramePending.Update) != 0)
            reasons.Add("every frame asked for the next with RequestUpdate, which state that settles stops doing; what moves over time asks with RequestAnimationFrame, which Settle runs out");
        if ((pending & FramePending.FrameRequest) != 0)
            reasons.Add("work kept being posted, or frames requested, after every frame");
        if ((pending & FramePending.BackgroundWork) != 0 && _background != null)
            reasons.Add($"background work still running: {string.Join(", ", _background.Outstanding)}");
        if (reasons.Count == 0)
            reasons.Add("the condition never held");
        return new TimeoutException($"Not ready after {timeout.TotalSeconds:0.###}s: {string.Join("; ", reasons)}.");
    }

    private void Begin(float deltaTime)
    {
        lock (_gate)
            _frameRequested = false;
        if (_background != null)
            _finishedBeforeFrame = _background.Progress().Finished;
        BeforeFrame?.Invoke();
        (_frame, _pending) = (_pending, _frame);
        _frame.Seal();
        _pending.BeginAfter(_frame);
        RealTime += deltaTime;
        if (RealTime >= _scheduledAt)
            _scheduledAt = double.PositiveInfinity;
        _target.BeginFrame();

        _ui.WindowBounds = WindowBounds;
        _ui.CanDrawTitleBar = CanDrawTitleBar;
        _ui.CaptionButtons = CaptionButtons;
        _ui.HiddenTop = HiddenTop;
        _ui.WindowIcon = WindowIcon;
        _ui.BeginFrame(_frame, deltaTime, RealTime, Width, Height);
    }

    private void End()
    {
        _ui.EndFrame();
        _target.EndFrame(_ui.WindowBackground);
        UpdateRequested = _ui.UpdateRequested;
        WakeAt = _ui.WakeAt;
        _scheduledAt = Math.Min(_scheduledAt, _ui.NonAmbientWakeAt);
        Cursor = _ui.Cursor;
        WantsTextInput = _ui.WantsTextInput;
        ImeCaret = _ui.ImeCaret;
        if (_ui.CompositionReset)
        {
            CompositionResets++;
            _pending.Compose(PreeditText.Empty);
        }
        QuitRequested = _ui.QuitRequested;
        RequestedWindowState = _ui.RequestedWindowState;
        FrontRequested = _ui.IsFrontRequested;
        WindowBackground = _ui.WindowBackground;
        TitleBarBackground = _ui.TitleBarBackground;
        _ui.ListTitleBarDragAreas(TitleBarDragAreas);
        if (_ui.ShowDialogs(FilePicker))
            _pending.DismissPopups();
        if (_ui.HasFoldersToOpen)
            OpenedFolders.AddRange(_ui.TakeFoldersToOpen());
        if (_ui.HasFilesToReveal)
            RevealedFiles.AddRange(_ui.TakeFilesToReveal());
        if (_ui.HasFilesToOpen)
            OpenedFiles.AddRange(_ui.TakeFilesToOpen());
        if (_ui.HasUrlsToOpen)
            OpenedUrls.AddRange(_ui.TakeUrlsToOpen());
        if (_ui.HasFontChanges)
            TakeFontChanges();
        if (_ui.HasFontListings)
            AnswerFontListings();
    }

    private void TakeFontChanges()
    {
        foreach (var change in _ui.TakeFontChanges())
        {
            if (change.Face != null)
                RegisteredFonts.Add(change.Face);
            else
                UnregisteredFonts.Add(change.Family);
            if (!_target.ApplyFontChange(change)) continue;
            _ui.InvalidateMeasurements();
            RequestFrame();
        }
    }

    private void AnswerFontListings()
    {
        var families = SystemFonts.ListFamilies();
        foreach (var onListed in _ui.TakeFontListings())
            onListed(families);
    }
}
