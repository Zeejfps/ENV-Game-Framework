using SoftwareRendererModule;
using SoftwareRendererOpenGlBackend;
using ZGF.BMFontModule;
using ZGF.Geometry;
using static GL46;

namespace ZGF.Gui.Sandbox;

internal readonly record struct DrawCommand(
    int Id, CommandKind Kind, int ZIndex, RectF Clip, float Opacity, float TranslationX, float TranslationY, float ScaleX, float ScaleY);

public sealed class SoftwareRenderedCanvas : ICanvas
{
    private readonly BitmapFont _font;
    private readonly ImageManager _imageManager;

    private readonly List<DrawCommand> _commands = new();
    private readonly Dictionary<int, DrawRectInputs> _rectCommandData = new();
    private readonly Dictionary<int, DrawTextInputs> _textCommandData = new();
    private readonly Dictionary<int, DrawImageInputs> _imageCommandData = new();
    private readonly Dictionary<int, DrawLineInputs> _lineCommandData = new();

    private Bitmap _colorBuffer;
    private BitmapRenderer _bitmapRenderer;
    private readonly Stack<RectF> _clipStack = new();
    private readonly Stack<float> _opacityStack = new();
    private readonly Stack<(float X, float Y)> _translationStack = new();
    private readonly Stack<(float X, float Y)> _scaleStack = new();

    private float CurrentOpacity() => _opacityStack.Count > 0 ? _opacityStack.Peek() : 1f;
    private (float X, float Y) CurrentTranslation() => _translationStack.Count > 0 ? _translationStack.Peek() : (0f, 0f);
    private (float X, float Y) CurrentScale() => _scaleStack.Count > 0 ? _scaleStack.Peek() : (1f, 1f);

