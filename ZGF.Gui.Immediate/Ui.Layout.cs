using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    internal enum ScopeKind
    {
        Id,
        Element,
        Scroll,
        Layer,
        StateScope,
        Group,
    }

    private readonly record struct OpenScope(ScopeKind Kind, object? State, Action<object>? End);

    /// <summary>What a call that opens something returns: disposing it ends that and every scope
    /// opened inside it that is still open, innermost first. A widget that is closed returns
    /// <c>default</c>, which is not <see cref="IsOpen"/> and ends nothing.</summary>
    public readonly struct Scope : IDisposable
    {
        private readonly Ui? _ui;
        private readonly int _depth;

        internal Scope(Ui ui, int depth)
        {
            _ui = ui;
            _depth = depth;
        }

        /// <summary>Record the content of the widget that returned this scope only while true.</summary>
        public bool IsOpen => _ui != null;

        public void Dispose() => _ui?.EndScopes(_depth);
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
        public float Synced = float.NaN;
        public float SyncedX = float.NaN;
        public ScrollBar Bar;
        public int RevealRow = -1;

        public float Offset => Horizontal ? OffsetX : OffsetY;
        public float Max => Horizontal ? MaxX : MaxY;
        public float Viewport => Horizontal ? ViewportX : ViewportY;
    }

    private readonly record struct OpenScroll(int Id, int Element, ScrollState State, Interaction ThumbX, Interaction ThumbY, Spacing Padding);

    /// <summary>A scroll's bar geometry, from the theme: the painted thumb, the thumb the pointer
    /// grabs, and the gutter reserved for it.</summary>
    internal readonly record struct ScrollBar(float Paint, float Hit, float Inset, float MinThumb)
    {
        public float Gutter => Paint + Inset * 2;

        public static ScrollBar From(UiTheme theme) =>
            new(theme.ScrollBarWidth, theme.ScrollBarHitWidth, theme.ScrollBarInset, theme.ScrollThumbMinLength);
    }

    private readonly record struct OpenPopup(int Element, int Id, UiRect Anchor, Align Align, float Gap, PopupSide Side);

    private enum ScrollSettle
    {
        None,
        Reposition,
        Relayout,
    }

    private readonly List<OpenScope> _scopes = new();
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
        return BeginScope(ScopeKind.Element);
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
    /// plus the offset. Drawn above the rest of the tree and unclipped, inside the window, unless
    /// <paramref name="clipped"/>, which keeps the clip of the scroll or clipped element it is
    /// recorded in — for a badge on a row of a list. Menus and tooltips use <see cref="Popup"/> instead.</summary>
    public Scope Float(float anchorX, float anchorY, float offsetX = 0f, float offsetY = 0f, Box box = default, int z = 100, int id = 0,
        bool clipped = false)
    {
        var index = OpenFloating(id, box, anchorX, anchorY, offsetX, offsetY, z);
        _tree[index].KeepsClip = clipped;
        return BeginScope(ScopeKind.Element);
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
        return BeginScope(ScopeKind.Element);
    }

    /// <summary>A text element: sized to its text plus the box's padding unless the box sizes it,
    /// with the text vertically centered and aligned by <paramref name="align"/>. A null
    /// <paramref name="family"/> means the theme's <see cref="UiTheme.FontFamily"/>. Each <c>\n</c>
    /// starts a new line; with <paramref name="wrap"/>, lines also break between words to fit the
    /// element's width, which then grows to fill its parent unless the box sizes it. A single line
    /// with an <paramref name="overflow"/> other than <see cref="TextOverflow.Clip"/> shrinks below
    /// its text's width when its parent runs out of room, down to its fit minimum, unless the box
    /// gives it a fixed, grow or percent width — <see cref="Sizing.Priority"/> on a fit width orders
    /// which text gives up room first — and is shortened around an ellipsis to the width it is laid
    /// out at.</summary>
    public void Text(string text, Box box = default, uint? color = null, float? size = null, bool bold = false, Align align = Align.Start, int id = 0,
        string? family = null, bool wrap = false, TextOverflow overflow = TextOverflow.Clip)
    {
        if (wrap && box.Width == null)
            box = box with { Width = Sizing.Grow() };
        var e = Describe(id, box, text, TextStyleFor(size, color ?? Theme.Text, bold, align, family), wrap);
        if (!wrap && overflow != TextOverflow.Clip && e.Lines == null)
        {
            e.Overflow = overflow;
            if (box.Width is not { } width || width.Kind == SizingKind.Fit)
            {
                var fit = box.Width ?? Sizing.Fit();
                e.Width = Sizing.Grow(fit.Min, MathF.Max(fit.Min, MathF.Min(fit.Max, e.ContentWidth + e.Padding.Left + e.Padding.Right))) with
                {
                    Priority = fit.Priority,
                };
            }
        }
        Push(e);
        Close();
    }

    /// <summary>The style <see cref="Text"/> draws in with these choices, for the runs of
    /// <see cref="RichText"/>. Styles are kept per choice, so the same choices give the same style.</summary>
    public TextStyle StyleText(uint? color = null, float? size = null, bool bold = false, string? family = null) =>
        TextStyleFor(size, color ?? Theme.Text, bold, Align.Start, family);

    /// <summary>A text element of styled runs that wrap together, as <see cref="Text"/> with
    /// <c>wrap</c> wraps one style: lines break between words across runs to fit the element's
    /// width, which grows to fill its parent unless the box sizes it. Without
    /// <paramref name="wrap"/> it keeps its lines, breaking only at '\n'. Code runs are drawn on a
    /// chip of <paramref name="codeBackground"/> when it is not transparent. Layouts are kept by the
    /// list's identity, so pass the same list while the text is the same. With an
    /// <paramref name="id"/>, runs with a <see cref="RichTextRun.Link"/> are links: the pointer turns
    /// to a hand and underlines the one it is over, and a left or middle click on one returns it.</summary>
    public RichTextClick RichText(IReadOnlyList<RichTextRun> runs, Box box = default, uint codeBackground = 0u, bool wrap = true, int id = 0)
    {
        if (wrap && box.Width == null)
            box = box with { Width = Sizing.Grow() };
        var hoveredLink = -1;
        var click = default(RichTextClick);
        if (id != 0 && HasLinks(runs))
            hoveredLink = InteractWithLinks(runs, box, wrap, id, out click);
        var e = Describe(id, box, null, null);
        var natural = LayOutRuns(runs, 0f);
        e.Runs = runs;
        e.RunLines = natural;
        e.HoveredRun = hoveredLink;
        e.Wrap = wrap;
        e.CodeBackground = codeBackground;
        e.ContentWidth = MathF.Ceiling(natural.MaxLineWidth);
        e.ContentHeight = MathF.Ceiling(natural.Height);
        Push(e);
        Close();
        return click;
    }

    private static bool HasLinks(IReadOnlyList<RichTextRun> runs)
    {
        foreach (var run in runs)
        {
            if (run.Link != null)
                return true;
        }
        return false;
    }

    /// <summary>The link run under the pointer, -1 when there is none, and the click on it.</summary>
    private int InteractWithLinks(IReadOnlyList<RichTextRun> runs, Box box, bool wrap, int id, out RichTextClick click)
    {
        click = default;
        var interaction = Interact(id);
        var hovered = LinkUnderPointer(runs, box, wrap, id);
        if (hovered < 0)
            return -1;
        SetCursor(MouseCursor.Hand);
        var link = runs[hovered].Link;
        if (interaction.Clicked)
            click = new RichTextClick(link, PointerButton.Left);
        else if (interaction.ClickedWith(PointerButton.Middle))
            click = new RichTextClick(link, PointerButton.Middle);
        return hovered;
    }

    /// <summary>The run with a link under the pointer, laid out as last frame laid out the element.</summary>
    private int LinkUnderPointer(IReadOnlyList<RichTextRun> runs, Box box, bool wrap, int id)
    {
        if (!IsHovered(id))
            return -1;
        if (!TryGetRect(id, out var rect))
            return -1;
        var padding = box.Padding ?? default;
        var width = rect.Width - padding.Left - padding.Right;
        var layout = LayOutRuns(runs, wrap ? width : 0f);
        var x = Input.Pointer.X - rect.X - padding.Left;
        var y = Input.Pointer.Y - rect.Y - padding.Top;
        var run = RichTextLayout.RunAt(layout, x, y);
        if (run < 0 || runs[run].Link == null)
            return -1;
        return run;
    }

    /// <summary>Paints custom content on the element being recorded, after its background and
    /// before its children, once its rect is known. Pass what the paint needs as
    /// <paramref name="state"/> with a <c>static</c> lambda and nothing is allocated per frame.</summary>
    public void Paint<TState>(TState state, Action<UiPainter, TState> paint) =>
        _tree.AddPaint(_open.Peek(), PaintCommand<TState>.Rent(state, paint));

    /// <summary>Convenience form of <see cref="Paint{TState}"/>; a capturing lambda allocates
    /// every frame.</summary>
    public void Paint(Action<UiPainter> paint) => Paint(paint, static (painter, p) => p(painter));

    /// <summary>Paints <paramref name="glyph"/> centered on the element being recorded, in
    /// <paramref name="color"/>, stroked as the theme's icons are.</summary>
    public void PaintGlyph(UiGlyph glyph, uint color)
    {
        var stroke = glyph == UiGlyph.DropdownArrow ? Theme.DropdownArrowWidth : Theme.IconStrokeWidth;
        Paint((glyph, color, stroke), static (p, s) => p.DrawGlyph(s.glyph, s.color, s.stroke));
    }

    public Scope Scroll(string id, Box box = default) => Scroll(GetId(id), box);

    /// <summary>A scrolling, clipped column — or row, when <paramref name="box"/> is
    /// <see cref="Direction.Horizontal"/>, which the wheel also scrolls. A column fills the space it
    /// is given unless sized and reserves its scrollbar gutter only while the content overflows; a
    /// row fills the width, fits its content's height and draws its bar over the content, only while
    /// the pointer is over it.</summary>
    public Scope Scroll(int id, Box box = default) =>
        Scroll(id, box.Direction == Direction.Horizontal ? ScrollAxes.Horizontal : ScrollAxes.Vertical, box);

    public Scope Scroll(string id, ScrollAxes axes, Box box = default) => Scroll(GetId(id), axes, box);

    internal static int HorizontalThumbId(int scrollId, ScrollAxes axes) => HashCode.Combine(scrollId, axes == ScrollAxes.Horizontal ? "thumb" : "thumbx");

    internal static int VerticalThumbId(int scrollId) => HashCode.Combine(scrollId, "thumb");

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
            state.OffsetX = Wheel(state.OffsetX, _wheelDeltaX * step, state.MaxX);
        if (id == _wheelY)
            state.OffsetY = Wheel(state.OffsetY, _wheelDeltaY * step, state.MaxY);

        var thumbX = state.ScrollsX ? Interact(HorizontalThumbId(id, axes)) : default;
        var thumbY = state.ScrollsY ? Interact(VerticalThumbId(id)) : default;
        DragThumb(state, thumbX, true);
        DragThumb(state, thumbY, false);
        state.OffsetX = MathF.Max(0f, state.OffsetX);
        state.OffsetY = MathF.Max(0f, state.OffsetY);

        var padding = box.Padding ?? default;
        var bar = ScrollBar.From(Theme);
        state.Bar = bar;
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
        _openScrolls.Push(new OpenScroll(id, element, state, thumbX, thumbY, padding));
        return BeginScope(ScopeKind.Scroll);
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

    /// <summary>Where a wheel moving <paramref name="by"/> takes <paramref name="offset"/>: never
    /// past <paramref name="max"/> as last laid out, though an offset already beyond it, waiting
    /// for content still to come, is left there.</summary>
    private static float Wheel(float offset, float by, float max) =>
        by < 0f ? MathF.Max(offset, MathF.Min(offset - by, max)) : offset - by;

    private static float IntoView(float viewStart, float viewEnd, float itemStart, float itemEnd) =>
        itemStart < viewStart ? itemStart - viewStart : itemEnd > viewEnd ? MathF.Min(itemEnd - viewEnd, itemStart - viewStart) : 0f;

    private void DragThumb(ScrollState state, Interaction thumb, bool x)
    {
        if (thumb.Id == 0) return;
        if (!TryGetDragStart(thumb, x ? state.OffsetX : state.OffsetY, out var startOffset)) return;
        var moved = x ? Input.Pointer.X - PressPosition.X : Input.Pointer.Y - PressPosition.Y;
        var viewport = x ? state.ViewportX : state.ViewportY;
        var content = x ? state.ContentX : state.ContentY;
        var max = x ? state.MaxX : state.MaxY;
        var travel = viewport - ThumbLength(viewport, content, state.Bar.MinThumb);
        if (travel <= 0f) return;
        var offset = Math.Clamp(startOffset + moved * max / travel, 0f, max);
        if (x) state.OffsetX = offset;
        else state.OffsetY = offset;
    }

    private static Spacing WithGutters(Spacing padding, ScrollState state, ScrollBar bar) => padding with
    {
        Right = padding.Right + Gutter(state.ScrollsY && state.MaxY > 0f, bar),
        Bottom = padding.Bottom + Gutter(state.ScrollsX && state.MaxX > 0f, bar),
    };

    /// <summary>Opens a scope that ends every scope opened after it when it is disposed, then runs
    /// <paramref name="end"/> on <paramref name="state"/> — so a widget built of several scopes
    /// returns one. Pass a <c>static</c> lambda and nothing is allocated per frame.</summary>
    public Scope Group(object state, Action<object> end) => BeginScope(ScopeKind.Group, state, end);

    /// <summary>The state of the innermost open <see cref="Group"/> holding a <typeparamref name="T"/>,
    /// or null outside any.</summary>
    public T? FindOpenGroup<T>() where T : class
    {
        for (var i = _scopes.Count - 1; i >= 0; i--)
        {
            if (_scopes[i].State is T state)
                return state;
        }
        return null;
    }

    private Scope BeginScope(ScopeKind kind, object? state = null, Action<object>? end = null)
    {
        _scopes.Add(new OpenScope(kind, state, end));
        return new Scope(this, _scopes.Count - 1);
    }

    private void EndScopes(int depth)
    {
        while (_scopes.Count > depth)
        {
            var top = _scopes.Count - 1;
            var scope = _scopes[top];
            _scopes.RemoveAt(top);
            EndScope(scope);
        }
    }

    private void EndScope(OpenScope scope)
    {
        switch (scope.Kind)
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
            case ScopeKind.Layer:
                EndLayer();
                break;
            case ScopeKind.StateScope:
                _stateOwners.Pop();
                _ids.Pop();
                break;
            case ScopeKind.Group:
                scope.End?.Invoke(scope.State!);
                break;
        }
    }

    private void EnsureScopesClosed()
    {
        if (_scopes.Count == 0) return;
        var kind = _scopes[_scopes.Count - 1].Kind;
        throw new InvalidOperationException($"An open {kind} scope was not disposed.");
    }

    private void EndScroll()
    {
        var scroll = _openScrolls.Pop();
        var state = scroll.State;
        var bar = state.Bar;
        var hit = state.Horizontal ? bar.Paint + bar.Inset : MathF.Max(bar.Hit, bar.Gutter);
        var crossBar = state.ScrollsX && state.ScrollsY && state.MaxX > 0f && state.MaxY > 0f ? bar.Gutter : 0f;
        if (state.ScrollsY && state.MaxY > 0f)
        {
            var viewport = state.ViewportY - crossBar;
            var length = ThumbLength(viewport, state.ContentY, bar.MinThumb);
            var start = (viewport - length) * MathF.Min(1f, state.OffsetY / state.MaxY);
            _tree[OpenFloating(scroll.ThumbY.Id, new Box { Width = hit, Height = length }, 1f, 0f, -hit, start, 1)].KeepsClip = true;
            Paint((ThumbColor(scroll.ThumbY), bar, false), PaintThumb);
            Close();
        }
        if (state.ScrollsX && state.MaxX > 0f && (!state.Horizontal || scroll.ThumbX.Held || PointerWithin(scroll.Id)))
        {
            var viewport = state.ViewportX - crossBar;
            var length = ThumbLength(viewport, state.ContentX, bar.MinThumb);
            var start = (viewport - length) * MathF.Min(1f, state.OffsetX / state.MaxX);
            _tree[OpenFloating(scroll.ThumbX.Id, new Box { Width = length, Height = hit }, 0f, 1f, start, -hit, 1)].KeepsClip = true;
            Paint((ThumbColor(scroll.ThumbX), bar, true), PaintThumb);
            Close();
        }
        Close();
        _frameScrolls.Add(scroll);
    }

    private bool PointerWithin(int id) => _previousRects.TryGetValue(id, out var rect) && rect.Contains(Input.Pointer);

    private static readonly Action<UiPainter, (uint Color, ScrollBar Bar, bool Horizontal)> PaintThumb = static (p, s) =>
    {
        var r = p.Rect;
        var thumb = s.Horizontal
            ? new UiRect(r.X, r.Bottom - s.Bar.Inset - s.Bar.Paint, r.Width, s.Bar.Paint)
            : new UiRect(r.Right - s.Bar.Inset - s.Bar.Paint, r.Y, s.Bar.Paint, r.Height);
        p.FillRect(thumb, s.Color, s.Bar.Paint / 2);
    };

    private uint ThumbColor(Interaction thumb) =>
        thumb.Held ? Theme.ScrollThumbHeld : thumb.Hovered ? Theme.ScrollThumbHovered : Theme.ScrollThumb;

    private void BeginScrolls()
    {
        _scopes.Clear();
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
                var padding = WithGutters(scroll.Padding, state, state.Bar);
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

    private static float Gutter(bool overflowing, ScrollBar bar) => overflowing ? bar.Gutter : 0f;

    private static float ThumbLength(float viewport, float content, float minThumb) =>
        content <= 0f ? 0f : MathF.Min(viewport, MathF.Max(minThumb, viewport * viewport / content));

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
            Background = box.Background ?? 0u,
            BorderColor = box.BorderColor ?? 0u,
            BorderWidth = box.BorderWidth ?? 0f,
            Radius = box.Radius ?? 0f,
            Shadow = box.Shadow ?? default,
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
