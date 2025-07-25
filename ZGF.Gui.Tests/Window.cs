using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class Window : View
{
    public string TitleText { get; }

    private readonly View _contents;
    public override IComponentCollection Children => _contents.Children;

    public Window(string titleText)
    {
        TitleText = titleText;
        Position = new RectF(200f, 200f, 640f, 500f);
        _contents = new View();

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
                _contents
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