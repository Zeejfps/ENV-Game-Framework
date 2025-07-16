using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowResizer : Component, IMouseFocusable
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
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        context.MouseInputSystem.EnableHover(this);
    }

    protected override void OnMouseEnter()
    {
        Console.WriteLine("Mouse Enter");
        _background.Style.BackgroundColor = 0x9C9CCE;
        _background.SetDirty();
        Context?.MouseInputSystem.TryFocus(this);
    }

    protected override void OnMouseExit()
    {
        Console.WriteLine("Mouse Exit");
        _background.Style.BackgroundColor = 0xCECECE;
        _background.SetDirty();
        Context?.MouseInputSystem.Blur(this);
    }

    public void HandleMouseButtonEvent(in MouseButtonEvent e)
    {
        Console.WriteLine($"Mouse Button Event: {e.Button}");
        _window.BringToFront();
    }

    public void HandleMouseWheelEvent()
    {
    }

    public void HandleMouseMoveEvent(in MouseMoveEvent e)
    {
    }
}