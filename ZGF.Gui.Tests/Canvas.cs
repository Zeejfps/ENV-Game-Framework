using PngSharp.Api;
using SoftwareRendererModule;
using SoftwareRendererOpenGlBackend;
using ZGF.BMFontModule;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class Canvas : ICanvas
{
    private readonly Bitmap _colorBuffer;
    private readonly BitmapRenderer _bitmapRenderer;
    private readonly BitmapFont _charcoalFont;

    public Canvas(Bitmap colorBuffer)
    {
        _colorBuffer = colorBuffer;
        _bitmapRenderer = new BitmapRenderer(colorBuffer);
        _charcoalFont = LoadFontFromFile("Assets/Fonts/Charcoal/Charcoal.xml");
    }

    public void BeginFrame()
    {
        _colorBuffer.Fill(0x000000);
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
        var codePoints = AsCodePoints(text);
        var cursorX = 50;
        foreach (var codePoint in codePoints)
        {
            if (!_charcoalFont.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;

            DrawGlyph(
                cursorX,
                50,
                glyphInfo);

            cursorX += glyphInfo.XAdvance;
        }
    }

    private void DrawGlyph(int cursorX, int cursorY, FontChar glyphInfo)
    {
        var pixels = _colorBuffer.Pixels;

        var glyphWidth = glyphInfo.Width;
        var glyphHeight = glyphInfo.Height;
        for (var y = 0; y < glyphHeight; y++)
        {
            var dstY = cursorY + (glyphHeight - y) - glyphInfo.YOffset;
            var srcY = glyphInfo.Y + y;
            for (var x = 0; x < glyphWidth; x++)
            {
                var dstX = cursorX + x + glyphInfo.XOffset;
                var srcX = glyphInfo.X + x;
                var dstIndex = dstY * _colorBuffer.Width + dstX;
                var srcIndex = srcY * _charcoalFont.Png.Width + srcX;
                var a = _charcoalFont.Png.PixelData[srcIndex*4 + 3];
                if (a > 0)
                    pixels[dstIndex] = 0x000000;
            }
        }

    }

    private IEnumerable<int> AsCodePoints(string s)
    {
        for(int i = 0; i < s.Length; ++i)
        {
            yield return char.ConvertToUtf32(s, i);
            if(char.IsHighSurrogate(s, i))
                i++;
        }
    }

    public void EndFrame()
    {
        _bitmapRenderer.Render();
    }

    private BitmapFont LoadFontFromFile(string path)
    {
        var fontFile = BMFontFileUtils.DeserializeFromXmlFile(path);
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var fontPngFilePath = Path.Combine(directory, fontFile.Pages[0].File);
        var fontPng = Png.DecodeFromFile(fontPngFilePath);
        return new BitmapFont(fontPng, fontFile);
    }

    private Bitmap PngToBitmap(IDecodedPng png)
    {
        var bmp = new Bitmap(png.Width, png.Height);
        var bmpPixels = bmp.Pixels;

        const int bytesPerPixel = 4;

        // Loop through each row of the source image from top to bottom (y = 0 to height-1).
        for (int y = 0; y < png.Height; y++)
        {
            // Calculate the starting index for the current source row.
            int srcRowStartIndex = y * png.Width * bytesPerPixel;

            // Calculate the starting index for the corresponding destination row.
            // This is the key to flipping the image: source row 'y' maps to destination row 'height - 1 - y'.
            int destRowStartIndex = (png.Height - 1 - y) * png.Width;

            // Loop through each pixel in the current row.
            for (int x = 0; x < png.Width; x++)
            {
                // Calculate the specific index for the source pixel's bytes.
                int srcByteIndex = srcRowStartIndex + (x * bytesPerPixel);

                // Read the individual color channels.
                byte r = png.PixelData[srcByteIndex + 0];
                byte g = png.PixelData[srcByteIndex + 1];
                byte b = png.PixelData[srcByteIndex + 2];
                byte a = png.PixelData[srcByteIndex + 3];

                // Pack the bytes into a 32-bit uint in AARRGGBB format.
                uint color = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;

                // This is your custom logic to turn any visible pixel into a specific color.
                // Note: 0xFF00FF is opaque cyan (0x00FF00FF).
                // If you wanted magenta (hot pink), you would use 0xFFFF00FF.
                if (a > 0)
                {
                    color = 0xFFFF00FF; // Using magenta (AARRGGBB) as it's a common debug color.
                }

                // Calculate the destination index and write the pixel.
                int destPixelIndex = destRowStartIndex + x;
                bmpPixels[destPixelIndex] = color;
            }
        }
        return bmp;
    }
}

public sealed class BitmapFont(IDecodedPng png, BMFontFile file)
{
    public IDecodedPng Png => png;

    public bool TryGetGlyphInfo(int codePoint, out FontChar o)
    {
        return file.TryGetFontChar(codePoint, out o);
    }
}