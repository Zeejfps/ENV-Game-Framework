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
        Console.WriteLine("Building");
        return new Button
        {
            ScreenRect = new Rect(30, 60, 100, 60),
            Child = new TextWidget("Hellog!")
            {
                FontFamily = "Segoe UI",
                Style = new TextStyle
                {
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                },
            },
            OnPressed = () =>
            {
                
            }
        };
    }
}