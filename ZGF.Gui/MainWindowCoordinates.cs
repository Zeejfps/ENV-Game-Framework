using GLFW;
using ZGF.Geometry;
using ZGF.Gui.Tests;

namespace ZGF.Gui;

public sealed class WindowCoordinates : IWindowCoordinates
{
    private readonly IntPtr _glfwHandle;
    private readonly RenderedCanvasBase _canvas;

    public WindowCoordinates(IntPtr glfwHandle, RenderedCanvasBase canvas)
    {
        _glfwHandle = glfwHandle;
        _canvas = canvas;
    }

    public PointI ToScreenPoints(PointF canvasPoint)
    {
        var window = (Window)_glfwHandle;
        Glfw.GetWindowSize(window, out var winW, out var winH);
        Glfw.GetWindowPosition(window, out var winX, out var winY);

        var windowPointX = canvasPoint.X * (winW / (float)_canvas.Width);
        var windowPointY = winH - canvasPoint.Y * (winH / (float)_canvas.Height);

        return new PointI(winX + (int)windowPointX, winY + (int)windowPointY);
    }

    public RectI ToScreenPoints(RectF canvasRect)
    {
        var topLeft = ToScreenPoints(new PointF(canvasRect.Left, canvasRect.Top));
        var bottomRight = ToScreenPoints(new PointF(canvasRect.Right, canvasRect.Bottom));
        return new RectI(
            X: topLeft.X,
            Y: topLeft.Y,
            Width: bottomRight.X - topLeft.X,
            Height: bottomRight.Y - topLeft.Y);
    }
}
