using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Retained.Sample.Controls;

public sealed record Checkbox : Widget
{
    public required Prop<bool> Checked { get; init; }
    public required Prop<string?> Label { get; init; }
    public Prop<uint> TextColor { get; init; } = Palette.Text;

    protected override IWidget Build(Context ctx)
    {
        var isChecked = Checked.ToReadable(ctx);
        var hovered = new State<bool>(false);
        return new KbmInput
        {
            OnClick = () => Checked.Write(!isChecked.Value),
            OnHoverEnter = () => hovered.Value = true,
            OnHoverExit = () => hovered.Value = false,
            Child = new Row
            {
                Gap = 8,
                Height = Palette.ControlHeight,
                CrossAxis = CrossAxisAlignment.Center,
                Children =
                [
                    new Box
                    {
                        Width = 18,
                        Height = 18,
                        BorderRadius = BorderRadiusStyle.All(4),
                        BorderSize = BorderSizeStyle.All(1),
                        Background = Prop.Bind(() => isChecked.Value ? Palette.Accent : hovered.Value ? Palette.ControlHovered : Palette.Control),
                        BorderColor = isChecked.Bind(c => BorderColorStyle.All(c ? Palette.Accent : Palette.ControlBorder)),
                        Children =
                        [
                            new Text
                            {
                                Value = "✓",
                                FontSize = 13,
                                Color = Palette.OnAccent,
                                HAlign = TextAlignment.Center,
                                VAlign = TextAlignment.Center,
                                Visible = Prop.Bind(isChecked),
                            },
                        ],
                    },
                    new Text { Value = Label, FontSize = Palette.FontSize, Color = TextColor },
                ],
            },
        };
    }
}
