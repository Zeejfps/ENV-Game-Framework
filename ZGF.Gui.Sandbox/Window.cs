using ZGF.Geometry;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Sandbox;

public sealed class Window : View
{
    public string TitleText { get; }

    private readonly ContainerView _contents;
    public new ChildrenCollection Children => _contents.Children;

    public Window(string titleText, InputSystem input, View titleBar)
    {
        TitleText = titleText;
        Position = new RectF(200f, 200f, 640f, 500f);
        _contents = new ContainerView();

        var leftBorder = new RectView
        {
            Width = 4f,
            BackgroundColor = 0xFFCECECE,
            BorderColor = new BorderColorStyle
            {
                Left = 0xFFFFFFFF,
            },
            BorderSize = new BorderSizeStyle
            {
                Left = 1
            }
        };
        
        var rightBorder = new RectView
        {
            Width = 4f,
            BackgroundColor = 0xFFCECECE,
            BorderColor = new BorderColorStyle
            {
                Right = 0xFF9C9C9C,
            },
            BorderSize = new BorderSizeStyle
            {
                Right = 1
            }
        };
        
        var bottomBorder = new RectView
        {
            Height = 4f,
            BackgroundColor = 0xFFCECECE,
            BorderColor = new BorderColorStyle
            {
                Bottom = 0xFF9C9C9C,
                Right = 0xFF9C9C9C,
                Left = 0xFFFFFFFF
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
            BackgroundColor = 0xFF000000,
            Padding = PaddingStyle.All(1),
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0xFF9C9C9C, Top = 0xFF9C9C9C,
                Right = 0xFFFFFFFF, Bottom = 0xFFFFFFFF
            },
            Children =
            {
                _contents
            }
        };
        
        var borderLayout = new BorderLayoutView
        {
            North = titleBar,
            West = leftBorder,
            Center = contentOutline,
            East = rightBorder,
            South = bottomBorder
        };
        
        var outline = new RectView
        {
            BackgroundColor = 0xFF000000,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = BorderColorStyle.All(0xFF000000),
            Children =
            {
                borderLayout
            }
        };
        AddChildToSelf(outline);

        titleBar.UseController(input, () => new WindowTitleBarDefaultKbmController(this, input));
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