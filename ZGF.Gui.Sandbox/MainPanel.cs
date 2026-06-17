using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Sandbox;

public sealed record MainPanel : Widget
{
    public required string ModelImageId { get; init; }

    protected override View CreateView(Context ctx)
    {
        var canvas = ctx.Canvas;
        var input = ctx.Require<InputSystem>();
        var clipboard = ctx.Get<IClipboard>();

        var background = new RectView
        {
            BackgroundColor = 0xFF9C9CCE,
        };

        var w1 = new Window("About This Computer", input,
            new WindowTitleBar { Title = "About This Computer" }.BuildView(ctx));
        var (textInput, listView) = BuildWindowContent(w1, canvas, input);

        var modelView = new ImageView(canvas)
        {
            ImageId = ModelImageId,
        };
        var w3 = new Window("3D View", input,
            new WindowTitleBar { Title = "3D View" }.BuildView(ctx))
        {
            Children =
            {
                modelView
            }
        };

        textInput.UseController(input, () => new TextInputViewKbmController(textInput, input, clipboard));
        listView.UseController(input, () => new DefaultVerticalListViewKbmController(listView));

        View WithBringToFront(Window window) => new KbmInput
        {
            OnMouseButton = (ref MouseButtonEvent e) =>
            {
                if (e.Phase == EventPhase.Capturing && e.State == InputState.Pressed)
                    window.BringToFront();
            },
            Child = new Raw { View = window },
        }.BuildView(ctx);

        return new ContainerView
        {
            Children =
            {
                background,
                WithBringToFront(w1),
                WithBringToFront(w3),
            }
        };
    }

    private static (TextInputView, VerticalListView) BuildWindowContent(Window window, ICanvas canvas, InputSystem input)
    {
        var scrollBar = new RectView
        {
            BackgroundColor = 0xFFEFEFEF,
        };

        var scrollBarContainer = new RectView
        {
            Width = 14f,
            BackgroundColor = 0xFF000000,
            Children =
            {
                new PaddingView
                {
                    Padding = new PaddingStyle
                    {
                        Left = 1,
                        Top = 1,
                        Bottom = 15
                    },
                    Children =
                    {
                        scrollBar
                    }
                }
            }
        };

        var progress = new RectView
        {
            BackgroundColor = 0xFFEFEFEF,
            BorderSize = new BorderSizeStyle
            {
                Top = 1,
            },
            BorderColor = BorderColorStyle.All(0x000000)
        };

        var textInput = new TextInputView(canvas)
        {
            Height = 30f
        };

        var textField = new RectView
        {
            BackgroundColor = 0xFFEFEFEF,
            BorderColor = BorderColorStyle.All(0xFF252525),
            BorderSize = BorderSizeStyle.All(1),
            Children =
            {
                new PaddingView
                {
                    Padding = PaddingStyle.All(4),
                    Children =
                    {
                        textInput
                    }
                }
            }
        };

        var bottomSection = new BorderLayoutView
        {
            East = scrollBarContainer,
            Center = progress,
            South = textField,
            Height = 200
        };

        var content = new ColumnView
        {
            Id = "Test",
            Gap = 5
        };

        for (var i = 0; i < 100; i++)
        {
            content.Children.Add(new RectView
            {
                BackgroundColor = 0xFF9C9C9C,
                Children =
                {
                    new PaddingView
                    {
                        Padding = PaddingStyle.All(4),
                        Children =
                        {
                            new TextView(canvas)
                            {
                                Text = $"Element: {i+1}"
                            }
                        }
                    }
                }
            });
        }

        var listView = new VerticalListView(input)
        {
            Gap = 5,
            Children =
            {
                content,
                bottomSection
            }
        };

        window.Children.Add(listView);

        return (textInput, listView);
    }
}
