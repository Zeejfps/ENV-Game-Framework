using SoftwareRendererModule;
using SoftwareRendererOpenGlBackend;
using ZGF.BMFontModule;
using static GL46;

namespace ZGF.Gui.Tests;

public sealed class Canvas : ICanvas
{
    private readonly Bitmap _colorBuffer;
    private readonly BitmapRenderer _bitmapRenderer;
    private readonly BitmapFont _font;

    public Canvas(Bitmap colorBuffer, BitmapFont font)
    {
        _colorBuffer = colorBuffer;
        _bitmapRenderer = new BitmapRenderer(colorBuffer);
        _font = font;
    }

    public void BeginFrame()
    {
        _colorBuffer.Fill(0x000000);
        glEnable(GL_BLEND);
    }

    public void AddCommand(in DrawRectCommand command)
    {
        var style = command.Style;
        var position = command.Position;
        var left = (int)position.Left;
        var right = (int)position.Right - 1;
        var bottom = (int)position.Bottom;
        var top = (int)position.Top - 1;
        
        var borderSize = style.BorderSize;
        
        var fillWidth = (int)(position.Width - borderSize.Left - borderSize.Right);
        var fillHeight = (int)(position.Height - borderSize.Top - borderSize.Bottom);
        
        var borderColor = style.BorderColor;
        
        Graphics.FillRect(_colorBuffer, left +  (int)borderSize.Left, bottom + (int)borderSize.Bottom, fillWidth, fillHeight, style.BackgroundColor);

        // Left Border
        DrawBorder(left, bottom, left, top+1, borderColor.Left, (int)borderSize.Left, 1, 0);
        
        // Right Border
        DrawBorder(right, bottom, right, top + 1, borderColor.Right, (int)borderSize.Right, -1, 0);
        
        // Top Border
        DrawBorder(left, top, right + 1, top, borderColor.Top, (int)borderSize.Top, 0, -1);
        
        // Bottom Border
        DrawBorder(left, bottom, right, bottom, borderColor.Bottom, (int)borderSize.Bottom, 0, 1);
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
        var text = command.Text;
        var codePoints = text.AsCodePoints();

        var lineHeight = _font.FontMetrics.Common.LineHeight;
        var position = command.Position;
        
        var lineStart = (int)position.Left;
        var cursorX = lineStart;
        var cursorY = (int)(position.Top - lineHeight);
        
        var style = command.Style;
        if (style.VerticalAlignment.IsSet)
        {
            switch (style.VerticalAlignment.Value)
            {
                case TextAlignment.Start:
                    break;
                case TextAlignment.Center:
                    cursorY = (int)(position.Top - (position.Height + lineHeight) * 0.5f);
                    break;
                case TextAlignment.End:
                    break;
                case TextAlignment.Justify:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        var prevCodePoint = default(int?);
        foreach (var codePoint in codePoints)
        {
            if (codePoint == '\n')
            {
                cursorY -= _font.FontMetrics.Common.LineHeight;
                cursorX = lineStart;
            }
            
            if (!_font.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;
            
            var kerningOffset = 0;
            if (prevCodePoint.HasValue &&
                _font.TryGetKerningPair(prevCodePoint.Value, codePoint, out kerningOffset))
            {
            }
            
            DrawGlyph(
                cursorX + kerningOffset,
                cursorY,
                glyphInfo);

            cursorX += glyphInfo.XAdvance;
        }
    }

    private void DrawGlyph(int cursorX, int cursorY, FontChar glyphInfo)
    {
        var pixels = _colorBuffer.Pixels;

        var lineHeight = _font.FontMetrics.Common.LineHeight;
        var baseHeight = _font.FontMetrics.Common.Base;
        var yUp = lineHeight - baseHeight;
        var yDown = baseHeight - (glyphInfo.YOffset + glyphInfo.Height);

        var glyphWidth = glyphInfo.Width;
        var glyphHeight = glyphInfo.Height;
        for (var y = 0; y < glyphHeight; y++)
        {
            var dstY = cursorY + (glyphHeight - y) + yUp + yDown;
            if (dstY >= _colorBuffer.Height)
                dstY = _colorBuffer.Height - 1;
            
            var srcY = glyphInfo.Y + y;
            for (var x = 0; x < glyphWidth; x++)
            {
                var dstX = cursorX + x + glyphInfo.XOffset;
                var srcX = glyphInfo.X + x;
                var dstIndex = dstY * _colorBuffer.Width + dstX;
                var srcIndex = srcY * _font.Png.Width + srcX;
                var a = _font.Png.PixelData[srcIndex*4 + 3];
                var rgb = 0x000000;
                var argb = ((uint)a << 24) | (uint)rgb;
                pixels[dstIndex] = BlendPixel(pixels[dstIndex], argb);
            }
        }

    }
    
    private uint BlendPixel(uint dstColor, uint srcColor)
    {
        // Extract ARGB components from both
        byte dstA = (byte)(dstColor >> 24);
        byte dstR = (byte)(dstColor >> 16);
        byte dstG = (byte)(dstColor >> 8);
        byte dstB = (byte)(dstColor);

        byte srcA = (byte)(srcColor >> 24);
        byte srcR = (byte)(srcColor >> 16);
        byte srcG = (byte)(srcColor >> 8);
        byte srcB = (byte)(srcColor);

        // Normalize alpha to range [0,1]
        float alpha = srcA / 255f;

        // Blend each channel
        byte outR = (byte)(srcR * alpha + dstR * (1 - alpha));
        byte outG = (byte)(srcG * alpha + dstG * (1 - alpha));
        byte outB = (byte)(srcB * alpha + dstB * (1 - alpha));
        byte outA = (byte)Math.Min(255, srcA + dstA * (1 - alpha)); // optional

        // Pack back into ARGB
        return ((uint)outA << 24) | ((uint)outR << 16) | ((uint)outG << 8) | outB;
    }

    public void EndFrame()
    {
        _bitmapRenderer.Render();
    }
}