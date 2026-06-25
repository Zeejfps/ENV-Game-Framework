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
    //
    // Line-only stroke styling rides along in spare fields:
    //   ShapeData2.zw = (dash length, gap length)   Flags bit 2 enables dashing
    //   Color2        = gradient end color          Flags bit 3 enables Color -> Color2 gradient
    //   Flags bits 0-1 = cap style (0 round, 1 butt, 2 square)
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
        public uint Color2;             // ARGB packed (line gradient end)
        public uint Flags;              // bits 0-1 cap, bit 2 dash, bit 3 gradient
    }

    private const uint ShapeFilledCircle = 0;
    private const uint ShapeRing = 1;
    private const uint ShapeLine = 2;
    private const uint ShapeBezier = 3;

    private const uint FlagDash = 1u << 2;
    private const uint FlagGradient = 1u << 3;

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

    // Render-only opacity (alpha multiplier) and an affine transform (scale + translation), composed
    // via push/pop like the clip stack but BAKED into each staged instance's color/position at stage
    // time — so the GPU sees ordinary opaque geometry and no shader/vertex-format change is needed.
    // A drawn local point p maps to p * _scale + _translation; sizes scale by _scale and radial
    // quantities (radius, stroke, corner radius, blur) by the mean of the two axes. The cached
    // _opacity/_scale/_translation mirror the stack tops for cheap per-draw reads (the glyph loop
    // reads them per glyph). All seeded to identity in BeginFrame; the seed is never popped (the
    // Pop* methods keep one entry as a floor, mirroring the clip stack's slot-0 invariant).
    private readonly Stack<float> _opacityStack = new();
    private readonly Stack<(Vector2 Scale, Vector2 Translation)> _xformStack = new();
    private float _opacity = 1f;
    private Vector2 _scale = Vector2.One;
    private Vector2 _translation = Vector2.Zero;

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

    /// <summary>
    /// Base paragraph direction applied to text whose <see cref="TextStyle.BaseDirection"/> is unset —
    /// i.e. the UI's writing direction. <see cref="BidiDirection.Auto"/> (the default) keeps the
    /// first-strong per-line heuristic and leaves Start alignment on the left; an RTL locale sets
    /// <see cref="BidiDirection.Rtl"/> so neutral/ambiguous lines lay out right-to-left and
    /// Start-aligned text moves to the right edge.
    /// </summary>
    public BidiDirection DefaultBaseDirection { get; set; } = BidiDirection.Auto;

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

        // Seed opacity/transform to identity. The seed is never popped (the Pop* methods keep one
        // entry as a floor, mirroring the clip stack's slot-0 invariant).
        _opacityStack.Clear();
        _opacityStack.Push(1f);
        _opacity = 1f;
        _xformStack.Clear();
        _xformStack.Push((Vector2.One, Vector2.Zero));
        _scale = Vector2.One;
        _translation = Vector2.Zero;
    }

    public void DrawRect(in DrawRectInputs inputs)
    {
        var style = inputs.Style;
        var pos = inputs.Position;

        // Snap origin with Ceiling and size separately so drawn dimensions don't depend
        // on the fractional part of the origin (which would otherwise wobble +/- 1px
        // across frames as subpixel layout drift flips Round's banker's-rounding tie).
        var savg = (_scale.X + _scale.Y) * 0.5f;
        var left = MathF.Ceiling(pos.Left * _scale.X + _translation.X);
        var bottom = MathF.Ceiling(pos.Bottom * _scale.Y + _translation.Y);
        var width = MathF.Ceiling(pos.Width * _scale.X);
        var height = MathF.Ceiling(pos.Height * _scale.Y);

        _stagedRects.Add(new StagedRect
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new RectInstance
            {
                Rect = new Vector4(left, bottom, width, height),
                BorderRadius = new Vector4(
                    style.BorderRadius.TopLeft.Value * savg,
                    style.BorderRadius.TopRight.Value * savg,
                    style.BorderRadius.BottomRight.Value * savg,
                    style.BorderRadius.BottomLeft.Value * savg),
                BorderSize = new Vector4(
                    MathF.Round(style.BorderSize.Top.Value * savg),
                    MathF.Round(style.BorderSize.Right.Value * savg),
                    MathF.Round(style.BorderSize.Bottom.Value * savg),
                    MathF.Round(style.BorderSize.Left.Value * savg)),
                BgColor = Tint(style.BackgroundColor),
                BorderColorTop = Tint(style.BorderColor.Top.Value),
                BorderColorRight = Tint(style.BorderColor.Right.Value),
                BorderColorBottom = Tint(style.BorderColor.Bottom.Value),
                BorderColorLeft = Tint(style.BorderColor.Left.Value),
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
        var sx = _scale.X;
        var sy = _scale.Y;
        var savg = (sx + sy) * 0.5f;

        var offsetX = shadow.OffsetX.IsSet ? shadow.OffsetX.Value : 0f;
        var offsetY = shadow.OffsetY.IsSet ? shadow.OffsetY.Value : 0f;
        var blur = shadow.Blur.IsSet ? shadow.Blur.Value : 0f;
        var spread = shadow.Spread.IsSet ? shadow.Spread.Value : 0f;
        // Treat the user-provided "blur" as a CSS-style blur radius (~2σ),
        // so the on-screen result roughly matches CSS box-shadow expectations.
        var sigma = MathF.Max(blur * 0.5f * savg, 0.0001f);

        // The shifted+spread source rect that produces the shadow, in world coords.
        var sLeft = MathF.Floor((pos.Left + offsetX - spread) * sx + _translation.X);
        var sBottom = MathF.Floor((pos.Bottom + offsetY - spread) * sy + _translation.Y);
        var sWidth = MathF.Ceiling((pos.Width + spread * 2f) * sx);
        var sHeight = MathF.Ceiling((pos.Height + spread * 2f) * sy);

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
                    radius.TopLeft.Value * savg,
                    radius.TopRight.Value * savg,
                    radius.BottomRight.Value * savg,
                    radius.BottomLeft.Value * savg),
                Sigma = sigma,
                Color = Tint(shadow.Color.Value),
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawLine(in DrawLineInputs inputs)
    {
        var p0 = inputs.Start;
        var p1 = inputs.End;
        var sx = _scale.X;
        var sy = _scale.Y;
        var savg = (sx + sy) * 0.5f;
        var tx = _translation.X;
        var ty = _translation.Y;
        var x0 = p0.X * sx + tx;
        var y0 = p0.Y * sy + ty;
        var x1 = p1.X * sx + tx;
        var y1 = p1.Y * sy + ty;
        var half = MathF.Max(inputs.Thickness, 0f) * 0.5f * savg;
        var pad = half + 1.5f;
        // Square caps extend ~half past each end; widen the AABB so a diagonal
        // cap corner isn't clipped.
        if (inputs.Cap == LineCap.Square) pad += half;
        var minX = MathF.Min(x0, x1) - pad;
        var minY = MathF.Min(y0, y1) - pad;
        var maxX = MathF.Max(x0, x1) + pad;
        var maxY = MathF.Max(y0, y1) + pad;

        var dashed = inputs.DashLength > 0f && inputs.GapLength > 0f;
        var grad = inputs.GradientEndColor.HasValue;
        var flags = (uint)inputs.Cap;
        if (dashed) flags |= FlagDash;
        if (grad) flags |= FlagGradient;

        _stagedShapes.Add(new StagedShape
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ShapeInstance
            {
                OuterRect = new Vector4(minX, minY, maxX - minX, maxY - minY),
                ShapeData = new Vector4(x0, y0, x1, y1),
                ShapeData2 = new Vector4(0f, 0f, dashed ? inputs.DashLength * savg : 0f, dashed ? inputs.GapLength * savg : 0f),
                HalfWidth = half,
                Color = Tint(inputs.Color),
                Color2 = Tint(grad ? inputs.GradientEndColor!.Value : inputs.Color),
                ShapeType = ShapeLine,
                Flags = flags,
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawBezier(in DrawBezierInputs inputs)
    {
        var p0 = inputs.Start;
        var p1 = inputs.Control;
        var p2 = inputs.End;
        var sx = _scale.X;
        var sy = _scale.Y;
        var savg = (sx + sy) * 0.5f;
        var tx = _translation.X;
        var ty = _translation.Y;
        var x0 = p0.X * sx + tx;
        var y0 = p0.Y * sy + ty;
        var x1 = p1.X * sx + tx;
        var y1 = p1.Y * sy + ty;
        var x2 = p2.X * sx + tx;
        var y2 = p2.Y * sy + ty;
        var half = MathF.Max(inputs.Thickness, 0f) * 0.5f * savg;
        var pad = half + 1.5f;
        var minX = MathF.Min(x0, MathF.Min(x1, x2)) - pad;
        var minY = MathF.Min(y0, MathF.Min(y1, y2)) - pad;
        var maxX = MathF.Max(x0, MathF.Max(x1, x2)) + pad;
        var maxY = MathF.Max(y0, MathF.Max(y1, y2)) + pad;

        var grad = inputs.GradientEndColor.HasValue;
        var dashed = inputs.DashLength > 0f && inputs.GapLength > 0f;
        var flags = 0u;
        if (grad) flags |= FlagGradient;
        if (dashed) flags |= FlagDash;

        _stagedShapes.Add(new StagedShape
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ShapeInstance
            {
                OuterRect = new Vector4(minX, minY, maxX - minX, maxY - minY),
                ShapeData = new Vector4(x0, y0, x1, y1),
                ShapeData2 = new Vector4(x2, y2, dashed ? inputs.DashLength * savg : 0f, dashed ? inputs.GapLength * savg : 0f),
                HalfWidth = half,
                Color = Tint(inputs.Color),
                Color2 = Tint(grad ? inputs.GradientEndColor!.Value : inputs.Color),
                ShapeType = ShapeBezier,
                Flags = flags,
                ClipIndex = (uint)_clipStack.Peek(),
            }
        });
    }

    public void DrawCircle(in DrawCircleInputs inputs)
    {
        var c = inputs.Center;
        var savg = (_scale.X + _scale.Y) * 0.5f;
        var cx = c.X * _scale.X + _translation.X;
        var cy = c.Y * _scale.Y + _translation.Y;
        var r = MathF.Max(inputs.Radius, 0f) * savg;
        var stroke = MathF.Max(inputs.Thickness, 0f) * savg;
        var ring = stroke > 0f;
        var half = stroke * 0.5f;
        var ext = (ring ? r + half : r) + 1.5f;
        var minX = cx - ext;
        var minY = cy - ext;

        _stagedShapes.Add(new StagedShape
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ShapeInstance
            {
                OuterRect = new Vector4(minX, minY, ext * 2f, ext * 2f),
                ShapeData = new Vector4(cx, cy, r, 0f),
                HalfWidth = half,
                Color = Tint(inputs.Color),
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
        var color = Tint(style.TextColor.Value);
        var clip = (uint)_clipStack.Peek();
        var rotation = style.Rotation.Value;
        var seq = _sequence++;
        var key = MakeKey(inputs.ZIndex, seq);

        var pos = inputs.Position;
        // Positions are computed in local (layout) space and mapped through the active transform at
        // glyph-emit time, so scale grows the glyphs and translation slides them.
        var sx = _scale.X;
        var sy = _scale.Y;
        var tx = _translation.X;
        var ty = _translation.Y;
        var invScale = 1f / _dpiScale;
        var metrics = _fonts.GetMetrics(font);
        var ascender = metrics.Ascender * invScale;
        var descender = metrics.Descender * invScale;
        var lineHeight = metrics.LineHeight * invScale;

        var baselineY = pos.Top - ascender;

        if (style.VerticalAlignment.IsSet && style.VerticalAlignment.Value == TextAlignment.Center)
        {
            var midline = pos.Top - pos.Height * 0.5f;
            baselineY = midline - (ascender + descender) * 0.5f;
        }

        var baseDir = style.BaseDirection.IsSet ? style.BaseDirection.Value : DefaultBaseDirection;
        var hAlign = style.HorizontalAlignment.IsSet ? style.HorizontalAlignment.Value : TextAlignment.Start;
        var placement = TextLayout.ResolveHorizontal(hAlign, baseDir == BidiDirection.Rtl);
        var features = style.FontFeatures.IsSet ? style.FontFeatures.Value : FontFeatureSet.None;
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

            ShapeAndDrawLine(font, lineSlice, pos.Left, pos.Width, placement, baseDir,
                baselineY, sx, sy, tx, ty, color, clip, rotation, key, features);

            if (nl < 0)
                break;

            baselineY -= lineHeight;
            sliceStart = lineEnd + 1;
        }
    }

    private void ShapeAndDrawLine(FontHandle font, ReadOnlySpan<char> line,
        float boxLeft, float boxWidth, TextPlacement placement, BidiDirection baseDir, float baselineY,
        float sx, float sy, float tx, float ty,
        uint color, uint clip, float rotation, long key, in FontFeatureSet features)
    {
        if (line.Length == 0)
            return;

        const int StackCap = 256;
        Span<ShapedGlyph> shaped = line.Length <= StackCap
            ? stackalloc ShapedGlyph[StackCap]
            : new ShapedGlyph[line.Length * 2];

        var n = _fonts.ShapeText(font, line, shaped, features, baseDir);

        // Glyphs come back in visual L→R order regardless of base direction, so every placement is
        // just a starting pen position; the emit loop below always advances rightward.
        var cursorX = boxLeft;
        if (placement != TextPlacement.Left)
        {
            // Width from the already-shaped run, so the line is shaped once instead
            // of twice (measure + emit).
            var total = 0f;
            for (var i = 0; i < n; i++)
                total += shaped[i].XAdvance;
            var w = total / _dpiScale;
            cursorX = placement == TextPlacement.Center
                ? boxLeft + (boxWidth - w) * 0.5f
                : boxLeft + boxWidth - w;
        }

        var atlasWidth = (float)_fonts.AtlasWidth;
        var atlasHeight = (float)_fonts.AtlasHeight;
        // Shaped positions and glyph bitmap dims come back in device pixels;
        // convert to logical points for layout, but keep atlas-pixel dims for UV.
        var invScale = 1f / _dpiScale;

        for (var i = 0; i < n; i++)
        {
            var sg = shaped[i];
            if (!_fonts.TryGetGlyph(new FontHandle(sg.FontId), sg.GlyphIndex, out var glyph))
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
                        Rect = new Vector4(glyphX * sx + tx, glyphY * sy + ty, glyphW * sx, glyphH * sy),
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

    private float MeasureLineWidth(FontHandle font, ReadOnlySpan<char> line, in FontFeatureSet features, BidiDirection baseDir)
    {
        if (line.Length == 0)
            return 0f;

        const int StackCap = 256;
        Span<ShapedGlyph> shaped = line.Length <= StackCap
            ? stackalloc ShapedGlyph[StackCap]
            : new ShapedGlyph[line.Length * 2];
        var n = _fonts.ShapeText(font, line, shaped, features, baseDir);
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
    /// Also carries the UI base direction so a popup opened under an RTL locale lays out
    /// right-to-left like its parent window.
    /// </summary>
    public void CopyFontsFrom(RenderedCanvasBase source)
    {
        foreach (var kv in source._fontsByFamily)
            _fontsByFamily[kv.Key] = kv.Value;
        DefaultBaseDirection = source.DefaultBaseDirection;
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

        var sx = _scale.X;
        var sy = _scale.Y;
        var offsetX = (pos.Left + (rectW - scaledWidth) * 0.5f) * sx + _translation.X;
        var offsetY = (pos.Bottom + (rectH - scaledHeight) * 0.5f) * sy + _translation.Y;

        var snappedLeft = MathF.Round(offsetX);
        var snappedBottom = MathF.Round(offsetY);
        var snappedRight = MathF.Round(offsetX + scaledWidth * sx);
        var snappedTop = MathF.Round(offsetY + scaledHeight * sy);

        _stagedImages.Add(new StagedImage
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ImageInstance
            {
                Rect = new Vector4(snappedLeft, snappedBottom, snappedRight - snappedLeft, snappedTop - snappedBottom),
                SrcUV = new Vector4(0f, 0f, 1f, 1f),
                Tint = Tint(inputs.TintColor),
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
        // Clips compose with the active transform so content drawn offset/scaled (an animating
        // subtree) is clipped in the same space it's drawn in.
        var sx = _scale.X;
        var sy = _scale.Y;
        var tx = _translation.X;
        var ty = _translation.Y;
        var current = _stagedClips[_clipStack.Peek()];
        var left = MathF.Ceiling(MathF.Max(rect.Left * sx + tx, current.X));
        var bottom = MathF.Ceiling(MathF.Max(rect.Bottom * sy + ty, current.Y));
        var right = MathF.Floor(MathF.Min(rect.Right * sx + tx, current.Z));
        var top = MathF.Floor(MathF.Min(rect.Top * sy + ty, current.W));
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

    public void PushOpacity(float opacity)
    {
        var clamped = opacity < 0f ? 0f : opacity > 1f ? 1f : opacity;
        var next = _opacity * clamped;
        _opacityStack.Push(next);
        _opacity = next;
    }

    public void PopOpacity()
    {
        if (_opacityStack.Count > 1) _opacityStack.Pop();
        _opacity = _opacityStack.Peek();
    }

    public void PushTranslation(float dx, float dy)
    {
        // A local offset (dx, dy) maps through the current scale, so a translating subtree inside a
        // scaled one moves at the scaled rate. Identity-preserving when no scale is active.
        _translation += new Vector2(dx, dy) * _scale;
        _xformStack.Push((_scale, _translation));
    }

    public void PopTranslation() => PopXform();

    public void PushScale(float sx, float sy, float pivotX, float pivotY)
    {
        var s = new Vector2(sx, sy);
        // Scale about the pivot (current local space): fold the pivot compensation into the
        // translation so a point p maps to p * (_scale * s) + _translation'.
        _translation += new Vector2(pivotX, pivotY) * (Vector2.One - s) * _scale;
        _scale *= s;
        _xformStack.Push((_scale, _translation));
    }

    public void PopScale() => PopXform();

    private void PopXform()
    {
        if (_xformStack.Count > 1) _xformStack.Pop();
        var top = _xformStack.Peek();
        _scale = top.Scale;
        _translation = top.Translation;
    }

    // Scales a packed ARGB color's alpha byte by the current opacity, leaving RGB untouched (the
    // pipeline is non-premultiplied). The opacity>=1 fast path returns the input unchanged so a
    // fully-opaque tree stages byte-identical instances and the idle-frame skip keeps working.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint Tint(uint argb)
    {
        var o = _opacity;
        if (o >= 1f) return argb;
        var a = (uint)(((argb >> 24) & 0xFFu) * o + 0.5f);
        if (a > 255u) a = 255u;
        return (a << 24) | (argb & 0x00FFFFFFu);
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
        var features = style.FontFeatures.IsSet ? style.FontFeatures.Value : FontFeatureSet.None;
        var baseDir = style.BaseDirection.IsSet ? style.BaseDirection.Value : DefaultBaseDirection;
        // Multi-line text: width is the widest line's shaped advance.
        var max = 0f;
        var i = 0;
        while (i <= text.Length)
        {
            var nl = text[i..].IndexOf('\n');
            var lineEnd = nl < 0 ? text.Length : i + nl;
            var w = MeasureLineWidth(font, text[i..lineEnd], features, baseDir);
            if (w > max) max = w;
            if (nl < 0) break;
            i = lineEnd + 1;
        }
        return max;
    }

    public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style)
    {
        if (prefixLength <= 0 || text.IsEmpty)
            return 0f;

        var font = ResolveFont(style);
        var features = style.FontFeatures.IsSet ? style.FontFeatures.Value : FontFeatureSet.None;
        var baseDir = style.BaseDirection.IsSet ? style.BaseDirection.Value : DefaultBaseDirection;

        const int StackCap = 256;
        Span<ShapedGlyph> shaped = text.Length <= StackCap
            ? stackalloc ShapedGlyph[StackCap]
            : new ShapedGlyph[text.Length * 2];
        var n = _fonts.ShapeText(font, text, shaped, features, baseDir);

        // Cluster ids are logical, so glyphs whose cluster precedes the caret are exactly the prefix —
        // summed from the full-line shaping, so contextual (cursive) advances and zero-width marks are
        // already baked in. Order-independent, so this holds whether the run is laid out L→R or R→L.
        var sum = 0f;
        for (var i = 0; i < n; i++)
            if (shaped[i].Cluster < prefixLength)
                sum += shaped[i].XAdvance;
        return sum / _dpiScale;
    }

    public float MeasureTextLineHeight(TextStyle style) => _fonts.GetMetrics(ResolveFont(style)).LineHeight / _dpiScale;

    public Size GetImageSize(string imageId) => GetImageSizeImpl(imageId);
    public int GetImageWidth(string imageId) => (int)GetImageSize(imageId).Width;
    public int GetImageHeight(string imageId) => (int)GetImageSize(imageId).Height;

    // Loads an image so it can later be drawn (DrawImage) / measured (GetImageSize) by the
    // path used as its id. Backends that support image draws (Metal, OpenGL) override this;
    // the default is a no-op so canvases without an image manager don't have to.
    public virtual void LoadImageFromFile(string path) { }

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
