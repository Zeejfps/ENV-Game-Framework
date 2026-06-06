using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class WindowCoordinates : IWindowCoordinates
{
    private readonly IWindow _window;
    private readonly RenderedCanvasBase _canvas;

    public WindowCoordinates(IWindow window, RenderedCanvasBase canvas)
    {
        _window = window;
        _canvas = canvas;
    }

    public PointI ToScreenPoints(PointF canvasPoint)
    {
        var winW = _window.Width;
        var winH = _window.Height;
        _window.GetPosition(out var winX, out var winY);

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
