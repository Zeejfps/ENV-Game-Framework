namespace ZGF.Gui.Immediate;

public sealed partial class Ui
{
    private const int VirtualOverscan = 1;

    /// <summary>The rows of a <see cref="VirtualScroll(int, int, float, Box)"/> to record this
    /// frame: <see cref="First"/> up to but not including <see cref="End"/>.</summary>
    public readonly struct VirtualList : IDisposable
    {
        private readonly Ui _ui;
        private readonly ScrollState _state;
        private readonly float _rowHeight;
        private readonly float _gap;
        private readonly float _top;

        internal VirtualList(Ui ui, ScrollState state, int count, int first, int end, float rowHeight, float gap, float top)
        {
            _ui = ui;
            _state = state;
            Count = count;
            First = first;
            End = end;
            _rowHeight = rowHeight;
            _gap = gap;
            _top = top;
        }

        public int Count { get; }
        public int First { get; }
        public int End { get; }

        /// <summary>Scrolls the least distance that shows row <paramref name="index"/> whole, from
        /// the next frame.</summary>
        public void ScrollIntoView(int index)
        {
            if (index < 0 || index >= Count) return;
            var top = _top + index * (_rowHeight + _gap);
            var bottom = top + _rowHeight;
            var offset = _state.Offset;
            if (top < offset)
                offset = top;
            else if (bottom > offset + _state.Viewport)
                offset = bottom - _state.Viewport;
            if (offset == _state.Offset) return;
            _state.Offset = offset;
            _ui.RequestFrame();
        }

        public void Dispose()
        {
            var remaining = Count - End;
            if (remaining > 0)
                _ui.Leaf(new Box { Width = 0f, Height = remaining * (_rowHeight + _gap) - _gap });
            _ui.EndScroll();
        }
    }

    public VirtualList VirtualScroll(string id, int count, float rowHeight, Box box = default) =>
        VirtualScroll(GetId(id), count, rowHeight, box);

    /// <summary>
    /// A <see cref="Scroll(int, Box)"/> over <paramref name="count"/> rows of which only those in
    /// view are recorded: record rows <see cref="VirtualList.First"/> to
    /// <see cref="VirtualList.End"/>, each one element exactly <paramref name="rowHeight"/> tall.
    /// Rows out of view are stood in for by spacers, so the scrollbar still spans them all. Scope
    /// row ids by item, not index, so their state follows the item.
    /// </summary>
    public VirtualList VirtualScroll(int id, int count, float rowHeight, Box box = default)
    {
        var gap = box.Gap ?? Style.Gap;
        Scroll(id, box with { Gap = gap });
        var state = GetState<ScrollState>(id);

        var stride = rowHeight + gap;
        var top = box.Padding?.Top ?? 0f;
        var viewport = state.Viewport > 0f ? state.Viewport : Height;
        var first = Math.Clamp((int)MathF.Floor((state.Offset - top) / stride) - VirtualOverscan, 0, count);
        var end = Math.Clamp(first + (int)MathF.Ceiling(viewport / stride) + 1 + VirtualOverscan * 2, first, count);

        if (first > 0)
            Leaf(new Box { Width = 0f, Height = first * stride - gap });
        return new VirtualList(this, state, count, first, end, rowHeight, gap, top);
    }
}
