namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    /// <summary>A row along the top of a pane, on <see cref="UiTheme.PaneToolbarBackground"/> so it
    /// joins the pane's selected tab and body: <see cref="UiTheme.PaneHeaderHeight"/> tall, padded by
    /// <see cref="UiTheme.ShellPaddingX"/>, its children centered. Anything set on
    /// <paramref name="box"/> wins. It draws no divider; a pane whose content scrolls under it adds a
    /// <see cref="Separator"/>.</summary>
    public static Ui.Scope PaneToolbar(this Ui ui, Box box = default)
    {
        var theme = ui.Theme;
        return ui.Row(box with
        {
            Height = box.Height ?? theme.PaneHeaderHeight,
            Padding = box.Padding ?? new Spacing(theme.ShellPaddingX, 0f),
            Gap = box.Gap ?? theme.GapTight,
            AlignY = box.AlignY ?? Align.Center,
            Background = box.Background ?? theme.PaneToolbarBackground,
        });
    }
}
