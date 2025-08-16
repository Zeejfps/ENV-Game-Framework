using GLFW;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class Center : View
{
    public ImageView ModelView { get; }

    public Center()
    {
        var background = new RectView
        {
            BackgroundColor = 0xFF9C9CCE,
        };
        
        AddChildToSelf(background);

        var w1 = new Window("About This Computer");
        BuildWindow(w1);
        w1.Controller = new WindowDefaultKbmController(w1);
        AddChildToSelf(w1);

        ModelView = new ImageView();
        var w3 = new Window("3D View")
        {
            Children =
            {
                ModelView
            }
        };
        w3.Controller = new WindowDefaultKbmController(w3);
        AddChildToSelf(w3);
    }

    private void BuildWindow(Window window)
    {
        var scrollBar = new RectView
        {
            BackgroundColor = 0xFFEFEFEF,
        };

        var scrollBarContainer = new RectView
        {
            PreferredWidth = 14f,
            BackgroundColor = 0xFF000000,
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

        var textInput = new TextInputView
        {
            PreferredHeight = 30f
        };
        textInput.Controller = new TextInputViewKbmController(textInput);

        var textField = new RectView
        {
            BackgroundColor = 0xFFEFEFEF,
            BorderColor = BorderColorStyle.All(0xFF252525),
            BorderSize = BorderSizeStyle.All(1),
            Padding = PaddingStyle.All(4),
            Children =
            {
                textInput
            }
        };

        var bottomSection = new BorderLayoutView
        {
            East = scrollBarContainer,
            Center = progress,
            South = textField,
            PreferredHeight = 200
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
                Padding = PaddingStyle.All(4),
                BackgroundColor = 0xFF9C9C9C,
                Children =
                {
                    new TextView
                    {
                        Text = $"Element: {i+1}"
                    }
                }
            });
        }

        var listView = new VerticalListView
        {
            Gap = 5,
            Children =
            {
                content,
                bottomSection
            }
        };

        window.Children.Add(listView);
    }
}