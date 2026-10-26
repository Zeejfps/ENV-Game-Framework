using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>Drives a <see cref="Ui"/> frame by frame against a recording canvas with synthetic
/// text metrics (8px per char, 16px per line) unless given others, in a window of
/// <see cref="Width"/> by <see cref="Height"/>. Input queued between frames arrives in the next one.
/// Popups draw over the window unless a <see cref="TestPopupHost"/> is given. The helpers that take
/// no body record <see cref="Body"/>.</summary>
public sealed class UiFrames<TState> where TState : class, IUiState
{
    private readonly RecordingCanvas _canvas;
    private readonly TestPopupHost? _popups;
    private readonly Ui<TState> _ui;
    private UiInput _frame = new();
    private UiInput _pending = new();

    /// <param name="state">What every frame's <see cref="Ui{T}"/> carries.</param>
    public UiFrames(TState state, ITextMeasurer? measurer = null, TestPopupHost? popups = null, float width = 400f, float height = 300f,
        Action<Ui<TState>>? body = null)
    {
        _canvas = new RecordingCanvas(measurer ?? new SyntheticTextMeasurer());
        _popups = popups;
        Width = width;
        Height = height;
        _ui = new Ui<TState>(state, _canvas, Clipboard, popups);
        Body = body;
    }

    /// <summary>Frames of <paramref name="app"/>: each one records what its <c>OnUpdate</c> does, on a
    /// <see cref="Ui{T}"/> carrying its UI state.</summary>
    public static UiFrames<TState> Running(ImmediateGuiApp<TState> app, ITextMeasurer? measurer = null,
        TestPopupHost? popups = null, float width = 400f, float height = 300f)
    {
        return new UiFrames<TState>(app.UiState, measurer, popups, width, height, app.Update);
    }

    /// <summary>What every frame's <see cref="Ui{T}"/> carries; change it between or during frames.</summary>
    public TState State => _ui.State;

    /// <summary>What <see cref="Frame(int)"/> and the helpers that take no body record.</summary>
    public Action<Ui<TState>>? Body { get; set; }

    /// <summary>Runs before every frame, as an app's loop runs the work posted to it.</summary>
    public Action? BeforeFrame { get; set; }

    /// <summary>The seconds each <see cref="Frame(int)"/> advances the clock by.</summary>
    public float FrameTime { get; set; }

    public TestClipboard Clipboard { get; } = new();
    public TestFilePicker FilePicker { get; } = new();

    /// <summary>What <see cref="Ui.ListSystemFonts"/> lists, answered once the frame that asked is drawn.</summary>
    public TestSystemFonts SystemFonts { get; } = new();

    /// <summary>The fonts the app registered with <see cref="Ui.RegisterFont"/>, in order.</summary>
    public List<FontFace> RegisteredFonts { get; } = new();

    /// <summary>The families the app unregistered with <see cref="Ui.UnregisterFont"/>, in order.</summary>
    public List<string> UnregisteredFonts { get; } = new();

    /// <summary>The folders the app asked to show in the OS file manager, in order.</summary>
    public List<string> OpenedFolders { get; } = new();

    /// <summary>The files the app asked to show selected in the OS file manager, in order.</summary>
    public List<string> RevealedFiles { get; } = new();

    /// <summary>The files the app asked to open in the OS's app for them, in order.</summary>
    public List<string> OpenedFiles { get; } = new();

    /// <summary>The web pages the app asked to open in the browser, in order.</summary>
    public List<string> OpenedUrls { get; } = new();
    public RecordingCanvas Canvas => _canvas;
    public float Width { get; set; }
    public float Height { get; set; }
    public double RealTime { get; private set; }
    public bool UpdateRequested { get; private set; }
    public double WakeAt { get; private set; }

    /// <summary>The cursor the last frame asked for.</summary>
    public MouseCursor Cursor { get; private set; }

    /// <summary>Whether a widget that takes text held focus at the end of the last frame.</summary>
    public bool WantsTextInput { get; private set; }

    /// <summary>Where the last frame put the IME caret, in UI points.</summary>
    public UiRect? ImeCaret { get; private set; }

    /// <summary>How many frames discarded the in-flight composition.</summary>
    public int CompositionResets { get; private set; }

    /// <summary>Whether a frame asked the app to close with <see cref="Ui.Quit"/>.</summary>
    public bool QuitRequested { get; private set; }

    /// <summary>The state the last frame asked the window for, or null when it asked for none.</summary>
    public WindowState? RequestedWindowState { get; private set; }

    /// <summary>Whether the last frame asked for the window to come to the front.</summary>
    public bool FrontRequested { get; private set; }

    /// <summary>The window background the last frame asked for.</summary>
    public uint WindowBackground { get; private set; }

    /// <summary>What <see cref="Ui.WindowBounds"/> reports from the next frame.</summary>
    public WindowBounds WindowBounds { get; set; }

