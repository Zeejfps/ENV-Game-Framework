using SoftwareRendererModule;
using SoftwareRendererOpenGlBackend;
using ZGF.BMFontModule;
using ZGF.Geometry;
using static GL46;

namespace ZGF.Gui.Tests;

internal readonly record struct DrawCommand(int Id, CommandKind Kind, int ZIndex, RectF Clip);

public sealed class SoftwareRenderedCanvas : ICanvas
{
    private readonly BitmapFont _font;
    private readonly ImageManager _imageManager;

    private readonly List<DrawCommand> _commands = new();
    private readonly Dictionary<int, DrawRectInputs> _rectCommandData = new();
    private readonly Dictionary<int, DrawTextInputs> _textCommandData = new();
    private readonly Dictionary<int, DrawImageInputs> _imageCommandData = new();

    private Bitmap _colorBuffer;
    private BitmapRenderer _bitmapRenderer;
    private readonly Stack<RectF> _clipStack = new();

    public SoftwareRenderedCanvas(int width, int height, BitmapFont font, ImageManager imageManager)
    {
        _colorBuffer = new Bitmap(width, height);
        _bitmapRenderer = new BitmapRenderer(_colorBuffer);
        _font = font;
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

    public void DrawRect(in DrawRectInputs inputs)
    {
        var id = _commands.Count;
        var clip = GetClip();
        _commands.Add(new DrawCommand(id, CommandKind.DrawRect, inputs.ZIndex, clip));
        _rectCommandData.Add(id, inputs);
    }

    public void DrawText(in DrawTextInputs inputs)
    {
        var id = _commands.Count;
        var clip = GetClip();
        _commands.Add(new DrawCommand(id, CommandKind.DrawText, inputs.ZIndex, clip));
        _textCommandData.Add(id, inputs);
    }

    public void DrawImage(in DrawImageInputs inputs)
    {
        var id = _commands.Count;
        var clip = GetClip();
        _commands.Add(new DrawCommand(id, CommandKind.DrawImage, inputs.ZIndex, clip));
        _imageCommandData.Add(id, inputs);
    }

    private RectF GetClip()
    {
        if (_clipStack.Count == 0)
            return new RectF(0, 0, Width, Height);
        
        return _clipStack.Peek();
    }

    public bool TryGetClip(out RectF rect)
    {
        return _clipStack.TryPeek(out rect);
    }

    public void PushClip(RectF rect)
    {
        var currentClip = GetClip();
        
        var left = rect.Left;
        if (left < currentClip.Left)
        {
            left = currentClip.Left;
        }
        
        var bottom = rect.Bottom;
        if (bottom < currentClip.Bottom)
        {
            bottom = currentClip.Bottom;
        }
        
        var right = rect.Right;
        if (right > currentClip.Right)
            right = currentClip.Right;
        
        var top = rect.Top;
        if (top > currentClip.Top)
            top = currentClip.Top;
        
        _clipStack.Push(new RectF
        {
            Left = left,
            Bottom = bottom,
            Width = right - left,
            Height = top - bottom,
        });
    }

    public void PopClip()
    {
        _clipStack.Pop();
    }

    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style)
    {
        var totalWidth = 0f;
        foreach (var codePoint in text.EnumerateCodePoints())
        {
            if (!_font.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;
            
            totalWidth += glyphInfo.XAdvance;
        }
        return totalWidth;
    }

    public float MeasureTextHeight(ReadOnlySpan<char> text, TextStyle style)
    {
        return _font.FontMetrics.Common.LineHeight;
    }

    public Size GetImageSize(string imageId)
    {
        return _imageManager.GetImageSize(imageId);
    }

    public int GetImageWidth(string imageId)
    {
        return _imageManager.GetImageWidth(imageId);
    }

    public int GetImageHeight(string imageId)
    {
        return _imageManager.GetImageHeight(imageId);
    }

    private void DrawBorder(
        int x0, int y0,
        int x1, int y1, 
        uint color, int borderSize,
        int dx, int dy,
        RectF clip)
    {
        if (borderSize <= 0)
            return;

        for (var i = 0; i < borderSize; i++)
        {
            Graphics.DrawLine(_colorBuffer, x0, y0, x1, y1, color, clip);
            x0 += dx;
            y0 += dy;
            x1 += dx;
            y1 += dy;
        }
    }

    private void DrawGlyph(int destX, int destY, FontChar glyphInfo, uint color, RectF clip)
    {
        var bufferPixels = _colorBuffer.Pixels;
        var bufferWidth = _colorBuffer.Width;

        var fontPixels = _font.Png.PixelData;
        var fontTextureWidth = _font.Png.Width;
        
        var destWidth = glyphInfo.Width;
        var destHeight = glyphInfo.Height;

        // 2. Clip the destination rectangle against the screen bounds.
        var loopStartX = Math.Max((int)clip.Left, destX);
        var loopEndX = Math.Min((int)clip.Right, destX + destWidth);
        var loopStartY = Math.Max((int)clip.Bottom, destY);
        var loopEndY = Math.Min((int)clip.Top, destY + destHeight);
        
        //Graphics.DrawRect(_colorBuffer, loopStartX, loopStartY, loopEndX - loopStartX, loopEndY - loopStartY, 0x00FF00);

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
                case CommandKind.DrawRect:
                    var rectCommand = _rectCommandData[command.Id];
                    ExecuteCommand(command, rectCommand);
                    break;
                case CommandKind.DrawText:
                    var textCommand = _textCommandData[command.Id];
                    ExecuteCommand(command, textCommand);
                    break;
                case CommandKind.DrawImage:
                    var imageCommand = _imageCommandData[command.Id];
                    ExecuteCommand(command, imageCommand);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        _bitmapRenderer.Render();
    }

    private void ExecuteCommand(in DrawCommand cmd, DrawImageInputs data)
    {
        var image = _imageManager.GetImageId(data.ImageId);
        var position = data.Position;
        var x = (int)position.Left;
        var y = (int)position.Bottom;
        var width = (int)position.Width;
        var height = (int)position.Height;
        
        var aspect = (float)image.Width / image.Height;
        float scaledWidth, scaledHeight;

        if (aspect > (float)width / height) // Image is wider than the area
        {
            scaledWidth = width;
            scaledHeight = width / aspect;
        }
        else 
        {
            scaledHeight = height;
            scaledWidth = height * aspect;
        }

        var offsetX = (int)(x + (width - scaledWidth) / 2);
        var offsetY = (int)(y + (height - scaledHeight) / 2);

        Graphics.BlitTransparent(
            _colorBuffer, offsetX, offsetY, (int)scaledWidth, (int)scaledHeight,
            image, 0, 0, image.Width, image.Height,
            data.Style.TintColor
        );
    }

    private void ExecuteCommand(in DrawCommand command, in DrawRectInputs data)
    {
        var style = data.Style;
        var position = data.Position;
        var left = (int)position.Left;
        var right = (int)position.Right - 1;
        var bottom = (int)position.Bottom;
        var top = (int)position.Top - 1;
        
        var borderSize = style.BorderSize;
        
        var fillWidth = (int)(position.Width - borderSize.Left - borderSize.Right);
        var fillHeight = (int)(position.Height - borderSize.Top - borderSize.Bottom);
        
        var borderColor = style.BorderColor;
        var clipRect = command.Clip;
        Graphics.FillRect(_colorBuffer, 
            left +  (int)borderSize.Left, bottom + (int)borderSize.Bottom, 
            fillWidth, fillHeight,
            style.BackgroundColor,
            clipRect);

        // Left Border
        DrawBorder(left, bottom, left, top+1, borderColor.Left, (int)borderSize.Left, 1, 0, clipRect);
        
        // Right Border
        DrawBorder(right, bottom, right, top + 1, borderColor.Right, (int)borderSize.Right, -1, 0, clipRect);
        
        // Top Border
        DrawBorder(left, top, right + 1, top, borderColor.Top, (int)borderSize.Top, 0, -1, clipRect);
        
        // Bottom Border
        DrawBorder(left, bottom, right, bottom, borderColor.Bottom, (int)borderSize.Bottom, 0, 1, clipRect);
    }

    private void ExecuteCommand(in DrawCommand cmd, in DrawTextInputs data)
    {
        
        var text = data.Text;

        var lineHeight = _font.FontMetrics.Common.LineHeight;
        var position = data.Position;
        
        //Graphics.DrawRect(_colorBuffer, (int)position.Left, (int)position.Bottom, (int)position.Width, (int)position.Height, 0x00ff00);
        
        var fontBase = _font.FontMetrics.Common.Base;
        var lineStart = (int)position.Left;
        var cursorX = lineStart;
        var cursorY = (int)(position.Top - fontBase);
        
        var style = data.Style;
        if (style.VerticalAlignment.IsSet)
        {
            switch (style.VerticalAlignment.Value)
            {
                case TextAlignment.Start:
                    break;
                case TextAlignment.Center:
                    //var textYOffset = (lineHeight - fontBase) / 2;
                    cursorY = (int)((position.Top - position.Height * 0.5f) - (fontBase * 0.5f));
                    break;
                case TextAlignment.End:
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
                    var width = MeasureTextWidth(text, style);
                    cursorX = (int)(position.Left + (position.Width - width) * 0.5f);
                    break;
                case TextAlignment.End:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        //Graphics.DrawLine(_colorBuffer, cursorX, cursorY, cursorX + (int)position.Width, cursorY, 0xFF0000, cmd.Clip);
        
        var color = style.TextColor;
        var prevCodePoint = default(int?);
        foreach (var codePoint in text.EnumerateCodePoints())
        {
            if (codePoint == '\n')
            {
                cursorY -= lineHeight;
                cursorX = lineStart;
            }
            
            if (!_font.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;
            
            var kerningOffset = 0;
            if (prevCodePoint.HasValue &&
                _font.TryGetKerningPair(prevCodePoint.Value, codePoint, out kerningOffset))
            {
            }
            
            var clipRect = cmd.Clip;
            var glyphX = cursorX + kerningOffset + glyphInfo.XOffset;
            var glyphY = cursorY + (fontBase - glyphInfo.YOffset) - glyphInfo.Height;

            var shouldDraw = true;
            var isClippingEnabled = true;
            if (isClippingEnabled)
            {

                var glyphWidth = glyphInfo.Width;
                var glyphHeight = glyphInfo.Height;

                // Perform an AABB (Axis-Aligned Bounding Box) intersection test.
                // We cull the glyph if it's completely outside the clip rectangle.
                if (glyphX + glyphWidth <= clipRect.Left ||     // Completely to the left
                    glyphX >= clipRect.Right ||                     // Completely to the right
                    glyphY >= clipRect.Top ||                       // Completely above
                    glyphY + glyphHeight <= clipRect.Bottom)    // Completely below
                {
                    shouldDraw = false;
                }
            }

            if (shouldDraw)
            {
                DrawGlyph(
                    glyphX,
                    glyphY,
                    glyphInfo,
                    color,
                    clipRect);
            }

            cursorX += glyphInfo.XAdvance;
            prevCodePoint = codePoint;
        }
    }
}