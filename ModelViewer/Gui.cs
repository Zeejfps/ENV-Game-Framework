using EasyGameFramework.Api;
using OpenGLSandbox;

namespace ModelViewer;

public sealed class Gui : StatefulWidget
{
    public Gui(IWindow window)
    {
        Window = window;
        Window.Resized += Window_OnResized;
    }

    public override void Dispose()
    {
        Window.Resized -= Window_OnResized;
        base.Dispose();
    }

    private void Window_OnResized()
    {
        SetDirty();
    }

    private IWindow Window { get; }

    protected override IWidget Build(IBuildContext context)
    {
        var window = Window;
        ScreenRect = new Rect(0f, 0f, window.ScreenWidth, window.ScreenHeight);

        var addButton = new TextButton("Hellog!")
        {
            OnClicked = () =>
            {
                Console.WriteLine("Clicked");
            }
        };

        return new PaddingWidget
        {
            ScreenRect = ScreenRect,
            Offsets = Offsets.All(10f),
            Child = new Column
            {
                Spacing = 10,
                Children =
                {
                    new TextButton("Test1!")
                    {
                        OnClicked = () =>
                        {
                            Console.WriteLine("Clicked");
                        }
                    },
                    addButton,
                    new TextButton("Test3!")
                    {
                        OnClicked = () =>
                        {
                            Console.WriteLine("Clicked");
                        }
                    },
                    new TextButton("Test4!")
                    {
                        OnClicked = () =>
                        {
                            Console.WriteLine("Clicked");
                        }
                    },
                    new TextField(),

                }
            }
        };
    }
}