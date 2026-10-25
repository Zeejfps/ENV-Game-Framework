using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

/// <summary>
/// The per-frame surface handed to <see cref="ImmediateGuiApp{TUiState}"/>.OnUpdate. Calls record elements; once
/// OnUpdate returns the tree is laid out and drawn. Interaction is answered during the call from
/// where each element was laid out on the previous frame, matched by id. Widgets are extension
/// methods built on this surface.
/// </summary>
public abstract partial class Ui
{
    private const int RootId = 17;
    private const long StateLifetimeFrames = 1000;
    private const long StatePruneInterval = 256;
    private const double MultiClickSeconds = 0.4;
    private const int PopupZ = 1_000_000;
    private const float MultiClickDistance = 4f;

    private readonly ICanvas _canvas;
    private readonly IClipboard _clipboard;
    private readonly IPopupHost? _popupHost;
    private readonly ConcurrentQueue<Action> _posted;
    private readonly List<Action<IFilePicker>> _dialogs = new();
    private readonly List<string> _foldersToOpen = new();
    private readonly List<string> _filesToReveal = new();
    private readonly List<string> _filesToOpen = new();
    private readonly List<string> _urlsToOpen = new();
    private readonly List<FontFace> _fontsToRegister = new();
    private readonly List<Action<IReadOnlyList<SystemFontFamily>>> _fontListings = new();
    private readonly UiPainter _painter;
    private readonly LayoutTree _tree = new();
    private readonly Stack<int> _open = new();
    private readonly Stack<int> _ids = new();
    private readonly Dictionary<(float Size, uint Color, bool Bold, Align Align, string? Family), TextStyle> _textStyles = new();
    private readonly Dictionary<int, (object State, long LastFrame, int Owner)> _states = new();
    private readonly Dictionary<int, long> _keptStates = new();
    private readonly Stack<int> _stateOwners = new();
    private readonly Dictionary<TextStyle, float> _lineHeights = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<TextStyle, CellMetrics> _cells = new(ReferenceEqualityComparer.Instance);
    private readonly FrameCache<StyledText, float> _widths = new();
    private float _measuredScale;
    private readonly List<int> _expiredStates = new();
    private List<HitRegion> _hits = new();
    private List<HitRegion> _previousHits = new();
    private Dictionary<int, UiRect> _rects = new();
    private Dictionary<int, UiRect> _previousRects = new();
    private Dictionary<int, UiRect> _contentRects = new();
    private Dictionary<int, UiRect> _previousContentRects = new();

    private long _frame;
    private int _hovered;
    private int _active;
    private bool _activeSeen;
    private int _focused;
    private bool _focusSeen;
    private bool _focusClaimed;
    private bool _focusTakesText;
    private bool _focusVisible;
    private readonly List<(int Id, bool TakesText)> _tabStops = new();
    private int _deferredTab;
    private long _deferredTabFrame;
    private readonly int[] _pressTargets = new int[UiInput.ButtonCount];
    private int _lastPressTarget;
    private double _lastPressTime = double.NegativeInfinity;
    private PointF _lastPressAt;
    private int _clickCount;
    private bool _showingDialogs;

    private protected Ui(IUiState state, ICanvas canvas, IClipboard clipboard, IPopupHost? popupHost, ConcurrentQueue<Action>? posted)
    {
        _state = state;
        _posted = posted ?? new ConcurrentQueue<Action>();
        _canvas = canvas;
        _clipboard = clipboard;
        _popupHost = popupHost;
        _painter = new UiPainter(this, canvas);
    }

    private UiInput _input = new();

    /// <summary>This frame's input as the code being recorded sees it: empty of keys outside the
    /// topmost <see cref="Layer"/>, and of everything outside the topmost <see cref="Modal"/>.</summary>
    public UiInput Input => _block switch
    {
        InputBlock.None => _input,
        InputBlock.Keys => _keyless,
        _ => _inert,
    };
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float DeltaTime { get; private set; }
    public double Time { get; private set; }

    /// <summary>Wall-clock seconds. Unlike <see cref="Time"/> it keeps running while the UI is idle,
    /// so it can time things like a caret blink.</summary>
    public double RealTime { get; private set; }

    /// <summary>The main window's bounds as of this frame; see <see cref="ImmediateGuiAppHost{TUiState}.WindowBounds"/>.</summary>
    public WindowBounds WindowBounds { get; internal set; }

