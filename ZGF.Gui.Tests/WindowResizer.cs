using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowResizer : Component, IMouseListener
{
    public WindowResizer()
    {
        Constraints = new RectF
        {
            Width = 16,
            Height = 16,
        };
        
        var resizer = new Rect
        {
            Style =
            {
                BackgroundColor = 0xCECECE,
                BorderSize = new BorderSizeStyle
                {
                    Left = 1,
                    Top = 1,
                },
                BorderColor = new BorderColorStyle
                {
                    Left = 0xFFFFFF,
                    Top = 0xFFFFFF,
                }
            }
        };
        
        Add(resizer);
        
        AddMouseListener(this);
    }

    public void HandleMouseEnterEvent()
    {
        Console.WriteLine("Mouse Enter");
    }

    public void HandleMouseExitEvent()
    {
        Console.WriteLine("Mouse Exit");
    }
}