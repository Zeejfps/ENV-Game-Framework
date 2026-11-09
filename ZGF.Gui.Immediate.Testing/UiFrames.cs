using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>Drives a <see cref="Ui"/> frame by frame against a recording canvas with synthetic
/// text metrics (8px per char, 16px per line) unless given others, in a window of
/// <see cref="Width"/> by <see cref="Height"/>. Input queued between frames arrives in the next one.
/// Popups draw over the window unless a <see cref="TestPopupHost"/> is given. The helpers that take
/// no body record <see cref="Body"/>. Frames run with a <see cref="UiSynchronizationContext"/> installed, so
/// <c>await</c> continues through <see cref="Ui.Post"/>.</summary>
public sealed class UiFrames<TState> where TState : class, IUiState
{
    private readonly RecordingCanvas _canvas;
    private readonly FrameDriver<TState> _driver;

    /// <param name="state">What every frame's <see cref="Ui{T}"/> carries.</param>
    /// <param name="background">The work <see cref="WaitUntilReady"/> waits for, which no other frames may be given.</param>
    public UiFrames(TState state, ITextMeasurer? measurer = null, TestPopupHost? popups = null, float width = 400f, float height = 300f,
        Action<Ui<TState>>? body = null, BackgroundWork? background = null)
    {
        _canvas = new RecordingCanvas(measurer ?? new SyntheticTextMeasurer());
        _driver = new FrameDriver<TState>(state, _canvas, popups, new RecordingTarget(_canvas, popups), width, height, body, background);
    }

    /// <summary>Frames of <paramref name="app"/>: each one records what its <c>OnUpdate</c> does, on a
    /// <see cref="Ui{T}"/> carrying its UI state.</summary>
    public static UiFrames<TState> Running(ImmediateGuiApp<TState> app, ITextMeasurer? measurer = null,
        TestPopupHost? popups = null, float width = 400f, float height = 300f, BackgroundWork? background = null)
    {
        return new UiFrames<TState>(app.UiState, measurer, popups, width, height, app.Update, background);
    }

    /// <summary>Has every frame's <c>MenuBar</c> record its menus into <paramref name="bar"/>, as on
    /// macOS, in place of drawing them.</summary>
    public UiFrames<TState> UseSystemMenuBar(ISystemMenuBar bar)
    {
        _driver.UseSystemMenuBar(bar);
        return this;
    }

    /// <summary>What every frame's <see cref="Ui{T}"/> carries; change it between or during frames.</summary>
    public TState State => _driver.State;

    /// <summary>What <see cref="Frame(int)"/> and the helpers that take no body record.</summary>
    public Action<Ui<TState>>? Body
    {
        get => _driver.Body;
        set => _driver.Body = value;
    }

    /// <summary>Runs before every frame, as an app's loop runs the work posted to it.</summary>
    public Action? BeforeFrame
    {
        get => _driver.BeforeFrame;
        set => _driver.BeforeFrame = value;
    }

    /// <summary>The seconds each <see cref="Frame(int)"/> advances the clock by.</summary>
    public float FrameTime
    {
        get => _driver.FrameTime;
        set => _driver.FrameTime = value;
    }

    public TestClipboard Clipboard => _driver.Clipboard;
    public TestFilePicker FilePicker => _driver.FilePicker;

    /// <summary>What <see cref="Ui.ListSystemFonts"/> lists, answered once the frame that asked is drawn.</summary>
    public TestSystemFonts SystemFonts => _driver.SystemFonts;

    /// <summary>The fonts the app registered with <see cref="Ui.RegisterFont"/>, in order.</summary>
    public List<FontFace> RegisteredFonts => _driver.RegisteredFonts;

    /// <summary>The families the app unregistered with <see cref="Ui.UnregisterFont"/>, in order.</summary>
    public List<string> UnregisteredFonts => _driver.UnregisteredFonts;

    /// <summary>The folders the app asked to show in the OS file manager, in order.</summary>
    public List<string> OpenedFolders => _driver.OpenedFolders;

    /// <summary>The files the app asked to show selected in the OS file manager, in order.</summary>
    public List<string> RevealedFiles => _driver.RevealedFiles;

