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
        var addButton = new TextButton("Hellog!")
        {
            ScreenRect = new Rect(ScreenRect.Right - 100, ScreenRect.Bottom, 100, 60),
            OnClicked = () =>
            {
                Console.WriteLine("Clicked");
            }
        };
        
        return new MultiChildWidget(new []
        {
            new PaddingWidget
            {
                ScreenRect = new Rect(ScreenRect.Right - 100, ScreenRect.Bottom, 100, 60),
                Offsets = Offsets.All(10),
                Child = addButton,
            }
        })
        {
            ScreenRect = ScreenRect,
        };
    }
}