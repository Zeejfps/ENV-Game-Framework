using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

internal static class AppStyles
{
    public static readonly UiStyle Dark = UiStyle.Dark with { WindowPadding = 0f, FontSize = 13f };
    public static readonly UiStyle Light = UiStyle.Light with { WindowPadding = 0f, FontSize = 13f };

    public static readonly WidgetTheme Widgets = new()
    {
        ControlHeight = 26f,
        ControlPaddingX = 10f,
        CornerRadius = 4f,
        RowHeight = 22f,
        MenuItemHeight = 24f,
    };

    public const float BarHeight = 24f;
}

internal static class AppWidgets
{
    /// <summary>A button that stays highlighted while <paramref name="selected"/>, for one of a set
    /// of choices.</summary>
    public static bool ToggleButton(this Ui ui, string label, bool selected, string id)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        var i = ui.Interact(id);
        if (i.Hovered) ui.SetCursor(MouseCursor.Hand);
        var background = selected ? style.Accent : i.Held ? style.Pressed(style.Control) : i.Hovered ? style.Hovered(style.Control) : style.Control;
        ui.Text(label, new Box
        {
            Width = Sizing.Fit(36f),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.ControlPaddingX, 0f),
            Background = background,
            BorderColor = selected ? style.Accent : style.Border,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        }, color: selected ? style.OnAccent : style.Text, align: Align.Center, id: i.Id);
        return i.Clicked && !selected;
    }
}
