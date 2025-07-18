using SoftwareRendererModule;
using SoftwareRendererOpenGlBackend;
using ZGF.BMFontModule;
using static GL46;

namespace ZGF.Gui.Tests;

enum ComandKind
{
    Rect,
    Text,
    Image,
}

internal readonly record struct DrawCommand(int Id, ComandKind Kind, int ZIndex)
{
}

public sealed class Canvas : ICanvas
{
    private readonly Bitmap _colorBuffer;
    private readonly BitmapRenderer _bitmapRenderer;
    private readonly BitmapFont _font;
    private readonly ITextMeasurer _textMeasurer;
    private readonly ImageManager _imageManager;

    private readonly List<DrawCommand> _commands = new();
    private readonly Dictionary<int, DrawRectCommand> _rectCommandData = new();
    private readonly Dictionary<int, DrawTextCommand> _textCommandData = new();
    private readonly Dictionary<int, DrawImageCommand> _imageCommandData = new();

    public Canvas(Bitmap colorBuffer, BitmapFont font, ITextMeasurer textMeasurer, ImageManager imageManager)
    {
        _colorBuffer = colorBuffer;
        _bitmapRenderer = new BitmapRenderer(colorBuffer);
        _font = font;
        _textMeasurer = textMeasurer;
        _imageManager = imageManager;
    }

    public void BeginFrame()
    {
        _colorBuffer.Fill(0x000000);
        glEnable(GL_BLEND);
        
        _commands.Clear();
        _rectCommandData.Clear();
        _textCommandData.Clear();
        _imageCommandData.Clear();
    }

    public void AddCommand(in DrawRectCommand command)
    {
        var id = _commands.Count;
        _commands.Add(new DrawCommand(id, ComandKind.Rect, command.ZIndex));
        _rectCommandData.Add(id, command);
    }

    public void AddCommand(in DrawTextCommand command)
    {
        var id = _commands.Count;
        _commands.Add(new DrawCommand(id, ComandKind.Text, command.ZIndex));
        _textCommandData.Add(id, command);
    }

    public void AddCommand(in DrawImageCommand command)
    {
        var id = _commands.Count;
        _commands.Add(new DrawCommand(id, ComandKind.Image, command.ZIndex));
        _imageCommandData.Add(id, command);
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

    private void DrawGlyph(int cursorX, int cursorY, FontChar glyphInfo)
    {
        if (cursorX < 0)
            cursorX = 0;
        
        if (cursorY < 0)
            cursorY = 0;
        
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
                continue;
            if (dstY < 0)
                continue;

            var srcY = glyphInfo.Y + y;
            for (var x = 0; x < glyphWidth; x++)
            {
                var dstX = cursorX + x + glyphInfo.XOffset;
                if (dstX < 0) continue;
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
        var srcA = (byte)((srcColor >> 24) & 0xFF);
        if (srcA == 0)
            return dstColor;

        if (srcA == 255)
            return srcColor;

        var srcR = (byte)((srcColor >> 16) & 0xFF);
        var srcG = (byte)((srcColor >> 8) & 0xFF);
        var srcB = (byte)((srcColor) & 0xFF);

        var dstA = (byte)((dstColor >> 24) & 0xFF);
        var dstR = (byte)((dstColor >> 16) & 0xFF);
        var dstG = (byte)((dstColor >> 8) & 0xFF);
        var dstB = (byte)((dstColor) & 0xFF);

        // Normalize alpha to range [0,1]
        var alpha = srcA / 255f;

        // Blend each channel
        var outR = (byte)(srcR * alpha + dstR * (1 - alpha));
        var outG = (byte)(srcG * alpha + dstG * (1 - alpha));
        var outB = (byte)(srcB * alpha + dstB * (1 - alpha));
        var outA = (byte)Math.Min(255, srcA + dstA * (1 - alpha)); // optional

        // Pack back into ARGB
        return ((uint)outA << 24) | ((uint)outR << 16) | ((uint)outG << 8) | outB;
    }

    public void EndFrame()
    {
        //_commands.Sort((x, y) => y.ZIndex.CompareTo(x.ZIndex));
        
        // var drawCommands = _commands
        //     .Select((cmd, index) => (cmd, index))
        //     .OrderBy(x => x.cmd.ZIndex) // Or .OrderByDescending
        //     .ThenBy(x => x.index)
        //     .Select(x => x.cmd)
        //     .ToList();
        
        foreach (var command in _commands)
        {
            switch (command.Kind)
            {
                case ComandKind.Rect:
                    var rectCommand = _rectCommandData[command.Id];
                    ExecuteCommand(rectCommand);
                    break;
                case ComandKind.Text:
                    var textCommand = _textCommandData[command.Id];
                    ExecuteCommand(textCommand);
                    break;
                case ComandKind.Image:
                    var imageCommand = _imageCommandData[command.Id];
                    ExecuteCommand(imageCommand);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        _bitmapRenderer.Render();
    }

    private void ExecuteCommand(DrawImageCommand command)
    {
        var image = _imageManager.GetImage(command.ImageUri);
        var position = command.Position;
        var x = (int)position.Left;
        var y = (int)position.Bottom;
        var width = (int)position.Width;
        var height = (int)position.Height;
        //Console.WriteLine($"{x},{y},{width},{height}");
        _colorBuffer.Blit(image, x, y, image.Width, image.Height, 0, 0, image.Width, image.Height);
    }

    private void ExecuteCommand(DrawRectCommand command)
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

    private void ExecuteCommand(DrawTextCommand command)
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

        if (style.HorizontalAlignment.IsSet)
        {
            switch (style.HorizontalAlignment.Value)
            {
                case TextAlignment.Start:
                    break;
                case TextAlignment.Center:
                    var width = _textMeasurer.MeasureTextWidth(text, style);
                    cursorX = (int)(position.Left + (position.Width - width) * 0.5f);
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
}