    /// <summary>The files the app asked to open in the OS's app for them, in order.</summary>
    public List<string> OpenedFiles => _driver.OpenedFiles;

    /// <summary>The web pages the app asked to open in the browser, in order.</summary>
    public List<string> OpenedUrls => _driver.OpenedUrls;
    public RecordingCanvas Canvas => _canvas;

    public float Width
    {
        get => _driver.Width;
        set => _driver.Width = value;
    }

    public float Height
    {
        get => _driver.Height;
        set => _driver.Height = value;
    }

    public double RealTime => _driver.RealTime;
    public bool UpdateRequested => _driver.UpdateRequested;
    public double WakeAt => _driver.WakeAt;

    /// <summary>The cursor the last frame asked for.</summary>
    public MouseCursor Cursor => _driver.Cursor;

    /// <summary>Whether a widget that takes text held focus at the end of the last frame.</summary>
    public bool WantsTextInput => _driver.WantsTextInput;

    /// <summary>Where the last frame put the IME caret, in UI points.</summary>
    public UiRect? ImeCaret => _driver.ImeCaret;

    /// <summary>How many frames discarded the in-flight composition.</summary>
    public int CompositionResets => _driver.CompositionResets;

    /// <summary>Whether a frame asked the app to close with <see cref="Ui.Quit"/>.</summary>
    public bool QuitRequested => _driver.QuitRequested;

    /// <summary>The state the last frame asked the window for, or null when it asked for none.</summary>
    public WindowState? RequestedWindowState => _driver.RequestedWindowState;

    /// <summary>Whether the last frame asked for the window to come to the front.</summary>
    public bool FrontRequested => _driver.FrontRequested;

    /// <summary>The window background the last frame asked for.</summary>
    public uint WindowBackground => _driver.WindowBackground;

    /// <summary>What <see cref="Ui.WindowBounds"/> reports from the next frame.</summary>
    public WindowBounds WindowBounds
    {
        get => _driver.WindowBounds;
        set => _driver.WindowBounds = value;
    }

    /// <summary>Whether the window lets frames draw in its title bar, from the next frame.</summary>
    public bool CanDrawTitleBar
    {
        get => _driver.CanDrawTitleBar;
        set => _driver.CanDrawTitleBar = value;
    }

    /// <summary>Where the window's caption buttons are over the title bar its content reaches under, from
    /// the next frame; null while it does not.</summary>
    public UiRect? CaptionButtons
    {
        get => _driver.CaptionButtons;
        set => _driver.CaptionButtons = value;
    }

    /// <summary>How much of the window, from its top, lies off the screen from the next frame, as a
    /// maximized window's does on Windows.</summary>
    public float HiddenTop
    {
        get => _driver.HiddenTop;
        set => _driver.HiddenTop = value;
    }

    /// <summary>The color of the title bar the last frame recorded, or null when it recorded none.</summary>
    public uint? TitleBarBackground => _driver.TitleBarBackground;

    /// <summary>Where the last frame's title bar drag areas were laid out.</summary>
    public List<UiRect> TitleBarDragAreas => _driver.TitleBarDragAreas;

    /// <summary>The icon the window shows, from the next frame; null while it has none.</summary>
    public WindowIcon? WindowIcon
    {
        get => _driver.WindowIcon;
        set => _driver.WindowIcon = value;
    }

    public T Run<T>(float deltaTime, Func<Ui<TState>, T> body) => _driver.Run(deltaTime, body);

    public void Run(float deltaTime, Action<Ui<TState>> body) => _driver.Run(deltaTime, body);

    /// <summary>Runs <paramref name="count"/> frames of <see cref="Body"/>, each <see cref="FrameTime"/> long.</summary>
    public UiFrames<TState> Frame(int count = 1)
    {
        _driver.Frame(count);
        return this;
    }

    /// <summary>Runs a frame of <see cref="Body"/> and then <paramref name="read"/>, and returns what it read.</summary>
    public T Frame<T>(Func<Ui<TState>, T> read) => _driver.Frame(read);

    /// <summary>Runs a frame of <see cref="Body"/> and then <paramref name="then"/>.</summary>
    public void Frame(Action<Ui<TState>> then) => _driver.Frame(then);

    public int RectCount => _canvas.Rects.Count;

