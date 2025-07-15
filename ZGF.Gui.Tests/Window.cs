using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class WindowFrame : Component
{
    public WindowFrame()
    {
        var outline = new Rect
        {
            Style =
            {
                BackgroundColor = 0x000000,
                BorderSize = BorderSizeStyle.All(1),
                BorderColor = BorderColorStyle.All(0x000000),
            }
        };
        
        var frame = new Rect
        {
            Style =
            {
                BackgroundColor = 0xCECECE,
                BorderSize = BorderSizeStyle.All(1),
                BorderColor = new BorderColorStyle
                {
                    Top = 0xFFFFFF,
                    Left = 0xFFFFFF,
                    Right = 0x9C9C9C,
                    Bottom = 0x9C9C9C
                },
                Padding = new PaddingStyle
                {
                    Left = 3,
                    Right = 3,
                    Bottom = 3,
                    Top = 20
                }
            }
        };
        
        outline.Add(frame);
        Add(outline);
    }
}

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
        
        var titlePanel = new Rect
        {
            Constraints = new RectF
            {
                Height = 20f,
            },
            Style =
            {
                BackgroundColor = 0xCECECE,
                BorderColor = new BorderColorStyle
                {
                    Top = 0xFFFFFF,
                    Left = 0xFFFFFF,
                    Right = 0x9C9C9C,
                },
                BorderSize = new BorderSizeStyle
                {
                    Top = 1,
                    Left = 1,
                    Right = 1,
                },
                Padding = PaddingStyle.All(2)
            }
        };

        var button = new Rect();
        button.AddStyleClass("inset_panel");
        titlePanel.Add(button);

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
        
        var contentOutline = new Rect();
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

        var test = new Rect
        {
            Style =
            {
                BackgroundColor = 0xCECECE
            },
        };
        test.AddStyleClass("inset_panel");
        columnLayout.Add(test);
        
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
}