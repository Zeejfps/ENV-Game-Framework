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
    private readonly BitmapFont _font;
    private readonly ITextMeasurer _textMeasurer;
    private readonly ImageManager _imageManager;

    private readonly List<DrawCommand> _commands = new();
    private readonly Dictionary<int, DrawRectCommand> _rectCommandData = new();
    private readonly Dictionary<int, DrawTextCommand> _textCommandData = new();
    private readonly Dictionary<int, DrawImageCommand> _imageCommandData = new();

    private Bitmap _colorBuffer;
    private BitmapRenderer _bitmapRenderer;

    public Canvas(int width, int height, BitmapFont font, ITextMeasurer textMeasurer, ImageManager imageManager)
    {
        _colorBuffer = new Bitmap(width, height);
        _bitmapRenderer = new BitmapRenderer(_colorBuffer);
        _font = font;
        _textMeasurer = textMeasurer;
        _imageManager = imageManager;
    }

    public int Width => _colorBuffer.Width;
    public int Height => _colorBuffer.Height;

    public void Resize(int width, int height)
    {
        _colorBuffer = new Bitmap(width, height);
        _bitmapRenderer.Dispose();
        _bitmapRenderer = new BitmapRenderer(_colorBuffer);
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

    private void DrawGlyph(int cursorX, int cursorY, FontChar glyphInfo, uint color)
    {
        var bufferPixels = _colorBuffer.Pixels;
        var bufferWidth = _colorBuffer.Width;
        var bufferHeight = _colorBuffer.Height;

        var fontPixels = _font.Png.PixelData;
        var fontTextureWidth = _font.Png.Width;

        // Get the font's baseline metric
        int fontBase = _font.FontMetrics.Common.Base;

        // --- COORDINATE CALCULATION FOR BOTTOM-LEFT ORIGIN ---
        // 1. Calculate the glyph's destination rectangle.
        // destX is the same as before.
        int destX = cursorX + glyphInfo.XOffset;

        // destY is the bottom-most coordinate of the glyph on the screen.
        // Start at the baseline (cursorY), go UP by the distance from baseline to the glyph's top (fontBase - yoffset),
        // then go DOWN by the glyph's height to find the bottom.
        int destY = cursorY + (fontBase - glyphInfo.YOffset) - glyphInfo.Height;

        int destWidth = glyphInfo.Width;
        int destHeight = glyphInfo.Height;

        // 2. Clip the destination rectangle against the screen bounds.
        int loopStartX = Math.Max(0, destX);
        int loopEndX = Math.Min(bufferWidth, destX + destWidth);
        int loopStartY = Math.Max(0, destY);
        int loopEndY = Math.Min(bufferHeight, destY + destHeight);

        if (loopStartX >= loopEndX || loopStartY >= loopEndY)
        {
            return;
        }

        // 3. Calculate starting offsets for the source texture based on clipping.
        int srcXOffset = loopStartX - destX;

        // The source Y needs to be calculated carefully due to the inverted coordinate systems.
        // We map the bottom of the clipped screen region to the bottom of the clipped glyph texture region.
        int y_on_screen_from_glyph_bottom = loopStartY - destY;
        int y_on_texture_from_glyph_top = (glyphInfo.Height - 1) - y_on_screen_from_glyph_bottom;
        int initialSrcY = glyphInfo.Y + y_on_texture_from_glyph_top;

        uint rgb = color & 0x00FFFFFF;

        // 4. Loop only over the visible part of the glyph
        for (int y = 0; y < (loopEndY - loopStartY); y++)
        {
            // Destination row is simple to calculate
            int dstRow = loopStartY + y;

            // Source row is calculated by starting at the initial pre-calculated
            // source Y and decrementing, because screen Y goes up while texture Y goes down.
            int srcRow = initialSrcY - y;

            // Pre-calculate row start indices
            int dstIndex = dstRow * bufferWidth + loopStartX;
            int srcIndex = (srcRow * fontTextureWidth + glyphInfo.X + srcXOffset) * 4;

            for (int x = 0; x < (loopEndX - loopStartX); x++)
            {
                byte alpha = fontPixels[srcIndex + 3];
                if (alpha > 0)
                {
                    uint foregroundPixel = ((uint)alpha << 24) | rgb;
                    bufferPixels[dstIndex] = Graphics.BlendPixel(bufferPixels[dstIndex], foregroundPixel);
                }

                dstIndex++;
                srcIndex += 4;
            }
        }
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
        
        var aspect = (float)image.Width / image.Height;
        if (height < width)
        {
            width = (int)(height * aspect);
        }
        else if (width < height)
        {
            height = (int)(width / aspect);
        }
        
        Graphics.BlitTransparent(
            _colorBuffer, x, y, width, height,
            image, 0, 0, image.Width, image.Height,
            0x000000
        );
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

        var color = style.TextColor;
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
                glyphInfo, color);

            cursorX += glyphInfo.XAdvance;
        }
    }
}