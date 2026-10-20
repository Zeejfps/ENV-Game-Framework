using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>
/// The per-frame surface handed to <see cref="Gui{T}"/>.Render. Calls record elements; once
/// Render returns the tree is laid out and drawn. Interaction is answered during the call from
/// where each element was laid out on the previous frame, matched by id. Widgets are extension
/// methods built on this surface (see <see cref="UiWidgets"/>).
/// </summary>
public sealed partial class Ui
{
    private const int RootId = 17;
    private const long StateLifetimeFrames = 1000;
    private const long StatePruneInterval = 256;

    private readonly RenderedCanvasBase _canvas;
    private readonly IWindow _window;
    private readonly UiPainter _painter;
    private readonly LayoutTree _tree = new();
    private readonly Stack<int> _open = new();
    private readonly Stack<int> _ids = new();
    private readonly Dictionary<(float Size, uint Color, bool Bold, Align Align), TextStyle> _textStyles = new();
    private readonly Dictionary<int, (object State, long LastFrame)> _states = new();
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

    internal Ui(RenderedCanvasBase canvas, UiTheme theme, IWindow window)
    {
        _canvas = canvas;
        _window = window;
        _painter = new UiPainter(this, canvas);
        Theme = theme;
    }

    public UiTheme Theme { get; }
    public UiInput Input { get; private set; } = new();
    public float Width => _canvas.Width;
    public float Height => _canvas.Height;
    public float DeltaTime { get; private set; }
    public double Time { get; private set; }

    /// <summary>True while a widget holds keyboard focus; app shortcuts should stand down.</summary>
    public bool WantsTextInput => _focused != 0;

    public string ClipboardText
    {
        get => _window.GetClipboardText() ?? "";
        set => _window.SetClipboardText(value);
    }

    internal MouseCursor Cursor { get; private set; }
    internal bool FrameRequested { get; private set; }

    /// <summary>Draws another frame after this one, for animation or state that settles over
    /// several frames.</summary>
    public void RequestFrame() => FrameRequested = true;

    public void SetCursor(MouseCursor cursor) => Cursor = cursor;

    internal void BeginFrame(UiInput input, float deltaTime)
    {
        _frame++;
        Input = input;
        DeltaTime = deltaTime;
        Time += deltaTime;
        FrameRequested = false;
        Cursor = MouseCursor.Default;
        _activeSeen = false;
        _focusSeen = false;
        _focusClaimed = false;

        (_hits, _previousHits) = (_previousHits, _hits);
        (_rects, _previousRects) = (_previousRects, _rects);
        _hits.Clear();
        _rects.Clear();
        _hovered = Topmost(_previousHits, null);

        _ids.Clear();
        _ids.Push(RootId);
        _tree.Clear();
        _open.Clear();
        BeginScrolls();

        Open(0, new Box
        {
            Width = Sizing.Fixed(Width),
            Height = Sizing.Fixed(Height),
            Padding = Theme.WindowPadding,
            Gap = Theme.Gap,
        });
    }

    internal void EndFrame()
    {
        Close();
        if (_open.Count != 0)
            throw new InvalidOperationException("A layout scope was not disposed.");
        if (_ids.Count != 1)
            throw new InvalidOperationException("An Id scope was not disposed.");

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
        _tree.Emit(_canvas, _painter, Width, Height, _hits, _rects);

        if (_active != 0 && !_activeSeen)
            _active = 0;
        if (_focused != 0 && (!_focusSeen || (Input.WasPressed(PointerButton.Left) && !_focusClaimed)))
            _focused = 0;
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
        if (_states.TryGetValue(id, out var entry) && entry.State is TState existing)
        {
            _states[id] = (existing, _frame);
            return existing;
        }

        var created = new TState();
        _states[id] = (created, _frame);
        return created;
    }

    public Interaction Interact(string label) => Interact(GetId(label));

    /// <summary>The click behavior every widget is built on: hover, press, hold-while-dragging and a
    /// click on release over the same element. Record an element with the returned
    /// <see cref="Interaction.Id"/> so it can be hit-tested next frame.</summary>
    public Interaction Interact(int id)
    {
        var hovered = _hovered == id && (_active == 0 || _active == id);
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

        return new Interaction(id, hovered, pressed, held, clicked);
    }

    /// <summary>Whether <paramref name="id"/> holds keyboard focus. Asking keeps it: focus is
    /// dropped at the end of a frame in which its owner never asked.</summary>
    public bool HasFocus(int id)
    {
        if (_focused != id) return false;
        _focusSeen = true;
        return true;
    }

    public void Focus(int id)
    {
        _focused = id;
        _focusClaimed = true;
        _focusSeen = true;
    }

    public void ClearFocus() => _focused = 0;

    public float MeasureText(string text, float? size = null, bool bold = false) =>
        MathF.Ceiling(_canvas.MeasureTextWidth(text, Style(size, Theme.Text, bold, Align.Start)));

    /// <summary>Width of the first <paramref name="length"/> characters, shaped in context of the
    /// whole line — for placing a caret.</summary>
    public float MeasureText(string text, int length, float? size = null, bool bold = false) =>
        length <= 0 ? 0f : _canvas.MeasureTextPrefix(text, length, Style(size, Theme.Text, bold, Align.Start));

    public float LineHeight(float? size = null) =>
        MathF.Ceiling(_canvas.MeasureTextLineHeight(Style(size, Theme.Text, false, Align.Start)));

    internal TextStyle Style(float? size, uint color, bool bold, Align align)
    {
        var key = (size ?? Theme.FontSize, color, bold, align);
        if (_textStyles.TryGetValue(key, out var style))
            return style;

        style = new TextStyle
        {
            TextColor = color,
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

    private int Topmost(List<HitRegion> hits, HashSet<int>? among)
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
        return best;
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
