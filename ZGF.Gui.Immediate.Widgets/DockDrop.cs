using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>What releasing a panel or window dragged in a <see cref="UiWidgets.DockSpace"/> does where the pointer is.</summary>
public enum DockDropKind
{
    /// <summary>Nothing: the pointer is over no place to dock, or a floating window is being moved.</summary>
    None,

    /// <summary>Joins a group as one of its tabs.</summary>
    Tab,

    /// <summary>Splits a group, taking the <see cref="DockDrop.Edge"/> side of it.</summary>
    Split,

    /// <summary>Docks along the <see cref="DockDrop.Edge"/> of the whole dock space.</summary>
    Edge,

    /// <summary>Floats in a window of its own, as Ctrl held over the dock space asks.</summary>
    Float,
}

/// <summary>A drag in a <see cref="UiWidgets.DockSpace"/> as its <see cref="DockDropPainter"/> shows it: what is
/// dragged, where the pointer is, and what releasing it there does. Rects are in window coordinates, as
/// <see cref="UiPainter"/> draws in.</summary>
public readonly struct DockDrop
{
    internal DockDrop(DockDropKind kind, DockEdge edge, UiRect ghost, UiRect? caret, UiRect? tabSlot, DockTab tab, PointF pointer)
    {
        Kind = kind;
        Edge = edge;
        Ghost = ghost;
        Caret = caret;
        TabSlot = tabSlot;
        Tab = tab;
        Pointer = pointer;
    }

    public DockDropKind Kind { get; }

    /// <summary>The side taken, for <see cref="DockDropKind.Split"/> and <see cref="DockDropKind.Edge"/>;
    /// <see cref="DockEdge.Center"/> otherwise.</summary>
    public DockEdge Edge { get; }

    /// <summary>Where the dragged panel will be once released: for <see cref="DockDropKind.Tab"/>, the group's body
    /// below its header; empty for <see cref="DockDropKind.None"/>.</summary>
    public UiRect Ghost { get; }

    /// <summary>For <see cref="DockDropKind.Tab"/> over a tab strip, the line between the tabs it goes between.</summary>
    public UiRect? Caret { get; }

    /// <summary>For <see cref="DockDropKind.Tab"/>, the part of the group's header from where the panel's tab goes to
    /// the header's end, to show the tab there: after the group's last tab or its lone panel's title
    /// (<see cref="DockHeader.TitleId"/>), or where <see cref="Caret"/> is. Null for a group showing no header, whose
    /// <see cref="Ghost"/> is the whole group.</summary>
    public UiRect? TabSlot { get; }

    /// <summary>The dragged panel's tab; for a dragged window, its front panel's.</summary>
    public DockTab Tab { get; }

    public PointF Pointer { get; }

    /// <summary>What releasing does, in a few words: "Release to add as tab".</summary>
    public string Hint => Kind switch
    {
        DockDropKind.None => "",
        DockDropKind.Tab => "Release to add as tab",
        DockDropKind.Float => "Release to float",
        DockDropKind.Split => Edge switch
        {
            DockEdge.Left => "Release to split left",
            DockEdge.Top => "Release to split top",
            DockEdge.Right => "Release to split right",
            DockEdge.Bottom => "Release to split bottom",
            _ => "Release to add as tab",
        },
        DockDropKind.Edge => Edge switch
        {
            DockEdge.Left => "Release to dock at the left edge",
            DockEdge.Top => "Release to dock at the top edge",
            DockEdge.Right => "Release to dock at the right edge",
            DockEdge.Bottom => "Release to dock at the bottom edge",
            _ => "Release to add as tab",
        },
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null),
    };
}

/// <summary>Records how a drag in a dock space looks, over everything else in it, each frame of the drag. The stock
/// one is <see cref="UiWidgets.PaintDockDrop"/>.</summary>
public delegate void DockDropPainter(Ui ui, in DockDrop drop);

public static partial class UiWidgets
{
    private const float DropChipOffsetX = 16f;
    private const float DropChipOffsetY = 14f;
    private const byte DropGhostAlpha = 0xD8;
    private const byte DropTintAlpha = 0x22;