    public DrawRectInputs FirstRect => _canvas.Rects[0].Inputs;

    public uint TextColor(string text) => _canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style.TextColor;

    public bool Drew(string text) => _canvas.Texts.Any(t => t.Inputs.Text == text);

    /// <summary>A rect on the canvas, which counts up from the window's bottom, in UI points.</summary>
    public UiRect ToUi(RectF rect) => new(rect.Left, Height - rect.Top, rect.Width, rect.Height);

    /// <summary>Every text the last frame drew, where it drew it in UI points.</summary>
    public IEnumerable<(string Text, UiRect Rect)> TextRects() => _canvas.Texts.Select(t => (t.Inputs.Text, ToUi(t.Inputs.Position)));

    /// <summary>Where the last frame drew <paramref name="text"/>, which it drew once, in UI points.</summary>
    public UiRect TextRect(string text) => ToUi(_canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Position);

    public UiFrames<TState> Wait(double seconds)
    {
        _driver.Wait(seconds);
        return this;
    }

    /// <summary>The host locks the pointer to the window, or frees it, as it does for <see cref="Ui.LockPointer"/>.</summary>
    public UiFrames<TState> LockPointer(bool locked)
    {
        _driver.LockPointer(locked);
        return this;
    }

    public UiFrames<TState> MovePointer(float x, float y)
    {
        _driver.MovePointer(x, y);
        return this;
    }

    public UiFrames<TState> MovePointer(PointF at) => MovePointer(at.X, at.Y);

    /// <summary>Moves to the middle of <paramref name="rect"/>.</summary>
    public UiFrames<TState> MovePointer(UiRect rect) => MovePointer(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

    public UiFrames<TState> Press(KeyModifiers modifiers = KeyModifiers.None) => Press(PointerButton.Left, modifiers);

    public UiFrames<TState> Press(PointerButton button, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.Press(button, modifiers);
        return this;
    }

    public UiFrames<TState> Release(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.Release(button, modifiers);
        return this;
    }

    public UiFrames<TState> Wheel(double y, double x = 0)
    {
        _driver.Wheel(y, x);
        return this;
    }

    /// <summary>A scroll on a trackpad: the fingers' <paramref name="gesture"/> phase, or the system's coast after
    /// them when <paramref name="momentum"/> is not <see cref="ScrollPhase.None"/>, as macOS reports them.</summary>
    public UiFrames<TState> Swipe(double x, double y, ScrollPhase gesture, ScrollPhase momentum = ScrollPhase.None)
    {
        _driver.Swipe(x, y, gesture, momentum);
        return this;
    }

    /// <summary>Files dragged from the OS file manager are dropped where the pointer is.</summary>
    public UiFrames<TState> DropFiles(params string[] paths)
    {
        _driver.DropFiles(paths);
        return this;
    }

    public UiFrames<TState> DismissPopups()
    {
        _driver.DismissPopups();
        return this;
    }

    /// <summary>Another app takes the focus, as when the user switches to it.</summary>
    public UiFrames<TState> Background()
    {
        _driver.SetForeground(false);
        return this;
    }

    /// <summary>The app takes the focus back.</summary>
    public UiFrames<TState> Foreground()
    {
        _driver.SetForeground(true);
        return this;
    }

    /// <summary>The main window is minimized to the dock or taskbar, or restored from it.</summary>
    public UiFrames<TState> Minimized(bool minimized)
    {
        _driver.Minimized(minimized);
        return this;
    }

    /// <summary>The app lost focus: held buttons come up without the user releasing them.</summary>
    public UiFrames<TState> LoseFocus()
    {
        _driver.LoseFocus();
        return this;
    }

    public UiFrames<TState> Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.Key(key, modifiers);
        return this;
    }

    public UiFrames<TState> ReleaseKey(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.ReleaseKey(key, modifiers);
        return this;
    }

    /// <summary>Presses <paramref name="shortcut"/>'s chords in turn, with the modifiers they take on
    /// the platform the tests run on.</summary>
    public UiFrames<TState> Key(Shortcut shortcut)
    {
        _driver.Key(shortcut);
        return this;
    }

    public UiFrames<TState> Type(string text)
    {
        _driver.Type(text);
        return this;
    }

