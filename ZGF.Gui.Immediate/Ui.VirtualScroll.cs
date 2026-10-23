namespace ZGF.Gui.Immediate;

/// <summary>A row of a virtual scroll that is <see cref="Height"/> tall rather than the row height,
/// as one showing details under it.</summary>
public readonly record struct TallRow(int Index, float Height);

public abstract partial class Ui
{
    private const int VirtualOverscan = 1;

    /// <summary>The rows of a <see cref="VirtualScroll(int, int, float, Box)"/> to record this
    /// frame: <see cref="First"/> up to but not including <see cref="End"/>.</summary>
    public readonly struct VirtualList : IDisposable
    {
        private readonly Ui _ui;
        private readonly Scope _scroll;
        private readonly ScrollState _state;
        private readonly float _rowHeight;
        private readonly float _gap;
        private readonly float _top;
        private readonly IReadOnlyList<TallRow>? _tall;

        internal VirtualList(Ui ui, Scope scroll, ScrollState state, int count, int first, int end, float rowHeight, float gap, float top,
            IReadOnlyList<TallRow>? tall)
        {
            _ui = ui;
            _scroll = scroll;
            _state = state;
            Count = count;
            First = first;
            End = end;
            _rowHeight = rowHeight;
            _gap = gap;
            _top = top;
            _tall = tall;
        }

        public int Count { get; }
        public int First { get; }
        public int End { get; }

        /// <summary>How far the list is scrolled, in points from the top of its content.</summary>
        public float Offset => _state.OffsetY;

        /// <summary>How far the list is scrolled sideways, for a list whose rows scroll both ways.</summary>
        public float OffsetX => _state.OffsetX;

        /// <summary>The height rows show in as last laid out, less a horizontal bar's gutter; zero
        /// until it has been laid out once.</summary>
        public float Viewport => VisibleHeight(_state);

        /// <summary>Scrolls to <paramref name="offset"/> points from the top of the content, from
        /// the next frame, clamped to the content then.</summary>
        public void ScrollTo(float offset)
        {
            offset = MathF.Max(0f, offset);
            if (offset == _state.OffsetY) return;
            _state.OffsetY = offset;
            _ui.RequestFrame();
        }

        /// <summary>Where row <paramref name="index"/> starts, in points from the top of the content.</summary>
        public float TopOf(int index) => _top + RowTop(index, _rowHeight, _gap, _tall);

        /// <summary>Scrolls the least distance that shows row <paramref name="index"/> whole, from the
        /// next frame, or from the first one after the list has been laid out and its viewport is known.</summary>
        public void ScrollIntoView(int index)
        {
            if (index < 0 || index >= Count) return;
            _state.RevealRow = index;
            if (Reveal(_state, Count, _rowHeight, _gap, _top, _tall))
                _ui.RequestFrame();
        }

        public void Dispose()
        {
            if (Count > End)
                _ui.Leaf(new Box { Width = 0f, Height = RowTop(Count, _rowHeight, _gap, _tall) - RowTop(End, _rowHeight, _gap, _tall) - _gap });
            _scroll.Dispose();
        }
    }

    public VirtualList VirtualScroll(string id, int count, float rowHeight, Box box = default) =>
        VirtualScroll(GetId(id), count, rowHeight, box);

    public VirtualList VirtualScroll(string id, int count, float rowHeight, ref float offset, Box box = default) =>
        VirtualScroll(GetId(id), count, rowHeight, ref offset, box);

    public VirtualList VirtualScroll(string id, int count, float rowHeight, ref float offsetX, ref float offsetY, Box box = default) =>
        VirtualScroll(GetId(id), count, rowHeight, ref offsetX, ref offsetY, box);

    /// <summary>A <see cref="VirtualScroll(int, int, float, Box)"/> whose offset the app owns, as
    /// <c>SplitPane</c>'s size is: a changed <paramref name="offset"/> scrolls the list, and the
    /// wheel, the bar and <see cref="VirtualList.ScrollTo"/> write it back — so several documents
    /// shown in turn by one list each keep their own place.</summary>
    /// <remarks>Rows in <paramref name="tall"/>, in order of index, are as tall as it says; record each
    /// at that height.</remarks>
    public VirtualList VirtualScroll(int id, int count, float rowHeight, ref float offset, Box box = default, IReadOnlyList<TallRow>? tall = null)
    {
        var state = GetState<ScrollState>(id);
        if (offset != state.Synced)
            state.OffsetY = MathF.Max(0f, offset);
        var list = VirtualScroll(id, ScrollAxes.Vertical, count, rowHeight, box, tall);
        offset = state.Synced = state.OffsetY;
        return list;
    }

    /// <summary>A <see cref="VirtualScroll(int, int, float, ref float, Box)"/> whose rows also scroll
    /// sideways when wider than it, as a code view's lines do: size rows at least as wide as the
    /// widest. Both offsets are the app's; each axis reserves its bar's gutter only while it
    /// overflows, so the last row and the last column are never under a bar.</summary>
    /// <remarks>Rows in <paramref name="tall"/>, in order of index, are as tall as it says; record each
    /// at that height.</remarks>
    public VirtualList VirtualScroll(int id, int count, float rowHeight, ref float offsetX, ref float offsetY, Box box = default,
        IReadOnlyList<TallRow>? tall = null)
    {
        var state = GetState<ScrollState>(id);
        if (offsetX != state.SyncedX)
            state.OffsetX = MathF.Max(0f, offsetX);
        if (offsetY != state.Synced)
            state.OffsetY = MathF.Max(0f, offsetY);
        var list = VirtualScroll(id, ScrollAxes.Both, count, rowHeight, box, tall);
        offsetX = state.SyncedX = state.OffsetX;
        offsetY = state.Synced = state.OffsetY;
        return list;
    }

