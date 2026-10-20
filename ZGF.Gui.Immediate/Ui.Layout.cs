namespace ZGF.Gui.Immediate;

public sealed partial class Ui
{
    internal enum ScopeKind
    {
        Id,
        Element,
        Scroll,
    }

    public readonly struct Scope : IDisposable
    {
        private readonly Ui _ui;
        private readonly ScopeKind _kind;

        internal Scope(Ui ui, ScopeKind kind)
        {
            _ui = ui;
            _kind = kind;
        }

        public void Dispose() => _ui.EndScope(_kind);
    }

    private sealed class ScrollState
    {
        public float Offset;
        public float Max;
        public float Content;
        public float Viewport;
        public float DragAnchorY;
        public float DragStartOffset;
    }

    private readonly record struct OpenScroll(int Element, ScrollState State, Interaction Thumb, float PaddingRight);

    private enum ScrollSettle
    {
        None,
        Reposition,
        Relayout,
    }

    private readonly Stack<OpenScroll> _openScrolls = new();
    private readonly List<OpenScroll> _frameScrolls = new();
    private HashSet<int> _scrollIds = new();
    private HashSet<int> _previousScrollIds = new();
    private int _wheelTarget;

    /// <summary>True when the element being recorded lays its children out left to right.</summary>
    public bool InRow => _open.Count > 0 && _tree[_open.Peek()].Horizontal;

    /// <summary>Opens an element; what is recorded until the scope ends becomes its children.</summary>
    public Scope Box(Box box = default) => Box(0, box);

    /// <summary>Opens an element hit-testable as <paramref name="id"/> — pass the
    /// <see cref="Interaction.Id"/> the widget interacted with.</summary>
    public Scope Box(int id, Box box = default)
    {
        Open(id, box);
        return new Scope(this, ScopeKind.Element);
    }

    public Scope Column(Box box = default) => Column(0, box);

    public Scope Column(int id, Box box = default) => Box(id, box with
    {
        Direction = Direction.Vertical,
        Width = box.Width ?? Sizing.Grow(),
        Gap = box.Gap ?? Theme.Gap,
    });

    public Scope Row(Box box = default) => Row(0, box);

    public Scope Row(int id, Box box = default) => Box(id, box with
    {
        Direction = Direction.Horizontal,
        Width = box.Width ?? Sizing.Grow(),
        Gap = box.Gap ?? Theme.Gap,
        AlignY = box.AlignY ?? Align.Center,
    });

    /// <summary>An element taken out of the flow and placed over its parent: its top-left sits at
    /// (<paramref name="anchorX"/>, <paramref name="anchorY"/>) — fractions of the parent's rect —
    /// plus the offset. Drawn above the rest of the tree and unclipped; the basis for popups.</summary>
    public Scope Float(float anchorX, float anchorY, float offsetX = 0f, float offsetY = 0f, Box box = default, int z = 100, int id = 0)
    {
        OpenFloating(id, box, anchorX, anchorY, offsetX, offsetY, z);
        return new Scope(this, ScopeKind.Element);
    }

    /// <summary>An element with no children.</summary>
    public void Leaf(Box box) => Leaf(0, box);

    public void Leaf(int id, Box box)
    {
        Open(id, box);
        Close();
    }

    /// <summary>A text element: sized to its text plus the box's padding unless the box sizes it,
    /// with the text vertically centered and aligned by <paramref name="align"/>.</summary>
    public void Text(string text, Box box = default, uint? color = null, float? size = null, bool bold = false, Align align = Align.Start, int id = 0)
    {
        Open(id, box, text, Style(size, color ?? Theme.Text, bold, align));
        Close();
    }

    /// <summary>Paints custom content on the element being recorded, after its background and
    /// before its children, once its rect is known. Pass what the paint needs as
    /// <paramref name="state"/> with a <c>static</c> lambda and nothing is allocated per frame.</summary>
    public void Paint<TState>(TState state, Action<UiPainter, TState> paint) =>
        _tree.AddPaint(_open.Peek(), PaintCommand<TState>.Rent(state, paint));

    /// <summary>Convenience form of <see cref="Paint{TState}"/>; a capturing lambda allocates
    /// every frame.</summary>
    public void Paint(Action<UiPainter> paint) => Paint(paint, static (painter, p) => p(painter));

    public Scope Scroll(string id, Box box = default) => Scroll(GetId(id), box);

