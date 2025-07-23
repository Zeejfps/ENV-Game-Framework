using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class Window : Component
{
    public string TitleText { get; }
    
    public Window(string titleText)
    {
        TitleText = titleText;
        Position = new RectF(200f, 200f, 340f, 300f);
        
        var outline = new Panel
        {
            BackgroundColor = 0x000000,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = BorderColorStyle.All(0x000000),
        };

        var titlePanel = new WindowTitleBar(titleText);
        titlePanel.Controller = new WindowTitleBarDefaultKbmController(this, titlePanel);

        var leftBorder = new Panel
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
        
        var rightBorder = new Panel
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
        
        var bottomBorder = new Panel
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
        
        var contentOutline = new Panel
        {
            Padding = new PaddingStyle
            {
                Bottom = 5
            },
            StyleClasses =
            {
                "inset_panel"
            }
        };

        var content = new Panel
        {
            PreferredHeight = 400f,
            BackgroundColor = 0xFF44FF,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = BorderColorStyle.All(0x0000FF)
        };
        
        var scrollBarContainer = new Panel
        {
            PreferredWidth = 14f,
            BackgroundColor = 0x000000,
            Padding = new PaddingStyle
            {
                Left = 1,
                Top = 1,
                Bottom = 15
            },
        };
        var scrollBar = new Panel
        {
            BackgroundColor = 0xEFEFEF,
        };
        scrollBarContainer.Children.Add(scrollBar);

        var progress = new Panel
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

        var textField = new Panel
        {
            BackgroundColor = 0xEFEFEF,
            BorderColor = BorderColorStyle.All(0x252525),
            BorderSize = BorderSizeStyle.All(1),
            Padding = PaddingStyle.All(4)
        };
        textField.Children.Add(textInput);

        var bottomSection = new BorderLayout
        {
            East = scrollBarContainer,
            Center = progress,
            South = textField,
        };
        
        var columnLayout = new Column
        {
            Children =
            {
                content,
                bottomSection
            }
        };

        var test = new ScrollView
        {
            Content = columnLayout,
        };
        
        var borderLayout = new BorderLayout
        {
            North = titlePanel,
            West = leftBorder,
            Center = contentOutline,
            East = rightBorder,
            South = bottomBorder
        };
        
        contentOutline.Children.Add(test);
        outline.Children.Add(borderLayout);
        Add(outline);

        var windowResizer = new WindowResizer();
        windowResizer.Controller = new WindowResizerDefaultKbmController(this, windowResizer);
        Add(windowResizer);
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