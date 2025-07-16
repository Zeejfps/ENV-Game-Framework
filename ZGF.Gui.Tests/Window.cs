using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class Window : Component
{
    public Window()
    {
        Position = new RectF(200f, 200f, 240f, 200f);
        
        var outline = new Rect
        {
            Style =
            {
                BackgroundColor = 0x000000,
                BorderSize = BorderSizeStyle.All(1),
                BorderColor = BorderColorStyle.All(0x000000),
            }
        };

        var titlePanel = new WindowTitleBar(this);

        var leftBorder = new Rect
        {
            Constraints = new RectF
            {
                Width = 4f,
            },
            Style =
            {
                BackgroundColor = 0xCECECE,
                BorderColor = new BorderColorStyle
                {
                    Left = 0xFFFFFF,
                },
                BorderSize = new BorderSizeStyle
                {
                    Left = 1
                }
            }
        };
        
        var rightBorder = new Rect
        {
            Constraints = new RectF
            {
                Width = 4f,
            },
            Style =
            {
                BackgroundColor = 0xCECECE,
                BorderColor = new BorderColorStyle
                {
                    Right = 0x9C9C9C,
                },
                BorderSize = new BorderSizeStyle
                {
                    Right = 1
                }
            }
        };
        
        var bottomBorder = new Rect
        {
            Constraints = new RectF
            {
                Height = 4f,
            },
            Style =
            {
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
            }
        };
        
        var contentOutline = new Rect
        {
            Style =
            {
                Padding = new PaddingStyle
                {
                    Bottom = 5
                }
            }
        };
        contentOutline.AddStyleClass("inset_panel");

        var content = new Rect
        {
            Style =
            {
                BackgroundColor = 0xFF00FF,
                BorderSize = BorderSizeStyle.All(1),
                BorderColor = BorderColorStyle.All(0x00FFFF)
            }
        };

        var columnLayout = new ColumnLayout();
        columnLayout.Add(content);
        
        var scrollBarContainer = new Rect
        {
            Constraints = new RectF
            {
                Width = 15,
            },
            Style =
            {
                BackgroundColor = 0x000000,
                Padding = new PaddingStyle
                {
                    Left = 1,
                    Top = 1,
                    Bottom = 15
                }
            }
        };
        var scrollBar = new Rect
        {
            Style =
            {
                BackgroundColor = 0xEFEFEF,
            }
        };
        scrollBarContainer.Add(scrollBar);

        var progress = new Rect
        {
            Style =
            {
                BackgroundColor = 0xEFEFEF,
                BorderSize = new BorderSizeStyle
                {
                    Top = 1,
                },
                BorderColor = BorderColorStyle.All(0x000000)
            }
        };
        
        var bottomSection = new BorderLayout
        {
            East = scrollBarContainer,
            Center = progress
        };
        columnLayout.Add(bottomSection);
        
        var borderLayout = new BorderLayout
        {
            North = titlePanel,
            West = leftBorder,
            Center = contentOutline,
            East = rightBorder,
            South = bottomBorder
        };
        
        contentOutline.Add(columnLayout);
        outline.Add(borderLayout);
        Add(outline);

        var windowResizer = new WindowResizer(this);
        Add(windowResizer);
    }

    protected override void OnLayoutSelf()
    {
        var left = Position.Left;
        if (left < Constraints.Left)
        {
            left = Constraints.Left;
        }

        var bottom = Position.Bottom;
        if (bottom < Constraints.Bottom)
        {
            bottom = Constraints.Bottom;
        }

        var top = Position.Top;
        if (top > Constraints.Top)
        {
            var delta = top - Constraints.Top;
            bottom -= delta;
        }
        
        var right = Position.Right;
        if (right > Constraints.Right)
        {
            var delta = right - Constraints.Right;
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
}