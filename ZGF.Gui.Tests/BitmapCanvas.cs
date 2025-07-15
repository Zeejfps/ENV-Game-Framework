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
        _colorBuffer.Fill(0x9C9CCE);
    }

    public void AddCommand(in DrawRectCommand command)
    {
        var style = command.Style;
        var position = command.Position;
        var left = (int)position.Left;
        var right = (int)position.Right;
        var bottom = (int)position.Bottom;
        var top = (int)position.Top;
        var width = (int)position.Width;
        var height = (int)position.Height;
        var borderSize = style.BorderSize;
        var borderColor = style.BorderColor;
        
        Graphics.FillRect(_colorBuffer, left, bottom, width, height, style.BackgroundColor);

        // Left Border
        DrawBorder(left, bottom, left, top-1, borderColor.Left, (int)borderSize.Left, 1, 0);
        
        // Right Border
        DrawBorder(right-1, bottom, right-1, top-1, borderColor.Right, (int)borderSize.Right, -1, 0);
        
        // Top Border
        DrawBorder(left, top-1, right-1, top-1, borderColor.Top, (int)borderSize.Top, 0, -1);
        
        // Bottom Border
        DrawBorder(left, bottom, right-1, bottom, borderColor.Bottom, (int)borderSize.Bottom, 0, 1);
    }

    private void DrawBorder(int x0, int y0, int x1, int y1, uint color, int borderSize, int dx, int dy)
    {
        if (borderSize <= 0)
            return;

        for (var i = 0; i < borderSize; i++)
        {
            Graphics.DrawLine(_colorBuffer, x0, y0, x1, y1, color);
            x0 += dx;
            y0 += dy;
            x1 += dx;
            y1 += dy;
        }
    }

    public void AddCommand(in DrawTextCommand command)
    {
    }

    public void EndFrame()
    {
        _bitmapRenderer.Render();
    }
}