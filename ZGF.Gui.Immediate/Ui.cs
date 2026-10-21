using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>
/// The per-frame surface handed to <see cref="Gui{T}"/>.Render. Calls record elements; once
/// Render returns the tree is laid out and drawn. Interaction is answered during the call from
/// where each element was laid out on the previous frame, matched by id. Widgets are extension
/// methods built on this surface.
/// </summary>
public sealed partial class Ui
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
    private readonly ConcurrentQueue<Action> _posted = new();
    private readonly List<Action<IFilePicker>> _dialogs = new();
    private readonly UiPainter _painter;
    private readonly LayoutTree _tree = new();
    private readonly Stack<int> _open = new();
    private readonly Stack<int> _ids = new();
    private readonly Dictionary<(float Size, uint Color, bool Bold, Align Align, string? Family), TextStyle> _textStyles = new();
    private readonly Dictionary<int, (object State, long LastFrame)> _states = new();
    private readonly Dictionary<TextStyle, float> _lineHeights = new(ReferenceEqualityComparer.Instance);
    private Dictionary<MeasureKey, float> _widths = new();
    private Dictionary<MeasureKey, float> _previousWidths = new();
    private float _measuredScale;
    private readonly List<int> _expiredStates = new();
    private List<HitRegion> _hits = new();
    private List<HitRegion> _previousHits = new();
    private Dictionary<int, UiRect> _rects = new();
    private Dictionary<int, UiRect> _previousRects = new();

    private long _frame;
    private int _hovered;
    private int _active;
    private bool _activeSeen;
    private int _focused;
    private bool _focusSeen;
    private bool _focusClaimed;
    private bool _focusTakesText;
    private readonly int[] _pressTargets = new int[UiInput.ButtonCount];
    private int _lastPressTarget;
    private double _lastPressTime = double.NegativeInfinity;
    private PointF _lastPressAt;
    private int _clickCount;

    private UiStyle _windowStyle = UiStyle.Dark;

    internal Ui(ICanvas canvas, IClipboard clipboard, IPopupHost? popupHost = null)
    {
        _canvas = canvas;
        _clipboard = clipboard;
        _popupHost = popupHost;
        _painter = new UiPainter(this, canvas);
    }

    /// <summary>The style provided around this point, or the window's style.</summary>
    public UiStyle Style => Get<UiStyle>() ?? _windowStyle;
    public UiInput Input { get; private set; } = new();
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float DeltaTime { get; private set; }
    public double Time { get; private set; }

    /// <summary>Wall-clock seconds. Unlike <see cref="Time"/> it keeps running while the UI is idle,
    /// so it can time things like a caret blink.</summary>
    public double RealTime { get; private set; }

    /// <summary>True while a widget that takes text holds keyboard focus; app shortcuts should
    /// stand down.</summary>
    public bool WantsTextInput => _focused != 0 && _focusTakesText;

    public string ClipboardText
    {
        get => _clipboard.GetText() ?? "";
        set => _clipboard.SetText(value);
    }

    /// <summary>Called, from any thread, when <see cref="Post"/> queues work.</summary>
    internal Action? Wake { get; set; }
    internal MouseCursor Cursor { get; private set; }
    internal bool FrameRequested { get; private set; }
    internal double WakeAt { get; private set; } = double.PositiveInfinity;

    /// <summary>Draws another frame after this one, for animation or state that settles over
    /// several frames.</summary>
    public void RequestFrame() => FrameRequested = true;

    /// <summary>Draws a frame once <paramref name="seconds"/> of <see cref="RealTime"/> have
    /// passed, without drawing the frames in between.</summary>
    public void RequestFrameIn(double seconds)
    {
        if (seconds <= 0)
            RequestFrame();
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
        _dialogs.Add(picker => picker.PickFile(title, initialDirectory, filters, path => Post(() => onPicked(path))));

    /// <summary>Shows the OS folder picker, as <see cref="PickFile"/> does.</summary>
    public void PickFolder(string title, string? initialDirectory, Action<string> onPicked) =>
        _dialogs.Add(picker => picker.PickFolder(title, initialDirectory, path => Post(() => onPicked(path))));

    /// <summary>Shows the OS save dialog, as <see cref="PickFile"/> does. See
    /// <see cref="IFilePicker.PickSaveFile"/>.</summary>
    public void PickSaveFile(string title, string? initialDirectory, string? suggestedFileName, IReadOnlyList<FileFilter>? filters, Action<string> onPicked) =>
        _dialogs.Add(picker => picker.PickSaveFile(title, initialDirectory, suggestedFileName, filters, path => Post(() => onPicked(path))));

    /// <summary>Runs the dialogs asked for during the last frame. The host calls it outside any
    /// frame, since a modal dialog pumps the window's events.</summary>
    internal void ShowDialogs(IFilePicker picker)
    {
        if (_dialogs.Count == 0) return;
        var dialogs = _dialogs.ToArray();
        _dialogs.Clear();
        foreach (var dialog in dialogs)
            dialog(picker);
    }

    internal bool HasDialogs => _dialogs.Count > 0;

    /// <summary>Sets the window's style for what is recorded from here on; the window's own
    /// background, padding and gap take the last one set. Each frame starts on
    /// <see cref="UiStyle.Dark"/>. <see cref="Provide{T}"/> still overrides it inside a scope.</summary>
    /// <returns><paramref name="style"/>, for <c>var style = ui.UseStyle(...)</c>.</returns>
    public UiStyle UseStyle(UiStyle style) => _windowStyle = style;

    internal void BeginFrame(UiInput input, float deltaTime, double realTime, float width, float height)
    {
        _frame++;
        while (_posted.TryDequeue(out var action))
            action();
        _windowStyle = UiStyle.Dark;
        Width = width;
        Height = height;
        BeginMeasurements();
        Input = input;
        DeltaTime = deltaTime;
        Time += deltaTime;
        RealTime = realTime;
        FrameRequested = false;
        WakeAt = double.PositiveInfinity;
        Cursor = MouseCursor.Default;
        _activeSeen = false;
        _focusSeen = false;
        _focusClaimed = false;

        (_hits, _previousHits) = (_previousHits, _hits);
        (_rects, _previousRects) = (_previousRects, _rects);
        _hits.Clear();
        _rects.Clear();
        _hovered = Topmost(_previousHits, null, out _);
        TrackPresses();

        _ids.Clear();
        _ids.Push(RootId);
        ResetContext();
        _tree.Clear();
        _open.Clear();
        BeginScrolls();

        Open(0, new Box { Width = Sizing.Fixed(Width), Height = Sizing.Fixed(Height) });
    }

    internal void EndFrame()
    {
        Close();
        if (_open.Count != 0)
            throw new InvalidOperationException("A layout scope was not disposed.");
        if (_ids.Count != 1)
            throw new InvalidOperationException("An Id scope was not disposed.");
        if (_provided.Count != 0)
            throw new InvalidOperationException("A Provide scope was not disposed.");

        ref var window = ref _tree[0];
        window.Padding = _windowStyle.WindowPadding;
        window.Gap = _windowStyle.Gap;
        window.Background = _windowStyle.Background;

        _tree.Solve();
        switch (SettleScrolls())
        {
            case ScrollSettle.Relayout:
                _tree.Solve();
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
        _tree.Emit(_canvas, _painter, Width, Height, _hits, _rects);

        if (_active != 0 && !_activeSeen)
            _active = 0;
        if (_focused != 0 && (!_focusSeen || (Input.WasPressed(PointerButton.Left) && !_focusClaimed)))
            _focused = 0;
        for (var button = 1; button < UiInput.ButtonCount; button++)
        {
            if (!Input.IsDown((PointerButton)button))
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
        return new Scope(this, ScopeKind.Id);
    }

    public Scope Id(int id)
    {
        _ids.Push(GetId(id));
        return new Scope(this, ScopeKind.Id);
    }

    /// <summary>Where the element with <paramref name="id"/> was laid out on the previous frame.</summary>
    public bool TryGetRect(int id, out UiRect rect) => _previousRects.TryGetValue(id, out rect);

    /// <summary>
    /// Widget-owned state that outlives a frame (a caret, an open dropdown), created on first use
    /// and kept while <paramref name="id"/> keeps being asked for. App state belongs in the
    /// <see cref="Gui{T}"/> state instead. Dropped after about a thousand frames unused.
    /// </summary>
    public TState GetState<TState>(int id) where TState : class, new()
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_states, id, out _);
        entry.LastFrame = _frame;
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
        var hovered = IsHovered(id);
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
                clicked = _hovered == id && Input.WasReleased(PointerButton.Left);
                _active = 0;
            }
        }

        var otherClicks = 0;
        if (hovered)
        {
            for (var button = 1; button < UiInput.ButtonCount; button++)
            {
                if (_pressTargets[button] == id && Input.WasReleased((PointerButton)button))
                    otherClicks |= 1 << button;
            }
        }

        return new Interaction(id, hovered, pressed, held, clicked, pressed ? _clickCount : 0, otherClicks);
    }

    private void TrackPresses()
    {
        for (var button = 1; button < UiInput.ButtonCount; button++)
        {
            if (Input.WasPressed((PointerButton)button))
                _pressTargets[button] = _hovered;
        }

        if (!Input.WasPressed(PointerButton.Left)) return;
        var pointer = Input.Pointer;
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
    }

    public void ClearFocus() => _focused = 0;

    /// <summary>Width of <paramref name="text"/> as <see cref="Text"/> draws it. A null
    /// <paramref name="family"/> means the style's <see cref="UiStyle.FontFamily"/>.</summary>
    public float MeasureText(string text, float? size = null, bool bold = false, string? family = null) =>
        MeasureWidth(text, TextStyleFor(size, Style.Text, bold, Align.Start, family));

    /// <summary>Width of the first <paramref name="length"/> characters, shaped in context of the
    /// whole line — for placing a caret.</summary>
    public float MeasureText(string text, int length, float? size = null, bool bold = false, string? family = null) =>
        length <= 0 ? 0f : _canvas.MeasureTextPrefix(text, length, TextStyleFor(size, Style.Text, bold, Align.Start, family));

    public float LineHeight(float? size = null, string? family = null) =>
        MeasureLineHeight(TextStyleFor(size, Style.Text, false, Align.Start, family));

    internal float MeasureWidth(string text, TextStyle style)
    {
        var key = new MeasureKey(text, style);
        if (_widths.TryGetValue(key, out var width))
            return width;
        if (!_previousWidths.TryGetValue(key, out width))
            width = MathF.Ceiling(_canvas.MeasureTextWidth(text, style));
        _widths[key] = width;
        return width;
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
        if (_canvas.DpiScale != _measuredScale)
        {
            _measuredScale = _canvas.DpiScale;
            _widths.Clear();
            _previousWidths.Clear();
            _lineHeights.Clear();
        }
        (_widths, _previousWidths) = (_previousWidths, _widths);
        _widths.Clear();
    }

    private readonly struct MeasureKey : IEquatable<MeasureKey>
    {
        private readonly string _text;
        private readonly TextStyle _style;

        public MeasureKey(string text, TextStyle style)
        {
            _text = text;
            _style = style;
        }

        public bool Equals(MeasureKey other) => ReferenceEquals(_style, other._style) && string.Equals(_text, other._text);

        public override bool Equals(object? obj) => obj is MeasureKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_text, RuntimeHelpers.GetHashCode(_style));
    }

    internal TextStyle TextStyleFor(float? size, uint color, bool bold, Align align, string? family)
    {
        var key = (size ?? Style.FontSize, color, bold, align, family ?? Style.FontFamily);
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
        var pointer = Input.Pointer;
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
            var x = popup.Align switch
            {
                Align.Center => anchor.X + (anchor.Width - e.W) / 2,
                Align.End => anchor.Right - e.W,
                _ => anchor.X,
            };
            var below = new UiRect(x, anchor.Bottom + popup.Gap, e.W, e.H);
            var above = below with { Y = anchor.Y - popup.Gap - e.H };
            var rect = _popupHost?.Place(below, above) ?? PopupPlacement.Fit(below, above, new UiRect(0f, 0f, Width, Height));
            e.OffsetX = rect.X;
            e.OffsetY = rect.Y;
            _tree.SetLayer(e.Layer, _popupHost?.Show(popup.Id, rect, e.PassThrough), rect);
        }
    }

    private void PruneStates()
    {
        _expiredStates.Clear();
        foreach (var (id, entry) in _states)
        {
            if (_frame - entry.LastFrame > StateLifetimeFrames)
                _expiredStates.Add(id);
        }
        foreach (var id in _expiredStates)
            _states.Remove(id);
    }
}
