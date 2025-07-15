using ZGF.Geometry;

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
                }
            }
        };

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
            Id = "content_outline",
            // Style =
            // {
            //     BackgroundColor = 0x000000,
            //     BorderSize = BorderSizeStyle.All(1),
            //     BorderColor = new BorderColorStyle
            //     {
            //         Left = 0x9C9C9C,
            //         Top = 0x9C9C9C,
            //         Right = 0xFFFFFF,
            //         Bottom = 0xFFFFFF
            //     },
            //     Padding = PaddingStyle.All(1)
            // }
        };

        var content = new Rect
        {
            Style =
            {
                BackgroundColor = 0xDEDEDE
            }
        };
        
        var borderLayout = new BorderLayout
        {
            North = titlePanel,
            West = leftBorder,
            Center = contentOutline,
            East = rightBorder,
            South = bottomBorder
        };
        
        contentOutline.Add(content);
        // frame.Add(contentOutline);
        outline.Add(borderLayout);
        Add(outline);
        
    }

    protected override void OnLayoutSelf()
    {
        foreach (var child in Children)
        {
            child.Constraints = Position;
            child.LayoutSelf();
        }
    }

    public void Move(float dx, float dy)
    {
        Position = Position with { Left = Position.Left + dx, Bottom = Position.Bottom + dy };
    }
}