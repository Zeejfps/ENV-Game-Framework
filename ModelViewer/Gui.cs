using System.Numerics;
using EasyGameFramework.Api;
using OpenGLSandbox;

namespace ModelViewer;

public sealed class Gui : Widget
{
    public Gui(IWindow window)
    {
        Window = window;
        ScreenRect = new Rect(0f, 0f, window.ScreenWidth, window.ScreenHeight);
    }

    private IWindow Window { get; }

    protected override IWidget Build(IBuildContext context)
    {
        return new Button
        {
            ScreenRect = new Rect(10, 10, 60, 60),
            OnPressed = () =>
            {
                
            }
        };
    }
}

public sealed class Button : StatefulWidget
{
    public Action OnPressed { get; set; }
    
    protected override IWidget Build(IBuildContext context)
    {
        return new InputListenerWidget
        {
            ScreenRect = ScreenRect,
            Child = new PanelWidget
            {
                ScreenRect = ScreenRect,
                Style = new PanelStyle
                {
                    BackgroundColor = Color.FromHex(0xff00ff, 1f),
                    BorderRadius = new Vector4(5f, 5f, 5f, 5f)
                }
            }
        };
    }
}