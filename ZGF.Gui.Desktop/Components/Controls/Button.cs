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
    public Prop<float> FontSize { get; init; }
    public PaddingStyle Padding { get; init; } = new() { Left = 10, Right = 10, Top = 4, Bottom = 4 };
    public Prop<bool> Enabled { get; init; } = true;
    public uint DisabledBackground { get; init; } = 0xFF374151;
    public uint DisabledTextColor { get; init; } = 0xFF9CA3AF;

    protected override IWidget Build(Context ctx)
    {
        var hovered = new State<bool>(false);
        var enabled = Enabled.ToReadable(ctx);
        return new KbmInput
        {
            OnClick = () =>
            {
                if (enabled.Value) OnClick();
            },
            OnHoverEnter = () => hovered.Value = true,
            OnHoverExit = () => hovered.Value = false,
            Child = new Box
            {
                Background = Prop.Bind(() => !enabled.Value ? DisabledBackground : hovered.Value ? HoverBackground : Background),
                BorderRadius = BorderRadiusStyle.All(4),
                Children =
                [
                    new Padding
                    {
                        Amount = Padding,
                        Children =
                        [
                            new Text
                            {
                                Value = Label,
                                Color = enabled.Bind(e => e ? TextColor : DisabledTextColor),
                                FontSize = FontSize,
                                HAlign = TextAlignment.Center,
                                VAlign = TextAlignment.Center,
                            },
                        ],
                    },
                ],
            },
        };
    }
}
