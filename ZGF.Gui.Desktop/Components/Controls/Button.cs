using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

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

    protected override View CreateView(Context ctx)
    {
        var label = new TextView(ctx.Canvas)
        {
            Text = Label,
            TextColor = TextColor,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        if (FontSize.IsSet) label.FontSize = FontSize;

        var button = new RectView
        {
            BackgroundColor = Background,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = Padding,
            Children = { label },
        };

        return new KbmInput
        {
            OnClick = OnClick,
            OnHoverEnter = () => button.BackgroundColor = HoverBackground,
            OnHoverExit = () => button.BackgroundColor = Background,
            Child = new Raw { View = button },
        }.BuildView(ctx);
    }
}
