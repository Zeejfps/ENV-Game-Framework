using GLFW;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class Center : MultiChildView
{
    public ImageView ModelView { get; }

    private readonly Window _w1;
    private readonly Window _w3;
    private readonly TextInputView _textInput;
    private readonly VerticalListView _listView;

    public Center()
    {
        var background = new RectView
        {
            BackgroundColor = 0xFF9C9CCE,
        };

        AddChildToSelf(background);

        _w1 = new Window("About This Computer");
        (_textInput, _listView) = BuildWindow(_w1);
        AddChildToSelf(_w1);

        ModelView = new ImageView();
        _w3 = new Window("3D View")
        {
            Children =
            {
                ModelView
            }
        };
        AddChildToSelf(_w3);

        _w1.Behaviors.Add(new WindowDefaultKbmController(_w1));
        _w3.Behaviors.Add(new WindowDefaultKbmController(_w3));
        _textInput.Behaviors.Add(new TextInputViewKbmController(_textInput));
        _listView.Behaviors.Add(new DefaultVerticalListViewKbmController(_listView));
    }

    private (TextInputView, VerticalListView) BuildWindow(Window window)
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

        return (textInput, listView);
    }
}