    /// <summary>The color the window is cleared to under everything drawn, and shown while it
    /// resizes. Kept until set again; <see cref="UiTheme.Dark"/>'s background until then. The native
    /// title bar follows it: dark on a dark background where the platform allows.</summary>
    public uint WindowBackground { get; set; } = UiTheme.Dark.Background;

    /// <summary>How many times larger than the monitor's own scale everything is drawn, e.g. 1.25
    /// for 125%. Kept until set again; 1 until then. A change shows from the next frame, the window
    /// keeping its size on the screen.</summary>
    public float Scale { get; set; } = 1f;

    /// <summary>True while a widget that takes text holds keyboard focus; app shortcuts should
    /// stand down.</summary>
    public bool WantsTextInput => _focused != 0 && _focusTakesText;

    /// <summary>True while focus was last moved by the keyboard — Tab, or a shortcut — rather than
    /// a press, so the widget holding it should draw <see cref="UiTheme.FocusRing"/>.</summary>
    public bool FocusVisible => _focused != 0 && _focusVisible;

    public string ClipboardText
    {
        get => _clipboard.GetText() ?? "";
        set => _clipboard.SetText(value);
    }

    /// <summary>Called, from any thread, when <see cref="Post"/> queues work.</summary>
    internal Action? Wake { get; set; }
    internal MouseCursor Cursor { get; private set; }
    internal bool UpdateRequested { get; private set; }
    internal double WakeAt { get; private set; } = double.PositiveInfinity;

    /// <summary>Draws another frame after this one, for animation or state that settles over
    /// several frames.</summary>
    public void RequestUpdate() => UpdateRequested = true;

    /// <summary>Draws a frame once <paramref name="seconds"/> of <see cref="RealTime"/> have
    /// passed, without drawing the frames in between.</summary>
    public void RequestUpdateIn(double seconds)
    {
        if (seconds <= 0)
            RequestUpdate();
        else
            WakeAt = Math.Min(WakeAt, RealTime + seconds);
    }

    public void SetCursor(MouseCursor cursor) => Cursor = cursor;

    /// <summary>Runs <paramref name="action"/> on the UI thread at the start of the next frame, and
    /// makes sure there is one. Safe from any thread — the way background work hands back results.</summary>
    public void Post(Action action)
    {
        _posted.Enqueue(action);
        Wake?.Invoke();
    }

    /// <summary>Shows the OS file picker once this frame is drawn. <paramref name="onPicked"/> runs
    /// at the start of a later frame with the chosen path, and not at all when the user cancels.
    /// See <see cref="IFilePicker.PickFile"/> for <paramref name="filters"/>.</summary>
    public void PickFile(string title, string? initialDirectory, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        _dialogs.Add(picker => picker.PickFile(title, initialDirectory, filters, InFrame(onPicked)));

    /// <summary>Shows the OS folder picker, as <see cref="PickFile"/> does.</summary>
    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) =>
        _dialogs.Add(picker => picker.PickFolder(title, initialDirectory, InFrame(onPicked)));

    /// <summary>Shows the OS save dialog, as <see cref="PickFile"/> does. See
    /// <see cref="IFilePicker.PickSaveFile"/>.</summary>
    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        _dialogs.Add(picker => picker.PickSaveFile(title, initialDirectory, suggestedFileName, filters, InFrame(onPicked)));

    private Action<string> InFrame(Action<string> onPicked) => path =>
    {
        if (_showingDialogs)
            Post(() => onPicked(path));
        else
            onPicked(path);
    };

    /// <summary>Hides every popup, then runs the dialogs asked for during the last frame. The host
    /// calls it outside any frame, since a modal dialog pumps the window's events. Returns whether
    /// any ran, after which open popups should be dismissed.</summary>
    internal bool ShowDialogs(IFilePicker picker)
    {
        if (_dialogs.Count == 0) return false;
        _popupHost?.HideAll();
        var dialogs = _dialogs.ToArray();
        _dialogs.Clear();
        _showingDialogs = true;
        try
        {
            foreach (var dialog in dialogs)
                dialog(picker);
        }
        finally
        {
            _showingDialogs = false;
        }
        return true;
    }

    /// <summary>Shows the folder at <paramref name="path"/> in the OS file manager once this frame is
    /// drawn.</summary>
    public void OpenFolder(string path) => _foldersToOpen.Add(path);

