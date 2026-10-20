using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Retained.Sample.Controls;

public sealed record ProgressBar : Widget
{
    public required Prop<float> Fraction { get; init; }

    protected override IWidget Build(Context ctx)
    {
        var fraction = Fraction.ToReadable(ctx);
        return new Box
        {
            Height = 8,
            Background = Palette.Control,
            BorderRadius = BorderRadiusStyle.All(4),
            Children =
            [
                new Row
                {
                    CrossAxis = CrossAxisAlignment.Stretch,
                    Children =
                    [
                        new Grow
                        {
                            Factor = Prop.Bind(fraction),
                            Child = new Box { Background = Palette.Accent, BorderRadius = BorderRadiusStyle.All(4) },
                        },
                        new Grow { Factor = fraction.Bind(f => 1f - f), Child = Empty.Widget },
                    ],
                },
            ],
        };
    }
}
