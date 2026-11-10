namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    /// <summary>A row along the top of a pane, on <see cref="UiTheme.PaneToolbarBackground"/> so it
    /// joins the pane's selected tab and body: as tall as the main toolbar, <see cref="UiTheme.ToolbarHeight"/>,
    /// so controls in it sit with the same room around them; padded by <see cref="UiTheme.ShellPaddingX"/>
    /// and <see cref="UiTheme.ToolbarPaddingY"/>, its children centered. Anything set on
    /// <paramref name="box"/> wins. It draws no divider; a pane whose content scrolls under it adds a
    /// <see cref="Separator"/>. Given an <paramref name="id"/>, its rect can be read the next frame
    /// with <see cref="Ui.TryGetRect"/>, as a toolbar that moves what doesn't fit into a menu needs.</summary>
    public static Ui.Scope PaneToolbar(this Ui ui, Box box = default) => ui.PaneToolbar(0, box);

    public static Ui.Scope PaneToolbar(this Ui ui, int id, Box box = default)
    {
        var theme = ui.Theme;
        return ui.Row(id, box with
        {
            Height = box.Height ?? theme.ToolbarHeight,
            Padding = box.Padding ?? new Spacing(theme.ShellPaddingX, theme.ToolbarPaddingY),
            Gap = box.Gap ?? theme.GapTight,
            AlignY = box.AlignY ?? Align.Center,
            Background = box.Background ?? theme.PaneToolbarBackground,
        });
    }
}