    /// <summary>Whether the window lets frames draw in its title bar, from the next frame.</summary>
    public bool CanDrawTitleBar { get; set; }

    /// <summary>Where the window's caption buttons are over the title bar its content reaches under, from
    /// the next frame; null while it does not.</summary>
    public UiRect? CaptionButtons { get; set; }

    /// <summary>The color of the title bar the last frame recorded, or null when it recorded none.</summary>
    public uint? TitleBarBackground { get; private set; }

    /// <summary>Where the last frame's title bar drag areas were laid out.</summary>
    public List<UiRect> TitleBarDragAreas { get; } = new();

    /// <summary>The icon the window shows, from the next frame; null while it has none.</summary>
    public WindowIcon? WindowIcon { get; set; }

    private Action<Ui<TState>> BoundBody => Body ?? throw new InvalidOperationException("No body is bound.");

    public T Run<T>(float deltaTime, Func<Ui<TState>, T> body)
    {
        Begin(deltaTime);
        var value = body(_ui);
        End();
        return value;
    }

    public void Run(float deltaTime, Action<Ui<TState>> body)
    {
        Begin(deltaTime);
        body(_ui);
        End();
    }

    /// <summary>Runs <paramref name="count"/> frames of <see cref="Body"/>, each <see cref="FrameTime"/> long.</summary>
    public UiFrames<TState> Frame(int count = 1)
    {
        var body = BoundBody;
        for (var i = 0; i < count; i++)
            Run(FrameTime, body);
        return this;
    }

    /// <summary>Runs a frame of <see cref="Body"/> and then <paramref name="read"/>, and returns what it read.</summary>
    public T Frame<T>(Func<Ui<TState>, T> read)
    {
        var body = BoundBody;
        Begin(FrameTime);
        body(_ui);
        var value = read(_ui);
        End();
        return value;
    }