    /// <summary>The IME replaces its composition with <paramref name="text"/>, caret at
    /// <paramref name="caret"/> (the end by default); an empty text ends it.</summary>
    public UiFrames<TState> Compose(string text, int? caret = null, IReadOnlyList<PreeditBlock>? blocks = null, int focusedBlock = -1)
    {
        _driver.Compose(text, caret, blocks, focusedBlock);
        return this;
    }

    /// <summary>Runs a frame of <paramref name="body"/> and returns where the element with the id
    /// <paramref name="id"/> gives was laid out on the frame before.</summary>
    public UiRect RectOf(Action<Ui<TState>> body, Func<Ui<TState>, int> id) => _driver.RectOf(body, id);

    /// <summary>As <see cref="RectOf(Action{Ui{TState}}, Func{Ui{TState}, int})"/> over <see cref="Body"/>.</summary>
    public UiRect RectOf(Func<Ui<TState>, int> id) => RectOf(_driver.BoundBody, id);

    /// <summary>As <see cref="RectOf(Func{Ui{TState}, int})"/> for the element <see cref="Ui.GetId(string)"/> names.</summary>
    public UiRect RectOf(string id) => RectOf(ui => ui.GetId(id));

    /// <summary>Clicks the middle of the element <paramref name="id"/> gives, as <see cref="Click(float, float, Action{Ui{TState}}, PointerButton, KeyModifiers)"/>.</summary>
    public UiFrames<TState> ClickOn(Action<Ui<TState>> body, Func<Ui<TState>, int> id, PointerButton button = PointerButton.Left)
    {
        _driver.Click(RectOf(body, id), body, button, KeyModifiers.None);
        return this;
    }

    /// <summary>As <see cref="ClickOn(Action{Ui{TState}}, Func{Ui{TState}, int}, PointerButton)"/> over <see cref="Body"/>.</summary>
    public UiFrames<TState> ClickOn(Func<Ui<TState>, int> id, PointerButton button = PointerButton.Left) => ClickOn(_driver.BoundBody, id, button);

    /// <summary>Clicks the middle of the element <see cref="Ui.GetId(string)"/> names.</summary>
    public UiFrames<TState> ClickOn(string id, PointerButton button = PointerButton.Left) => ClickOn(ui => ui.GetId(id), button);

    /// <summary>Moves to (x, y), lets a frame see the pointer there, then presses and releases
    /// <paramref name="button"/> over two more frames, running <paramref name="body"/> each time.</summary>
    public UiFrames<TState> Click(float x, float y, Action<Ui<TState>> body, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.Click(x, y, body, button, modifiers);
        return this;
    }

