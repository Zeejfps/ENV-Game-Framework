using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ZGF.Fonts;
using ZGF.Geometry;

namespace ZGF.Gui;

public abstract class RenderedCanvasBase : ICanvas
{
    // ---------- Per-instance struct types ----------

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct RectInstance
    {
        public Vector4 Rect;            // x, y, w, h
        public Vector4 BorderRadius;    // tl, tr, br, bl
        public Vector4 BorderSize;      // top, right, bottom, left
        public uint BgColor;
        public uint BorderColorTop;
        public uint BorderColorRight;
        public uint BorderColorBottom;
        public uint BorderColorLeft;
        public uint ClipIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct GlyphInstance
    {
        public Vector4 Rect;
        public Vector4 AtlasUV;
        public uint Color;
        public uint ClipIndex;
        public float Rotation; // radians, rotation about the glyph rect's center
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ImageInstance
    {
        public Vector4 Rect;
        public Vector4 SrcUV;
        public uint Tint;
        public uint ClipIndex;
        public float Rotation; // radians, rotation about the rect's center
        public uint TextureId; // not uploaded to GPU; used only for batching
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ShadowInstance
    {
        public Vector4 OuterRect;       // drawn quad: covers the entire blurred shadow region
        public Vector4 ShadowRect;      // (post-offset, post-spread) source rect in world coords
        public Vector4 BorderRadius;    // tl, tr, br, bl
        public float Sigma;             // blur stddev
        public uint Color;              // ARGB packed
        public uint ClipIndex;
        private uint _pad;              // keep 16-byte alignment
    }

    // Generic anti-aliased SDF primitive (see canvas_shape shaders).
    //   ShapeType 0 filled circle: ShapeData = (cx, cy, radius, _),                       HalfWidth unused
    //   ShapeType 1 ring:          ShapeData = (cx, cy, radius, _),                       HalfWidth = half stroke
    //   ShapeType 2 line/capsule:  ShapeData = (x0, y0, x1, y1),                          HalfWidth = half thickness
    //   ShapeType 3 quad bezier:   ShapeData = (p0.xy, control.xy), ShapeData2 = (p2.xy, _, _), HalfWidth = half thickness
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct ShapeInstance
    {
        public Vector4 OuterRect;       // drawn quad (padded AABB), x, y, w, h
        public Vector4 ShapeData;
        public Vector4 ShapeData2;
        public float HalfWidth;
        public uint Color;              // ARGB packed
        public uint ShapeType;
        public uint ClipIndex;
    }

    private const uint ShapeFilledCircle = 0;
    private const uint ShapeRing = 1;
    private const uint ShapeLine = 2;
    private const uint ShapeBezier = 3;

    // Pack=1 so these stage entries have no trailing padding and can be compared
    // as raw bytes against the previous frame's snapshot (see FrameUnchanged).
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct StagedRect { public long Key; public RectInstance Inst; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct StagedGlyph { public long Key; public GlyphInstance Inst; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct StagedImage { public long Key; public ImageInstance Inst; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct StagedShadow { public long Key; public ShadowInstance Inst; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct StagedShape { public long Key; public ShapeInstance Inst; }

    public enum DrawKind : byte { Rect, Glyph, Image, Shadow, Shape }

    public readonly struct DrawCall
    {
        public DrawKind Kind { get; init; }
        public int InstanceStart { get; init; }
        public int InstanceCount { get; init; }
        public uint TextureId { get; init; } // image draws only
    }

    // ---------- Capacities ----------

    protected const int MaxRects = 4096;
    protected const int MaxGlyphs = 16384;
    protected const int MaxImages = 1024;
    protected const int MaxShadows = 512;
    protected const int MaxShapes = 2048;
    protected const int MaxClips = 256;

    // ---------- Per-frame staging state ----------

    private readonly List<StagedRect> _stagedRects = new();
    private readonly List<StagedGlyph> _stagedGlyphs = new();
    private readonly List<StagedImage> _stagedImages = new();
    private readonly List<StagedShadow> _stagedShadows = new();
    private readonly List<StagedShape> _stagedShapes = new();
    private readonly List<Vector4> _stagedClips = new();
    private readonly Stack<int> _clipStack = new();
    private int _sequence;

    // Sorted-output scratch (re-allocated as needed)
    private RectInstance[] _curRects = Array.Empty<RectInstance>();
    private GlyphInstance[] _curGlyphs = Array.Empty<GlyphInstance>();
    private ImageInstance[] _curImages = Array.Empty<ImageInstance>();
    private ShadowInstance[] _curShadows = Array.Empty<ShadowInstance>();
    private ShapeInstance[] _curShapes = Array.Empty<ShapeInstance>();
    private int _curRectCount, _curGlyphCount, _curImageCount, _curShadowCount, _curShapeCount;

    private DrawKind[] _itemKind = Array.Empty<DrawKind>();
    private int[] _itemSrc = Array.Empty<int>();
    private int[] _itemNext = Array.Empty<int>();
    private DrawKind[] _batchKind = Array.Empty<DrawKind>();
    private uint[] _batchTex = Array.Empty<uint>();
    private float[] _batchMinX = Array.Empty<float>();
    private float[] _batchMinY = Array.Empty<float>();
    private float[] _batchMaxX = Array.Empty<float>();
    private float[] _batchMaxY = Array.Empty<float>();
    private int[] _batchHead = Array.Empty<int>();
    private int[] _batchTail = Array.Empty<int>();
    private int[] _batchCountArr = Array.Empty<int>();
    private int _batchCount;

    // Previous-frame mirrors for byte-equal compare
    private RectInstance[] _prevRects = Array.Empty<RectInstance>();
    private GlyphInstance[] _prevGlyphs = Array.Empty<GlyphInstance>();
    private ImageInstance[] _prevImages = Array.Empty<ImageInstance>();
    private ShadowInstance[] _prevShadows = Array.Empty<ShadowInstance>();
    private ShapeInstance[] _prevShapes = Array.Empty<ShapeInstance>();
    private Vector4[] _prevClips = Array.Empty<Vector4>();
    private int _prevRectCount, _prevGlyphCount, _prevImageCount, _prevShadowCount, _prevShapeCount, _prevClipCount;

    // Previous-frame staged snapshots (pre-sort) used to detect an entirely
    // unchanged frame before paying for the sort/materialize pass.
    private StagedRect[] _snapRects = Array.Empty<StagedRect>();
    private StagedGlyph[] _snapGlyphs = Array.Empty<StagedGlyph>();
    private StagedImage[] _snapImages = Array.Empty<StagedImage>();
    private StagedShadow[] _snapShadows = Array.Empty<StagedShadow>();
    private StagedShape[] _snapShapes = Array.Empty<StagedShape>();
    private int _snapRectCount, _snapGlyphCount, _snapImageCount, _snapShadowCount, _snapShapeCount;
    private bool _hasSnapshot;

    private readonly List<DrawCall> _drawCalls = new();

    // ---------- Font/sizing state ----------

    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;
    private readonly Dictionary<string, FontHandle> _fontsByFamily = new();
    private int _width, _height;
    private float _dpiScale;

    public int LastFrameUploadCount { get; protected set; }

    protected RenderedCanvasBase(
        int width, int height,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        float dpiScale = 1f)
    {
        _width = width;
        _height = height;
        _fonts = fonts;
        _defaultFont = defaultFont;
        _dpiScale = dpiScale > 0f ? dpiScale : 1f;
    }

    public int Width => _width;
    public int Height => _height;
    // Canvas coordinates are in logical points. Glyphs are baked into the atlas
    // at device pixels (logical * DpiScale) and then drawn onto logical-sized
    // rects so the GPU downsamples instead of upscaling on HiDPI displays.
    public float DpiScale => _dpiScale;
    protected FreeTypeFontBackend FontBackend => _fonts;

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        OnResize(width, height);
    }

    /// <summary>
    /// Updates the DPI scale used for baking font glyph sizes into the atlas.
    /// Call this when a popup canvas is reused on a different monitor whose
    /// content scale differs from the one the canvas was created at — otherwise
    /// glyphs render at the wrong device-pixel size on the new monitor.
    /// </summary>
    public void UpdateDpiScale(float dpiScale)
    {
        if (dpiScale <= 0f) return;
        _dpiScale = dpiScale;
    }

    // ---------- ICanvas ----------

    public void BeginFrame()
    {
        _stagedRects.Clear();
        _stagedGlyphs.Clear();
        _stagedImages.Clear();
        _stagedShadows.Clear();
        _stagedShapes.Clear();
        _stagedClips.Clear();
        _clipStack.Clear();
        _sequence = 0;

        // Slot 0 is the default fullscreen clip.
        _stagedClips.Add(new Vector4(0, 0, _width, _height));
        _clipStack.Push(0);
    }

    public void DrawRect(in DrawRectInputs inputs)
    {
        var style = inputs.Style;
        var pos = inputs.Position;

        // Snap origin with Ceiling and size separately so drawn dimensions don't depend
        // on the fractional part of the origin (which would otherwise wobble +/- 1px
        // across frames as subpixel layout drift flips Round's banker's-rounding tie).
        var left = MathF.Ceiling(pos.Left);
        var bottom = MathF.Ceiling(pos.Bottom);
        var width = MathF.Ceiling(pos.Width);
        var height = MathF.Ceiling(pos.Height);

        _stagedRects.Add(new StagedRect
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new RectInstance
            {
                Rect = new Vector4(left, bottom, width, height),
                BorderRadius = new Vector4(
                    style.BorderRadius.TopLeft.Value,
                    style.BorderRadius.TopRight.Value,
                    style.BorderRadius.BottomRight.Value,
                    style.BorderRadius.BottomLeft.Value),
                BorderSize = new Vector4(
                    MathF.Round(style.BorderSize.Top.Value),
                    MathF.Round(style.BorderSize.Right.Value),
                    MathF.Round(style.BorderSize.Bottom.Value),
                    MathF.Round(style.BorderSize.Left.Value)),
                BgColor = style.BackgroundColor,
                BorderColorTop = style.BorderColor.Top.Value,
                BorderColorRight = style.BorderColor.Right.Value,
                BorderColorBottom = style.BorderColor.Bottom.Value,
                BorderColorLeft = style.BorderColor.Left.Value,
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawBoxShadow(in DrawBoxShadowInputs inputs)
    {
        var shadow = inputs.Shadow;
        if (!shadow.IsActive)
            return;

        var pos = inputs.Position;
        var radius = inputs.BorderRadius;

        var offsetX = shadow.OffsetX.IsSet ? shadow.OffsetX.Value : 0f;
        var offsetY = shadow.OffsetY.IsSet ? shadow.OffsetY.Value : 0f;
        var blur = shadow.Blur.IsSet ? shadow.Blur.Value : 0f;
        var spread = shadow.Spread.IsSet ? shadow.Spread.Value : 0f;
        // Treat the user-provided "blur" as a CSS-style blur radius (~2σ),
        // so the on-screen result roughly matches CSS box-shadow expectations.
        var sigma = MathF.Max(blur * 0.5f, 0.0001f);

        // The shifted+spread source rect that produces the shadow, in world coords.
        var sLeft = MathF.Floor(pos.Left + offsetX - spread);
        var sBottom = MathF.Floor(pos.Bottom + offsetY - spread);
        var sWidth = MathF.Ceiling(pos.Width + spread * 2f);
        var sHeight = MathF.Ceiling(pos.Height + spread * 2f);

        // Inflate the drawn quad to include the ~3σ penumbra.
        var pad = MathF.Ceiling(sigma * 3f + 1f);
        var oLeft = sLeft - pad;
        var oBottom = sBottom - pad;
        var oWidth = sWidth + pad * 2f;
        var oHeight = sHeight + pad * 2f;

        _stagedShadows.Add(new StagedShadow
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ShadowInstance
            {
                OuterRect = new Vector4(oLeft, oBottom, oWidth, oHeight),
                ShadowRect = new Vector4(sLeft, sBottom, sWidth, sHeight),
                BorderRadius = new Vector4(
                    radius.TopLeft.Value,
                    radius.TopRight.Value,
                    radius.BottomRight.Value,
                    radius.BottomLeft.Value),
                Sigma = sigma,
                Color = shadow.Color.Value,
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawLine(in DrawLineInputs inputs)
    {
        var p0 = inputs.Start;
        var p1 = inputs.End;
        var half = MathF.Max(inputs.Thickness, 0f) * 0.5f;
        var pad = half + 1.5f;
        var minX = MathF.Min(p0.X, p1.X) - pad;
        var minY = MathF.Min(p0.Y, p1.Y) - pad;
        var maxX = MathF.Max(p0.X, p1.X) + pad;
        var maxY = MathF.Max(p0.Y, p1.Y) + pad;

        _stagedShapes.Add(new StagedShape
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ShapeInstance
            {
                OuterRect = new Vector4(minX, minY, maxX - minX, maxY - minY),
                ShapeData = new Vector4(p0.X, p0.Y, p1.X, p1.Y),
                HalfWidth = half,
                Color = inputs.Color,
                ShapeType = ShapeLine,
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawBezier(in DrawBezierInputs inputs)
    {
        var p0 = inputs.Start;
        var p1 = inputs.Control;
        var p2 = inputs.End;
        var half = MathF.Max(inputs.Thickness, 0f) * 0.5f;
        var pad = half + 1.5f;
        var minX = MathF.Min(p0.X, MathF.Min(p1.X, p2.X)) - pad;
        var minY = MathF.Min(p0.Y, MathF.Min(p1.Y, p2.Y)) - pad;
        var maxX = MathF.Max(p0.X, MathF.Max(p1.X, p2.X)) + pad;
        var maxY = MathF.Max(p0.Y, MathF.Max(p1.Y, p2.Y)) + pad;

        _stagedShapes.Add(new StagedShape
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ShapeInstance
            {
                OuterRect = new Vector4(minX, minY, maxX - minX, maxY - minY),
                ShapeData = new Vector4(p0.X, p0.Y, p1.X, p1.Y),
                ShapeData2 = new Vector4(p2.X, p2.Y, 0f, 0f),
                HalfWidth = half,
                Color = inputs.Color,
                ShapeType = ShapeBezier,
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawCircle(in DrawCircleInputs inputs)
    {
        var c = inputs.Center;
        var r = MathF.Max(inputs.Radius, 0f);
        var stroke = MathF.Max(inputs.Thickness, 0f);
        var ring = stroke > 0f;
        var half = stroke * 0.5f;
        var ext = (ring ? r + half : r) + 1.5f;
        var minX = c.X - ext;
        var minY = c.Y - ext;

        _stagedShapes.Add(new StagedShape
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ShapeInstance
            {
                OuterRect = new Vector4(minX, minY, ext * 2f, ext * 2f),
                ShapeData = new Vector4(c.X, c.Y, r, 0f),
                HalfWidth = half,
                Color = inputs.Color,
                ShapeType = ring ? ShapeRing : ShapeFilledCircle,
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawText(in DrawTextInputs inputs)
    {
        var text = inputs.Text;
        if (string.IsNullOrEmpty(text))
            return;

        var style = inputs.Style;
        var font = ResolveFont(style);
        var color = style.TextColor.Value;
        var clip = (uint)_clipStack.Peek();
        var rotation = style.Rotation.Value;
        var seq = _sequence++;
        var key = MakeKey(inputs.ZIndex, seq);

        var pos = inputs.Position;
        // Metrics come back in device pixels (FT is set at logical * DpiScale);
        // convert to logical points for layout math.
        var invScale = 1f / _dpiScale;
        var metrics = _fonts.GetMetrics(font);
        var ascender = metrics.Ascender * invScale;
        var descender = metrics.Descender * invScale;
        var lineHeight = metrics.LineHeight * invScale;

        var lineStart = pos.Left;
        var baselineY = pos.Top - ascender;

        if (style.VerticalAlignment.IsSet && style.VerticalAlignment.Value == TextAlignment.Center)
        {
            var midline = pos.Top - pos.Height * 0.5f;
            baselineY = midline - (ascender + descender) * 0.5f;
        }

        var hCenter = style.HorizontalAlignment.IsSet && style.HorizontalAlignment.Value == TextAlignment.Center;
        var textSpan = text.AsSpan();

        var sliceStart = 0;
        while (sliceStart <= textSpan.Length)
        {
            var nl = textSpan[sliceStart..].IndexOf('\n');
            int lineEnd;
            if (nl < 0)
            {
                lineEnd = textSpan.Length;
            }
            else
            {
                lineEnd = sliceStart + nl;
            }

            var lineSlice = textSpan[sliceStart..lineEnd];

            ShapeAndDrawLine(font, lineSlice, lineStart, pos.Left, pos.Width, hCenter,
                baselineY, color, clip, rotation, key);

            if (nl < 0)
                break;

            baselineY -= lineHeight;
            sliceStart = lineEnd + 1;
        }
    }

    private void ShapeAndDrawLine(FontHandle font, ReadOnlySpan<char> line, float lineStartX,
        float boxLeft, float boxWidth, bool hCenter, float baselineY,
        uint color, uint clip, float rotation, long key)
    {
        if (line.Length == 0)
            return;

        const int StackCap = 256;
        Span<ShapedGlyph> shaped = line.Length <= StackCap
            ? stackalloc ShapedGlyph[StackCap]
            : new ShapedGlyph[line.Length * 2];

        var n = _fonts.ShapeText(font, line, shaped);

        var cursorX = lineStartX;
        if (hCenter)
        {
            // Width from the already-shaped run, so the line is shaped once instead
            // of twice (measure + emit).
            var total = 0f;
            for (var i = 0; i < n; i++)
                total += shaped[i].XAdvance;
            cursorX = boxLeft + (boxWidth - total / _dpiScale) * 0.5f;
        }

        var atlasWidth = (float)_fonts.AtlasWidth;
        var atlasHeight = (float)_fonts.AtlasHeight;
        // Shaped positions and glyph bitmap dims come back in device pixels;
        // convert to logical points for layout, but keep atlas-pixel dims for UV.
        var invScale = 1f / _dpiScale;

        for (var i = 0; i < n; i++)
        {
            var sg = shaped[i];
            if (!_fonts.TryGetGlyph(font, sg.GlyphIndex, out var glyph))
            {
                cursorX += sg.XAdvance * invScale;
                continue;
            }

            if (glyph.Width > 0 && glyph.Height > 0)
            {
                var glyphW = glyph.Width * invScale;
                var glyphH = glyph.Height * invScale;
                var glyphX = MathF.Round(cursorX + sg.XOffset * invScale) + glyph.BitmapLeft * invScale;
                var glyphY = MathF.Round(baselineY + sg.YOffset * invScale) + glyph.BitmapTop * invScale - glyphH;

                var atlasU = glyph.AtlasX / atlasWidth;
                var atlasV = glyph.AtlasY / atlasHeight;
                var atlasW = glyph.Width / atlasWidth;
                var atlasH = glyph.Height / atlasHeight;

                _stagedGlyphs.Add(new StagedGlyph
                {
                    Key = key,
                    Inst = new GlyphInstance
                    {
                        Rect = new Vector4(glyphX, glyphY, glyphW, glyphH),
                        AtlasUV = new Vector4(atlasU, atlasV, atlasW, atlasH),
                        Color = color,
                        ClipIndex = clip,
                        Rotation = rotation,
                    }
                });
            }

            cursorX += sg.XAdvance * invScale;
        }
    }

    private float MeasureLineWidth(FontHandle font, ReadOnlySpan<char> line)
    {
        if (line.Length == 0)
            return 0f;

        const int StackCap = 256;
        Span<ShapedGlyph> shaped = line.Length <= StackCap
            ? stackalloc ShapedGlyph[StackCap]
            : new ShapedGlyph[line.Length * 2];
        var n = _fonts.ShapeText(font, line, shaped);
        var total = 0f;
        for (var i = 0; i < n; i++)
            total += shaped[i].XAdvance;
        return total / _dpiScale;
    }

    public void RegisterFont(string family, FontHandle handle)
    {
        _fontsByFamily[family] = handle;
    }

    /// <summary>
    /// Copies the source canvas's font-family registry onto this canvas. Popup
    /// canvases need this so views using non-default font families (e.g. icons,
    /// monospace) can resolve them when measuring/drawing text in a popup window.
    /// </summary>
    public void CopyFontsFrom(RenderedCanvasBase source)
    {
        foreach (var kv in source._fontsByFamily)
            _fontsByFamily[kv.Key] = kv.Value;
    }

    private FontHandle ResolveFont(TextStyle style)
    {
        var baseFont = _defaultFont;
        if (style.FontFamily.IsSet && style.FontFamily.Value is { } family &&
            _fontsByFamily.TryGetValue(family, out var resolved))
        {
            baseFont = resolved;
        }

        if (style.FontSize.IsSet)
        {
            // Caller's FontSize is in logical points; bake at device pixels so the
            // atlas glyph is rendered 1:1 by the linear sampler on Retina.
            var pixelSize = (int)MathF.Round(style.FontSize.Value * _dpiScale);
            if (pixelSize > 0)
                baseFont = _fonts.GetSizedVariant(baseFont, pixelSize);
        }

        if (style.FontWeight.IsSet && style.FontWeight.Value == FontWeight.Bold)
            baseFont = _fonts.GetEmboldenedVariant(baseFont);

        return baseFont;
    }

    public void DrawImage(in DrawImageInputs inputs)
    {
        var pos = inputs.Position;
        var imageId = inputs.ImageId;
        var size = GetImageSize(imageId);
        var imageW = (int)size.Width;
        var imageH = (int)size.Height;
        var rectW = (int)pos.Width;
        var rectH = (int)pos.Height;

        // Aspect-fit: scale to longest matching extent, then center.
        var aspect = (float)imageW / imageH;
        float scaledWidth, scaledHeight;
        if (aspect > (float)rectW / rectH)
        {
            scaledWidth = rectW;
            scaledHeight = rectW / aspect;
        }
        else
        {
            scaledHeight = rectH;
            scaledWidth = rectH * aspect;
        }

        var offsetX = pos.Left + (rectW - scaledWidth) * 0.5f;
        var offsetY = pos.Bottom + (rectH - scaledHeight) * 0.5f;

        var snappedLeft = MathF.Round(offsetX);
        var snappedBottom = MathF.Round(offsetY);
        var snappedRight = MathF.Round(offsetX + scaledWidth);
        var snappedTop = MathF.Round(offsetY + scaledHeight);

        _stagedImages.Add(new StagedImage
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ImageInstance
            {
                Rect = new Vector4(snappedLeft, snappedBottom, snappedRight - snappedLeft, snappedTop - snappedBottom),
                SrcUV = new Vector4(0f, 0f, 1f, 1f),
                Tint = inputs.TintColor,
                ClipIndex = (uint)_clipStack.Peek(),
                Rotation = inputs.Rotation,
                TextureId = GetImageTextureId(imageId),
            }
        });
    }

    public bool TryGetClip(out RectF rect)
    {
        if (_clipStack.Count <= 1)
        {
            rect = default;
            return false;
        }

        var slot = _clipStack.Peek();
        var c = _stagedClips[slot];
        rect = new RectF(c.X, c.Y, c.Z - c.X, c.W - c.Y);
        return true;
    }

    public void PushClip(RectF rect)
    {
        var current = _stagedClips[_clipStack.Peek()];
        var left = MathF.Ceiling(MathF.Max(rect.Left, current.X));
        var bottom = MathF.Ceiling(MathF.Max(rect.Bottom, current.Y));
        var right = MathF.Floor(MathF.Min(rect.Right, current.Z));
        var top = MathF.Floor(MathF.Min(rect.Top, current.W));
        if (right < left) right = left;
        if (top < bottom) top = bottom;

        var merged = new Vector4(left, bottom, right, top);
        var slot = InternClipSlot(merged);
        _clipStack.Push(slot);
    }

    public void PopClip()
    {
        // Always keep the default fullscreen clip on the stack.
        if (_clipStack.Count > 1)
            _clipStack.Pop();
    }

    private int InternClipSlot(Vector4 clip)
    {
        // Linear scan dedup. Frame-level clip count is small enough to make this cheap.
        for (var i = 0; i < _stagedClips.Count; i++)
        {
            if (_stagedClips[i] == clip)
                return i;
        }
        if (_stagedClips.Count >= MaxClips)
            return 0; // Overflow fallback: default fullscreen clip.
        var idx = _stagedClips.Count;
        _stagedClips.Add(clip);
        return idx;
    }

    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style)
    {
        var font = ResolveFont(style);
        // Multi-line text: width is the widest line's shaped advance.
        var max = 0f;
        var i = 0;
        while (i <= text.Length)
        {
            var nl = text[i..].IndexOf('\n');
            var lineEnd = nl < 0 ? text.Length : i + nl;
            var w = MeasureLineWidth(font, text[i..lineEnd]);
            if (w > max) max = w;
            if (nl < 0) break;
            i = lineEnd + 1;
        }
        return max;
    }

    public float MeasureTextLineHeight(TextStyle style) => _fonts.GetMetrics(ResolveFont(style)).LineHeight / _dpiScale;

    public Size GetImageSize(string imageId) => GetImageSizeImpl(imageId);
    public int GetImageWidth(string imageId) => (int)GetImageSize(imageId).Width;
    public int GetImageHeight(string imageId) => (int)GetImageSize(imageId).Height;

    // ---------- EndFrame: sort, batch, upload, draw ----------

    public void EndFrame()
    {
        if (FrameUnchanged())
        {
            // Idle frame: staged content is byte-identical to last frame, so the
            // sorted buffers, GPU uploads and draw calls from last frame are all
            // still valid. Skip the whole sort/materialize/upload/build pass.
            LastFrameUploadCount = 0;
            UpdateAtlasIfDirty();
            IssueDraws(_drawCalls);
            return;
        }

        SnapshotStaged();
        SortStaged();
        BuildBatches();
        UploadIfChanged();
        UpdateAtlasIfDirty();
        IssueDraws(_drawCalls);
    }

    private bool FrameUnchanged()
    {
        // Draw order is deterministic for a static UI, so the unsorted staged
        // buffers are byte-identical frame-to-frame when nothing changed. Checking
        // this before sorting lets idle frames skip the whole materialize pass.
        return _hasSnapshot
            && StagedMatch(_stagedRects, _snapRects, _snapRectCount)
            && StagedMatch(_stagedGlyphs, _snapGlyphs, _snapGlyphCount)
            && StagedMatch(_stagedImages, _snapImages, _snapImageCount)
            && StagedMatch(_stagedShadows, _snapShadows, _snapShadowCount)
            && StagedMatch(_stagedShapes, _snapShapes, _snapShapeCount)
            && ArraysMatch(_stagedClips, _prevClips, _prevClipCount);
    }

    private void SnapshotStaged()
    {
        SnapshotList(_stagedRects, ref _snapRects, out _snapRectCount);
        SnapshotList(_stagedGlyphs, ref _snapGlyphs, out _snapGlyphCount);
        SnapshotList(_stagedImages, ref _snapImages, out _snapImageCount);
        SnapshotList(_stagedShadows, ref _snapShadows, out _snapShadowCount);
        SnapshotList(_stagedShapes, ref _snapShapes, out _snapShapeCount);
        _hasSnapshot = true;
    }

    private static void SnapshotList<T>(List<T> src, ref T[] dst, out int count) where T : unmanaged
    {
        EnsureCapacity(ref dst, src.Count);
        CollectionsMarshal.AsSpan(src).CopyTo(dst);
        count = src.Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool StagedMatch<T>(List<T> cur, T[] snap, int snapCount) where T : unmanaged
    {
        if (cur.Count != snapCount) return false;
        var a = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(cur));
        var b = MemoryMarshal.AsBytes(snap.AsSpan(0, snapCount));
        return a.SequenceEqual(b);
    }

    private void SortStaged()
    {
        _stagedRects.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedGlyphs.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedImages.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedShadows.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedShapes.Sort(static (a, b) => a.Key.CompareTo(b.Key));
    }

    private static void EnsureCapacity<T>(ref T[] array, int count)
    {
        if (array.Length < count)
            array = new T[Math.Max(count, array.Length * 2)];
    }

    // Walks primitives in paint order (z, then sequence) and merges each into the
    // most recent same-kind draw call reachable without crossing an overlapping
    // primitive of another kind, so reordered primitives never overlap and the
    // result is pixel-identical to strict paint order.
    private void BuildBatches()
    {
        var total = _stagedRects.Count + _stagedGlyphs.Count + _stagedImages.Count + _stagedShadows.Count + _stagedShapes.Count;
        EnsureCapacity(ref _itemKind, total);
        EnsureCapacity(ref _itemSrc, total);
        EnsureCapacity(ref _itemNext, total);
        EnsureCapacity(ref _batchKind, total);
        EnsureCapacity(ref _batchTex, total);
        EnsureCapacity(ref _batchMinX, total);
        EnsureCapacity(ref _batchMinY, total);
        EnsureCapacity(ref _batchMaxX, total);
        EnsureCapacity(ref _batchMaxY, total);
        EnsureCapacity(ref _batchHead, total);
        EnsureCapacity(ref _batchTail, total);
        EnsureCapacity(ref _batchCountArr, total);
        _batchCount = 0;

        int ri = 0, gi = 0, ii = 0, si = 0, shi = 0;
        var rN = _stagedRects.Count;
        var gN = _stagedGlyphs.Count;
        var iN = _stagedImages.Count;
        var sN = _stagedShadows.Count;
        var shN = _stagedShapes.Count;
        var item = 0;

        while (ri < rN || gi < gN || ii < iN || si < sN || shi < shN)
        {
            var rKey = ri < rN ? _stagedRects[ri].Key : long.MaxValue;
            var gKey = gi < gN ? _stagedGlyphs[gi].Key : long.MaxValue;
            var iKey = ii < iN ? _stagedImages[ii].Key : long.MaxValue;
            var sKey = si < sN ? _stagedShadows[si].Key : long.MaxValue;
            var shKey = shi < shN ? _stagedShapes[shi].Key : long.MaxValue;

            DrawKind pick;
            if (rKey <= gKey && rKey <= iKey && rKey <= sKey && rKey <= shKey) pick = DrawKind.Rect;
            else if (gKey <= iKey && gKey <= sKey && gKey <= shKey) pick = DrawKind.Glyph;
            else if (iKey <= sKey && iKey <= shKey) pick = DrawKind.Image;
            else if (sKey <= shKey) pick = DrawKind.Shadow;
            else pick = DrawKind.Shape;

            int src;
            uint tex = 0;
            float minX, minY, maxX, maxY;
            switch (pick)
            {
                case DrawKind.Rect:
                    src = ri++;
                    Bounds(_stagedRects[src].Inst.Rect, 0f, _stagedRects[src].Inst.ClipIndex, out minX, out minY, out maxX, out maxY);
                    break;
                case DrawKind.Glyph:
                    src = gi++;
                    Bounds(_stagedGlyphs[src].Inst.Rect, _stagedGlyphs[src].Inst.Rotation, _stagedGlyphs[src].Inst.ClipIndex, out minX, out minY, out maxX, out maxY);
                    break;
                case DrawKind.Image:
                    src = ii++;
                    tex = _stagedImages[src].Inst.TextureId;
                    Bounds(_stagedImages[src].Inst.Rect, _stagedImages[src].Inst.Rotation, _stagedImages[src].Inst.ClipIndex, out minX, out minY, out maxX, out maxY);
                    break;
                case DrawKind.Shadow:
                    src = si++;
                    Bounds(_stagedShadows[src].Inst.OuterRect, 0f, _stagedShadows[src].Inst.ClipIndex, out minX, out minY, out maxX, out maxY);
                    break;
                default:
                    src = shi++;
                    Bounds(_stagedShapes[src].Inst.OuterRect, 0f, _stagedShapes[src].Inst.ClipIndex, out minX, out minY, out maxX, out maxY);
                    break;
            }

            _itemKind[item] = pick;
            _itemSrc[item] = src;
            AssignBatch(item, pick, tex, minX, minY, maxX, maxY);
            item++;
        }

        Materialize();
    }

    private void AssignBatch(int item, DrawKind kind, uint tex, float minX, float minY, float maxX, float maxY)
    {
        var empty = maxX <= minX || maxY <= minY;
        var target = -1;
        for (var b = _batchCount - 1; b >= 0; b--)
        {
            if (_batchKind[b] == kind && (kind != DrawKind.Image || _batchTex[b] == tex))
            {
                target = b;
                break;
            }
            if (!empty &&
                !(maxX <= _batchMinX[b] || _batchMaxX[b] <= minX || maxY <= _batchMinY[b] || _batchMaxY[b] <= minY))
                break;
        }

        if (target < 0)
        {
            target = _batchCount++;
            _batchKind[target] = kind;
            _batchTex[target] = tex;
            _batchMinX[target] = float.PositiveInfinity;
            _batchMinY[target] = float.PositiveInfinity;
            _batchMaxX[target] = float.NegativeInfinity;
            _batchMaxY[target] = float.NegativeInfinity;
            _batchHead[target] = -1;
            _batchTail[target] = -1;
            _batchCountArr[target] = 0;
        }

        if (!empty)
        {
            if (minX < _batchMinX[target]) _batchMinX[target] = minX;
            if (minY < _batchMinY[target]) _batchMinY[target] = minY;
            if (maxX > _batchMaxX[target]) _batchMaxX[target] = maxX;
            if (maxY > _batchMaxY[target]) _batchMaxY[target] = maxY;
        }

        if (_batchHead[target] < 0) _batchHead[target] = item;
        else _itemNext[_batchTail[target]] = item;
        _batchTail[target] = item;
        _itemNext[item] = -1;
        _batchCountArr[target]++;
    }

    private void Materialize()
    {
        EnsureCapacity(ref _curRects, _stagedRects.Count);
        EnsureCapacity(ref _curGlyphs, _stagedGlyphs.Count);
        EnsureCapacity(ref _curImages, _stagedImages.Count);
        EnsureCapacity(ref _curShadows, _stagedShadows.Count);
        EnsureCapacity(ref _curShapes, _stagedShapes.Count);

        _drawCalls.Clear();
        int rc = 0, gc = 0, ic = 0, sc = 0, shc = 0;

        for (var b = 0; b < _batchCount; b++)
        {
            var kind = _batchKind[b];
            int start;
            switch (kind)
            {
                case DrawKind.Rect:
                    start = rc;
                    for (var it = _batchHead[b]; it >= 0; it = _itemNext[it])
                        _curRects[rc++] = _stagedRects[_itemSrc[it]].Inst;
                    break;
                case DrawKind.Glyph:
                    start = gc;
                    for (var it = _batchHead[b]; it >= 0; it = _itemNext[it])
                        _curGlyphs[gc++] = _stagedGlyphs[_itemSrc[it]].Inst;
                    break;
                case DrawKind.Image:
                    start = ic;
                    for (var it = _batchHead[b]; it >= 0; it = _itemNext[it])
                        _curImages[ic++] = _stagedImages[_itemSrc[it]].Inst;
                    break;
                case DrawKind.Shadow:
                    start = sc;
                    for (var it = _batchHead[b]; it >= 0; it = _itemNext[it])
                        _curShadows[sc++] = _stagedShadows[_itemSrc[it]].Inst;
                    break;
                default:
                    start = shc;
                    for (var it = _batchHead[b]; it >= 0; it = _itemNext[it])
                        _curShapes[shc++] = _stagedShapes[_itemSrc[it]].Inst;
                    break;
            }

            _drawCalls.Add(new DrawCall
            {
                Kind = kind,
                InstanceStart = start,
                InstanceCount = _batchCountArr[b],
                TextureId = _batchTex[b],
            });
        }

        _curRectCount = rc;
        _curGlyphCount = gc;
        _curImageCount = ic;
        _curShadowCount = sc;
        _curShapeCount = shc;
    }

    private void Bounds(Vector4 rect, float rotation, uint clipIndex,
        out float minX, out float minY, out float maxX, out float maxY)
    {
        minX = rect.X;
        minY = rect.Y;
        maxX = rect.X + rect.Z;
        maxY = rect.Y + rect.W;

        if (rotation != 0f)
        {
            var cx = (minX + maxX) * 0.5f;
            var cy = (minY + maxY) * 0.5f;
            var hw = (maxX - minX) * 0.5f;
            var hh = (maxY - minY) * 0.5f;
            var cs = MathF.Abs(MathF.Cos(rotation));
            var sn = MathF.Abs(MathF.Sin(rotation));
            var ex = hw * cs + hh * sn;
            var ey = hw * sn + hh * cs;
            minX = cx - ex; maxX = cx + ex;
            minY = cy - ey; maxY = cy + ey;
        }

        var clip = _stagedClips[(int)clipIndex];
        if (clip.X > minX) minX = clip.X;
        if (clip.Y > minY) minY = clip.Y;
        if (clip.Z < maxX) maxX = clip.Z;
        if (clip.W < maxY) maxY = clip.W;
    }

    private void UploadIfChanged()
    {
        LastFrameUploadCount = 0;

        if (!ArraysMatch(_stagedClips, _prevClips, _prevClipCount))
        {
            EnsureCapacity(ref _prevClips, _stagedClips.Count);
            for (var i = 0; i < _stagedClips.Count; i++) _prevClips[i] = _stagedClips[i];
            _prevClipCount = _stagedClips.Count;

            UploadClips(_stagedClips);
            LastFrameUploadCount++;
        }

        if (!ArraysMatch(_curRects, _curRectCount, _prevRects, _prevRectCount))
        {
            EnsureCapacity(ref _prevRects, _curRectCount);
            Array.Copy(_curRects, _prevRects, _curRectCount);
            _prevRectCount = _curRectCount;

            UploadRectInstances(_curRects, _curRectCount);
            LastFrameUploadCount++;
        }

        if (!ArraysMatch(_curGlyphs, _curGlyphCount, _prevGlyphs, _prevGlyphCount))
        {
            EnsureCapacity(ref _prevGlyphs, _curGlyphCount);
            Array.Copy(_curGlyphs, _prevGlyphs, _curGlyphCount);
            _prevGlyphCount = _curGlyphCount;

            UploadGlyphInstances(_curGlyphs, _curGlyphCount);
            LastFrameUploadCount++;
        }

        if (!ArraysMatch(_curImages, _curImageCount, _prevImages, _prevImageCount))
        {
            EnsureCapacity(ref _prevImages, _curImageCount);
            Array.Copy(_curImages, _prevImages, _curImageCount);
            _prevImageCount = _curImageCount;

            UploadImageInstances(_curImages, _curImageCount);
            LastFrameUploadCount++;
        }

        if (!ArraysMatch(_curShadows, _curShadowCount, _prevShadows, _prevShadowCount))
        {
            EnsureCapacity(ref _prevShadows, _curShadowCount);
            Array.Copy(_curShadows, _prevShadows, _curShadowCount);
            _prevShadowCount = _curShadowCount;

            UploadShadowInstances(_curShadows, _curShadowCount);
            LastFrameUploadCount++;
        }

        if (!ArraysMatch(_curShapes, _curShapeCount, _prevShapes, _prevShapeCount))
        {
            EnsureCapacity(ref _prevShapes, _curShapeCount);
            Array.Copy(_curShapes, _prevShapes, _curShapeCount);
            _prevShapeCount = _curShapeCount;

            UploadShapeInstances(_curShapes, _curShapeCount);
            LastFrameUploadCount++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ArraysMatch<T>(T[] cur, int curCount, T[] prev, int prevCount) where T : unmanaged
    {
        if (curCount != prevCount) return false;
        var a = MemoryMarshal.AsBytes(cur.AsSpan(0, curCount));
        var b = MemoryMarshal.AsBytes(prev.AsSpan(0, curCount));
        return a.SequenceEqual(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ArraysMatch(List<Vector4> cur, Vector4[] prev, int prevCount)
    {
        if (cur.Count != prevCount) return false;
        var a = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(cur));
        var b = MemoryMarshal.AsBytes(prev.AsSpan(0, prevCount));
        return a.SequenceEqual(b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long MakeKey(int z, int seq)
    {
        // Z is signed int; offset by int.MinValue so the unsigned reinterpretation sorts correctly.
        var zUnsigned = (uint)(z - int.MinValue);
        return ((long)zUnsigned << 32) | (uint)seq;
    }

    // ---------- Abstract hooks ----------

    protected abstract void UploadRectInstances(RectInstance[] data, int count);
    protected abstract void UploadGlyphInstances(GlyphInstance[] data, int count);
    protected abstract void UploadImageInstances(ImageInstance[] data, int count);
    protected abstract void UploadShadowInstances(ShadowInstance[] data, int count);
    protected abstract void UploadShapeInstances(ShapeInstance[] data, int count);
    protected abstract void UploadClips(List<Vector4> clips);
    protected abstract void UpdateAtlasIfDirty();
    protected abstract void IssueDraws(IReadOnlyList<DrawCall> drawCalls);
    protected abstract void OnResize(int width, int height);
    protected abstract Size GetImageSizeImpl(string imageId);
    protected abstract uint GetImageTextureId(string imageId);
}