    /// <summary>Runs a frame of <see cref="Body"/> and then <paramref name="then"/>.</summary>
    public void Frame(Action<Ui<TState>> then)
    {
        var body = BoundBody;
        Begin(FrameTime);
        body(_ui);
        then(_ui);
        End();
    }

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
        RealTime += seconds;
        return this;
    }

    public UiFrames<TState> MovePointer(float x, float y)
    {
        _pending.MovePointer(new PointF(x, y));
        return this;
    }

    public UiFrames<TState> MovePointer(PointF at) => MovePointer(at.X, at.Y);

    /// <summary>Moves to the middle of <paramref name="rect"/>.</summary>
    public UiFrames<TState> MovePointer(UiRect rect) => MovePointer(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

    public UiFrames<TState> Press(KeyModifiers modifiers = KeyModifiers.None) => Press(PointerButton.Left, modifiers);

    public UiFrames<TState> Press(PointerButton button, KeyModifiers modifiers = KeyModifiers.None)
    {
        _pending.Button((int)button, InputAction.Press, modifiers);
        return this;
    }

    public UiFrames<TState> Release(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        _pending.Button((int)button, InputAction.Release, modifiers);
        return this;
    }

    public UiFrames<TState> Wheel(double y, double x = 0)
    {
        _pending.Scroll(x, y);
        return this;
    }

    /// <summary>Files dragged from the OS file manager are dropped where the pointer is.</summary>
    public UiFrames<TState> DropFiles(params string[] paths)
    {
        _pending.DropFiles(paths);
        return this;
    }

    public UiFrames<TState> DismissPopups()
    {
        _pending.DismissPopups();
        return this;
    }

    /// <summary>Another app takes the focus, as when the user switches to it.</summary>
    public UiFrames<TState> Background()
    {
        _pending.SetForeground(false);
        return this;
    }

    /// <summary>The app takes the focus back.</summary>
    public UiFrames<TState> Foreground()
    {
        _pending.SetForeground(true);
        return this;
    }

    /// <summary>The app lost focus: held buttons come up without the user releasing them.</summary>
    public UiFrames<TState> LoseFocus()
    {
        _pending.ReleaseAll();
        _pending.DismissPopups();
        return this;
    }

    public UiFrames<TState> Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None)
    {
        _pending.Key(key, InputAction.Press, modifiers);
        return this;
    }

    public UiFrames<TState> Type(string text)
    {
        foreach (var rune in text.EnumerateRunes())
            _pending.Character((uint)rune.Value);
        return this;
    }

    /// <summary>The IME replaces its composition with <paramref name="text"/>, caret at
    /// <paramref name="caret"/> (the end by default); an empty text ends it.</summary>
    public UiFrames<TState> Compose(string text, int? caret = null, IReadOnlyList<PreeditBlock>? blocks = null, int focusedBlock = -1)
    {
        _pending.Compose(text.Length == 0 ? PreeditText.Empty : new PreeditText(text, caret ?? text.Length, blocks ?? [], focusedBlock));
        return this;
    }

    /// <summary>Runs a frame of <paramref name="body"/> and returns where the element with the id
    /// <paramref name="id"/> gives was laid out on the frame before.</summary>
    public UiRect RectOf(Action<Ui<TState>> body, Func<Ui<TState>, int> id) => Run(0f, ui =>
    {
        body(ui);
        return ui.TryGetRect(id(ui), out var rect) ? rect : throw new InvalidOperationException("Not laid out.");
    });

    /// <summary>As <see cref="RectOf(Action{Ui{TState}}, Func{Ui{TState}, int})"/> over <see cref="Body"/>.</summary>
    public UiRect RectOf(Func<Ui<TState>, int> id) => RectOf(BoundBody, id);

    /// <summary>As <see cref="RectOf(Func{Ui{TState}, int})"/> for the element <see cref="Ui.GetId(string)"/> names.</summary>
    public UiRect RectOf(string id) => RectOf(ui => ui.GetId(id));

    /// <summary>Clicks the middle of the element <paramref name="id"/> gives, as <see cref="Click(float, float, Action{Ui{TState}}, PointerButton, KeyModifiers)"/>.</summary>
    public UiFrames<TState> ClickOn(Action<Ui<TState>> body, Func<Ui<TState>, int> id, PointerButton button = PointerButton.Left) => Click(RectOf(body, id), body, button);

    /// <summary>As <see cref="ClickOn(Action{Ui{TState}}, Func{Ui{TState}, int}, PointerButton)"/> over <see cref="Body"/>.</summary>
    public UiFrames<TState> ClickOn(Func<Ui<TState>, int> id, PointerButton button = PointerButton.Left) => ClickOn(BoundBody, id, button);

    /// <summary>Clicks the middle of the element <see cref="Ui.GetId(string)"/> names.</summary>
    public UiFrames<TState> ClickOn(string id, PointerButton button = PointerButton.Left) => ClickOn(ui => ui.GetId(id), button);

    /// <summary>Moves to (x, y), lets a frame see the pointer there, then presses and releases
    /// <paramref name="button"/> over two more frames, running <paramref name="body"/> each time.</summary>
    public UiFrames<TState> Click(float x, float y, Action<Ui<TState>> body, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        MovePointer(x, y);
        Run(1f / 60f, body);
        Press(button, modifiers);
        Run(1f / 60f, body);
        Release(button, modifiers);
        Run(1f / 60f, body);
        return this;
    }

    /// <summary>As <see cref="Click(float, float, Action{Ui{TState}}, PointerButton, KeyModifiers)"/> over <see cref="Body"/>.</summary>
    public UiFrames<TState> Click(float x, float y, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Click(x, y, BoundBody, button, modifiers);

    /// <summary>Clicks the middle of <paramref name="rect"/>, as <see cref="Click(float, float, PointerButton, KeyModifiers)"/>.</summary>
    public UiFrames<TState> Click(UiRect rect, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Click(rect, BoundBody, button, modifiers);

    /// <summary>Clicks at (x, y) as <see cref="Click(float, float, PointerButton, KeyModifiers)"/>, then
    /// presses and releases <paramref name="button"/> again over two more frames.</summary>
    public UiFrames<TState> DoubleClick(float x, float y, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None)
    {
        var body = BoundBody;
        Click(x, y, body, button, modifiers);
        Press(button, modifiers);
        Run(1f / 60f, body);
        Release(button, modifiers);
        Run(1f / 60f, body);
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
        MovePointer(from).Frame();
        Press(modifiers).Frame();
        foreach (var point in path)
            MovePointer(point).Frame();
        return this;
    }

    private void Begin(float deltaTime)
    {
        BeforeFrame?.Invoke();
        (_frame, _pending) = (_pending, _frame);
        _frame.Seal();
        _pending.BeginAfter(_frame);
        RealTime += deltaTime;
        _canvas.Reset();
        _popups?.BeginFrame();

        _ui.WindowBounds = WindowBounds;
        _ui.CanDrawTitleBar = CanDrawTitleBar;
        _ui.CaptionButtons = CaptionButtons;
        _ui.WindowIcon = WindowIcon;
        _ui.BeginFrame(_frame, deltaTime, RealTime, Width, Height);
    }

    private void End()
    {
        _ui.EndFrame();
        UpdateRequested = _ui.UpdateRequested;
        WakeAt = _ui.WakeAt;
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
            TakeFontChange(change);
    }

    private void TakeFontChange(FontChange change)
    {
        if (change.Face != null)
            RegisteredFonts.Add(change.Face);
        else
            UnregisteredFonts.Add(change.Family);
    }

    private void AnswerFontListings()
    {
        var families = SystemFonts.ListFamilies();
        foreach (var onListed in _ui.TakeFontListings())
            onListed(families);
    }

    private UiFrames<TState> Click(UiRect rect, Action<Ui<TState>> body, PointerButton button, KeyModifiers modifiers = KeyModifiers.None) =>
        Click(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, body, button, modifiers);
}
