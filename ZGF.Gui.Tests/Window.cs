using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class Window : View
{
    public string TitleText { get; }
    
    public Window(string titleText)
    {
        TitleText = titleText;
        Position = new RectF(200f, 200f, 440f, 400f);

        var titlePanel = new WindowTitleBarView(titleText);
        titlePanel.Controller = new WindowTitleBarDefaultKbmController(this, titlePanel);

        var leftBorder = new RectView
        {
            PreferredWidth = 4f,
            BackgroundColor = 0xCECECE,
            BorderColor = new BorderColorStyle
            {
                Left = 0xFFFFFF,
            },
            BorderSize = new BorderSizeStyle
            {
                Left = 1
            }
        };
        
        var rightBorder = new RectView
        {
            PreferredWidth = 4f,
            BackgroundColor = 0xCECECE,
            BorderColor = new BorderColorStyle
            {
                Right = 0x9C9C9C,
            },
            BorderSize = new BorderSizeStyle
            {
                Right = 1
            }
        };
        
        var bottomBorder = new RectView
        {
            PreferredHeight = 4f,
            BackgroundColor = 0xCECECE,
            BorderColor = new BorderColorStyle
            {
                Bottom = 0x9C9C9C,
                Right = 0x9C9C9C,
                Left = 0xFFFFFF
            },
            BorderSize = new BorderSizeStyle
            {
                Bottom = 1,
                Right = 1,
                Left = 1,
            }
        };
        
        var scrollBar = new RectView
        {
            BackgroundColor = 0xEFEFEF,
        };
        
        var scrollBarContainer = new RectView
        {
            PreferredWidth = 14f,
            BackgroundColor = 0x000000,
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
            BackgroundColor = 0xEFEFEF,
            BorderSize = new BorderSizeStyle
            {
                Top = 1,
            },
            BorderColor = BorderColorStyle.All(0x000000)
        };

        var textInput = new TextInput
        {
            PreferredHeight = 30f
        };
        textInput.Controller = new TextInputDefaultKbmController(textInput);

        var textField = new RectView
        {
            BackgroundColor = 0xEFEFEF,
            BorderColor = BorderColorStyle.All(0x252525),
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
        
        // var content = new RectView
        // {
        //     PreferredHeight = 400f,
        //     BackgroundColor = 0xFF44FF,
        //     BorderSize = BorderSizeStyle.All(1),
        //     BorderColor = BorderColorStyle.All(0x0000FF)
        // };

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
                BackgroundColor = 0x9C9C9C,
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
        
        var contentOutline = new RectView
        {
            Padding = new PaddingStyle
            {
                Bottom = 5
            },
            StyleClasses =
            {
                "inset_panel"
            },
            Children =
            {
                listView
            }
        };
        
        var borderLayout = new BorderLayoutView
        {
            North = titlePanel,
            West = leftBorder,
            Center = contentOutline,
            East = rightBorder,
            South = bottomBorder
        };
        
        var outline = new RectView
        {
            BackgroundColor = 0x000000,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = BorderColorStyle.All(0x000000),
            Children =
            {
                borderLayout
            }
        };
        AddChildToSelf(outline);

        // var windowResizer = new WindowResizer();
        // windowResizer.Controller = new WindowResizerDefaultKbmController(this, windowResizer);
        // AddChildToSelf(windowResizer);
    }

    protected override void OnLayoutSelf()
    {
        var left = Position.Left;
        if (left < LeftConstraint)
        {
            left = LeftConstraint;
        }

        var bottom = Position.Bottom;
        if (bottom < BottomConstraint)
        {
            bottom = BottomConstraint;
        }

        var top = Position.Top;
        if (top > TopConstraint)
        {
            var delta = top - TopConstraint;
            bottom -= delta;
        }
        
        var right = Position.Right;
        if (right > RightConstraint)
        {
            var delta = right - RightConstraint;
            left -= delta;
        }
        
        Position = Position with { Left = left, Bottom = bottom};
    }

    public void Move(float dx, float dy)
    {
        Position = Position with { Left = Position.Left + dx, Bottom = Position.Bottom + dy };
    }

    public void BringToFront()
    {
        Parent?.BringToFront(this);
    }

    public override string ToString()
    {
        return $"Window - {TitleText} - {ZIndex}";
    }
}