    /// <summary>
    /// A <see cref="Scroll(int, Box)"/> over <paramref name="count"/> rows of which only those in
    /// view are recorded: record rows <see cref="VirtualList.First"/> to
    /// <see cref="VirtualList.End"/>, each one element exactly <paramref name="rowHeight"/> tall,
    /// touching unless <paramref name="box"/> gives a gap. Rows out of view are stood in for by
    /// spacers, so the scrollbar still spans them all. Scope row ids by item, not index, so their
    /// state follows the item.
    /// </summary>
    public VirtualList VirtualScroll(int id, int count, float rowHeight, Box box = default) =>
        VirtualScroll(id, ScrollAxes.Vertical, count, rowHeight, box);

    private VirtualList VirtualScroll(int id, ScrollAxes axes, int count, float rowHeight, Box box, IReadOnlyList<TallRow>? tall = null)
    {
        if (tall is { Count: 0 })
            tall = null;
        var gap = box.Gap ?? 0f;
        var scroll = Scroll(id, axes, box with { Direction = Direction.Vertical, Gap = gap });
        var state = GetState<ScrollState>(id);

        var stride = rowHeight + gap;
        var top = box.Padding?.Top ?? 0f;
        var viewportY = VisibleHeight(state);
        if (viewportY > 0f)
        {
            Reveal(state, count, rowHeight, gap, top, tall);
            var content = top + (box.Padding?.Bottom ?? 0f) + MathF.Max(0f, RowTop(count, rowHeight, gap, tall) - gap);
            state.OffsetY = Math.Clamp(state.OffsetY, 0f, MathF.Max(0f, content - viewportY));
            _tree[_openScrolls.Peek().Element].ScrollY = state.OffsetY;
        }
        var viewport = viewportY > 0f ? viewportY : Height;
        int first, end;
        if (tall == null)
        {
            first = Math.Clamp((int)MathF.Floor((state.OffsetY - top) / stride) - VirtualOverscan, 0, count);
            end = Math.Clamp(first + (int)MathF.Ceiling(viewport / stride) + 1 + VirtualOverscan * 2, first, count);
        }
        else
        {
            first = Math.Clamp(RowAt(state.OffsetY - top, rowHeight, gap, tall) - VirtualOverscan, 0, count);
            end = Math.Clamp(RowAt(state.OffsetY - top + viewport, rowHeight, gap, tall) + 1 + VirtualOverscan, first, count);
        }

        if (first > 0)
            Leaf(new Box { Width = 0f, Height = RowTop(first, rowHeight, gap, tall) - gap });
        return new VirtualList(this, scroll, state, count, first, end, rowHeight, gap, top, tall);
    }

    /// <summary>Scrolls the least distance that shows <see cref="ScrollState.RevealRow"/> whole once
    /// the viewport is known, and forgets it. Returns whether the offset changed.</summary>
    private static bool Reveal(ScrollState state, int count, float rowHeight, float gap, float top, IReadOnlyList<TallRow>? tall)
    {
        var index = state.RevealRow;
        if (index < 0) return false;
        var viewport = VisibleHeight(state);
        if (viewport <= 0f) return false;
        state.RevealRow = -1;
        if (index >= count) return false;
        var rowTop = top + RowTop(index, rowHeight, gap, tall);
        var rowBottom = rowTop + RowSize(index, rowHeight, tall);
        var offset = state.OffsetY;
        if (rowTop < offset)
            offset = rowTop;
        else if (rowBottom > offset + viewport)
            offset = rowBottom - viewport;
        if (offset == state.OffsetY) return false;
        state.OffsetY = offset;
        return true;
    }

    /// <summary>Scrolls the <see cref="VirtualScroll(int, int, float, Box)"/> with <paramref name="id"/>
    /// the least distance that shows row <paramref name="index"/> whole, from the next frame it is
    /// recorded in with its viewport known.</summary>
    public void ScrollRowIntoView(int id, int index)
    {
        if (index < 0) return;
        GetState<ScrollState>(id).RevealRow = index;
        RequestFrame();
    }

    /// <summary>How far row <paramref name="index"/> starts below the first.</summary>
    private static float RowTop(int index, float rowHeight, float gap, IReadOnlyList<TallRow>? tall)
    {
        var top = index * (rowHeight + gap);
        if (tall == null) return top;
        foreach (var row in tall)
        {
            if (row.Index >= index) break;
            top += row.Height - rowHeight;
        }
        return top;
    }

    private static float RowSize(int index, float rowHeight, IReadOnlyList<TallRow>? tall)
    {
        if (tall == null) return rowHeight;
        foreach (var row in tall)
        {
            if (row.Index == index) return row.Height;
            if (row.Index > index) break;
        }
        return rowHeight;
    }

    /// <summary>The row at <paramref name="offset"/> below the first row's top.</summary>
    private static int RowAt(float offset, float rowHeight, float gap, IReadOnlyList<TallRow> tall)
    {
        var stride = rowHeight + gap;
        var extra = 0f;
        foreach (var row in tall)
        {
            var rowTop = row.Index * stride + extra;
            if (offset < rowTop) break;
            if (offset < rowTop + row.Height + gap) return row.Index;
            extra += row.Height - rowHeight;
        }
        return (int)MathF.Floor((offset - extra) / stride);
    }

    private static float VisibleHeight(ScrollState state) =>
        state.ViewportY <= 0f ? 0f : MathF.Max(0f, state.ViewportY - Gutter(state.ScrollsX && state.MaxX > 0f, state.Bar));
}
