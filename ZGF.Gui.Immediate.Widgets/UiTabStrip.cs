namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What the pointer did to one tab: <see cref="Selected"/> when it is pressed while not
/// selected, so the app selects it; <see cref="Closed"/> on its close button or a middle click.</summary>
public readonly record struct TabResult(bool Selected, bool Closed)
{
    /// <summary>The tab's element id, for <see cref="Ui.TryGetRect"/>.</summary>
    public int Id { get; init; }

    /// <summary>The tab is held and the pointer has moved far enough to drag it off: the cue to
    /// <see cref="Ui.BeginDrag"/>. Only for a tab recorded as draggable.</summary>
    public bool Dragged { get; init; }
}

/// <summary>An open <see cref="UiWidgets.TabStrip"/>: record each tab with <see cref="Tab"/>, then
/// dispose.</summary>
public readonly struct TabStrip : IDisposable
{
    private readonly Ui _ui;
    private readonly Ui.Scope _row;
    private readonly Ui.Scope _scroll;
    private readonly int _id;
    private readonly TabList _list;
    private readonly int _picked;
    private readonly string? _listTooltip;
    private readonly uint _background;

    internal TabStrip(Ui ui, Ui.Scope row, Ui.Scope scroll, int id, TabList list, int picked, string? listTooltip, uint background)
    {
        _ui = ui;
        _row = row;
        _scroll = scroll;
        _id = id;
        _list = list;
        _picked = picked;
        _listTooltip = listTooltip;
        _background = background;
    }

    /// <summary>One tab, between the theme's <see cref="UiTheme.TabMinWidth"/> and
    /// <see cref="UiTheme.TabMaxWidth"/> wide, its label shortened with an ellipsis past that, and
    /// <paramref name="tooltip"/> shown while the pointer rests on it. The label doubles as the id
    /// unless <paramref name="id"/> is given; scope tabs by document with <c>ui.Id</c> when labels
    /// repeat. A tab that becomes selected is scrolled into view. Tab focuses the tab and then its
    /// close button; Enter or Space selects or closes. A selected tab that is not
    /// <paramref name="current"/> is marked in a muted color instead of the accent. A tab picked
    /// from the strip's tab list returns <see cref="TabResult.Selected"/> on the next frame.
    /// <paramref name="icon"/>, when given, sits before the label.</summary>
    public TabResult Tab(string label, bool selected, bool closable = true, string? id = null, string? tooltip = null, bool draggable = false,
        bool current = true, UiIcon? icon = null)
    {
        var tab = UiWidgets.Tab(_ui, _id, label, selected, closable, id, tooltip, draggable, current, icon);
        _list.Add(new ListedTab(tab.Id, label, selected));
        if (tab.Id != _picked)
            return tab;
        return tab with { Selected = !selected };
    }

    public void Dispose()
    {
        UiWidgets.FadeClippedEdges(_ui, _id, _background);
        _scroll.Dispose();
        UiWidgets.TabListButton(_ui, _id, _list, _listTooltip);
        _row.Dispose();
    }
}

internal readonly record struct ListedTab(int Id, string Label, bool Selected);

/// <summary>The tabs a <see cref="TabStrip"/> recorded this frame, for its tab list, and the one
/// picked from that list, which the strip selects on the next frame only.</summary>
internal sealed class TabList
{
    private readonly List<ListedTab> _tabs = new();
    private int _picked;

    public List<ListedTab> Tabs => _tabs;

    /// <summary>Empties the list for this frame's tabs and hands over the tab picked last frame,
    /// or zero.</summary>
    public int Restart()
    {
        _tabs.Clear();
        var picked = _picked;
        _picked = 0;
        return picked;
    }

    public void Add(ListedTab tab) => _tabs.Add(tab);

    public void Pick(int tabId) => _picked = tabId;

    /// <summary>Changes whenever a tab opens, closes or moves, so an open list closes then.</summary>
    public int Revision()
    {
        var hash = new HashCode();
        foreach (var tab in _tabs)
            hash.Add(tab.Id);
        return hash.ToHashCode();
    }
}

public static partial class UiWidgets
{
    private sealed class TabState
    {
        public bool WasSelected;
    }

    internal static int TabListButtonId(int stripId) => HashCode.Combine(stripId, "list");

    /// <summary>A row of tabs attached to what is below them. When they overflow it scrolls sideways
    /// by wheel, with no bar, fades its tabs out at each edge with more past it, and ends in a
    /// button whose menu lists every tab to pick from, shown with <paramref name="listTooltip"/>.</summary>
    public static TabStrip TabStrip(this Ui ui, string id, Box box = default, string? listTooltip = null) =>
        ui.TabStrip(ui.GetId(id), box, listTooltip);

    public static TabStrip TabStrip(this Ui ui, int stripId, Box box = default, string? listTooltip = null)
    {
        var theme = ui.Theme;
        var background = box.Background ?? theme.TabStripBackground;
        var row = ui.Row(HashCode.Combine(stripId, "row"), new Box
        {
            Width = box.Width ?? Sizing.Grow(),
            Height = box.Height ?? Sizing.Fit(),
            Gap = 0f,
            AlignY = Align.Center,
            Background = background,
        });
        var scroll = ui.Scroll(stripId, ScrollAxes.Horizontal, box with
        {
            Direction = Direction.Horizontal,
            Width = Sizing.Grow(),
            Gap = box.Gap ?? 0f,
            AlignY = box.AlignY ?? Align.End,
            Background = null,
        }, showBars: false);
        var list = ui.GetState<TabList>(HashCode.Combine(stripId, "tabs"));
        var picked = list.Restart();
        return new TabStrip(ui, row, scroll, stripId, list, picked, listTooltip, background);
    }

