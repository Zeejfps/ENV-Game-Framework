using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Cameras;
using EasyGameFramework.Api.Rendering;

namespace Pong;

public sealed class Viewport : IViewport
{
    public float Left { get; set; }
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float AspectRatio { get; set; }

    private ICamera Camera { get; set; }

    public Viewport(float aspectRatio)
    {
        Left = 0f;
        Top = 1f;
        Right = 1f;
        Bottom = 0f;
        AspectRatio = aspectRatio;
    }

    public Vector2 ToWorldPoint(Vector2 viewportPoint, OrthographicCamera camera)
    {
        // var viewportWidth = Window.ViewportWidth;
        // var viewportHeight = Window.ViewportHeight;
        //
        // var xt = (viewportPoint.X / viewportWidth) - 0.5f;
        // var yt = 0.5f - (viewportPoint.Y / viewportHeight);
        //
        // var x = xt * camera.Rect.Width;
        // var y = yt * camera.Rect.Height;
        //
        // return new Vector2(x, y);
        
        return Vector2.Zero;
    }
}