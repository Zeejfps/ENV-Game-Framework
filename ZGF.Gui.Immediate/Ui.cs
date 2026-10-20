using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>
/// The per-frame surface handed to <see cref="Gui{T}"/>.Render. Calls record elements; once
/// Render returns the tree is laid out and drawn. Interaction is answered during the call from
/// where each element was laid out on the previous frame, matched by id.
/// </summary>
public sealed partial class Ui
{
    private const int RootId = 17;

    private readonly RenderedCanvasBase _canvas;
    private readonly IWindow _window;
    private readonly LayoutTree _tree = new();
    private readonly Stack<int> _open = new();
    private readonly Stack<int> _ids = new();
    private readonly Dictionary<(float Size, uint Color, bool Bold, Align Align), TextStyle> _textStyles = new();
    private List<HitRegion> _hits = new();
    private List<HitRegion> _previousHits = new();
    private Dictionary<int, UiRect> _rects = new();
    private Dictionary<int, UiRect> _previousRects = new();

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
        Theme = theme;
    }

    public UiTheme Theme { get; }
    public UiInput Input { get; private set; } = new();
    public float Width => _canvas.Width;
    public float Height => _canvas.Height;
    public float DeltaTime { get; private set; }
    public double Time { get; private set; }

    /// <summary>True while a widget owns the keyboard; app shortcuts should stand down.</summary>
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
        _tree.Emit(_canvas, Width, Height, _hits, _rects);

        if (_active != 0 && !_activeSeen)
            _active = 0;
        if (_focused != 0 && (!_focusSeen || (Input.WasPressed(PointerButton.Left) && !_focusClaimed)))
            _focused = 0;
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

    /// <summary>The click behavior every widget is built on: hover, press, hold-while-dragging and a
    /// click on release over the same element. Pass the id of the element the widget records.</summary>
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

        return new Interaction(hovered, pressed, held, clicked);
    }

    public bool HasFocus(int id) => _focused == id;

    public void Focus(int id)
    {
        _focused = id;
        _focusClaimed = true;
        _focusSeen = true;
    }

    private bool KeepFocus(int id)
    {
        if (_focused != id) return false;
        _focusSeen = true;
        return true;
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

    private int Open(int id, in Box box, string? text = null, TextStyle? style = null) =>
        Push(Describe(id, box, text, style));

    private int OpenFloating(int id, in Box box, float anchorX, float anchorY, float offsetX, float offsetY, int z)
    {
        var e = Describe(id, box, null, null);
        e.Floating = true;
        e.AnchorX = anchorX;
        e.AnchorY = anchorY;
        e.OffsetX = offsetX;
        e.OffsetY = offsetY;
        e.Z = z;
        return Push(e);
    }

    private void Leaf(int id, in Box box, string? text = null, TextStyle? style = null)
    {
        Open(id, box, text, style);
        Close();
    }

    private void Close() => _open.Pop();

    private int Push(in Element element)
    {
        var index = _tree.Add(element);
        _open.Push(index);
        return index;
    }

    private Element Describe(int id, in Box box, string? text, TextStyle? style)
    {
        var e = new Element
        {
            Id = id,
            Parent = _open.Count > 0 ? _open.Peek() : -1,
            Horizontal = box.Direction == Direction.Horizontal,
            Width = box.Width ?? Sizing.Fit(),
            Height = box.Height ?? Sizing.Fit(),
            Padding = box.Padding ?? default,
            Gap = box.Gap ?? 0f,
            AlignX = box.AlignX ?? Align.Start,
            AlignY = box.AlignY ?? Align.Start,
            Background = box.Background,
            BorderColor = box.BorderColor,
            BorderWidth = box.BorderWidth,
            Radius = box.Radius,
            Clip = box.Clip,
        };

        if (!string.IsNullOrEmpty(text) && style != null)
        {
            e.Text = text;
            e.TextStyle = style;
            e.ContentWidth = MathF.Ceiling(_canvas.MeasureTextWidth(text, style));
            e.ContentHeight = MathF.Ceiling(_canvas.MeasureTextLineHeight(style));
        }
        return e;
    }

    private bool ParentIsHorizontal => _open.Count > 0 && _tree[_open.Peek()].Horizontal;

    /// <summary>Strokes a line across the element being recorded, in fractions of its rect
    /// (0,0 top-left to 1,1 bottom-right).</summary>
    public void Line(float x0, float y0, float x1, float y1, float thickness, uint color) =>
        _tree.AddLine(_open.Peek(), new LinePaint { X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Thickness = thickness, Color = color });

    public float MeasureText(string text, float? size = null, bool bold = false) =>
        MathF.Ceiling(_canvas.MeasureTextWidth(text, Style(size, Theme.Text, bold, Align.Start)));

    public float LineHeight(float? size = null) =>
        MathF.Ceiling(_canvas.MeasureTextLineHeight(Style(size, Theme.Text, false, Align.Start)));

    private float MeasurePrefix(string text, int length) =>
        length <= 0 ? 0f : _canvas.MeasureTextPrefix(text, length, Style(null, Theme.Text, false, Align.Start));

    private TextStyle Style(float? size, uint color, bool bold, Align align)
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

    private static string DisplayText(string label)
    {
        var hidden = label.IndexOf("##", StringComparison.Ordinal);
        return hidden < 0 ? label : label[..hidden];
    }
}