    /// <summary>Fades the strip's tabs into its <paramref name="background"/> at each edge that has
    /// tabs scrolled past it.</summary>
    internal static void FadeClippedEdges(Ui ui, int stripId, uint background)
    {
        var offset = ui.ScrollOffset(stripId).X;
        var max = ui.ScrollMax(stripId).X;
        if (offset > 0f)
            FadeEdge(ui, background, Align.Start);
        if (offset < max)
            FadeEdge(ui, background, Align.End);
    }

    private static void FadeEdge(Ui ui, uint background, Align edge)
    {
        var width = ui.Theme.TabFadeWidth;
        var atEnd = edge == Align.End;
        var anchorX = atEnd ? 1f : 0f;
        var offsetX = atEnd ? -width : 0f;
        var box = new Box { Width = width, Height = Sizing.Grow() };
        using (ui.Float(anchorX, 0f, offsetX, 0f, box, z: 1, clipped: true))
            ui.Paint((background, edge), static (p, s) => p.FillFade(p.Rect, s.background, s.edge));
    }

    /// <summary>While the strip overflows, the button at its end whose menu lists every tab, the
    /// selected one marked. Picking the selected tab scrolls it into view.</summary>
    internal static void TabListButton(Ui ui, int stripId, TabList list, string? tooltip)
    {
        if (!ui.CanScroll(stripId)) return;
        var theme = ui.Theme;
        using (ui.Box(new Box { Padding = new Spacing(theme.GapTight, 0f) }))
        using (var menu = ui.DropdownButton(TabListButtonId(stripId), tooltip, revision: list.Revision()))
        {
            if (!menu.IsOpen) return;
            foreach (var tab in list.Tabs)
                ListTab(ui, stripId, list, tab);
        }
    }

    private static void ListTab(Ui ui, int stripId, TabList list, ListedTab tab)
    {
        if (!PickListedTab(ui, tab)) return;
        if (tab.Selected)
        {
            ui.ScrollIntoView(stripId, tab.Id);
            return;
        }
        list.Pick(tab.Id);
        ui.RequestUpdate();
    }

    private static bool PickListedTab(Ui ui, ListedTab tab)
    {
        using (ui.Id(tab.Id))
            return ui.MenuRadioItem(tab.Label, tab.Selected);
    }

    internal static TabResult Tab(Ui ui, int stripId, string label, bool selected, bool closable, string? id, string? tooltip, bool draggable, bool current,
        UiIcon? icon)
    {
        var theme = ui.Theme;
        var tab = Pressable(ui, ui.GetId(id ?? label), true, out var picked, out var tabRing, hand: false);
        var closeId = CloseButtonId(tab.Id);

        var state = ui.GetState<TabState>(HashCode.Combine(tab.Id, "tab"));
        if (!selected)
            state.WasSelected = false;
        else if (!state.WasSelected)
            state.WasSelected = ui.ScrollIntoView(stripId, tab.Id);

        var closed = closable && tab.ClickedWith(PointerButton.Middle);
        var hovered = tab.Hovered || ui.IsHovered(closeId);
        using (ui.Row(tab.Id, new Box
        {
            Width = Sizing.Fit(theme.TabMinWidth, theme.TabMaxWidth),
            Height = theme.TabHeight,
            Padding = new Spacing(theme.TabPaddingX, 0f, closable ? theme.GapTight : theme.TabPaddingX, 0f),
            Gap = theme.GapTight,
            Background = selected ? theme.TabActiveBackground : hovered ? theme.TabHoverBackground : 0u,
        }))
        {
            ui.Paint((selected ? current ? theme.Accent : theme.BorderStrong : 0u, theme.ActiveIndicatorWidth, theme.Separator, theme.SeparatorWidth), static (p, s) =>
            {
                var r = p.Rect;
                if (s.Item1 != 0)
                    p.FillRect(r with { Height = s.ActiveIndicatorWidth }, s.Item1);
                p.FillRect(new UiRect(r.Right - s.SeparatorWidth, r.Y, s.SeparatorWidth, r.Height), s.Separator);
            });
            if (tabRing) FocusRing(ui, 0f);
            if (icon != null)
                ui.Icon(icon.Value);
            ui.Text(label, color: selected ? theme.Text : theme.TextMuted, overflow: TextOverflow.Ellipsis);
            if (closable)
            {
                ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
                var idle = !selected && !hovered;
                if (ui.CloseButton(closeId, hideIdle: idle))
                    closed = true;
            }
        }
        if (tooltip != null)
            ui.Tooltip(tab.Id, tooltip);
        return new TabResult((tab.Pressed || picked) && !selected, closed) { Id = tab.Id, Dragged = draggable && ui.ShouldBeginDrag(tab) };
    }
}