    private static uint ScaleAlpha(uint argb, float opacity)
    {
        if (opacity >= 1f) return argb;
        var a = (uint)(((argb >> 24) & 0xFFu) * opacity + 0.5f);
        if (a > 255u) a = 255u;
        return (a << 24) | (argb & 0x00FFFFFFu);
    }

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
        _lineCommandData.Clear();
    }

    public void DrawRect(in DrawRectInputs inputs)
    {
        var id = _commands.Count;
        var clip = GetClip();
        var t = CurrentTranslation();
        var s = CurrentScale();
        _commands.Add(new DrawCommand(id, CommandKind.DrawRect, inputs.ZIndex, clip, CurrentOpacity(), t.X, t.Y, s.X, s.Y));
        _rectCommandData.Add(id, inputs);
    }

    public void DrawText(in DrawTextInputs inputs)
    {
        var id = _commands.Count;
        var clip = GetClip();
        var t = CurrentTranslation();
        var s = CurrentScale();
        _commands.Add(new DrawCommand(id, CommandKind.DrawText, inputs.ZIndex, clip, CurrentOpacity(), t.X, t.Y, s.X, s.Y));
        _textCommandData.Add(id, inputs);
    }

    public void DrawImage(in DrawImageInputs inputs)
    {
        var id = _commands.Count;
        var clip = GetClip();
        var t = CurrentTranslation();
        var s = CurrentScale();
        _commands.Add(new DrawCommand(id, CommandKind.DrawImage, inputs.ZIndex, clip, CurrentOpacity(), t.X, t.Y, s.X, s.Y));
        _imageCommandData.Add(id, inputs);
    }

    public void DrawLine(in DrawLineInputs inputs)
    {
        var id = _commands.Count;
        var clip = GetClip();
        var t = CurrentTranslation();
        var s = CurrentScale();
        _commands.Add(new DrawCommand(id, CommandKind.DrawLine, inputs.ZIndex, clip, CurrentOpacity(), t.X, t.Y, s.X, s.Y));
        _lineCommandData.Add(id, inputs);
    }

    public void DrawCircle(in DrawCircleInputs inputs)
    {
        // Circles are not implemented in the software canvas yet (used by GPU
        // backends only); no test currently rasterizes one.
    }

    public void DrawBezier(in DrawBezierInputs inputs)
    {
        // Bezier curves are not implemented in the software canvas yet (used by
        // GPU backends only); no test currently rasterizes one.
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

    public void PushOpacity(float opacity) => _opacityStack.Push(CurrentOpacity() * Math.Clamp(opacity, 0f, 1f));
    public void PopOpacity() { if (_opacityStack.Count > 0) _opacityStack.Pop(); }
    public void PushTranslation(float dx, float dy) { var c = CurrentTranslation(); var s = CurrentScale(); _translationStack.Push((c.X + dx * s.X, c.Y + dy * s.Y)); }
    public void PopTranslation() { if (_translationStack.Count > 0) _translationStack.Pop(); }

    public void PushScale(float sx, float sy, float pivotX, float pivotY)
    {
        var c = CurrentTranslation();
        var s = CurrentScale();
        _translationStack.Push((c.X + pivotX * (1f - sx) * s.X, c.Y + pivotY * (1f - sy) * s.Y));
        _scaleStack.Push((s.X * sx, s.Y * sy));
    }

    public void PopScale()
    {
        if (_scaleStack.Count > 0) _scaleStack.Pop();
        if (_translationStack.Count > 0) _translationStack.Pop();
    }

    public void PushClip(RectF rect)
    {
        // Compose with the active transform so clipped content drawn offset/scaled is clipped in the
        // same space it's drawn in (mirrors the GPU canvas).
        var tr = CurrentTranslation();
        var sc = CurrentScale();
        rect = new RectF { Left = rect.Left * sc.X + tr.X, Bottom = rect.Bottom * sc.Y + tr.Y, Width = rect.Width * sc.X, Height = rect.Height * sc.Y };

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

    public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style)
    {
        var len = Math.Clamp(prefixLength, 0, text.Length);
        return MeasureTextWidth(text[..len], style);
    }

    public float MeasureTextLineHeight(TextStyle style)
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
        var fontTextureWidth = (int)_font.Png.Ihdr.Width;
        
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
        // _commands.Sort((x, y) => x.ZIndex.CompareTo(y.ZIndex));
        
        var drawCommands = _commands
            .Select((cmd, index) => (cmd, index))
            .OrderBy(x => x.cmd.ZIndex) // Or .OrderByDescending
            .ThenBy(x => x.index)
            .Select(x => x.cmd)
            .ToList();
        
        foreach (var command in drawCommands)
        {
            switch (command.Kind)
            {
                case CommandKind.DrawRect:
                    var rectCommand = _rectCommandData[command.Id];
                    ExecuteDrawRectCommand(command, rectCommand);
                    break;
                case CommandKind.DrawText:
                    var textCommand = _textCommandData[command.Id];
                    ExecuteDrawTextCommand(command, textCommand);
                    break;
                case CommandKind.DrawImage:
                    var imageCommand = _imageCommandData[command.Id];
                    ExecuteCommand(command, imageCommand);
                    break;
                case CommandKind.DrawLine:
                    var lineCommand = _lineCommandData[command.Id];
                    ExecuteDrawLineCommand(command, lineCommand);
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

        var sx = cmd.ScaleX;
        var sy = cmd.ScaleY;
        var offsetX = (int)((x + (width - scaledWidth) / 2) * sx + cmd.TranslationX);
        var offsetY = (int)((y + (height - scaledHeight) / 2) * sy + cmd.TranslationY);

        Graphics.BlitTransparent(
            _colorBuffer, offsetX, offsetY, (int)(scaledWidth * sx), (int)(scaledHeight * sy),
            image, 0, 0, image.Width, image.Height,
            ScaleAlpha(data.TintColor, cmd.Opacity)
        );
    }

    private void ExecuteDrawRectCommand(in DrawCommand command, in DrawRectInputs data)
    {
        var style = data.Style;
        var position = data.Position;
        var op = command.Opacity;
        var sx = command.ScaleX;
        var sy = command.ScaleY;
        var tx = command.TranslationX;
        var ty = command.TranslationY;
        var left = (int)(position.Left * sx + tx);
        var right = (int)(position.Right * sx + tx) - 1;
        var bottom = (int)(position.Bottom * sy + ty);
        var top = (int)(position.Top * sy + ty) - 1;

        // Glyph bitmaps and border strokes stay at native thickness in the software path; only
        // positions and the fill extent scale (good enough for the sandbox — the GPU canvas scales
        // everything including text).
        var borderSize = style.BorderSize;

        var fillWidth = (int)(position.Width * sx) - (int)borderSize.Left - (int)borderSize.Right;
        var fillHeight = (int)(position.Height * sy) - (int)borderSize.Top - (int)borderSize.Bottom;

        var borderColor = style.BorderColor;
        var clipRect = command.Clip;
        Graphics.FillRectWithBlending(_colorBuffer,
            left +  (int)borderSize.Left, bottom + (int)borderSize.Bottom,
            fillWidth, fillHeight,
            ScaleAlpha(style.BackgroundColor, op),
            clipRect);

        // Left Border
        DrawBorder(left, bottom, left, top+1, ScaleAlpha(borderColor.Left, op), (int)borderSize.Left, 1, 0, clipRect);

        // Right Border
        DrawBorder(right, bottom, right, top + 1, ScaleAlpha(borderColor.Right, op), (int)borderSize.Right, -1, 0, clipRect);

        // Top Border
        DrawBorder(left, top, right + 1, top, ScaleAlpha(borderColor.Top, op), (int)borderSize.Top, 0, -1, clipRect);

        // Bottom Border
        DrawBorder(left, bottom, right, bottom, ScaleAlpha(borderColor.Bottom, op), (int)borderSize.Bottom, 0, 1, clipRect);
    }

    private void ExecuteDrawTextCommand(in DrawCommand cmd, in DrawTextInputs data)
    {
        var text = data.Text;

        var lineHeight = _font.FontMetrics.Common.LineHeight;
        var position = data.Position;
        
        //Graphics.DrawRect(_colorBuffer, (int)position.Left, (int)position.Bottom, (int)position.Width, (int)position.Height, 0x00ff00);
        
        var fontBase = _font.FontMetrics.Common.Base;
        var sx = cmd.ScaleX;
        var sy = cmd.ScaleY;
        var tx = cmd.TranslationX;
        var ty = cmd.TranslationY;

        // Glyph bitmaps stay native-size in the software path; only the pen origin is transformed.
        var lineStart = (int)(position.Left * sx + tx);
        var cursorX = lineStart;
        var cursorY = (int)((position.Top - fontBase) * sy + ty);

        var style = data.Style;
        if (style.VerticalAlignment.IsSet)
        {
            switch (style.VerticalAlignment.Value)
            {
                case TextAlignment.Start:
                    break;
                case TextAlignment.Center:
                    cursorY = (int)((position.Top - position.Height * 0.5f) * sy - (fontBase * 0.5f) + ty);
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
                    cursorX = (int)((position.Left + (position.Width - width) * 0.5f) * sx + tx);
                    break;
                case TextAlignment.End:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        //Graphics.DrawLine(_colorBuffer, cursorX, cursorY, cursorX + (int)position.Width, cursorY, 0xFF0000, cmd.Clip);
        
        var color = ScaleAlpha(style.TextColor, cmd.Opacity);
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

    private void ExecuteDrawLineCommand(in DrawCommand command, in DrawLineInputs data)
    {
        var sx = command.ScaleX;
        var sy = command.ScaleY;
        var tx = command.TranslationX;
        var ty = command.TranslationY;
        Graphics.DrawLine(_colorBuffer,
            (int)(data.Start.X * sx + tx), (int)(data.Start.Y * sy + ty),
            (int)(data.End.X * sx + tx), (int)(data.End.Y * sy + ty),
            ScaleAlpha(data.Color, command.Opacity), command.Clip);
    }

    public void DrawBoxShadow(in DrawBoxShadowInputs inputs)
    {
        // Box shadows not implemented in the software canvas yet.
    }
}