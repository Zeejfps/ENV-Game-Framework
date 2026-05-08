using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGL.NET;
using ZGF.BMFontModule;
using ZGF.Geometry;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public sealed unsafe class OpenGlRenderedCanvas : ICanvas, IDisposable
{
    // ---------- Per-instance struct types ----------

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct RectInstance : IEquatable<RectInstance>
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
    private struct GlyphInstance : IEquatable<GlyphInstance>
    {
        public Vector4 Rect;
        public Vector4 AtlasUV;
        public uint Color;
        public uint ClipIndex;

        public bool Equals(GlyphInstance other) =>
            Rect.Equals(other.Rect) &&
            AtlasUV.Equals(other.AtlasUV) &&
            Color == other.Color &&
            ClipIndex == other.ClipIndex;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ImageInstance : IEquatable<ImageInstance>
    {
        public Vector4 Rect;
        public Vector4 SrcUV;
        public uint Tint;
        public uint ClipIndex;
        public uint TextureId; // not uploaded to GPU; used only for batching

        public bool Equals(ImageInstance other) =>
            Rect.Equals(other.Rect) &&
            SrcUV.Equals(other.SrcUV) &&
            Tint == other.Tint &&
            ClipIndex == other.ClipIndex &&
            TextureId == other.TextureId;
    }

    private struct StagedRect { public long Key; public RectInstance Inst; }
    private struct StagedGlyph { public long Key; public GlyphInstance Inst; }
    private struct StagedImage { public long Key; public ImageInstance Inst; }

    private enum DrawKind : byte { Rect, Glyph, Image }

    private readonly struct DrawCall
    {
        public DrawKind Kind { get; init; }
        public int InstanceStart { get; init; }
        public int InstanceCount { get; init; }
        public uint TextureId { get; init; } // image draws only
    }

    // ---------- Capacities ----------

    private const int MaxRects = 4096;
    private const int MaxGlyphs = 16384;
    private const int MaxImages = 1024;
    private const int MaxClips = 256;       // matches shader array length

    // ---------- GL resources ----------

    private uint _rectShader, _glyphShader, _imageShader;
    private int _rectProjLoc, _glyphProjLoc, _imageProjLoc;
    private int _glyphAtlasLoc, _imageTexLoc;
    private uint _rectVao, _glyphVao, _imageVao;
    private uint _unitQuadVbo;
    private uint _rectInstanceVbo, _glyphInstanceVbo, _imageInstanceVbo;
    private uint _clipUbo;
    private uint _fontAtlasTextureId;
    private float _fontAtlasWidth;
    private float _fontAtlasHeight;

    // ---------- Per-frame state ----------

    private readonly List<StagedRect> _stagedRects = new();
    private readonly List<StagedGlyph> _stagedGlyphs = new();
    private readonly List<StagedImage> _stagedImages = new();
    private readonly List<Vector4> _stagedClips = new();
    private readonly Stack<int> _clipStack = new();
    private int _sequence;

    // Sorted-output scratch (re-allocated as needed)
    private RectInstance[] _curRects = Array.Empty<RectInstance>();
    private GlyphInstance[] _curGlyphs = Array.Empty<GlyphInstance>();
    private ImageInstance[] _curImages = Array.Empty<ImageInstance>();
    private int _curRectCount, _curGlyphCount, _curImageCount;

    // Previous-frame mirrors for byte-equal compare
    private RectInstance[] _prevRects = Array.Empty<RectInstance>();
    private GlyphInstance[] _prevGlyphs = Array.Empty<GlyphInstance>();
    private ImageInstance[] _prevImages = Array.Empty<ImageInstance>();
    private Vector4[] _prevClips = Array.Empty<Vector4>();
    private int _prevRectCount, _prevGlyphCount, _prevImageCount, _prevClipCount;

    private readonly List<DrawCall> _drawCalls = new();

    // ---------- Externals ----------

    private readonly BitmapFont _font;
    private readonly GlImageManager _imageManager;
    private int _width, _height;
    private Matrix4x4 _projection;

    // Optional debug counters
    public int LastFrameUploadCount { get; private set; }

    public OpenGlRenderedCanvas(int width, int height, BitmapFont font, GlImageManager imageManager)
    {
        _width = width;
        _height = height;
        _font = font;
        _imageManager = imageManager;

        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);

        var shadersDir = Path.Combine("Assets", "Shaders");
        _rectShader = new ShaderProgramCompiler()
            .WithVertexShader(Path.Combine(shadersDir, "canvas_rect.vert.glsl"))
            .WithFragmentShader(Path.Combine(shadersDir, "canvas_rect.frag.glsl"))
            .Compile().Id;
        _glyphShader = new ShaderProgramCompiler()
            .WithVertexShader(Path.Combine(shadersDir, "canvas_glyph.vert.glsl"))
            .WithFragmentShader(Path.Combine(shadersDir, "canvas_glyph.frag.glsl"))
            .Compile().Id;
        _imageShader = new ShaderProgramCompiler()
            .WithVertexShader(Path.Combine(shadersDir, "canvas_image.vert.glsl"))
            .WithFragmentShader(Path.Combine(shadersDir, "canvas_image.frag.glsl"))
            .Compile().Id;

        _rectProjLoc = glGetUniformLocation(_rectShader, "u_projection");
        _glyphProjLoc = glGetUniformLocation(_glyphShader, "u_projection");
        _imageProjLoc = glGetUniformLocation(_imageShader, "u_projection");
        _glyphAtlasLoc = glGetUniformLocation(_glyphShader, "u_atlas");
        _imageTexLoc = glGetUniformLocation(_imageShader, "u_texture");

        UploadProjection();

        // Sampler units: glyph atlas = 0, image texture = 0 (each draw rebinds GL_TEXTURE0).
        glUseProgram(_glyphShader);
        glUniform1i(_glyphAtlasLoc, 0);
        glUseProgram(_imageShader);
        glUniform1i(_imageTexLoc, 0);

        SetupUnitQuad();
        SetupInstanceBuffers();
        SetupClipUbo();
        SetupFontAtlas();
    }

    public int Width => _width;
    public int Height => _height;

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);
        UploadProjection();
    }

    private void UploadProjection()
    {
        var p = _projection;
        var ptr = &p.M11;
        glUseProgram(_rectShader);
        glUniformMatrix4fv(_rectProjLoc, 1, false, ptr);
        glUseProgram(_glyphShader);
        glUniformMatrix4fv(_glyphProjLoc, 1, false, ptr);
        glUseProgram(_imageShader);
        glUniformMatrix4fv(_imageProjLoc, 1, false, ptr);
        AssertNoGlError();
    }

    // ---------- GL setup helpers ----------

    private void SetupUnitQuad()
    {
        // Unit quad in [0,1] x [0,1]; two triangles, 6 vertices.
        Span<float> verts = stackalloc float[12]
        {
            0f, 0f,
            1f, 0f,
            0f, 1f,
            1f, 0f,
            1f, 1f,
            0f, 1f,
        };

        uint vbo;
        glGenBuffers(1, &vbo);
        glBindBuffer(GL_ARRAY_BUFFER, vbo);
        fixed (float* ptr = &verts[0])
            glBufferData(GL_ARRAY_BUFFER, verts.Length * sizeof(float), ptr, GL_STATIC_DRAW);
        AssertNoGlError();
        _unitQuadVbo = vbo;
    }

    private void SetupInstanceBuffers()
    {
        // Allocate three persistent instance VBOs sized for max capacity.
        _rectInstanceVbo = AllocInstanceVbo(MaxRects * sizeof(RectInstance));
        _glyphInstanceVbo = AllocInstanceVbo(MaxGlyphs * sizeof(GlyphInstance));
        _imageInstanceVbo = AllocInstanceVbo(MaxImages * sizeof(ImageInstance));

        _rectVao = CreateRectVao();
        _glyphVao = CreateGlyphVao();
        _imageVao = CreateImageVao();
    }

    private uint AllocInstanceVbo(int sizeBytes)
    {
        uint vbo;
        glGenBuffers(1, &vbo);
        glBindBuffer(GL_ARRAY_BUFFER, vbo);
        glBufferData(GL_ARRAY_BUFFER, sizeBytes, (void*)0, GL_DYNAMIC_DRAW);
        AssertNoGlError();
        return vbo;
    }

    private uint CreateRectVao()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        glBindVertexArray(vao);

        // Per-vertex unit pos at location 0.
        glBindBuffer(GL_ARRAY_BUFFER, _unitQuadVbo);
        glVertexAttribPointer(0, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0);
        glEnableVertexAttribArray(0);

        glBindBuffer(GL_ARRAY_BUFFER, _rectInstanceVbo);
        var stride = sizeof(RectInstance);

        AddFloatInstanceAttrib(1, 4, stride, OffsetOf<RectInstance>(nameof(RectInstance.Rect)));
        AddFloatInstanceAttrib(2, 4, stride, OffsetOf<RectInstance>(nameof(RectInstance.BorderRadius)));
        AddFloatInstanceAttrib(3, 4, stride, OffsetOf<RectInstance>(nameof(RectInstance.BorderSize)));
        AddUintInstanceAttrib(4, stride, OffsetOf<RectInstance>(nameof(RectInstance.BgColor)));
        AddUintInstanceAttrib(5, stride, OffsetOf<RectInstance>(nameof(RectInstance.BorderColorTop)));
        AddUintInstanceAttrib(6, stride, OffsetOf<RectInstance>(nameof(RectInstance.BorderColorRight)));
        AddUintInstanceAttrib(7, stride, OffsetOf<RectInstance>(nameof(RectInstance.BorderColorBottom)));
        AddUintInstanceAttrib(8, stride, OffsetOf<RectInstance>(nameof(RectInstance.BorderColorLeft)));
        AddUintInstanceAttrib(9, stride, OffsetOf<RectInstance>(nameof(RectInstance.ClipIndex)));

        glBindVertexArray(0);
        AssertNoGlError();
        return vao;
    }

    private uint CreateGlyphVao()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        glBindVertexArray(vao);

        glBindBuffer(GL_ARRAY_BUFFER, _unitQuadVbo);
        glVertexAttribPointer(0, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0);
        glEnableVertexAttribArray(0);

        glBindBuffer(GL_ARRAY_BUFFER, _glyphInstanceVbo);
        var stride = sizeof(GlyphInstance);

        AddFloatInstanceAttrib(1, 4, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.Rect)));
        AddFloatInstanceAttrib(2, 4, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.AtlasUV)));
        AddUintInstanceAttrib(3, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.Color)));
        AddUintInstanceAttrib(4, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.ClipIndex)));

        glBindVertexArray(0);
        AssertNoGlError();
        return vao;
    }

    private uint CreateImageVao()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        glBindVertexArray(vao);

        glBindBuffer(GL_ARRAY_BUFFER, _unitQuadVbo);
        glVertexAttribPointer(0, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0);
        glEnableVertexAttribArray(0);

        glBindBuffer(GL_ARRAY_BUFFER, _imageInstanceVbo);
        var stride = sizeof(ImageInstance);

        AddFloatInstanceAttrib(1, 4, stride, OffsetOf<ImageInstance>(nameof(ImageInstance.Rect)));
        AddFloatInstanceAttrib(2, 4, stride, OffsetOf<ImageInstance>(nameof(ImageInstance.SrcUV)));
        AddUintInstanceAttrib(3, stride, OffsetOf<ImageInstance>(nameof(ImageInstance.Tint)));
        AddUintInstanceAttrib(4, stride, OffsetOf<ImageInstance>(nameof(ImageInstance.ClipIndex)));

        glBindVertexArray(0);
        AssertNoGlError();
        return vao;
    }

    private static void AddFloatInstanceAttrib(uint index, int components, int stride, int offset)
    {
        glVertexAttribPointer(index, components, GL_FLOAT, false, stride, (void*)offset);
        glEnableVertexAttribArray(index);
        glVertexAttribDivisor(index, 1);
    }

    private static void AddUintInstanceAttrib(uint index, int stride, int offset)
    {
        glVertexAttribIPointer(index, 1, GL_UNSIGNED_INT, stride, (void*)offset);
        glEnableVertexAttribArray(index);
        glVertexAttribDivisor(index, 1);
    }

    private static int OffsetOf<T>(string field) => (int)Marshal.OffsetOf<T>(field);

    private void SetupClipUbo()
    {
        uint ubo;
        glGenBuffers(1, &ubo);
        glBindBuffer(GL_UNIFORM_BUFFER, ubo);
        glBufferData(GL_UNIFORM_BUFFER, MaxClips * sizeof(Vector4), (void*)0, GL_DYNAMIC_DRAW);
        glBindBufferBase(GL_UNIFORM_BUFFER, 0, ubo);
        AssertNoGlError();
        _clipUbo = ubo;
    }

    private void SetupFontAtlas()
    {
        var png = _font.Png;
        var width = png.Width;
        var height = png.Height;
        var rgba = DecodePngToRgba(png);

        uint tex;
        glGenTextures(1, &tex);
        glBindTexture(GL_TEXTURE_2D, tex);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        fixed (byte* ptr = &rgba[0])
            glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        AssertNoGlError();

        _fontAtlasTextureId = tex;
        _fontAtlasWidth = width;
        _fontAtlasHeight = height;
    }

    private static byte[] DecodePngToRgba(PngSharp.Api.IDecodedPng png)
    {
        var width = png.Width;
        var height = png.Height;
        var src = png.PixelData;
        var bpp = png.BytesPerPixel;
        var output = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIndex = (y * width + x) * bpp;
                // Flip Y so the texture's V-axis matches OpenGL Y-up.
                var dstY = height - 1 - y;
                var dstIndex = (dstY * width + x) * 4;

                byte r = 0, g = 0, b = 0, a = 255;
                switch (png.ColorType)
                {
                    case PngSharp.Spec.Chunks.IHDR.ColorType.TrueColorWithAlpha:
                        r = src[srcIndex];
                        g = src[srcIndex + 1];
                        b = src[srcIndex + 2];
                        a = src[srcIndex + 3];
                        break;
                    case PngSharp.Spec.Chunks.IHDR.ColorType.TrueColor:
                        r = src[srcIndex];
                        g = src[srcIndex + 1];
                        b = src[srcIndex + 2];
                        break;
                    case PngSharp.Spec.Chunks.IHDR.ColorType.GrayscaleWithAlpha:
                        r = g = b = src[srcIndex];
                        a = src[srcIndex + 1];
                        break;
                    case PngSharp.Spec.Chunks.IHDR.ColorType.Grayscale:
                        r = g = b = src[srcIndex];
                        break;
                    default:
                        throw new NotSupportedException($"PNG ColorType '{png.ColorType}' is not supported.");
                }

                output[dstIndex] = r;
                output[dstIndex + 1] = g;
                output[dstIndex + 2] = b;
                output[dstIndex + 3] = a;
            }
        }
        return output;
    }

    // ---------- ICanvas ----------

    public void BeginFrame()
    {
        _stagedRects.Clear();
        _stagedGlyphs.Clear();
        _stagedImages.Clear();
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
        _stagedRects.Add(new StagedRect
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new RectInstance
            {
                Rect = new Vector4(pos.Left, pos.Bottom, pos.Width, pos.Height),
                BorderRadius = Vector4.Zero,
                BorderSize = new Vector4(
                    style.BorderSize.Top.Value,
                    style.BorderSize.Right.Value,
                    style.BorderSize.Bottom.Value,
                    style.BorderSize.Left.Value),
                BgColor = style.BackgroundColor.Value,
                BorderColorTop = style.BorderColor.Top.Value,
                BorderColorRight = style.BorderColor.Right.Value,
                BorderColorBottom = style.BorderColor.Bottom.Value,
                BorderColorLeft = style.BorderColor.Left.Value,
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
        var color = style.TextColor.Value;
        var clip = (uint)_clipStack.Peek();
        var seq = _sequence++;
        var key = MakeKey(inputs.ZIndex, seq);

        var pos = inputs.Position;
        var lineHeight = _font.FontMetrics.Common.LineHeight;
        var fontBase = _font.FontMetrics.Common.Base;

        var lineStart = (int)pos.Left;
        var cursorX = lineStart;
        var cursorY = (int)(pos.Top - fontBase);

        if (style.VerticalAlignment.IsSet)
        {
            switch (style.VerticalAlignment.Value)
            {
                case TextAlignment.Center:
                    cursorY = (int)((pos.Top - pos.Height * 0.5f) - (fontBase * 0.5f));
                    break;
                case TextAlignment.Start:
                case TextAlignment.End:
                    break;
            }
        }

        if (style.HorizontalAlignment.IsSet)
        {
            switch (style.HorizontalAlignment.Value)
            {
                case TextAlignment.Center:
                    var width = MeasureTextWidth(text.AsSpan(), style);
                    cursorX = (int)(pos.Left + (pos.Width - width) * 0.5f);
                    break;
                case TextAlignment.Start:
                case TextAlignment.End:
                    break;
            }
        }

        int? prevCodePoint = null;
        foreach (var codePoint in text.EnumerateCodePoints())
        {
            if (codePoint == '\n')
            {
                cursorY -= lineHeight;
                cursorX = lineStart;
                prevCodePoint = null;
                continue;
            }

            if (!_font.TryGetGlyphInfo(codePoint, out var glyph))
                continue;

            var kerning = 0;
            if (prevCodePoint.HasValue)
                _font.TryGetKerningPair(prevCodePoint.Value, codePoint, out kerning);

            var glyphX = cursorX + kerning + glyph.XOffset;
            var glyphY = cursorY + (fontBase - glyph.YOffset) - glyph.Height;
            var glyphW = glyph.Width;
            var glyphH = glyph.Height;

            if (glyphW > 0 && glyphH > 0)
            {
                // Atlas was uploaded with Y flipped, so V coordinate is bottom-up.
                var atlasU = glyph.X / _fontAtlasWidth;
                var atlasV = (_fontAtlasHeight - glyph.Y - glyph.Height) / _fontAtlasHeight;
                var atlasW = glyph.Width / _fontAtlasWidth;
                var atlasH = glyph.Height / _fontAtlasHeight;

                _stagedGlyphs.Add(new StagedGlyph
                {
                    Key = key,
                    Inst = new GlyphInstance
                    {
                        Rect = new Vector4(glyphX, glyphY, glyphW, glyphH),
                        AtlasUV = new Vector4(atlasU, atlasV, atlasW, atlasH),
                        Color = color,
                        ClipIndex = clip,
                    }
                });
            }

            cursorX += glyph.XAdvance;
            prevCodePoint = codePoint;
        }
    }

    public void DrawImage(in DrawImageInputs inputs)
    {
        var pos = inputs.Position;
        var imageId = inputs.ImageId;
        var size = _imageManager.GetImageSize(imageId);
        var imageW = (int)size.Width;
        var imageH = (int)size.Height;
        var rectW = (int)pos.Width;
        var rectH = (int)pos.Height;

        // Aspect-fit: same math as SoftwareRenderedCanvas.ExecuteCommand.
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

        _stagedImages.Add(new StagedImage
        {
            Key = MakeKey(inputs.ZIndex, _sequence++),
            Inst = new ImageInstance
            {
                Rect = new Vector4(offsetX, offsetY, scaledWidth, scaledHeight),
                SrcUV = new Vector4(0f, 0f, 1f, 1f),
                Tint = inputs.Style.TintColor.Value,
                ClipIndex = (uint)_clipStack.Peek(),
                TextureId = _imageManager.GetTextureId(imageId),
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
        var left = MathF.Max(rect.Left, current.X);
        var bottom = MathF.Max(rect.Bottom, current.Y);
        var right = MathF.Min(rect.Right, current.Z);
        var top = MathF.Min(rect.Top, current.W);
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
        var totalWidth = 0f;
        foreach (var codePoint in text.EnumerateCodePoints())
        {
            if (!_font.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;
            totalWidth += glyphInfo.XAdvance;
        }
        return totalWidth;
    }

    public float MeasureTextLineHeight(TextStyle style) => _font.FontMetrics.Common.LineHeight;

    public Size GetImageSize(string imageId) => _imageManager.GetImageSize(imageId);
    public int GetImageWidth(string imageId) => _imageManager.GetImageWidth(imageId);
    public int GetImageHeight(string imageId) => _imageManager.GetImageHeight(imageId);

    // ---------- EndFrame: sort, batch, upload, draw ----------

    public void EndFrame()
    {
        SortAndMaterialize();
        BuildDrawCalls();
        UploadIfChanged();
        IssueDraws();
    }

    private void SortAndMaterialize()
    {
        _stagedRects.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedGlyphs.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        _stagedImages.Sort(static (a, b) => a.Key.CompareTo(b.Key));

        EnsureCapacity(ref _curRects, _stagedRects.Count);
        EnsureCapacity(ref _curGlyphs, _stagedGlyphs.Count);
        EnsureCapacity(ref _curImages, _stagedImages.Count);

        for (var i = 0; i < _stagedRects.Count; i++)
            _curRects[i] = _stagedRects[i].Inst;
        for (var i = 0; i < _stagedGlyphs.Count; i++)
            _curGlyphs[i] = _stagedGlyphs[i].Inst;
        for (var i = 0; i < _stagedImages.Count; i++)
            _curImages[i] = _stagedImages[i].Inst;

        _curRectCount = _stagedRects.Count;
        _curGlyphCount = _stagedGlyphs.Count;
        _curImageCount = _stagedImages.Count;
    }

    private static void EnsureCapacity<T>(ref T[] array, int count)
    {
        if (array.Length < count)
            array = new T[Math.Max(count, array.Length * 2)];
    }

    private void BuildDrawCalls()
    {
        _drawCalls.Clear();

        int ri = 0, gi = 0, ii = 0;
        var rN = _curRectCount;
        var gN = _curGlyphCount;
        var iN = _curImageCount;

        DrawKind activeKind = DrawKind.Rect;
        var activeStart = 0;
        var activeCount = 0;
        uint activeTexture = 0;
        var hasActive = false;

        while (ri < rN || gi < gN || ii < iN)
        {
            // Pick kind whose next staged item has the smallest sort key.
            var rKey = ri < rN ? _stagedRects[ri].Key : long.MaxValue;
            var gKey = gi < gN ? _stagedGlyphs[gi].Key : long.MaxValue;
            var iKey = ii < iN ? _stagedImages[ii].Key : long.MaxValue;

            DrawKind pick;
            if (rKey <= gKey && rKey <= iKey) pick = DrawKind.Rect;
            else if (gKey <= iKey) pick = DrawKind.Glyph;
            else pick = DrawKind.Image;

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

        // Clip UBO
        if (!ArraysMatch(_stagedClips, _prevClips, _prevClipCount))
        {
            EnsureCapacity(ref _prevClips, _stagedClips.Count);
            for (var i = 0; i < _stagedClips.Count; i++) _prevClips[i] = _stagedClips[i];
            _prevClipCount = _stagedClips.Count;

            glBindBuffer(GL_UNIFORM_BUFFER, _clipUbo);
            if (_stagedClips.Count > 0)
            {
                fixed (Vector4* ptr = &_prevClips[0])
                    glBufferSubData(GL_UNIFORM_BUFFER, 0, _stagedClips.Count * sizeof(Vector4), ptr);
            }
            LastFrameUploadCount++;
        }

        // Rects
        if (!ArraysMatch(_curRects, _curRectCount, _prevRects, _prevRectCount))
        {
            EnsureCapacity(ref _prevRects, _curRectCount);
            Array.Copy(_curRects, _prevRects, _curRectCount);
            _prevRectCount = _curRectCount;

            if (_curRectCount > 0)
            {
                glBindBuffer(GL_ARRAY_BUFFER, _rectInstanceVbo);
                fixed (RectInstance* ptr = &_curRects[0])
                    glBufferSubData(GL_ARRAY_BUFFER, 0, _curRectCount * sizeof(RectInstance), ptr);
            }
            LastFrameUploadCount++;
        }

        // Glyphs
        if (!ArraysMatch(_curGlyphs, _curGlyphCount, _prevGlyphs, _prevGlyphCount))
        {
            EnsureCapacity(ref _prevGlyphs, _curGlyphCount);
            Array.Copy(_curGlyphs, _prevGlyphs, _curGlyphCount);
            _prevGlyphCount = _curGlyphCount;

            if (_curGlyphCount > 0)
            {
                glBindBuffer(GL_ARRAY_BUFFER, _glyphInstanceVbo);
                fixed (GlyphInstance* ptr = &_curGlyphs[0])
                    glBufferSubData(GL_ARRAY_BUFFER, 0, _curGlyphCount * sizeof(GlyphInstance), ptr);
            }
            LastFrameUploadCount++;
        }

        // Images
        if (!ArraysMatch(_curImages, _curImageCount, _prevImages, _prevImageCount))
        {
            EnsureCapacity(ref _prevImages, _curImageCount);
            Array.Copy(_curImages, _prevImages, _curImageCount);
            _prevImageCount = _curImageCount;

            if (_curImageCount > 0)
            {
                glBindBuffer(GL_ARRAY_BUFFER, _imageInstanceVbo);
                fixed (ImageInstance* ptr = &_curImages[0])
                    glBufferSubData(GL_ARRAY_BUFFER, 0, _curImageCount * sizeof(ImageInstance), ptr);
            }
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

    private void IssueDraws()
    {
        if (_drawCalls.Count == 0)
            return;

        glDisable(GL_DEPTH_TEST);
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glActiveTexture(GL_TEXTURE0);

        DrawKind boundKind = (DrawKind)255;
        uint boundTexture = 0;

        foreach (var call in _drawCalls)
        {
            if (call.Kind != boundKind)
            {
                switch (call.Kind)
                {
                    case DrawKind.Rect:
                        glUseProgram(_rectShader);
                        glBindVertexArray(_rectVao);
                        break;
                    case DrawKind.Glyph:
                        glUseProgram(_glyphShader);
                        glBindVertexArray(_glyphVao);
                        glBindTexture(GL_TEXTURE_2D, _fontAtlasTextureId);
                        boundTexture = _fontAtlasTextureId;
                        break;
                    case DrawKind.Image:
                        glUseProgram(_imageShader);
                        glBindVertexArray(_imageVao);
                        boundTexture = 0;
                        break;
                }
                boundKind = call.Kind;
            }

            if (call.Kind == DrawKind.Image && call.TextureId != boundTexture)
            {
                glBindTexture(GL_TEXTURE_2D, call.TextureId);
                boundTexture = call.TextureId;
            }

            glDrawArraysInstancedBaseInstance(GL_TRIANGLES, 0, 6, call.InstanceCount, (uint)call.InstanceStart);
        }

        glBindVertexArray(0);
    }

    // ---------- Helpers ----------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long MakeKey(int z, int seq)
    {
        // Z is signed int; offset by int.MinValue so the unsigned reinterpretation sorts correctly.
        var zUnsigned = (uint)(z - int.MinValue);
        return ((long)zUnsigned << 32) | (uint)seq;
    }

    public void Dispose()
    {
        DeleteBuffer(ref _rectInstanceVbo);
        DeleteBuffer(ref _glyphInstanceVbo);
        DeleteBuffer(ref _imageInstanceVbo);
        DeleteBuffer(ref _unitQuadVbo);
        DeleteBuffer(ref _clipUbo);
        DeleteVao(ref _rectVao);
        DeleteVao(ref _glyphVao);
        DeleteVao(ref _imageVao);
        if (_fontAtlasTextureId != 0)
        {
            var id = _fontAtlasTextureId;
            glDeleteTextures(1, &id);
            _fontAtlasTextureId = 0;
        }
        if (_rectShader != 0) { glDeleteProgram(_rectShader); _rectShader = 0; }
        if (_glyphShader != 0) { glDeleteProgram(_glyphShader); _glyphShader = 0; }
        if (_imageShader != 0) { glDeleteProgram(_imageShader); _imageShader = 0; }
    }

    private static void DeleteBuffer(ref uint buf)
    {
        if (buf == 0) return;
        var id = buf;
        glDeleteBuffers(1, &id);
        buf = 0;
    }

    private static void DeleteVao(ref uint vao)
    {
        if (vao == 0) return;
        var id = vao;
        glDeleteVertexArrays(1, &id);
        vao = 0;
    }
}
