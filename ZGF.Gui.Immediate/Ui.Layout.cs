namespace ZGF.Gui.Immediate;

public sealed partial class Ui
{
    internal enum ScopeKind
    {
        Id,
        Element,
        Scroll,
        Context,
        Layer,
        Modal,
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

    internal sealed class ScrollState
    {
        public bool Horizontal;
        public bool ScrollsX;
        public bool ScrollsY;
        public float OffsetX;
        public float OffsetY;
        public float MaxX;
        public float MaxY;
        public float ContentX;
        public float ContentY;
        public float ViewportX;
        public float ViewportY;
        public int Parent;
        public float DragAnchor;
        public float DragStartOffset;
        public float Synced = float.NaN;

        public float Offset => Horizontal ? OffsetX : OffsetY;
        public float Max => Horizontal ? MaxX : MaxY;
        public float Viewport => Horizontal ? ViewportX : ViewportY;
    }

    private readonly record struct OpenScroll(int Id, int Element, ScrollState State, Interaction ThumbX, Interaction ThumbY, Spacing Padding, float BarWidth);

    private readonly record struct OpenPopup(int Element, int Id, UiRect Anchor, Align Align, float Gap, PopupSide Side);

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
    private readonly List<OpenPopup> _popups = new();
    private int _wheelX;
    private int _wheelY;
    private float _wheelDeltaX;
    private float _wheelDeltaY;

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
    /// plus the offset. Drawn above the rest of the tree and unclipped, inside the window; menus and
    /// tooltips use <see cref="Popup"/> instead.</summary>
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

    /// <summary>
    /// Opens a popup: what is recorded inside is laid out at its own size and shown below
    /// <paramref name="anchor"/>, or above it when there is no room below, aligned to its start,
    /// center or end. It is shown in a window of its own where the platform allows, so it can
    /// extend past the main window, and over the main window otherwise. Coordinates stay the main
    /// window's UI space, so <paramref name="anchor"/> is usually a <see cref="TryGetRect"/> result
    /// and interaction inside works as anywhere else. The popup is hit-testable as
    /// <paramref name="id"/> and keeps the pointer from what is under it, unless
    /// <paramref name="passThrough"/>, which takes no pointer input at all — for a tooltip.
    /// <see cref="PopupSide.End"/> shows it beside the anchor instead — after it, or before it when
    /// there is no room — aligned to the anchor's top, middle or bottom: for a submenu.
    /// </summary>
    public Scope Popup(int id, UiRect anchor, Box box = default, Align align = Align.Start, float gap = 0f, bool passThrough = false,
        PopupSide side = PopupSide.Below)
    {
        var e = Describe(passThrough ? 0 : id, box with
        {
            Width = box.Width ?? Sizing.Fit(),
            Height = box.Height ?? Sizing.Fit(),
        }, null, null);
        e.Parent = 0;
        e.Floating = true;
        e.Layer = _popups.Count + 1;
        e.PassThrough = passThrough;
        e.Z = PopupZ + _popups.Count;
        var element = Push(e);
        _popups.Add(new OpenPopup(element, id, anchor, align, gap, side));
        return new Scope(this, ScopeKind.Element);
    }

