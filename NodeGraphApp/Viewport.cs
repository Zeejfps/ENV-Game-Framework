using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class Viewport
{
    public required RectF Bounds { get; set; }
    public Camera Camera { get; }
    
    private readonly Window _window;

    public Viewport(Window window, Camera camera)
    {
        _window = window;
        Camera = camera;
    }

    public void Update()
    {
        var width = _window.FramebufferWidth;
        var height = _window.FramebufferHeight;
        var x = Bounds.Left * width;
        var y = Bounds.Bottom * height;
        var w = Bounds.Width * width;
        var h = Bounds.Height * height;
        var aspectRatio = w / h;
        Camera.AspectRatio = aspectRatio;
        GL46.glViewport((int)x, (int)y, (int)w, (int)h);
        GL46.glScissor((int)x, (int)y, (int)w, (int)h);
    }

    public Vector2 ScreenToViewportNdcPoint(Vector2 screenPoint)
    {
        var window = _window;
        var bounds = Bounds;
        var windowWidth = window.Width;
        var windowHeight = window.Height;

        // Convert screenPoint to normalized screen space [0,1]
        var normX = screenPoint.X / windowWidth;
        var normY = 1f - (screenPoint.Y / windowHeight); // Flip from top down to bottom up

        // Convert to viewport-local normalized coordinates [0,1] within the viewport
        var localX = (normX - bounds.Left) / bounds.Width;
        var localY = (normY - bounds.Bottom) / bounds.Height;
        
        //Console.WriteLine($"Local: {localX}, {localY}");

        // Convert to NDC [-1,1], with (0,0) at the center of the viewport
        var ndcX = localX * 2f - 1f; // Maps [0,1] to [-1,1]
        var ndcY =  localY * 2f - 1f; // Maps [0,1] to [-1,1]

        return new Vector2(ndcX, ndcY);
    }

    public bool TryScreenToViewportNdcPoint(Vector2 screenPoint, out Vector2 ndcPoint)
    { 
        ndcPoint = ScreenToViewportNdcPoint(screenPoint);
        return ndcPoint.X is >= -1f and <= 1f && ndcPoint.Y is >= -1f and <= 1f;
    }

    public Vector2 ScreenToWorldPoint(Vector2 screenPoint)
    {
        var camera = Camera;
        var ndcCoordinates = ScreenToViewportNdcPoint(screenPoint);
        return CoordinateUtils.NdcToCameraViewPoint(camera, ndcCoordinates) + camera.Position;
    }

    public Vector2 ScreenToCameraViewPoint(Vector2 screenPoint)
    {
        var camera = Camera;
        var ndcCoords = ScreenToViewportNdcPoint(screenPoint);
        return CoordinateUtils.NdcToCameraViewPoint(camera, ndcCoords);
    }

    public bool ContainsScreenPoint(Vector2 mousePosition)
    {
        return TryScreenToViewportNdcPoint(mousePosition, out _);
    }
}