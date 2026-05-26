using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

public sealed class DialogCloseButton : HoverableButton
{
    public DialogCloseButton(Action onClick, string? tooltip = "Close")
        : base(onClick, tooltip)
    {
        PreferredWidth = 28;
        PreferredHeight = 28;

        // Theme-reactive color states. The Derived bindings below auto-track these and
        // hovered → either source change repaints.
        var normalText = new State<uint>(ThemePresets.Dark.Dialog.CloseTextNormal);
        var hoverText = new State<uint>(ThemePresets.Dark.Dialog.CloseTextHover);
        var normalBg = new State<uint>(ThemePresets.Dark.Dialog.CloseNormal);
        var hoverBg = new State<uint>(ThemePresets.Dark.Dialog.CloseHover);
        this.BindToTheme(t =>
        {
            normalText.Value = t.Dialog.CloseTextNormal;
            hoverText.Value = t.Dialog.CloseTextHover;
            normalBg.Value = t.Dialog.CloseNormal;
            hoverBg.Value = t.Dialog.CloseHover;
        });

        var label = new TextView
        {
            Text = LucideIcons.X,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        label.BindTextColor(() => IsHovered.Value ? hoverText.Value : normalText.Value);

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Children = { label }
        };
        background.BindBackgroundColor(() => IsHovered.Value ? hoverBg.Value : normalBg.Value);

        SetBackground(background);
    }
}
