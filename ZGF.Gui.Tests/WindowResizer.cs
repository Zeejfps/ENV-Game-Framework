using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowResizer : Component, IMouseListener, ICaptureMouse
{
    private readonly Window _window;

    private Rect _background;
    
    public WindowResizer(Window window)
    {
        _window = window;
        Constraints = new RectF
        {
            Width = 16,
            Height = 16,
        };
        
        _background = new Rect
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
        
        Add(_background);
        
        AddMouseListener(this);
    }

    public void HandleMouseEnterEvent()
    {
        Console.WriteLine("Mouse Enter");
        _background.Style.BackgroundColor = 0x9C9CCE;
        _background.SetDirty();
        CaptureMouse(this);
    }

    public void HandleMouseExitEvent()
    {
        Console.WriteLine("Mouse Exit");
        _background.Style.BackgroundColor = 0xCECECE;
        _background.SetDirty();
        ReleaseMouse(this);
    }

    public void HandleMouseButtonEvent()
    {
        Console.WriteLine("Mouse Button Event");
        _window.BringToFront();
    }

    public void HandleMouseWheelEvent()
    {
    }

    public void HandleMouseMoveEvent()
    {
    }
}