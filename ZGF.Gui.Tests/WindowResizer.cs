using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class WindowResizer : Component
{
    private readonly Window _window;

    private readonly Panel _background;
    
    public WindowResizer(Window window)
    {
        _window = window;
        _background = new Panel
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
        _background.BackgroundColor = 0x9C9CCE;
        Context?.MouseInputSystem.TryFocus(this);
    }

    protected override void OnMouseExit()
    {
        Console.WriteLine("Mouse Exit");
        _background.BackgroundColor = 0xCECECE;
        Context?.MouseInputSystem.Blur(this);
    }

    protected override void OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        Console.WriteLine($"Mouse Button Event: {e.Button}");
        _window.BringToFront();
    }

    protected override void OnLayoutSelf()
    {
        var width = 16f;
        var height = 16f;
        var constraints = Constraints;
        var left = constraints.Right - width - 5;
        var bottom = constraints.Bottom + 5;
        Position = new RectF
        {
            Left = left,
            Bottom = bottom,
            Width = width,
            Height = height
        };
    }
}