using ZGF.Gui;
using ZGF.Gui.Bindings;

namespace GitGui;

public sealed class DialogButton : HoverableButton
{
    public DialogButton(string label, Action onClick) : base(onClick)
    {
        var labelView = new TextView
        {
            Text = label,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        labelView.BindTextColor(() => IsEnabled ? 0xFFFFFFFFu : DialogPalette.RowTextMissing);

        var background = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(6),
            Children = { labelView },
        };
        // Hover styling only when enabled — a disabled button shouldn't react to the pointer.
        DialogPalette.BindBorderedButtonChrome(background,
            () => IsEnabled && IsHovered);
        SetBackground(background);
    }
}