    /// <summary>A text element: sized to its text plus the box's padding unless the box sizes it,
    /// with the text vertically centered and aligned by <paramref name="align"/>. A null
    /// <paramref name="family"/> means the theme's <see cref="UiTheme.FontFamily"/>. Each <c>\n</c>
    /// starts a new line; with <paramref name="wrap"/>, lines also break between words to fit the
    /// element's width, which then grows to fill its parent unless the box sizes it.</summary>
    public void Text(string text, Box box = default, uint? color = null, float? size = null, bool bold = false, Align align = Align.Start, int id = 0,
        string? family = null, bool wrap = false)
    {
        if (wrap && box.Width == null)
            box = box with { Width = Sizing.Grow() };
        Push(Describe(id, box, text, TextStyleFor(size, color ?? Theme.Text, bold, align, family), wrap));
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

    /// <summary>A scrolling, clipped column — or row, when <paramref name="box"/> is
    /// <see cref="Direction.Horizontal"/>, which the wheel also scrolls. A column fills the space it
    /// is given unless sized and reserves its scrollbar gutter only while the content overflows; a
    /// row fills the width, fits its content's height and draws its bar over the content.</summary>
    public Scope Scroll(int id, Box box = default) =>
        Scroll(id, box.Direction == Direction.Horizontal ? ScrollAxes.Horizontal : ScrollAxes.Vertical, box);

    public Scope Scroll(string id, ScrollAxes axes, Box box = default) => Scroll(GetId(id), axes, box);

    /// <summary>A scrolling, clipped element along <paramref name="axes"/>. <see cref="ScrollAxes.Both"/>
    /// is a column whose content also scrolls sideways when wider than it, as a code or image view
    /// does; it fills the space it is given unless sized and reserves a gutter for each bar only while
    /// that axis overflows. The wheel scrolls the innermost scroll under the pointer that can move
    /// that way, and a vertical wheel scrolls sideways only where nothing around can take it.</summary>
    public Scope Scroll(int id, ScrollAxes axes, Box box = default)
    {
        var state = GetState<ScrollState>(id);
        var horizontal = axes == ScrollAxes.Horizontal;
        state.Horizontal = horizontal;
        state.ScrollsX = axes != ScrollAxes.Vertical;
        state.ScrollsY = axes != ScrollAxes.Horizontal;
        state.Parent = _openScrolls.Count > 0 ? _openScrolls.Peek().Id : 0;
        var step = Theme.ScrollWheelStep;
        if (id == _wheelX)
            state.OffsetX -= _wheelDeltaX * step;
        if (id == _wheelY)
            state.OffsetY -= _wheelDeltaY * step;

        var thumbX = state.ScrollsX ? Interact(HashCode.Combine(id, horizontal ? "thumb" : "thumbx")) : default;
        var thumbY = state.ScrollsY ? Interact(HashCode.Combine(id, "thumb")) : default;
        DragThumb(state, thumbX, true);
        DragThumb(state, thumbY, false);
        state.OffsetX = MathF.Max(0f, state.OffsetX);
        state.OffsetY = MathF.Max(0f, state.OffsetY);

        var padding = box.Padding ?? default;
        var bar = Theme.ScrollBarWidth;
        var element = Open(id, horizontal
            ? box with
            {
                Direction = Direction.Horizontal,
                Width = box.Width ?? Sizing.Grow(),
                Height = box.Height ?? Sizing.Fit(),
                Gap = box.Gap ?? Theme.Gap,
                AlignY = box.AlignY ?? Align.Center,
                Clip = true,
                ScrollX = state.OffsetX,
            }
            : box with
            {
                Direction = Direction.Vertical,
                Width = box.Width ?? Sizing.Grow(),
                Height = box.Height ?? Sizing.Grow(),
                Gap = box.Gap ?? Theme.Gap,
                Padding = WithGutters(padding, state, bar),
                Clip = true,
                ScrollX = state.ScrollsX ? state.OffsetX : 0f,
                ScrollY = state.OffsetY,
            });
        _scrollIds.Add(id);
        _openScrolls.Push(new OpenScroll(id, element, state, thumbX, thumbY, padding, bar));
        return new Scope(this, ScopeKind.Scroll);
    }

    /// <summary>How far the scroll with <paramref name="id"/> is scrolled, in points from the top-left
    /// of its content; zero for one never recorded.</summary>
    public (float X, float Y) ScrollOffset(int id) =>
        _states.TryGetValue(id, out var entry) && entry.State is ScrollState state ? (state.OffsetX, state.OffsetY) : (0f, 0f);

    /// <summary>Scrolls the scroll with <paramref name="id"/> to (<paramref name="x"/>, <paramref name="y"/>)
    /// points from the top-left of its content, from the next frame, clamped to its content then.</summary>
    public void ScrollTo(int id, float x, float y)
    {
        var state = GetState<ScrollState>(id);
        x = MathF.Max(0f, x);
        y = MathF.Max(0f, y);
        if (x == state.OffsetX && y == state.OffsetY) return;
        state.OffsetX = x;
        state.OffsetY = y;
        RequestFrame();
    }

    /// <summary>Scrolls the <see cref="Scroll(int, Box)"/> with <paramref name="scrollId"/> the least
    /// distance that shows the element with <paramref name="id"/> whole, from the next frame. Both
    /// must have been laid out on the previous frame; returns false when either was not.</summary>
    public bool ScrollIntoView(int scrollId, int id)
    {
        if (!_previousRects.TryGetValue(scrollId, out var view) || !_previousRects.TryGetValue(id, out var item))
            return false;
        if (_previousContentRects.TryGetValue(scrollId, out var content))
            view = content;

        var state = GetState<ScrollState>(scrollId);
        var dx = state.ScrollsX ? IntoView(view.X, view.Right, item.X, item.Right) : 0f;
        var dy = state.ScrollsY ? IntoView(view.Y, view.Bottom, item.Y, item.Bottom) : 0f;
        if (dx == 0f && dy == 0f) return true;
        state.OffsetX += dx;
        state.OffsetY += dy;
        RequestFrame();
        return true;
    }

    /// <summary>Where the content of the element with <paramref name="id"/> was laid out on the
    /// previous frame: its rect less its padding — for a scroll, less the gutters its bars took.</summary>
    public bool TryGetContentRect(int id, out UiRect rect)
    {
        if (_previousContentRects.TryGetValue(id, out rect)) return true;
        return _previousRects.TryGetValue(id, out rect);
    }

    private static float IntoView(float viewStart, float viewEnd, float itemStart, float itemEnd) =>
        itemStart < viewStart ? itemStart - viewStart : itemEnd > viewEnd ? MathF.Min(itemEnd - viewEnd, itemStart - viewStart) : 0f;

    private void DragThumb(ScrollState state, Interaction thumb, bool x)
    {
        if (thumb.Id == 0) return;
        var pointer = x ? Input.Pointer.X : Input.Pointer.Y;
        if (thumb.Pressed)
        {
            state.DragAnchor = pointer;
            state.DragStartOffset = x ? state.OffsetX : state.OffsetY;
        }
        else if (thumb.Held)
        {
            var viewport = x ? state.ViewportX : state.ViewportY;
            var content = x ? state.ContentX : state.ContentY;
            var max = x ? state.MaxX : state.MaxY;
            var travel = viewport - ThumbLength(viewport, content);
            if (travel <= 0f) return;
            var offset = state.DragStartOffset + (pointer - state.DragAnchor) * max / travel;
            if (x) state.OffsetX = offset;
            else state.OffsetY = offset;
        }
    }

    private static Spacing WithGutters(Spacing padding, ScrollState state, float bar) => padding with
    {
        Right = padding.Right + Gutter(state.ScrollsY && state.MaxY > 0f, bar),
        Bottom = padding.Bottom + Gutter(state.ScrollsX && state.MaxX > 0f, bar),
    };

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
            case ScopeKind.Context:
                EndProvide();
                break;
            case ScopeKind.Layer:
                EndLayer();
                break;
            case ScopeKind.Modal:
                Close();
                EndLayer();
                break;
        }
    }

