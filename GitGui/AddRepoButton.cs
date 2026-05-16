using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

public sealed class AddRepoButton : MultiChildView
{
    public AddRepoButton()
    {
        PreferredHeight = 30;

        var isHovered = new State<bool>(false);

        var background = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(6),
            Children =
            {
                new TextView
                {
                    Text = "+  Add Repository",
                    TextColor = DialogPalette.RowText,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                }
            }
        };
        background.BindBackgroundColor(isHovered,
            h => h ? DialogPalette.ButtonHover : DialogPalette.ButtonNormal);
        background.BindBorderColor(isHovered,
            h => BorderColorStyle.All(h ? DialogPalette.ButtonBorderHover : DialogPalette.ButtonBorder));
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IMessageBus>()?.Broadcast<AddRepoMessage>(),
            h => isHovered.Value = h));
    }
}
