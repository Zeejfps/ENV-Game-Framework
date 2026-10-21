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

    /// <summary>One tab. The label doubles as the id unless <paramref name="id"/> is given; scope
    /// tabs by document with <c>ui.Id</c> when labels repeat. A tab that becomes selected is
    /// scrolled into view.</summary>
    public TabResult Tab(string label, bool selected, bool closable = true, string? id = null) =>
        UiWidgets.Tab(_ui, _id, label, selected, closable, id);

    public void Dispose() => _scroll.Dispose();
}

public static partial class UiWidgets
{
    private const float TabCloseSize = 18f;

    private sealed class TabState
    {
        public bool WasSelected;
    }

    /// <summary>A row of tabs that scrolls sideways, by wheel or its bar, when they overflow.</summary>
    public static TabStrip TabStrip(this Ui ui, string id, Box box = default)
    {
        var theme = ui.Theme;
        var stripId = ui.GetId(id);
        var scroll = ui.Scroll(stripId, box with
        {
            Direction = Direction.Horizontal,
            Gap = box.Gap ?? 1f,
            AlignY = box.AlignY ?? Align.End,
            Background = box.Background != 0 ? box.Background : theme.Surface,
        });
        return new TabStrip(ui, scroll, stripId);
    }

    internal static TabResult Tab(Ui ui, int stripId, string label, bool selected, bool closable, string? id)
    {
        var theme = ui.Theme;
        var tab = ui.Interact(id ?? label);
        var closeId = HashCode.Combine(tab.Id, "close");
        var close = closable ? ui.Interact(closeId) : Interaction.Inert(closeId);
        if (close.Hovered) ui.SetCursor(MouseCursor.Hand);

        var state = ui.GetState<TabState>(HashCode.Combine(tab.Id, "tab"));
        if (selected && !state.WasSelected)
            ui.ScrollIntoView(stripId, tab.Id);
        state.WasSelected = selected;

        var closed = closable && (close.Clicked || tab.ClickedWith(PointerButton.Middle) || close.ClickedWith(PointerButton.Middle));
        var hovered = tab.Hovered || close.Hovered;
        using (ui.Row(tab.Id, new Box
        {
            Width = Sizing.Fit(),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.ControlPaddingX, 0f, closable ? 6f : theme.ControlPaddingX, 0f),
            Gap = 6f,
            Background = selected ? theme.Background : hovered ? theme.Hovered(theme.Surface) : theme.Surface,
        }))
        {
            if (selected)
                ui.Paint(theme.Accent, static (p, color) => p.FillRect(p.Rect with { Height = 2f }, color));
            ui.Text(id != null ? label : Visible(ui, tab.Id, label), color: selected ? theme.Text : theme.TextMuted);
            if (closable)
            {
                var glyph = close.Hovered ? theme.Text : selected || hovered ? theme.TextMuted : 0u;
                using (ui.Box(closeId, new Box
                {
                    Width = TabCloseSize,
                    Height = TabCloseSize,
                    Radius = 4f,
                    Background = close.Held ? theme.Pressed(theme.Control) : close.Hovered ? theme.Control : 0u,
                }))
                {
                    if (glyph != 0)
                        ui.Paint(glyph, static (p, color) => DrawCross(p, color));
                }
            }
        }
        return new TabResult(tab.Pressed && !selected, closed);
    }

    private static void DrawCross(UiPainter p, uint color)
    {
        var r = p.Rect;
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        const float h = 3.5f;
        p.Line(new(cx - h, cy - h), new(cx + h, cy + h), 1.5f, color);
        p.Line(new(cx - h, cy + h), new(cx + h, cy - h), 1.5f, color);
    }
}
