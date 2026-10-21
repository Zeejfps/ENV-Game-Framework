using ZGF.Desktop;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What the pointer did to one tab: <see cref="Selected"/> when it is pressed while not
/// selected, so the app selects it; <see cref="Closed"/> on its close button or a middle click.</summary>
public readonly record struct TabResult(bool Selected, bool Closed);

/// <summary>An open <see cref="UiWidgets.TabStrip"/>: record each tab with <see cref="Tab"/>, then
/// dispose.</summary>
public readonly struct TabStrip : IDisposable
{
    private readonly Ui _ui;
    private readonly Ui.Scope _scroll;
    private readonly int _id;

    internal TabStrip(Ui ui, Ui.Scope scroll, int id)
    {
        _ui = ui;
        _scroll = scroll;
        _id = id;
    }

    /// <summary>One tab, between the theme's <see cref="UiTheme.TabMinWidth"/> and
    /// <see cref="UiTheme.TabMaxWidth"/> wide, its label shortened with an ellipsis past that, and
    /// <paramref name="tooltip"/> shown while the pointer rests on it. The label doubles as the id
    /// unless <paramref name="id"/> is given; scope tabs by document with <c>ui.Id</c> when labels
    /// repeat. A tab that becomes selected is scrolled into view. Tab focuses the tab and then its
    /// close button; Enter or Space selects or closes.</summary>
    public TabResult Tab(string label, bool selected, bool closable = true, string? id = null, string? tooltip = null) =>
        UiWidgets.Tab(_ui, _id, label, selected, closable, id, tooltip);

    public void Dispose() => _scroll.Dispose();
}

public static partial class UiWidgets
{
    private sealed class TabState
    {
        public bool WasSelected;
    }

    /// <summary>A row of tabs attached to what is below them, which scrolls sideways, by wheel or
    /// its bar, when they overflow.</summary>
    public static TabStrip TabStrip(this Ui ui, string id, Box box = default)
    {
        var theme = ui.Theme;
        var stripId = ui.GetId(id);
        var scroll = ui.Scroll(stripId, box with
        {
            Direction = Direction.Horizontal,
            Gap = box.Gap ?? 0f,
            AlignY = box.AlignY ?? Align.End,
            Background = box.Background ?? theme.Surface,
        });
        return new TabStrip(ui, scroll, stripId);
    }

    internal static TabResult Tab(Ui ui, int stripId, string label, bool selected, bool closable, string? id, string? tooltip)
    {
        var theme = ui.Theme;
        var tab = Pressable(ui, ui.GetId(id ?? label), true, out var picked, out var tabRing, hand: false);
        var closeId = HashCode.Combine(tab.Id, "close");
        var close = Pressable(ui, closeId, closable, out var closeKeyed, out var closeRing);

        var state = ui.GetState<TabState>(HashCode.Combine(tab.Id, "tab"));
        if (!selected)
            state.WasSelected = false;
        else if (!state.WasSelected)
            state.WasSelected = ui.ScrollIntoView(stripId, tab.Id);

        var closed = closable && (close.Clicked || closeKeyed || tab.ClickedWith(PointerButton.Middle) || close.ClickedWith(PointerButton.Middle));
        var hovered = tab.Hovered || close.Hovered;
        using (ui.Row(tab.Id, new Box
        {
            Width = Sizing.Fit(theme.TabMinWidth, theme.TabMaxWidth),
            Height = theme.TabHeight,
            Padding = new Spacing(theme.TabPaddingX, 0f, closable ? theme.GapTight : theme.TabPaddingX, 0f),
            Gap = theme.GapTight,
            Background = selected ? theme.EditorBackground : hovered ? theme.RowHover : 0u,
        }))
        {
            ui.Paint((selected ? theme.Accent : 0u, theme.ActiveIndicatorWidth, theme.Separator, theme.SeparatorWidth), static (p, s) =>
            {
                var r = p.Rect;
                if (s.Item1 != 0)
                    p.FillRect(r with { Height = s.ActiveIndicatorWidth }, s.Item1);
                p.FillRect(new UiRect(r.Right - s.SeparatorWidth, r.Y, s.SeparatorWidth, r.Height), s.Separator);
            });
            if (tabRing) FocusRing(ui, 0f);
            ui.Text(id != null ? label : Visible(ui, tab.Id, label), color: selected ? theme.Text : theme.TextMuted, overflow: TextOverflow.Ellipsis);
            if (closable)
            {
                ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
                var glyph = close.Hovered || closeRing ? theme.Text : selected || hovered ? theme.TextMuted : 0u;
                using (ui.Box(closeId, new Box
                {
                    Width = theme.TabCloseSize,
                    Height = theme.TabCloseSize,
                    Radius = theme.CornerRadius,
                    Background = close.Held ? theme.ControlPressed : close.Hovered ? theme.ControlHovered : 0u,
                }))
                {
                    if (glyph != 0)
                        ui.Paint((glyph, theme.IconStrokeWidth), static (p, s) => DrawCross(p, s.glyph, s.IconStrokeWidth));
                    if (closeRing) FocusRing(ui, theme.CornerRadius);
                }
            }
        }
        if (tooltip != null)
            ui.Tooltip(tab.Id, tooltip);
        return new TabResult((tab.Pressed || picked) && !selected, closed);
    }

    private static void DrawCross(UiPainter p, uint color, float stroke)
    {
        var r = p.Rect;
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        const float h = 3.5f;
        p.Line(new(cx - h, cy - h), new(cx + h, cy + h), stroke, color);
        p.Line(new(cx - h, cy + h), new(cx + h, cy - h), stroke, color);
    }
}
