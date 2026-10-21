using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

internal static class AppThemes
{
    public static readonly UiTheme Dark = Compact(UiTheme.Dark);
    public static readonly UiTheme Light = Compact(UiTheme.Light);

    public const float BarHeight = 24f;

    private static UiTheme Compact(UiTheme theme) => theme with
    {
        FontSize = 13f,
        ScreenPadding = 0f,
        ControlHeight = 26f,
        ControlPaddingX = 10f,
        CornerRadius = 4f,
        RowHeight = 22f,
        MenuItemHeight = 24f,
    };
}

internal static class AppWidgets
{
    /// <summary>A button that stays highlighted while <paramref name="selected"/>, for one of a set
    /// of choices.</summary>
    public static bool ToggleButton(this Ui ui, string label, bool selected, string id)
    {
        var theme = ui.Theme;
        var i = ui.Interact(id);
        if (i.Hovered) ui.SetCursor(MouseCursor.Hand);
        var background = selected ? theme.Accent : i.Held ? theme.Pressed(theme.Control) : i.Hovered ? theme.Hovered(theme.Control) : theme.Control;
        ui.Text(label, new Box
        {
            Width = Sizing.Fit(36f),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.ControlPaddingX, 0f),
            Background = background,
            BorderColor = selected ? theme.Accent : theme.Border,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        }, color: selected ? theme.OnAccent : theme.Text, align: Align.Center, id: i.Id);
        return i.Clicked && !selected;
    }
}