    /// <summary>As <see cref="Click(float, float, Action{Ui{TState}}, PointerButton, KeyModifiers)"/> over <see cref="Body"/>.</summary>
    public UiFrames<TState> Click(float x, float y, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Click(x, y, _driver.BoundBody, button, modifiers);

    /// <summary>Clicks the middle of <paramref name="rect"/>, as <see cref="Click(float, float, PointerButton, KeyModifiers)"/>.</summary>
    public UiFrames<TState> Click(UiRect rect, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.Click(rect, _driver.BoundBody, button, modifiers);
        return this;
    }

    /// <summary>Clicks at (x, y) as <see cref="Click(float, float, PointerButton, KeyModifiers)"/>, then
    /// presses and releases <paramref name="button"/> again over two more frames.</summary>
    public UiFrames<TState> DoubleClick(float x, float y, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.DoubleClick(x, y, button, modifiers);
        return this;
    }

    /// <summary>Double-clicks the middle of <paramref name="rect"/>, as <see cref="DoubleClick(float, float, PointerButton, KeyModifiers)"/>.</summary>
    public UiFrames<TState> DoubleClick(UiRect rect, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        DoubleClick(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, button, modifiers);

    /// <summary>Moves to <paramref name="from"/> and presses there, then moves through
    /// <paramref name="path"/>, a frame of <see cref="Body"/> after each step; release to drop.</summary>
    public UiFrames<TState> Drag(PointF from, params ReadOnlySpan<PointF> path) => Drag(KeyModifiers.None, from, path);

    /// <summary>As <see cref="Drag(PointF, ReadOnlySpan{PointF})"/>, pressing with <paramref name="modifiers"/> held.</summary>
    public UiFrames<TState> Drag(KeyModifiers modifiers, PointF from, params ReadOnlySpan<PointF> path)
    {
        _driver.Drag(modifiers, from, path);
        return this;
    }

    /// <summary>The last frame as a test sees it. The first call runs a frame of <see cref="Body"/> without
    /// advancing time to build it; every frame after builds one too.</summary>
    public SemanticSnapshot Snapshot() => _driver.Snapshot();

    /// <summary>The one node <paramref name="selector"/> names in the last frame; throws a
    /// <see cref="UiTargetException"/> when there is none or more than one.</summary>
    public SemanticNode Find(Selector selector) => _driver.Find(selector);

    /// <summary>Whether a press in the last frame would reach the node <paramref name="selector"/> names,
    /// through the same topmost-hit and modal rules input follows.</summary>
    public TargetResult Target(Selector selector) => _driver.Target(selector);

    /// <summary>Moves the pointer onto the node <paramref name="selector"/> names, at a point a press would
    /// reach it, and runs a frame of <see cref="Body"/>; throws a <see cref="UiTargetException"/>, moving
    /// nothing, when no such point exists, or when after that frame a press there would no longer reach it.</summary>
    public UiFrames<TState> MoveTo(Selector selector)
    {
        _driver.MoveTo(selector);
        return this;
    }

    /// <summary>Clicks the node <paramref name="selector"/> names, as <see cref="MoveTo"/> then a press and a
    /// release over two more frames; throws a <see cref="UiTargetException"/>, clicking nothing, when a press
    /// would not reach it.</summary>
    public UiFrames<TState> ClickOn(Selector selector, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        _driver.ClickOn(selector, button, modifiers);
        return this;
    }

    /// <summary>As <see cref="WaitUntil"/>, until exactly one node matches <paramref name="selector"/>, and
    /// returns it; the <see cref="TimeoutException"/> says what was found instead and holds the last snapshot.</summary>
    public SemanticNode WaitFor(Selector selector, TimeSpan timeout) => _driver.WaitFor(selector, timeout);

    /// <summary>What still moves after the last frame, which readiness does not wait for and <see cref="Settle"/> runs out.</summary>
    public FramePending Animating => _driver.Animating;

    /// <summary>The seconds apart <see cref="Settle"/> runs frames.</summary>
    public const float SettleFrameTime = FrameDriver<TState>.SettleFrameTime;

    /// <summary>Runs frames of <see cref="Body"/> <see cref="SettleFrameTime"/> apart until one ends with nothing
    /// but ambient updates pending, or <paramref name="maxSeconds"/> of virtual time have passed.</summary>
    public SettleResult Settle(double maxSeconds) => _driver.Settle(maxSeconds, SettleFrameTime);

    /// <summary>Runs a frame of <see cref="Body"/>, then more without advancing time for as long as work is posted
    /// or one asks for the next with <see cref="Ui.RequestUpdate"/>, until neither is pending and
    /// <paramref name="predicate"/> holds.
    /// Throws a <see cref="TimeoutException"/> once <paramref name="timeout"/> of real time has passed.</summary>
    public UiFrames<TState> WaitUntil(Func<bool> predicate, TimeSpan timeout)
    {
        _driver.WaitUntil(predicate, timeout);
        return this;
    }

    /// <summary>As <see cref="WaitUntil"/>, until the <see cref="BackgroundWork"/> these frames were given has
    /// finished and a frame has run after it. Frames asked for with <see cref="Ui.RequestAnimationFrame"/> are
    /// not waited for; <see cref="Animating"/> reports them and <see cref="Settle"/> runs them out.</summary>
    public UiFrames<TState> WaitUntilReady(TimeSpan timeout)
    {
        _driver.WaitUntilReady(timeout);
        return this;
    }

    private sealed class RecordingTarget(RecordingCanvas canvas, TestPopupHost? popups) : IFrameTarget
    {
        public void BeginFrame()
        {
            canvas.Reset();
            popups?.BeginFrame();
        }

        public void EndFrame(uint background) { }

        public bool ApplyFontChange(FontChange change) => false;

        public T Run<T>(Func<T> work) => work();
    }
}