    /// <summary>A vertically scrolling, clipped column. Fills the space it is given unless sized.
    /// The scrollbar gutter is only reserved while the content overflows.</summary>
    public Scope Scroll(int id, Box box = default)
    {
        var state = GetState<ScrollState>(id);
        if (id == _wheelTarget)
            state.Offset -= Input.ScrollY * Theme.ScrollWheelStep;

        var thumb = Interact(HashCode.Combine(id, "thumb"));
        if (thumb.Pressed)
        {
            state.DragAnchorY = Input.Pointer.Y;
            state.DragStartOffset = state.Offset;
        }
        else if (thumb.Held)
        {
            var travel = state.Viewport - ThumbHeight(state);
            if (travel > 0f)
                state.Offset = state.DragStartOffset + (Input.Pointer.Y - state.DragAnchorY) * state.Max / travel;
        }
        state.Offset = Math.Clamp(state.Offset, 0f, state.Max);

        var padding = box.Padding ?? default;
        var element = Open(id, box with
        {
            Direction = Direction.Vertical,
            Width = box.Width ?? Sizing.Grow(),
            Height = box.Height ?? Sizing.Grow(),
            Gap = box.Gap ?? Theme.Gap,
            Padding = padding with { Right = padding.Right + Gutter(state.Max > 0f) },
            Clip = true,
            ScrollY = state.Offset,
        });
        _scrollIds.Add(id);
        _openScrolls.Push(new OpenScroll(element, state, thumb, padding.Right));
        return new Scope(this, ScopeKind.Scroll);
    }

    private void EndScope(ScopeKind kind)
    {
        switch (kind)
        {
            case ScopeKind.Id:
                _ids.Pop();
                break;
            case ScopeKind.Element:
                Close();
                break;
            case ScopeKind.Scroll:
                EndScroll();
                break;
        }
    }

    private void EndScroll()
    {
        var scroll = _openScrolls.Pop();
        var state = scroll.State;
        if (state.Max > 0f)
        {
            var thumbHeight = ThumbHeight(state);
            var top = (state.Viewport - thumbHeight) * (state.Offset / state.Max);
            var color = scroll.Thumb.Held ? Theme.TextMuted : scroll.Thumb.Hovered ? Theme.ControlBorder : Theme.Control;
            OpenFloating(scroll.Thumb.Id, new Box
            {
                Width = Theme.ScrollBarWidth,
                Height = thumbHeight,
                Background = color,
                Radius = Theme.ScrollBarWidth / 2,
            }, 1f, 0f, -Theme.ScrollBarWidth - 2f, top, 1);
            Close();
        }
        Close();
        _frameScrolls.Add(scroll);
    }

    private void BeginScrolls()
    {
        _openScrolls.Clear();
        _frameScrolls.Clear();
        (_scrollIds, _previousScrollIds) = (_previousScrollIds, _scrollIds);
        _scrollIds.Clear();
        _wheelTarget = Input.ScrollY != 0f ? Topmost(_previousHits, _previousScrollIds) : 0;
    }

    private ScrollSettle SettleScrolls()
    {
        var result = ScrollSettle.None;
        foreach (var scroll in _frameScrolls)
        {
            ref var e = ref _tree[scroll.Element];
            var state = scroll.State;
            var max = MathF.Max(0f, e.ChildExtent - e.H);
            if (max != state.Max || e.H != state.Viewport || e.ChildExtent != state.Content)
                RequestFrame();
            state.Content = e.ChildExtent;
            state.Viewport = e.H;
            state.Max = max;

            var paddingRight = scroll.PaddingRight + Gutter(max > 0f);
            if (e.Padding.Right != paddingRight)
            {
                e.Padding = e.Padding with { Right = paddingRight };
                result = ScrollSettle.Relayout;
            }

            var clamped = Math.Clamp(state.Offset, 0f, max);
            if (clamped == state.Offset) continue;
            state.Offset = clamped;
            e.ScrollY = clamped;
            if (result == ScrollSettle.None)
                result = ScrollSettle.Reposition;
        }
        return result;
    }

    private float Gutter(bool overflowing) => overflowing ? Theme.ScrollBarWidth + 6f : 0f;

    private static float ThumbHeight(ScrollState state) =>
        state.Content <= 0f ? 0f : MathF.Min(state.Viewport, MathF.Max(24f, state.Viewport * state.Viewport / state.Content));

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
            ScrollX = box.ScrollX,
            ScrollY = box.ScrollY,
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
            e.ContentWidth = MeasureWidth(text, style);
            e.ContentHeight = MeasureLineHeight(style);
        }
        return e;
    }
}
