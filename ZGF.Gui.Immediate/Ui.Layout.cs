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

    private readonly record struct OpenScroll(int Element, int ThumbId, ScrollState State, Interaction Thumb, float PaddingRight);

    private enum ScrollSettle
    {
        None,
        Reposition,
        Relayout,
    }

    private readonly Dictionary<int, ScrollState> _scrolls = new();
    private readonly Stack<OpenScroll> _openScrolls = new();
    private readonly List<OpenScroll> _frameScrolls = new();
    private HashSet<int> _scrollIds = new();
    private HashSet<int> _previousScrollIds = new();
    private int _wheelTarget;

    /// <summary>A raw element. Everything else is built from these.</summary>
    public Scope Box(Box box = default, string? id = null)
    {
        Open(id == null ? 0 : GetId(id), box);
        return new Scope(this, ScopeKind.Element);
    }

    public Scope Column(Box box = default, string? id = null) => Box(box with
    {
        Direction = Direction.Vertical,
        Width = box.Width ?? Sizing.Grow(),
        Gap = box.Gap ?? Theme.Gap,
    }, id);

    public Scope Row(Box box = default, string? id = null) => Box(box with
    {
        Direction = Direction.Horizontal,
        Width = box.Width ?? Sizing.Grow(),
        Gap = box.Gap ?? Theme.Gap,
        AlignY = box.AlignY ?? Align.Center,
    }, id);

    /// <summary>A padded column on a raised background.</summary>
    public Scope Panel(Box box = default, string? id = null) => Column(box with
    {
        Padding = box.Padding ?? Theme.PanelPadding,
        Background = box.Background != 0 ? box.Background : Theme.Panel,
        BorderColor = box.BorderColor != 0 ? box.BorderColor : Theme.PanelBorder,
        BorderWidth = box.BorderWidth > 0f ? box.BorderWidth : 1f,
        Radius = box.Radius > 0f ? box.Radius : Theme.CornerRadius,
    }, id);

    /// <summary>An element taken out of the flow and placed over its parent: its top-left sits at
    /// (<paramref name="anchorX"/>, <paramref name="anchorY"/>) — fractions of the parent's rect —
    /// plus the offset. Drawn above the rest of the tree and unclipped; the basis for popups.</summary>
    public Scope Float(float anchorX, float anchorY, float offsetX = 0f, float offsetY = 0f, Box box = default, int z = 100, string? id = null)
    {
        OpenFloating(id == null ? 0 : GetId(id), box, anchorX, anchorY, offsetX, offsetY, z);
        return new Scope(this, ScopeKind.Element);
    }

    /// <summary>Pushes what follows to the end of the row or column, or leaves a fixed gap.</summary>
    public void Spacer(float? size = null)
    {
        var main = size is { } fixedSize ? Sizing.Fixed(fixedSize) : Sizing.Grow();
        if (ParentIsHorizontal)
            Leaf(0, new Box { Width = main, Height = 0f });
        else
            Leaf(0, new Box { Width = 0f, Height = main });
    }

    /// <summary>A vertically scrolling, clipped column. Fills the space it is given unless sized.</summary>
    public Scope Scroll(string id, Box box = default)
    {
        var scrollId = GetId(id);
        if (!_scrolls.TryGetValue(scrollId, out var state))
            _scrolls[scrollId] = state = new ScrollState();

        if (scrollId == _wheelTarget)
            state.Offset -= Input.ScrollY * Theme.ScrollWheelStep;

        var thumbId = HashCode.Combine(scrollId, "thumb");
        var thumb = Interact(thumbId);
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
        var element = Open(scrollId, box with
        {
            Direction = Direction.Vertical,
            Width = box.Width ?? Sizing.Grow(),
            Height = box.Height ?? Sizing.Grow(),
            Gap = box.Gap ?? Theme.Gap,
            Padding = padding with { Right = padding.Right + Gutter(state.Max > 0f) },
            Clip = true,
        });
        _tree[element].ScrollY = state.Offset;
        _scrollIds.Add(scrollId);
        _openScrolls.Push(new OpenScroll(element, thumbId, state, thumb, padding.Right));
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
            OpenFloating(scroll.ThumbId, new Box
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
}
