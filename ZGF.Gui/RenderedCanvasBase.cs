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
    public struct RectInstance : IEquatable<RectInstance>
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

        public bool Equals(RectInstance other) =>
            Rect.Equals(other.Rect) &&
            BorderRadius.Equals(other.BorderRadius) &&
            BorderSize.Equals(other.BorderSize) &&
            BgColor == other.BgColor &&
            BorderColorTop == other.BorderColorTop &&
            BorderColorRight == other.BorderColorRight &&
            BorderColorBottom == other.BorderColorBottom &&
            BorderColorLeft == other.BorderColorLeft &&
            ClipIndex == other.ClipIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GlyphInstance : IEquatable<GlyphInstance>
    {
        public Vector4 Rect;
        public Vector4 AtlasUV;
        public uint Color;
        public uint ClipIndex;
        public float Rotation; // radians, rotation about the glyph rect's center

        public bool Equals(GlyphInstance other) =>
            Rect.Equals(other.Rect) &&
            AtlasUV.Equals(other.AtlasUV) &&
            Color == other.Color &&
            ClipIndex == other.ClipIndex &&
            Rotation == other.Rotation;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageInstance : IEquatable<ImageInstance>
    {
        public Vector4 Rect;
        public Vector4 SrcUV;
        public uint Tint;
        public uint ClipIndex;
        public float Rotation; // radians, rotation about the rect's center
        public uint TextureId; // not uploaded to GPU; used only for batching

        public bool Equals(ImageInstance other) =>
            Rect.Equals(other.Rect) &&
            SrcUV.Equals(other.SrcUV) &&
            Tint == other.Tint &&
            ClipIndex == other.ClipIndex &&
            Rotation == other.Rotation &&
            TextureId == other.TextureId;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ShadowInstance : IEquatable<ShadowInstance>
    {
        public Vector4 OuterRect;       // drawn quad: covers the entire blurred shadow region
        public Vector4 ShadowRect;      // (post-offset, post-spread) source rect in world coords
        public Vector4 BorderRadius;    // tl, tr, br, bl
        public float Sigma;             // blur stddev
        public uint Color;              // ARGB packed
        public uint ClipIndex;
        private uint _pad;              // keep 16-byte alignment

        public bool Equals(ShadowInstance other) =>
            OuterRect.Equals(other.OuterRect) &&
            ShadowRect.Equals(other.ShadowRect) &&
            BorderRadius.Equals(other.BorderRadius) &&
            Sigma == other.Sigma &&
            Color == other.Color &&
            ClipIndex == other.ClipIndex;
    }

    private struct StagedRect { public long Key; public RectInstance Inst; }
    private struct StagedGlyph { public long Key; public GlyphInstance Inst; }
    private struct StagedImage { public long Key; public ImageInstance Inst; }
    private struct StagedShadow { public long Key; public ShadowInstance Inst; }

    public enum DrawKind : byte { Rect, Glyph, Image, Shadow }

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
    protected const int MaxClips = 256;

    // ---------- Per-frame staging state ----------

    private readonly List<StagedRect> _stagedRects = new();
    private readonly List<StagedGlyph> _stagedGlyphs = new();
    private readonly List<StagedImage> _stagedImages = new();
    private readonly List<StagedShadow> _stagedShadows = new();
    private readonly List<Vector4> _stagedClips = new();
    private readonly Stack<int> _clipStack = new();
    private int _sequence;

    // Sorted-output scratch (re-allocated as needed)
    private RectInstance[] _curRects = Array.Empty<RectInstance>();
    private GlyphInstance[] _curGlyphs = Array.Empty<GlyphInstance>();
    private ImageInstance[] _curImages = Array.Empty<ImageInstance>();
    private ShadowInstance[] _curShadows = Array.Empty<ShadowInstance>();
    private int _curRectCount, _curGlyphCount, _curImageCount, _curShadowCount;

    // Previous-frame mirrors for byte-equal compare
    private RectInstance[] _prevRects = Array.Empty<RectInstance>();
    private GlyphInstance[] _prevGlyphs = Array.Empty<GlyphInstance>();
    private ImageInstance[] _prevImages = Array.Empty<ImageInstance>();
    private ShadowInstance[] _prevShadows = Array.Empty<ShadowInstance>();
    private Vector4[] _prevClips = Array.Empty<Vector4>();
    private int _prevRectCount, _prevGlyphCount, _prevImageCount, _prevShadowCount, _prevClipCount;

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

            var cursorX = lineStart;
            if (hCenter)
            {
                var width = MeasureLineWidth(font, lineSlice);
                cursorX = pos.Left + (pos.Width - width) * 0.5f;
            }

            DrawShapedLine(font, lineSlice, cursorX, baselineY, color, clip, rotation, key);

            if (nl < 0)
                break;

            baselineY -= lineHeight;
            sliceStart = lineEnd + 1;
        }
    }

    private void DrawShapedLine(FontHandle font, ReadOnlySpan<char> line, float startX, float baselineY,
        uint color, uint clip, float rotation, long key)
    {
        if (line.Length == 0)
            return;

        const int StackCap = 256;
        Span<ShapedGlyph> shaped = line.Length <= StackCap
            ? stackalloc ShapedGlyph[StackCap]
            : new ShapedGlyph[line.Length * 2];

        var n = _fonts.ShapeText(font, line, shaped);
        var cursorX = startX;
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
        SortAndMaterialize();
        BuildDrawCalls();
        UploadIfChanged();
        UpdateAtlasIfDirty();
        IssueDraws(_drawCalls);
    }

    private void SortAndMaterialize()
    {
        _stagedRects.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedGlyphs.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedImages.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedShadows.Sort(static (a, b) => a.Key.CompareTo(b.Key));

        EnsureCapacity(ref _curRects, _stagedRects.Count);
        EnsureCapacity(ref _curGlyphs, _stagedGlyphs.Count);
        EnsureCapacity(ref _curImages, _stagedImages.Count);
        EnsureCapacity(ref _curShadows, _stagedShadows.Count);

        for (var i = 0; i < _stagedRects.Count; i++)
            _curRects[i] = _stagedRects[i].Inst;
        for (var i = 0; i < _stagedGlyphs.Count; i++)
            _curGlyphs[i] = _stagedGlyphs[i].Inst;
        for (var i = 0; i < _stagedImages.Count; i++)
            _curImages[i] = _stagedImages[i].Inst;
        for (var i = 0; i < _stagedShadows.Count; i++)
            _curShadows[i] = _stagedShadows[i].Inst;

        _curRectCount = _stagedRects.Count;
        _curGlyphCount = _stagedGlyphs.Count;
        _curImageCount = _stagedImages.Count;
        _curShadowCount = _stagedShadows.Count;
    }

    private static void EnsureCapacity<T>(ref T[] array, int count)
    {
        if (array.Length < count)
            array = new T[Math.Max(count, array.Length * 2)];
    }

    private void BuildDrawCalls()
    {
        _drawCalls.Clear();

        int ri = 0, gi = 0, ii = 0, si = 0;
        var rN = _curRectCount;
        var gN = _curGlyphCount;
        var iN = _curImageCount;
        var sN = _curShadowCount;

        DrawKind activeKind = DrawKind.Rect;
        var activeStart = 0;
        var activeCount = 0;
        uint activeTexture = 0;
        var hasActive = false;

        while (ri < rN || gi < gN || ii < iN || si < sN)
        {
            // Pick kind whose next staged item has the smallest sort key.
            var rKey = ri < rN ? _stagedRects[ri].Key : long.MaxValue;
            var gKey = gi < gN ? _stagedGlyphs[gi].Key : long.MaxValue;
            var iKey = ii < iN ? _stagedImages[ii].Key : long.MaxValue;
            var sKey = si < sN ? _stagedShadows[si].Key : long.MaxValue;

            DrawKind pick;
            if (rKey <= gKey && rKey <= iKey && rKey <= sKey) pick = DrawKind.Rect;
            else if (gKey <= iKey && gKey <= sKey) pick = DrawKind.Glyph;
            else if (iKey <= sKey) pick = DrawKind.Image;
            else pick = DrawKind.Shadow;

            uint pickTex = 0;
            int pickIndex = 0;
            switch (pick)
            {
                case DrawKind.Rect: pickIndex = ri; break;
                case DrawKind.Glyph: pickIndex = gi; break;
                case DrawKind.Image:
                    pickIndex = ii;
                    pickTex = _curImages[ii].TextureId;
                    break;
                case DrawKind.Shadow: pickIndex = si; break;
            }

            var canExtend = hasActive && pick == activeKind &&
                            (pick != DrawKind.Image || pickTex == activeTexture);

            if (!canExtend)
            {
                if (hasActive)
                    _drawCalls.Add(new DrawCall
                    {
                        Kind = activeKind,
                        InstanceStart = activeStart,
                        InstanceCount = activeCount,
                        TextureId = activeTexture,
                    });

                activeKind = pick;
                activeStart = pickIndex;
                activeCount = 0;
                activeTexture = pickTex;
                hasActive = true;
            }

            activeCount++;
            switch (pick)
            {
                case DrawKind.Rect: ri++; break;
                case DrawKind.Glyph: gi++; break;
                case DrawKind.Image: ii++; break;
                case DrawKind.Shadow: si++; break;
            }
        }

        if (hasActive)
            _drawCalls.Add(new DrawCall
            {
                Kind = activeKind,
                InstanceStart = activeStart,
                InstanceCount = activeCount,
                TextureId = activeTexture,
            });
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ArraysMatch<T>(T[] cur, int curCount, T[] prev, int prevCount) where T : IEquatable<T>
    {
        if (curCount != prevCount) return false;
        for (var i = 0; i < curCount; i++)
            if (!cur[i].Equals(prev[i])) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ArraysMatch(List<Vector4> cur, Vector4[] prev, int prevCount)
    {
        if (cur.Count != prevCount) return false;
        for (var i = 0; i < cur.Count; i++)
            if (cur[i] != prev[i]) return false;
        return true;
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
    protected abstract void UploadClips(List<Vector4> clips);
    protected abstract void UpdateAtlasIfDirty();
    protected abstract void IssueDraws(IReadOnlyList<DrawCall> drawCalls);
    protected abstract void OnResize(int width, int height);
    protected abstract Size GetImageSizeImpl(string imageId);
    protected abstract uint GetImageTextureId(string imageId);
}