    private void EndScroll()
    {
        var scroll = _openScrolls.Pop();
        var state = scroll.State;
        var bar = scroll.BarWidth;
        var crossBar = state.ScrollsX && state.ScrollsY && state.MaxX > 0f && state.MaxY > 0f ? bar + 2f : 0f;
        if (state.ScrollsY && state.MaxY > 0f)
        {
            var viewport = state.ViewportY - crossBar;
            var length = ThumbLength(viewport, state.ContentY);
            var start = (viewport - length) * MathF.Min(1f, state.OffsetY / state.MaxY);
            OpenFloating(scroll.ThumbY.Id, new Box { Width = bar, Height = length, Background = ThumbColor(scroll.ThumbY), Radius = bar / 2 }, 1f, 0f, -bar - 2f, start, 1);
            Close();
        }
        if (state.ScrollsX && state.MaxX > 0f)
        {
            var viewport = state.ViewportX - crossBar;
            var length = ThumbLength(viewport, state.ContentX);
            var start = (viewport - length) * MathF.Min(1f, state.OffsetX / state.MaxX);
            OpenFloating(scroll.ThumbX.Id, new Box { Width = length, Height = bar, Background = ThumbColor(scroll.ThumbX), Radius = bar / 2 }, 0f, 1f, start, -bar - 1f, 1);
            Close();
        }
        Close();
        _frameScrolls.Add(scroll);
    }

    private uint ThumbColor(Interaction thumb) =>
        thumb.Held ? Theme.ScrollThumbHeld : thumb.Hovered ? Theme.ScrollThumbHovered : Theme.ScrollThumb;

