using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;

namespace Pong;

public sealed class World2D
{
    private IWindow Window { get; }

    public World2D(IWindow window)
    {
        Window = window;
    }

    public Vector2 ViewportToWorldPoint(Vector2 viewportPoint, OrthographicCamera camera)
    {
        var viewportWidth = Window.ViewportWidth;
        var viewportHeight = Window.ViewportHeight;

        var xt = (viewportPoint.X / viewportWidth) - 0.5f;
        var yt = 0.5f - (viewportPoint.Y / viewportHeight);

        var x = xt * camera.Rect.Width;
        var y = yt * camera.Rect.Height;
        
        return new Vector2(x, y);
    }
}