    /// <summary>The stock look of a drag in a dock space: a ghost of the panel where it will land, titled as its
    /// header is and saying what releasing does — joining a group, its tab in the group's header beside the others —
    /// and its name beside the pointer — for a painter of an app's own to fall back to.</summary>
    public static void PaintDockDrop(this Ui ui, in DockDrop drop)
    {
        var theme = ui.Theme;
        var title = drop.Tab.Title;
        var titleWidth = ui.MeasureText(title, family: drop.Tab.TitleFont);
        var chipHeight = theme.FontSize + theme.TooltipPaddingY * 2f;
        var chip = new UiRect(drop.Pointer.X + DropChipOffsetX, drop.Pointer.Y + DropChipOffsetY, titleWidth + theme.TooltipPaddingX * 2f,
            chipHeight);
        ui.Paint(new DropLook(theme, drop.Kind == DockDropKind.None ? null : drop.Ghost, drop.Caret, drop.Hint, title, drop.Tab.TitleFont,
            titleWidth, chip, drop.Pointer, drop.TabSlot), static (p, look) => look.Paint(p));
    }

    private readonly record struct DropLook(UiTheme Theme, UiRect? Ghost, UiRect? Caret, string Hint, string Title, string? Font, float TitleWidth,
        UiRect Chip, PointF Pointer, UiRect? TabSlot)
    {
        public void Paint(UiPainter p)
        {
            if (Ghost != null)
                PaintGhost(p, Ghost.Value, titled: TabSlot == null);
            if (TabSlot != null)
                PaintTab(p, TabSlot.Value);
            else if (Caret != null)
                p.FillRect(Caret.Value, Theme.Accent);

            // Below and right of the pointer, or across it from an edge of the dock space it would run past.
            var chip = Chip;
            if (chip.Right > p.Rect.Right)
                chip = chip with { X = Pointer.X - DropChipOffsetX - chip.Width };
            if (chip.Bottom > p.Rect.Bottom)
                chip = chip with { Y = Pointer.Y - DropChipOffsetY - chip.Height };
            p.FillRect(chip, Theme.SurfaceRaised, Theme.RadiusSmall, Theme.BorderStrong, Theme.BorderWidth);
            p.Text(chip with { X = chip.X + Theme.TooltipPaddingX, Width = chip.Width - Theme.TooltipPaddingX * 2f }, Title, Theme.Text,
                family: Font);
        }

        // The panel as it will look docked: a header with its title marked in the accent, unless it is to be titled by
        // a tab in a group's header, over its body, which says what releasing does. Too short for both, it is the body
        // alone.
        private void PaintGhost(UiPainter p, UiRect ghost, bool titled)
        {
            var t = Theme;
            p.FillRect(ghost, Faded(t.PaneBackground, DropGhostAlpha), t.RadiusSmall);
            p.FillRect(ghost, Faded(t.Accent, DropTintAlpha), t.RadiusSmall, t.Accent, t.BorderWidth);

            var body = ghost;
            if (titled && ghost.Height >= t.TabHeight * 3f)
            {
                var header = ghost with { Height = t.TabHeight };
                p.FillRect(new UiRect(header.X, header.Bottom - t.SeparatorWidth, header.Width, t.SeparatorWidth), t.Separator);
                var tabWidth = MathF.Min(TitleWidth + t.TabPaddingX * 2f, header.Width);
                p.Text(header with { X = header.X + t.TabPaddingX, Width = tabWidth - t.TabPaddingX * 2f }, Title, t.Text, family: Font);
                p.FillRect(new UiRect(header.X, header.Bottom - t.ActiveIndicatorWidth, tabWidth, t.ActiveIndicatorWidth), t.Accent);
                body = ghost with { Y = header.Bottom, Height = ghost.Height - t.TabHeight };
            }
            p.Text(body, Hint, t.TextMuted, align: Align.Center);
        }

        // The panel's tab in the group's header where it goes, in front as it will be once dropped, over whatever
        // tabs follow it there.
        private void PaintTab(UiPainter p, UiRect slot)
        {
            var t = Theme;
            var tab = slot with { Width = MathF.Min(TitleWidth + t.TabPaddingX * 2f, slot.Width) };
            p.FillRect(tab, t.TabStripBackground);
            p.FillRect(tab, Faded(t.Accent, DropTintAlpha));
            p.Text(tab with { X = tab.X + t.TabPaddingX, Width = MathF.Max(0f, tab.Width - t.TabPaddingX * 2f) }, Title, t.Text,
                family: Font);
            p.FillRect(new UiRect(tab.X, tab.Bottom - t.ActiveIndicatorWidth, tab.Width, t.ActiveIndicatorWidth), t.Accent);
        }

        private static uint Faded(uint color, byte alpha) => (color & 0x00FFFFFFu) | ((uint)alpha << 24);
    }
}
