using ZGF.Gui.Desktop.Components.VerticalScrollBar;
using ZGF.Gui.Desktop.Components.VirtualWidgetList;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Widgets;
using ZGF.Gui;

namespace ZGF.Gui.Desktop.Components.Controls;

/// <summary>
/// A virtualized list whose rows are real widgets, with a synced scrollbar — the widgets-first
/// counterpart to <see cref="ScrollArea"/> (which materializes <em>all</em> of its children). Only the
/// visible window of rows is built: a small pool of <typeparamref name="TRow"/> is recycled and rebound
/// as it scrolls, so a list of millions costs the same as a screenful.
///
/// Build an empty row once in <see cref="CreateRow"/> and push per-index data in <see cref="BindRow"/>;
/// because rows are real views, their own controllers handle hover/click (a per-row button lights up on
/// hover with no hit-testing here). Give the list a bounded height (a fill slot, fixed, or MaxHeight) for
/// scrolling to engage. <see cref="ItemCount"/> is fixed per build; rebuild to change it.
/// </summary>
public sealed record VirtualList<TRow> : Widget where TRow : View
{
    public required int ItemCount { get; init; }
    public required float RowHeight { get; init; }
    public required Func<TRow> CreateRow { get; init; }
    public required Action<TRow, int> BindRow { get; init; }

    /// <summary>Track/thumb colors for the scrollbar; unset, falls back to <see cref="ScrollBarStyle.Default"/>.</summary>
    public Prop<ScrollBarStyle> Style { get; init; } = ScrollBarStyle.Default;

    protected override IWidget Build(Context ctx)
    {
        var list = new VirtualWidgetListView<TRow>
        {
            ItemCount = ItemCount,
            RowHeight = RowHeight,
            CreateRow = CreateRow,
            BindRow = BindRow,
        };
        var thumb = new VerticalScrollBarThumbView();

        return new KbmInput
        {
            Controller = _ => new VirtualListScrollController<TRow>(list, thumb),
            Child = new BorderLayout
            {
                Center = new Raw { View = list },
                East = new ScrollBar { Thumb = thumb, Style = Style },
            },
        };
    }
}

/// <summary>
/// Keeps a <see cref="VirtualWidgetListView{TRow}"/> and a scrollbar thumb in sync (position and thumb
/// scale, both ways) and handles wheel scrolling for the area it is registered on. Modeled on
/// <c>ScrollAreaKbmController</c>; subscriptions follow the controller's mounted lifetime.
/// </summary>
internal sealed class VirtualListScrollController<TRow> : KeyboardMouseController, IDisposable where TRow : View
{
    private readonly VirtualWidgetListView<TRow> _list;
    private readonly VerticalScrollBarThumbView _thumb;

    public VirtualListScrollController(VirtualWidgetListView<TRow> list, VerticalScrollBarThumbView thumb)
    {
        _list = list;
        _thumb = thumb;
        _list.LayoutSynced += Resync;
        _thumb.ScrollPositionChanged += OnThumbScrolled;
    }

    public void Dispose()
    {
        _list.LayoutSynced -= Resync;
        _thumb.ScrollPositionChanged -= OnThumbScrolled;
    }

    // content -> thumb. Runs from every layout pass, so it doubles as the seam that keeps the thumb's
    // scale right as the item count or viewport changes. SetScrollPositionNormalized never echoes back,
    // so there is no feedback loop.
    private void Resync()
    {
        var viewport = _list.Position.Height;
        var content = _list.ContentHeight;
        if (content <= viewport || viewport <= 0f)
        {
            _thumb.Scale = 1f;
            _thumb.SetScrollPositionNormalized(0f);
            return;
        }
        _thumb.Scale = viewport / content;
        var max = content - viewport;
        _thumb.SetScrollPositionNormalized(max > 0f ? _list.ScrollY / max : 0f);
    }

    // thumb -> content
    private void OnThumbScrolled(float normalized)
    {
        var max = _list.ContentHeight - _list.Position.Height;
        if (max > 0f) _list.SetScrollY(normalized * max);
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;
        _list.OnWheel(e.DeltaX, e.DeltaY);
        e.Consume();
    }
}