    private void BeginScrolls()
    {
        _openScrolls.Clear();
        _frameScrolls.Clear();
        _popups.Clear();
        (_scrollIds, _previousScrollIds) = (_previousScrollIds, _scrollIds);
        _scrollIds.Clear();
        _wheelX = _wheelY = 0;
        _wheelDeltaX = _wheelDeltaY = 0f;
        if (_input.ScrollY == 0f && _input.ScrollX == 0f) return;
        Topmost(_previousHits, null, out var topZ);
        var target = Topmost(_previousHits, _previousScrollIds, out var targetZ);
        if (target == 0 || targetZ < topZ) return;

        if (_input.ScrollX != 0f && WheelTarget(target, x: true) is var x and not 0)
        {
            _wheelX = x;
            _wheelDeltaX = _input.ScrollX;
        }
        if (_input.ScrollY == 0f) return;
        if (WheelTarget(target, x: false) is var y and not 0)
        {
            _wheelY = y;
            _wheelDeltaY = _input.ScrollY;
        }
        else if (WheelTarget(target, x: true) is var sideways and not 0)
        {
            if (_wheelX != sideways) _wheelDeltaX = 0f;
            _wheelX = sideways;
            _wheelDeltaX += _input.ScrollY;
        }
    }

    private int WheelTarget(int id, bool x)
    {
        for (var depth = 0; id != 0 && depth < 64; depth++)
        {
            if (!_states.TryGetValue(id, out var entry) || entry.State is not ScrollState state) return 0;
            if (x ? state.ScrollsX && state.MaxX > 0f : state.ScrollsY && state.MaxY > 0f)
                return id;
            id = state.Parent;
        }
        return 0;
    }

    private ScrollSettle SettleScrolls()
    {
        var result = ScrollSettle.None;
        foreach (var scroll in _frameScrolls)
        {
            ref var e = ref _tree[scroll.Element];
            var state = scroll.State;
            var contentX = state.Horizontal ? e.ChildExtent : e.CrossExtent;
            var contentY = state.Horizontal ? e.CrossExtent : e.ChildExtent;
            var maxX = state.ScrollsX ? MathF.Max(0f, contentX - e.W) : 0f;
            var maxY = state.ScrollsY ? MathF.Max(0f, contentY - e.H) : 0f;
            if (maxX != state.MaxX || maxY != state.MaxY || e.W != state.ViewportX || e.H != state.ViewportY
                || contentX != state.ContentX || contentY != state.ContentY)
                RequestFrame();
            state.ContentX = contentX;
            state.ContentY = contentY;
            state.ViewportX = e.W;
            state.ViewportY = e.H;
            state.MaxX = maxX;
            state.MaxY = maxY;

            if (!state.Horizontal)
            {
                var padding = WithGutters(scroll.Padding, state, scroll.BarWidth);
                if (e.Padding != padding)
                {
                    e.Padding = padding;
                    result = ScrollSettle.Relayout;
                }
            }

            var clampedX = Math.Clamp(state.OffsetX, 0f, maxX);
            var clampedY = Math.Clamp(state.OffsetY, 0f, maxY);
            if (clampedX == state.OffsetX && clampedY == state.OffsetY) continue;
            state.OffsetX = clampedX;
            state.OffsetY = clampedY;
            e.ScrollX = state.ScrollsX ? clampedX : 0f;
            e.ScrollY = state.ScrollsY ? clampedY : 0f;
            if (result == ScrollSettle.None)
                result = ScrollSettle.Reposition;
        }
        return result;
    }

    private static float Gutter(bool overflowing, float barWidth) => overflowing ? barWidth + 6f : 0f;

    private static float ThumbLength(float viewport, float content) =>
        content <= 0f ? 0f : MathF.Min(viewport, MathF.Max(24f, viewport * viewport / content));

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

    private Element Describe(int id, in Box box, string? text, TextStyle? style, bool wrap = false)
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
            e.LineHeight = MeasureLineHeight(style);
            if (wrap || text.Contains('\n'))
            {
                var lines = BreakLines(text, style, float.PositiveInfinity);
                e.Lines = lines;
                e.Wrap = wrap;
                e.ContentWidth = lines.Width;
                e.ContentHeight = lines.Count * e.LineHeight;
            }
            else
            {
                e.ContentWidth = MeasureWidth(text, style);
                e.ContentHeight = e.LineHeight;
            }
        }
        return e;
    }
}