    internal bool HasFoldersToOpen => _foldersToOpen.Count > 0;

    /// <summary>The folders asked for since the last call, which the host shows.</summary>
    internal string[] TakeFoldersToOpen()
    {
        var folders = _foldersToOpen.ToArray();
        _foldersToOpen.Clear();
        return folders;
    }

    /// <summary>Shows the file at <paramref name="path"/> selected in its folder in the OS file manager
    /// once this frame is drawn.</summary>
    public void RevealFile(string path) => _filesToReveal.Add(path);

    internal bool HasFilesToReveal => _filesToReveal.Count > 0;

    /// <summary>The files asked for since the last call, which the host reveals.</summary>
    internal string[] TakeFilesToReveal()
    {
        var files = _filesToReveal.ToArray();
        _filesToReveal.Clear();
        return files;
    }

    /// <summary>Opens the file at <paramref name="path"/> in the app the OS opens its kind of file with,
    /// once this frame is drawn.</summary>
    public void OpenFile(string path) => _filesToOpen.Add(path);

    internal bool HasFilesToOpen => _filesToOpen.Count > 0;

    /// <summary>The files asked to be opened since the last call, which the host opens.</summary>
    internal string[] TakeFilesToOpen()
    {
        var files = _filesToOpen.ToArray();
        _filesToOpen.Clear();
        return files;
    }

    /// <summary>Opens <paramref name="url"/> in the default web browser once this frame is drawn.</summary>
    public void OpenUrl(string url) => _urlsToOpen.Add(url);

    internal bool HasUrlsToOpen => _urlsToOpen.Count > 0;

    /// <summary>The web pages asked for since the last call, which the host opens.</summary>
    internal string[] TakeUrlsToOpen()
    {
        var urls = _urlsToOpen.ToArray();
        _urlsToOpen.Clear();
        return urls;
    }

    /// <summary>Draws text in <paramref name="font"/>'s family with it from the next frame, in place of
    /// whatever drew that family before.</summary>
    public void RegisterFont(FontFace font) => _fontsToRegister.Add(font);

    internal bool HasFontsToRegister => _fontsToRegister.Count > 0;

    /// <summary>The fonts asked for since the last call, which the host registers.</summary>
    internal FontFace[] TakeFontsToRegister()
    {
        var fonts = _fontsToRegister.ToArray();
        _fontsToRegister.Clear();
        return fonts;
    }

    /// <summary>Lists the installed fonts off the UI thread once this frame is drawn;
    /// <paramref name="onListed"/> runs at the start of a later frame with them. See
    /// <see cref="ISystemFonts.ListFamilies"/>.</summary>
    public void ListSystemFonts(Action<IReadOnlyList<SystemFontFamily>> onListed) => _fontListings.Add(onListed);

    internal bool HasFontListings => _fontListings.Count > 0;

    /// <summary>The listings asked for since the last call, which the host answers.</summary>
    internal Action<IReadOnlyList<SystemFontFamily>>[] TakeFontListings()
    {
        var listings = _fontListings.ToArray();
        _fontListings.Clear();
        return listings;
    }

    /// <summary>Forgets every measured width, line height and cell: fonts changed.</summary>
    internal void InvalidateMeasurements() => _measuredScale = 0f;

    internal bool HasDialogs => _dialogs.Count > 0;

    /// <summary>Closes the app once this frame is drawn: <c>Run</c> returns.</summary>
    public void Quit() => QuitRequested = true;

    internal bool QuitRequested { get; private set; }

