namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    /// <summary>A row along the top of a pane, on <see cref="UiTheme.PaneToolbarBackground"/> so it
    /// joins the pane's selected tab and body: as tall as the main toolbar, <see cref="UiTheme.ToolbarHeight"/>,
    /// so controls in it sit with the same room around them; padded by <see cref="UiTheme.ShellPaddingX"/>
    /// and <see cref="UiTheme.ToolbarPaddingY"/>, its children centered. Anything set on
    /// <paramref name="box"/> wins. It draws no divider; a pane whose content scrolls under it adds a
    /// <see cref="Separator"/>.</summary>
    public static Ui.Scope PaneToolbar(this Ui ui, Box box = default)
    {
        var theme = ui.Theme;
        return ui.Row(box with
        {
            Height = box.Height ?? theme.ToolbarHeight,
            Padding = box.Padding ?? new Spacing(theme.ShellPaddingX, theme.ToolbarPaddingY),
            Gap = box.Gap ?? theme.GapTight,
            AlignY = box.AlignY ?? Align.Center,
            Background = box.Background ?? theme.PaneToolbarBackground,
        });
    }
}
