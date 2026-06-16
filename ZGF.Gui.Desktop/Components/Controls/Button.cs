using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Components.Controls;

public sealed record Button : Widget
{
    public required string Label { get; init; }
    public required Action OnClick { get; init; }
    public uint Background { get; init; } = 0xFF3B82F6;
    public uint HoverBackground { get; init; } = 0xFF2563EB;
    public uint TextColor { get; init; } = 0xFFFFFFFF;
    public StyleValue<float> FontSize { get; init; }
    public PaddingStyle Padding { get; init; } = new() { Left = 10, Right = 10, Top = 4, Bottom = 4 };

    protected override IWidget Build(Context ctx)
    {
        var hovered = new State<bool>(false);
        return new KbmInput
        {
            OnClick = OnClick,
            OnHoverEnter = () => hovered.Value = true,
            OnHoverExit = () => hovered.Value = false,
            Child = new Box
            {
                Background = hovered.Map(h => h ? HoverBackground : Background),
                BorderRadius = BorderRadiusStyle.All(4),
                Padding = Padding,
                Children =
                [
                    new Text
                    {
                        Value = Label,
                        Color = TextColor,
                        FontSize = FontSize,
                        HAlign = TextAlignment.Center,
                        VAlign = TextAlignment.Center,
                    },
                ],
            },
        };
    }
}