    internal void BeginFrame(UiInput input, float deltaTime, double realTime, float width, float height)
    {
        _frame++;
        while (_posted.TryDequeue(out var action))
            action();
        Width = width;
        Height = height;
        BeginMeasurements();
        _input = input;
        DeltaTime = deltaTime;
        Time += deltaTime;
        RealTime = realTime;
        UpdateRequested = false;
        WakeAt = double.PositiveInfinity;
        Cursor = MouseCursor.Default;
        _activeSeen = false;
        _focusSeen = false;
        _focusClaimed = false;
        _tabStops.Clear();
        BeginIme();
        if (input.WasPressed(PointerButton.Left) || input.WasPressed(PointerButton.Right) || input.WasPressed(PointerButton.Middle))
            _focusVisible = false;

        (_hits, _previousHits) = (_previousHits, _hits);
        (_rects, _previousRects) = (_previousRects, _rects);
        (_contentRects, _previousContentRects) = (_previousContentRects, _contentRects);
        _hits.Clear();
        _rects.Clear();
        _contentRects.Clear();
        _hovered = Topmost(_previousHits, null, out _);
        TrackPresses();

        _ids.Clear();
        _ids.Push(RootId);
        _stateOwners.Clear();
        AdvanceDrag();
        BeginLayers(input);
        _scopes.Clear();
        _tree.Clear();
        _open.Clear();
        _surfaces = 0;
        BeginScrolls();

        Open(0, new Box { Width = Sizing.Fixed(Width), Height = Sizing.Fixed(Height) });
    }

    internal void EndFrame()
    {
        Close();
        EnsureScopesClosed();

        _tree.Solve(this);
        switch (SettleScrolls())
        {
            case ScrollSettle.Relayout:
                _tree.Solve(this);
                if (SettleScrolls() != ScrollSettle.None)
                    _tree.Position();
                break;
            case ScrollSettle.Reposition:
                _tree.Position();
                break;
        }
        if (_popups.Count > 0)
        {
            PlacePopups();
            _tree.Position();
        }
        _tree.Emit(_canvas, _painter, Width, Height, _hits, _rects, _contentRects);

        if (_active != 0 && !_activeSeen)
            _active = 0;
        if (_focused != 0 && (!_focusSeen || (_input.WasPressed(PointerButton.Left) && !_focusClaimed)))
            _focused = 0;
        TraverseFocus();
        EndLayers();
        EndIme();
        EndIconImages();
        for (var button = 1; button < UiInput.ButtonCount; button++)
        {
            if (!_input.IsDown((PointerButton)button))
                _pressTargets[button] = 0;
        }
        if (_frame % StatePruneInterval == 0)
            PruneStates();
    }

    public int GetId(string label) => HashCode.Combine(_ids.Peek(), label);

    public int GetId(int value) => HashCode.Combine(_ids.Peek(), value);

    /// <summary>Scopes widget ids, so repeated labels (a "Delete" per list row) stay distinct.</summary>
    public Scope Id(string id)
    {
        _ids.Push(GetId(id));
        return BeginScope(ScopeKind.Id);
    }

    public Scope Id(int id)
    {
        _ids.Push(GetId(id));
        return BeginScope(ScopeKind.Id);
    }

    /// <summary>
    /// An <see cref="Id(string)"/> scope whose widget state outlives the frames in which its content
    /// is not recorded, for as long as <see cref="KeepState"/> is called with the same id instead —
    /// the content of a hidden tab keeps its scroll offsets, carets and expanded rows.
    /// </summary>
    public Scope StateScope(string id)
    {
        var scope = GetId(id);
        _ids.Push(scope);
        _stateOwners.Push(scope);
        _keptStates[scope] = _frame;
        return BeginScope(ScopeKind.StateScope);
    }

    /// <summary>Keeps the state of the <see cref="StateScope"/> with <paramref name="id"/> alive
    /// through a frame in which its content is not recorded.</summary>
    public void KeepState(string id) => _keptStates[GetId(id)] = _frame;

    /// <summary>Where the element with <paramref name="id"/> was laid out on the previous frame.</summary>
    public bool TryGetRect(int id, out UiRect rect) => _previousRects.TryGetValue(id, out rect);

