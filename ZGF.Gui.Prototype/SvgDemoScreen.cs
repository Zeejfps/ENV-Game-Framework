using ZGF.Gui;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Prototype;

/// <summary>
/// SVG widget demo: intrinsic-size icons, theme recoloring via currentColor, GPU tinting,
/// and an animated resize that exercises the settle-debounced re-rasterization.
/// Run with: dotnet run --project ZGF.Gui.Prototype -- svg
/// </summary>
public sealed record SvgDemoScreen : Widget
{
    private static readonly string[] IconRow =
    [
        "Assets/Icons/feather-heart.svg",
        "Assets/Icons/feather-settings.svg",
        "Assets/Icons/feather-github.svg",
        "Assets/Icons/feather-zap.svg",
        "Assets/Icons/feather-star.svg",
        "Assets/Icons/tabler-flame.svg",
        "Assets/Icons/material-home.svg",
        "Assets/Icons/synthetic-evenodd.svg",
        "Assets/Icons/google-g.svg",
    ];

    protected override IWidget Build(Context ctx)
    {
        var ticker = ctx.Require<IFrameTicker>();
        var iconColor = new State<uint>(0xFFE0E0E0);
        var animatedSize = new State<float>(96f);
        var animating = new State<bool>(false);

        var time = 0f;
        Action<float> animate = null!;
        animate = dt =>
        {
            time += dt;
            animatedSize.Value = 96f + 64f * MathF.Sin(time * 1.5f);
        };

        return new Box
        {
            Background = 0xFF1E1E1E,
            Children =
            [
                new Padding
                {
                    Amount = PaddingStyle.All(16),
                    Children =
                    [
                        new Column
                        {
                            Gap = 14,
                            CrossAxis = CrossAxisAlignment.Stretch,
                            Children =
                            [
                                new Text { Value = "SVG Icons", FontSize = 20, Color = 0xFFE0E0E0 },
                                new Text
                                {
                                    Value = "Intrinsic size (24pt), recolored via currentColor:",
                                    FontSize = 13, Color = 0xFF9CA3AF,
                                },
                                new Row
                                {
                                    Gap = 12,
                                    Children =
                                    [
                                        .. IconRow.Select(IWidget (path) => new SvgImage { Source = path, Color = iconColor }),
                                    ],
                                },
                                new Text
                                {
                                    Value = "Same icons at 48pt — re-rastered, not scaled:",
                                    FontSize = 13, Color = 0xFF9CA3AF,
                                },
                                new Row
                                {
                                    Gap = 12,
                                    Children =
                                    [
                                        .. IconRow.Select(IWidget (path) => new SvgImage
                                        {
                                            Source = path, Color = iconColor, Width = 48, Height = 48,
                                        }),
                                    ],
                                },
                                new Row
                                {
                                    Gap = 8,
                                    Children =
                                    [
                                        new Button
                                        {
                                            Label = "Toggle color",
                                            FontSize = 13,
                                            OnClick = () => iconColor.Value =
                                                iconColor.Value == 0xFFE0E0E0 ? 0xFF34D399 : 0xFFE0E0E0,
                                        },
                                        new Button
                                        {
                                            Label = "Animate size",
                                            FontSize = 13,
                                            OnClick = () =>
                                            {
                                                if (animating.Value)
                                                    ticker.Remove(animate);
                                                else
                                                    ticker.Add(animate);
                                                animating.Value = !animating.Value;
                                            },
                                        },
                                    ],
                                },
                                new Text
                                {
                                    Value = "Animated resize (debounced re-raster on settle):",
                                    FontSize = 13, Color = 0xFF9CA3AF,
                                },
                                new Row
                                {
                                    Children =
                                    [
                                        new SvgImage
                                        {
                                            Source = "Assets/Icons/feather-settings.svg",
                                            Color = iconColor,
                                            Width = animatedSize,
                                            Height = animatedSize,
                                        },
                                        new SvgImage
                                        {
                                            Source = "Assets/Icons/feather-heart.svg",
                                            Color = 0xFFF87171,
                                            Tint = 0xFFFFFFFF,
                                            Width = animatedSize,
                                            Height = animatedSize,
                                        },
                                    ],
                                },
                                new Text
                                {
                                    Value = "Google sign-in button (multicolor fills):",
                                    FontSize = 13, Color = 0xFF9CA3AF,
                                },
                                new Row
                                {
                                    Children =
                                    [
                                        new Box
                                        {
                                            Background = 0xFFFFFFFF,
                                            BorderRadius = BorderRadiusStyle.All(4),
                                            Children =
                                            [
                                                new Padding
                                                {
                                                    Amount = new PaddingStyle { Left = 12, Right = 12, Top = 8, Bottom = 8 },
                                                    Children =
                                                    [
                                                        new Row
                                                        {
                                                            Gap = 10,
                                                            CrossAxis = CrossAxisAlignment.Center,
                                                            Children =
                                                            [
                                                                new SvgImage
                                                                {
                                                                    Source = "Assets/Icons/google-g.svg",
                                                                    Width = 20,
                                                                    Height = 20,
                                                                },
                                                                new Text
                                                                {
                                                                    Value = "Sign in with Google",
                                                                    FontSize = 14,
                                                                    Color = 0xFF3C4043,
                                                                },
                                                            ],
                                                        },
                                                    ],
                                                },
                                            ],
                                        },
                                        new Spacer(),
                                    ],
                                },
                                new Text
                                {
                                    Value = "GPU tint (no re-raster), rotated:",
                                    FontSize = 13, Color = 0xFF9CA3AF,
                                },
                                new Row
                                {
                                    Gap = 12,
                                    Children =
                                    [
                                        new SvgImage { Source = "Assets/Icons/feather-star.svg", Color = 0xFFFFFFFF, Tint = 0xFFFBBF24, Width = 40, Height = 40 },
                                        new SvgImage { Source = "Assets/Icons/feather-star.svg", Color = 0xFFFFFFFF, Tint = 0xFF60A5FA, Width = 40, Height = 40 },
                                        new SvgImage { Source = "Assets/Icons/feather-star.svg", Color = 0xFFFFFFFF, Tint = 0xFFF87171, Width = 40, Height = 40, Rotation = 0.5f },
                                    ],
                                },
                            ],
                        },
                    ],
                },
            ],
        };
    }
}
