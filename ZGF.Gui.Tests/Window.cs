using ZGF.Geometry;

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

        var contentOutline = new Rect
        {
            Style =
            {
                BackgroundColor = 0x000000,
                BorderSize = BorderSizeStyle.All(1),
                BorderColor = new BorderColorStyle
                {
                    Left = 0x9C9C9C,
                    Top = 0x9C9C9C,
                    Right = 0xFFFFFF,
                    Bottom = 0xFFFFFF
                },
                Padding = PaddingStyle.All(1)
            }
        };

        var content = new Rect
        {
            Style =
            {
                BackgroundColor = 0xDEDEDE
            }
        };
        
        contentOutline.Add(content);
        frame.Add(contentOutline);
        outline.Add(frame);
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
}