    /// <summary>
    /// Widget-owned state that outlives a frame (a caret, an open dropdown), created on first use
    /// and kept while <paramref name="id"/> keeps being asked for. App state belongs on the
    /// app instead. Dropped after about a thousand frames unused, unless it
    /// belongs to a <see cref="StateScope"/> that is being kept.
    /// </summary>
    public TState GetState<TState>(int id) where TState : class, new()
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_states, id, out _);
        entry.LastFrame = _frame;
        entry.Owner = _stateOwners.Count > 0 ? _stateOwners.Peek() : 0;
        if (entry.State is TState existing)
            return existing;

        var created = new TState();
        entry.State = created;
        return created;
    }

    public Interaction Interact(string label) => Interact(GetId(label));

    /// <summary>Whether the pointer is over the element with <paramref name="id"/>, as
    /// <see cref="Interaction.Hovered"/> would say, without interacting with it.</summary>
    public bool IsHovered(int id) => _hovered == id && (_active == 0 || _active == id);

    /// <summary>The click behavior every widget is built on: hover, press, hold-while-dragging and a
    /// click on release over the same element. Record an element with the returned
    /// <see cref="Interaction.Id"/> so it can be hit-tested next frame.</summary>
    public Interaction Interact(int id)
    {
        return Interact(id, IsHovered(id));
    }

    /// <summary>
    /// <see cref="Interact(int)"/> for the background of the element with <paramref name="area"/>:
    /// hovered while the pointer is over the element and over nothing in it that takes the pointer
    /// itself — empty space, text, or a scroll's own background — and over no popup. Record it
    /// before what is inside the element; for a window dragged from anywhere but its controls.
    /// </summary>
    public Interaction InteractBackground(int id, int area)
    {
        var free = _active == 0 || _active == id;
        return Interact(id, free && IsBackgroundUnderPointer(area));
    }

    private bool IsBackgroundUnderPointer(int area)
    {
        if (!_previousRects.TryGetValue(area, out var rect))
            return false;
        if (!rect.Contains(_input.Pointer))
            return false;
        if (_hovered == area)
            return true;
        if (!_previousScrollIds.Contains(_hovered))
            return false;
        Topmost(_previousHits, null, out var z);
        return z < PopupZ;
    }

    private Interaction Interact(int id, bool hovered)
    {
        var pressed = false;
        if (hovered && Input.WasPressed(PointerButton.Left))
        {
            _active = id;
            pressed = true;
        }

        var held = _active == id;
        var clicked = false;
        if (held)
        {
            _activeSeen = true;
            if (Input.WasReleased(PointerButton.Left) || !Input.IsDown(PointerButton.Left))
            {
                clicked = _hovered == id && Input.WasReleased(PointerButton.Left) && !Input.WasCancelled(PointerButton.Left);
                _active = 0;
            }
        }

        var otherClicks = 0;
        if (hovered)
        {
            for (var button = 1; button < UiInput.ButtonCount; button++)
            {
                if (_pressTargets[button] == id && Input.WasReleased((PointerButton)button) && !Input.WasCancelled((PointerButton)button))
                    otherClicks |= 1 << button;
            }
        }

        return new Interaction(id, hovered, pressed, held, clicked, pressed ? _clickCount : 0, otherClicks);
    }

    private void TrackPresses()
    {
        for (var button = 1; button < UiInput.ButtonCount; button++)
        {
            if (_input.WasPressed((PointerButton)button))
                _pressTargets[button] = _hovered;
        }

        if (!_input.WasPressed(PointerButton.Left)) return;
        var pointer = _input.Pointer;
        var repeated = _hovered == _lastPressTarget
            && RealTime - _lastPressTime <= MultiClickSeconds
            && MathF.Abs(pointer.X - _lastPressAt.X) <= MultiClickDistance
            && MathF.Abs(pointer.Y - _lastPressAt.Y) <= MultiClickDistance;
        _clickCount = repeated ? _clickCount + 1 : 1;
        _lastPressTarget = _hovered;
        _lastPressTime = RealTime;
        _lastPressAt = pointer;
    }

    /// <summary>Whether <paramref name="id"/> holds keyboard focus. Asking keeps it: focus is
    /// dropped at the end of a frame in which its owner never asked.</summary>
    public bool HasFocus(int id)
    {
        if (_focused != id) return false;
        _focusSeen = true;
        return true;
    }

    /// <summary>Gives <paramref name="id"/> keyboard focus. A widget that only navigates with keys
    /// (a list, a tree) passes <paramref name="takesText"/> false, so app shortcuts keep working
    /// and the window is not asked for text input.</summary>
    public void Focus(int id, bool takesText = true)
    {
        _focused = id;
        _focusTakesText = takesText;
        _focusClaimed = true;
        _focusSeen = true;
        _focusVisible = !_input.WasPressed(PointerButton.Left) && !_input.WasPressed(PointerButton.Right) && !_input.WasPressed(PointerButton.Middle);
    }

    public void ClearFocus() => _focused = 0;

    /// <summary>Puts <paramref name="id"/> in this frame's Tab order, the order stops are recorded
    /// in, and answers <see cref="HasFocus"/>. Tab and Shift+Tab left over once the frame is recorded
    /// move focus to the next or previous stop of the topmost <see cref="Layer"/>; a widget that
    /// wants Tab for itself takes the key first. <paramref name="takesText"/> is as for
    /// <see cref="Focus"/>.</summary>
    public bool TabStop(int id, bool takesText = false)
    {
        if (_block == InputBlock.None)
            _tabStops.Add((id, takesText));
        return HasFocus(id);
    }

    /// <summary>Moves focus to the next Tab stop, or the previous one when <paramref name="direction"/>
    /// is negative, as Tab would — but only once the layers that stop being recorded have given
    /// focus back, so a menu closing on Tab hands it on from where it was opened.</summary>
    public void MoveFocus(int direction)
    {
        _deferredTab = Math.Sign(direction);
        _deferredTabFrame = _frame;
        RequestUpdate();
    }

    private void TraverseFocus()
    {
        var direction = 0;
        foreach (var stroke in _input.Keys)
        {
            if (stroke.Key != KeyboardKey.Tab || (stroke.Modifiers & ~KeyModifiers.Shift) != 0) continue;
            direction = (stroke.Modifiers & KeyModifiers.Shift) != 0 ? -1 : 1;
        }
        if (direction != 0 && _tabStops.Count > 0)
        {
            _input.TakeKey(KeyboardKey.Tab);
            _deferredTab = 0;
            StepFocus(direction);
            return;
        }
        if (_deferredTab == 0) return;
        if (_tabStops.Count > 0 && _frame > _deferredTabFrame)
        {
            StepFocus(_deferredTab);
            _deferredTab = 0;
        }
        else if (_frame == _deferredTabFrame || LayersChanged())
        {
            RequestUpdate();
        }
        else
        {
            _deferredTab = 0;
        }
    }

    private void StepFocus(int direction)
    {
        var at = -1;
        for (var i = 0; i < _tabStops.Count; i++)
        {
            if (_tabStops[i].Id != _focused) continue;
            at = i;
            break;
        }
        var count = _tabStops.Count;
        var next = at < 0 ? (direction > 0 ? 0 : count - 1) : (at + direction + count) % count;
        (_focused, _focusTakesText) = _tabStops[next];
        _focusVisible = true;
        RequestUpdate();
    }

    /// <summary>Width of <paramref name="text"/> as <see cref="Text"/> draws it. A null
    /// <paramref name="family"/> means the theme's <see cref="UiTheme.FontFamily"/>.</summary>
    public float MeasureText(string text, float? size = null, bool bold = false, string? family = null) =>
        MeasureWidth(text, TextStyleFor(size, Theme.Text, bold, Align.Start, family));

    /// <summary>Width of the first <paramref name="length"/> characters, shaped in context of the
    /// whole line — for placing a caret.</summary>
    public float MeasureText(string text, int length, float? size = null, bool bold = false, string? family = null) =>
        length <= 0 ? 0f : _canvas.MeasureTextPrefix(text, length, TextStyleFor(size, Theme.Text, bold, Align.Start, family));

    public float LineHeight(float? size = null, string? family = null) =>
        MeasureLineHeight(TextStyleFor(size, Theme.Text, false, Align.Start, family));

    /// <summary>The cell of a monospaced grid in this font: the pitch
    /// <see cref="UiPainter.GlyphRun"/> places columns at, and the line height.</summary>
    public CellMetrics MeasureCell(float? size = null, bool bold = false, string? family = null)
    {
        var style = TextStyleFor(size, Theme.Text, bold, Align.Start, family);
        ref var cell = ref CollectionsMarshal.GetValueRefOrAddDefault(_cells, style, out var exists);
        if (!exists)
            cell = _canvas.MeasureCellSize(style);
        return cell;
    }

    internal float MeasureWidth(string text, TextStyle style)
    {
        var key = new StyledText(text, style);
        if (_widths.TryGet(key, out var width))
            return width;
        return _widths.Keep(key, MathF.Ceiling(_canvas.MeasureTextWidth(text, style)));
    }

    internal float MeasureLineHeight(TextStyle style)
    {
        ref var height = ref CollectionsMarshal.GetValueRefOrAddDefault(_lineHeights, style, out var exists);
        if (!exists)
            height = MathF.Ceiling(_canvas.MeasureTextLineHeight(style));
        return height;
    }

    // Widths are kept while their text was measured this frame or the last, so an unchanged
    // screen is never re-shaped and text that stops being drawn ages out a frame later. A scale
    // change re-rasterizes every font, so it invalidates everything.
    private void BeginMeasurements()
    {
        var invalidate = _canvas.DpiScale != _measuredScale;
        if (invalidate)
        {
            _measuredScale = _canvas.DpiScale;
            _lineHeights.Clear();
            _cells.Clear();
        }
        BeginLines(invalidate);
        _widths.BeginFrame(invalidate);
    }

    internal TextStyle TextStyleFor(float? size, uint color, bool bold, Align align, string? family)
    {
        var key = (size ?? Theme.FontSize, color, bold, align, family ?? Theme.FontFamily);
        if (_textStyles.TryGetValue(key, out var style))
            return style;

        style = new TextStyle
        {
            TextColor = color,
            FontFamily = new StyleValue<string>(key.Item5, key.Item5 != null),
            FontSize = key.Item1,
            FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
            HorizontalAlignment = align switch
            {
                Align.Center => TextAlignment.Center,
                Align.End => TextAlignment.End,
                _ => TextAlignment.Start,
            },
            VerticalAlignment = TextAlignment.Center,
        };
        _textStyles[key] = style;
        return style;
    }

    private int Topmost(List<HitRegion> hits, HashSet<int>? among, out int z)
    {
        var pointer = _input.Pointer;
        var best = 0;
        var bestZ = int.MinValue;
        var bestOrder = int.MinValue;
        foreach (var hit in hits)
        {
            if (among != null && !among.Contains(hit.Id)) continue;
            if (!hit.Rect.Contains(pointer)) continue;
            if (hit.Z < bestZ || (hit.Z == bestZ && hit.Order < bestOrder)) continue;
            best = hit.Id;
            bestZ = hit.Z;
            bestOrder = hit.Order;
        }
        z = bestZ;
        return best;
    }

    private void PlacePopups()
    {
        foreach (var popup in _popups)
        {
            ref var e = ref _tree[popup.Element];
            e.W = MathF.Ceiling(e.W);
            e.H = MathF.Ceiling(e.H);
            var anchor = popup.Anchor;
            UiRect preferred, fallback;
            if (popup.Side == PopupSide.End)
            {
                var y = popup.Align switch
                {
                    Align.Center => anchor.Y + (anchor.Height - e.H) / 2,
                    Align.End => anchor.Bottom - e.H,
                    _ => anchor.Y,
                };
                preferred = new UiRect(anchor.Right + popup.Gap, y, e.W, e.H);
                fallback = preferred with { X = anchor.X - popup.Gap - e.W };
            }
            else
            {
                var x = popup.Align switch
                {
                    Align.Center => anchor.X + (anchor.Width - e.W) / 2,
                    Align.End => anchor.Right - e.W,
                    _ => anchor.X,
                };
                preferred = new UiRect(x, anchor.Bottom + popup.Gap, e.W, e.H);
                fallback = preferred with { Y = anchor.Y - popup.Gap - e.H };
            }
            var rect = _popupHost?.Place(preferred, fallback) ?? PopupPlacement.Fit(preferred, fallback, new UiRect(0f, 0f, Width, Height));
            e.OffsetX = rect.X;
            e.OffsetY = rect.Y;
            var reach = e.Shadow.Extent;
            var surface = new UiRect(rect.X - reach.Left, rect.Y - reach.Top, rect.Width + reach.Left + reach.Right, rect.Height + reach.Top + reach.Bottom);
            _tree.SetLayer(e.Layer, _popupHost?.Show(popup.Id, rect, surface, e.PassThrough), surface);
        }
    }

    private void PruneStates()
    {
        _expiredStates.Clear();
        foreach (var (owner, lastFrame) in _keptStates)
        {
            if (_frame - lastFrame > StateLifetimeFrames)
                _expiredStates.Add(owner);
        }
        foreach (var owner in _expiredStates)
            _keptStates.Remove(owner);

        _expiredStates.Clear();
        foreach (var (id, entry) in _states)
        {
            if (_frame - entry.LastFrame > StateLifetimeFrames && (entry.Owner == 0 || !_keptStates.ContainsKey(entry.Owner)))
                _expiredStates.Add(id);
        }
        foreach (var id in _expiredStates)
            _states.Remove(id);
    }
}
