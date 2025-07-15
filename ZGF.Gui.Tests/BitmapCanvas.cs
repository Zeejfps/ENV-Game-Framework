using SoftwareRendererModule;
using SoftwareRendererOpenGlBackend;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class BitmapCanvas : ICanvas
{
    private readonly Bitmap _colorBuffer;
    private readonly BitmapRenderer _bitmapRenderer;

    public BitmapCanvas(Bitmap colorBuffer)
    {
        _colorBuffer = colorBuffer;
        _bitmapRenderer = new BitmapRenderer(colorBuffer);
    }

    public void BeginFrame()
    {
        _colorBuffer.Fill(0x000000);
    }

    public void DrawRect(RectF position, RectStyle style)
    {
        var x = (int)position.Left;
        var y = (int)position.Bottom;
        var width = (int)position.Width;
        var height = (int)position.Height;
        Graphics.DrawRect(_colorBuffer, x, y, width, height, 0xFF00FF);
    }

    public void DrawText(RectF position, string text, TextStyle style)
    {
    }

    public void EndFrame()
    {
        _bitmapRenderer.Render();
    }
}