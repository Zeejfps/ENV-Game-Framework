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
        return new TextButton("Hellog!")
        {
            ScreenRect = new Rect(30, 60, 100, 60),
            OnClicked = () =>
            {
                Console.WriteLine("Clicked");
            }
        };
    }
}