namespace ZGF.Gui.Immediate;

public sealed partial class Ui
{
    internal enum ScopeKind
    {
        Id,
        Element,
        Scroll,
        Context,
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
        public float Offset;
        public float Max;
        public float Content;
        public float Viewport;
        public float DragAnchor;
        public float DragStartOffset;
    }

    private readonly record struct OpenScroll(int Element, ScrollState State, Interaction Thumb, float PaddingRight, float BarWidth);

    private readonly record struct OpenPopup(int Element, int Id, UiRect Anchor, Align Align, float Gap);

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
    /// </summary>
    public Scope Popup(int id, UiRect anchor, Box box = default, Align align = Align.Start, float gap = 0f, bool passThrough = false)
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
        _popups.Add(new OpenPopup(element, id, anchor, align, gap));
        return new Scope(this, ScopeKind.Element);
    }

    /// <summary>A text element: sized to its text plus the box's padding unless the box sizes it,
    /// with the text vertically centered and aligned by <paramref name="align"/>. A null
    /// <paramref name="family"/> means the theme's <see cref="UiTheme.FontFamily"/>.</summary>
    public void Text(string text, Box box = default, uint? color = null, float? size = null, bool bold = false, Align align = Align.Start, int id = 0, string? family = null)
    {
        Open(id, box, text, TextStyleFor(size, color ?? Theme.Text, bold, align, family));
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
    public Scope Scroll(int id, Box box = default)
    {
        var state = GetState<ScrollState>(id);
        var horizontal = box.Direction == Direction.Horizontal;
        state.Horizontal = horizontal;
        if (id == _wheelTarget)
            state.Offset -= (horizontal ? Input.ScrollY + Input.ScrollX : Input.ScrollY) * Theme.ScrollWheelStep;

        var thumb = Interact(HashCode.Combine(id, "thumb"));
        var pointer = horizontal ? Input.Pointer.X : Input.Pointer.Y;
        if (thumb.Pressed)
        {
            state.DragAnchor = pointer;
            state.DragStartOffset = state.Offset;
        }
        else if (thumb.Held)
        {
            var travel = state.Viewport - ThumbLength(state);
            if (travel > 0f)
                state.Offset = state.DragStartOffset + (pointer - state.DragAnchor) * state.Max / travel;
        }
        state.Offset = Math.Clamp(state.Offset, 0f, state.Max);

        var padding = box.Padding ?? default;
        var bar = Theme.ScrollBarWidth;
        var element = Open(id, horizontal
            ? box with
            {
                Width = box.Width ?? Sizing.Grow(),
                Height = box.Height ?? Sizing.Fit(),
                Gap = box.Gap ?? Theme.Gap,
                AlignY = box.AlignY ?? Align.Center,
                Clip = true,
                ScrollX = state.Offset,
            }
            : box with
            {
                Direction = Direction.Vertical,
                Width = box.Width ?? Sizing.Grow(),
                Height = box.Height ?? Sizing.Grow(),
                Gap = box.Gap ?? Theme.Gap,
                Padding = padding with { Right = padding.Right + Gutter(state.Max > 0f, bar) },
                Clip = true,
                ScrollY = state.Offset,
            });
        _scrollIds.Add(id);
        _openScrolls.Push(new OpenScroll(element, state, thumb, padding.Right, bar));
        return new Scope(this, ScopeKind.Scroll);
    }

    /// <summary>Scrolls the <see cref="Scroll(int, Box)"/> with <paramref name="scrollId"/> the least
    /// distance that shows the element with <paramref name="id"/> whole, from the next frame. Both
    /// must have been laid out on the previous frame; returns false when either was not.</summary>
    public bool ScrollIntoView(int scrollId, int id)
    {
        if (!_previousRects.TryGetValue(scrollId, out var view) || !_previousRects.TryGetValue(id, out var item))
            return false;

        var state = GetState<ScrollState>(scrollId);
        var (viewStart, viewEnd, itemStart, itemEnd) = state.Horizontal
            ? (view.X, view.Right, item.X, item.Right)
            : (view.Y, view.Bottom, item.Y, item.Bottom);
        var delta = itemStart < viewStart ? itemStart - viewStart : itemEnd > viewEnd ? MathF.Min(itemEnd - viewEnd, itemStart - viewStart) : 0f;
        if (delta == 0f) return true;
        state.Offset += delta;
        RequestFrame();
        return true;
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
            case ScopeKind.Context:
                EndProvide();
                break;
        }
    }

    private void EndScroll()
    {
        var scroll = _openScrolls.Pop();
        var state = scroll.State;
        if (state.Max > 0f)
        {
            var length = ThumbLength(state);
            var start = (state.Viewport - length) * (state.Offset / state.Max);
            var color = scroll.Thumb.Held ? Theme.ScrollThumbHeld : scroll.Thumb.Hovered ? Theme.ScrollThumbHovered : Theme.ScrollThumb;
            var bar = scroll.BarWidth;
            if (state.Horizontal)
                OpenFloating(scroll.Thumb.Id, new Box { Width = length, Height = bar, Background = color, Radius = bar / 2 }, 0f, 1f, start, -bar - 1f, 1);
            else
                OpenFloating(scroll.Thumb.Id, new Box { Width = bar, Height = length, Background = color, Radius = bar / 2 }, 1f, 0f, -bar - 2f, start, 1);
            Close();
        }
        Close();
        _frameScrolls.Add(scroll);
    }

    private void BeginScrolls()
    {
        _openScrolls.Clear();
        _frameScrolls.Clear();
        _popups.Clear();
        (_scrollIds, _previousScrollIds) = (_previousScrollIds, _scrollIds);
        _scrollIds.Clear();
        _wheelTarget = 0;
        if (Input.ScrollY == 0f && Input.ScrollX == 0f) return;
        Topmost(_previousHits, null, out var topZ);
        var target = Topmost(_previousHits, _previousScrollIds, out var targetZ);
        _wheelTarget = targetZ >= topZ ? target : 0;
    }

    private ScrollSettle SettleScrolls()
    {
        var result = ScrollSettle.None;
        foreach (var scroll in _frameScrolls)
        {
            ref var e = ref _tree[scroll.Element];
            var state = scroll.State;
            var viewport = state.Horizontal ? e.W : e.H;
            var max = MathF.Max(0f, e.ChildExtent - viewport);
            if (max != state.Max || viewport != state.Viewport || e.ChildExtent != state.Content)
                RequestFrame();
            state.Content = e.ChildExtent;
            state.Viewport = viewport;
            state.Max = max;

            var paddingRight = scroll.PaddingRight + Gutter(max > 0f, scroll.BarWidth);
            if (!state.Horizontal && e.Padding.Right != paddingRight)
            {
                e.Padding = e.Padding with { Right = paddingRight };
                result = ScrollSettle.Relayout;
            }

            var clamped = Math.Clamp(state.Offset, 0f, max);
            if (clamped == state.Offset) continue;
            state.Offset = clamped;
            if (state.Horizontal)
                e.ScrollX = clamped;
            else
                e.ScrollY = clamped;
            if (result == ScrollSettle.None)
                result = ScrollSettle.Reposition;
        }
        return result;
    }

    private static float Gutter(bool overflowing, float barWidth) => overflowing ? barWidth + 6f : 0f;

    private static float ThumbLength(ScrollState state) =>
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
