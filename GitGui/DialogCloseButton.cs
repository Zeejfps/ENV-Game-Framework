using ZGF.Gui;
using ZGF.Gui.Bindings;

namespace GitGui;

public sealed class DialogCloseButton : HoverableButton
{
    public DialogCloseButton(Action onClick, string? tooltip = "Close")
        : base(onClick, tooltip)
    {
        PreferredWidth = 28;
        PreferredHeight = 28;

        var label = new TextView
        {
            Text = LucideIcons.X,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        label.BindTextColor(IsHovered,
            h => h ? DialogPalette.CloseTextHover : DialogPalette.CloseTextNormal);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { label }
        };
        background.BindBackgroundColor(IsHovered,
            h => h ? DialogPalette.CloseHover : DialogPalette.CloseNormal);

        SetBackground(background);
    }
}
