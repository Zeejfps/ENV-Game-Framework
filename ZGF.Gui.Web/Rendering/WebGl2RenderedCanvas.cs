using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Fonts;
using ZGF.Gui;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;
using static ZGF.Gui.Web.Rendering.Gl;

namespace ZGF.Gui.Web.Rendering;

/// <summary>
/// WebGL2 backend for <see cref="RenderedCanvasBase"/>. A structural port of the
/// desktop OpenGlRenderedCanvas (+ its shared resources, folded in since the web
/// host has a single canvas/context): instanced rect/glyph/shadow/shape/image
/// pipelines, a std140 clip UBO, and a font-atlas texture.
///
/// Differences from desktop:
///  - calls go through the <see cref="Webgl2"/> [JSImport] shim, not native GL;
///  - WebGL2 has no base-instance draw, so per-draw instance-pointer rebinding is
///    always used (the desktop fallback path);
///  - shaders are the desktop GLSL adapted to GLSL ES 3.00 by <see cref="GlslEs"/>;
///  - the font atlas is re-uploaded whole when dirty (simpler than sub-rect +
///    UNPACK_ROW_LENGTH; optimize later).
///
/// Image draws are not wired yet (the canvas stubs the image hooks); rects,
/// glyphs/text, shadows and shapes are.
///
/// STATUS: scaffolding — never compiled or run here. See docs/web-font-rendering.md.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed unsafe class WebGl2RenderedCanvas : RenderedCanvasBase
{
    private int _rectProgram, _glyphProgram, _imageProgram, _shadowProgram, _shapeProgram;
    private int _rectProjLoc, _glyphProjLoc, _imageProjLoc, _shadowProjLoc, _shapeProjLoc;
    private int _glyphAtlasLoc, _imageTexLoc;

    private int _unitQuadVbo;
    private int _rectVao, _glyphVao, _imageVao, _shadowVao, _shapeVao;
    private int _rectVbo, _glyphVbo, _imageVbo, _shadowVbo, _shapeVbo;
    private int _rectCap, _glyphCap, _imageCap, _shadowCap, _shapeCap;
    private int _clipUbo;
    private int _fontAtlasTex;

    private Matrix4x4 _projection;

    public WebGl2RenderedCanvas(
        int width, int height,
        IGlyphSource fonts,
        FontHandle defaultFont,
        float dpiScale = 1f)
        : base(width, height, fonts, defaultFont, dpiScale)
    {
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);

        CompilePrograms();
        SetupUnitQuad();
        SetupInstanceBuffers();
        SetupClipUbo();
        SetupFontAtlas();
        UploadProjection();
    }

    protected override void OnResize(int width, int height)
    {
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);
        UploadProjection();
    }

    // ---- shader programs ----

    private void CompilePrograms()
    {
        _rectProgram = Link("canvas_rect.vert.glsl", "canvas_rect.frag.glsl");
        _glyphProgram = Link("canvas_glyph.vert.glsl", "canvas_glyph.frag.glsl");
        _imageProgram = Link("canvas_image.vert.glsl", "canvas_image.frag.glsl");
        _shadowProgram = Link("canvas_shadow.vert.glsl", "canvas_shadow.frag.glsl");
        _shapeProgram = Link("canvas_shape.vert.glsl", "canvas_shape.frag.glsl");

        _rectProjLoc = Webgl2.GetUniformLocation(_rectProgram, "u_projection");
        _glyphProjLoc = Webgl2.GetUniformLocation(_glyphProgram, "u_projection");
        _imageProjLoc = Webgl2.GetUniformLocation(_imageProgram, "u_projection");
        _shadowProjLoc = Webgl2.GetUniformLocation(_shadowProgram, "u_projection");
        _shapeProjLoc = Webgl2.GetUniformLocation(_shapeProgram, "u_projection");
        _glyphAtlasLoc = Webgl2.GetUniformLocation(_glyphProgram, "u_atlas");
        _imageTexLoc = Webgl2.GetUniformLocation(_imageProgram, "u_texture");

        BindClipBlock(_rectProgram);
        BindClipBlock(_glyphProgram);
        BindClipBlock(_imageProgram);
        BindClipBlock(_shadowProgram);
        BindClipBlock(_shapeProgram);

        Webgl2.UseProgram(_glyphProgram);
        Webgl2.Uniform1i(_glyphAtlasLoc, 0);
        Webgl2.UseProgram(_imageProgram);
        Webgl2.Uniform1i(_imageTexLoc, 0);
    }

    private static int Link(string vertName, string fragName)
    {
        var vert = Compile(VERTEX_SHADER, GlslEs.Adapt(LoadShader(vertName)), vertName);
        var frag = Compile(FRAGMENT_SHADER, GlslEs.Adapt(LoadShader(fragName)), fragName);
        var program = Webgl2.CreateProgram();
        Webgl2.AttachShader(program, vert);
        Webgl2.AttachShader(program, frag);
        Webgl2.LinkProgram(program);
        if (Webgl2.GetProgramLinked(program) == 0)
            throw new InvalidOperationException($"Link failed ({vertName}/{fragName}): {Webgl2.GetProgramInfoLog(program)}");
        return program;
    }

    private static int Compile(int type, string source, string name)
    {
        var shader = Webgl2.CreateShader(type);
        Webgl2.ShaderSource(shader, source);
        Webgl2.CompileShader(shader);
        if (Webgl2.GetShaderCompiled(shader) == 0)
            throw new InvalidOperationException($"Compile failed ({name}): {Webgl2.GetShaderInfoLog(shader)}");
        return shader;
    }

    private static string LoadShader(string fileName)
        => AppUtilsAssets.LoadString(typeof(WebGl2RenderedCanvas).Assembly, fileName);

    private static void BindClipBlock(int program)
    {
        var idx = Webgl2.GetUniformBlockIndex(program, "ClipRects");
        if (idx >= 0)
            Webgl2.UniformBlockBinding(program, idx, 0);
    }

    private void UploadProjection()
    {
        var m = _projection;
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref m.M11, 16));
        Webgl2.UseProgram(_rectProgram); Webgl2.UniformMatrix4fv(_rectProjLoc, bytes);
        Webgl2.UseProgram(_glyphProgram); Webgl2.UniformMatrix4fv(_glyphProjLoc, bytes);
        Webgl2.UseProgram(_imageProgram); Webgl2.UniformMatrix4fv(_imageProjLoc, bytes);
        Webgl2.UseProgram(_shadowProgram); Webgl2.UniformMatrix4fv(_shadowProjLoc, bytes);
        Webgl2.UseProgram(_shapeProgram); Webgl2.UniformMatrix4fv(_shapeProjLoc, bytes);
    }

    // ---- buffers / VAOs ----

    private void SetupUnitQuad()
    {
        Span<float> verts = stackalloc float[12] { 0f, 0f, 1f, 0f, 0f, 1f, 1f, 0f, 1f, 1f, 0f, 1f };
        _unitQuadVbo = Webgl2.CreateBuffer();
        Webgl2.BindBuffer(ARRAY_BUFFER, _unitQuadVbo);
        Webgl2.BufferDataSize(ARRAY_BUFFER, verts.Length * sizeof(float), STATIC_DRAW);
        Webgl2.BufferSubData(ARRAY_BUFFER, 0, MemoryMarshal.AsBytes(verts));
    }

    private void SetupInstanceBuffers()
    {
        _rectVbo = AllocVbo(MaxRects * sizeof(RectInstance)); _rectCap = MaxRects;
        _glyphVbo = AllocVbo(MaxGlyphs * sizeof(GlyphInstance)); _glyphCap = MaxGlyphs;
        _imageVbo = AllocVbo(MaxImages * sizeof(ImageInstance)); _imageCap = MaxImages;
        _shadowVbo = AllocVbo(MaxShadows * sizeof(ShadowInstance)); _shadowCap = MaxShadows;
        _shapeVbo = AllocVbo(MaxShapes * sizeof(ShapeInstance)); _shapeCap = MaxShapes;

        _rectVao = CreateRectVao();
        _glyphVao = CreateGlyphVao();
        _imageVao = CreateImageVao();
        _shadowVao = CreateShadowVao();
        _shapeVao = CreateShapeVao();
    }

    private static int AllocVbo(int sizeBytes)
    {
        var vbo = Webgl2.CreateBuffer();
        Webgl2.BindBuffer(ARRAY_BUFFER, vbo);
        Webgl2.BufferDataSize(ARRAY_BUFFER, sizeBytes, DYNAMIC_DRAW);
        return vbo;
    }

    private void BindUnitQuad()
    {
        Webgl2.BindBuffer(ARRAY_BUFFER, _unitQuadVbo);
        Webgl2.VertexAttribPointer(0, 2, FLOAT, 0, 2 * sizeof(float), 0);
        Webgl2.EnableVertexAttribArray(0);
    }

    private static int Off<T>(string field) => (int)Marshal.OffsetOf<T>(field);
    private static void Fa(uint i, int comps, int stride, int off)
    {
        Webgl2.VertexAttribPointer((int)i, comps, FLOAT, 0, stride, off);
        Webgl2.EnableVertexAttribArray((int)i);
        Webgl2.VertexAttribDivisor((int)i, 1);
    }
    private static void Ua(uint i, int stride, int off)
    {
        Webgl2.VertexAttribIPointer((int)i, 1, UNSIGNED_INT, stride, off);
        Webgl2.EnableVertexAttribArray((int)i);
        Webgl2.VertexAttribDivisor((int)i, 1);
    }

    private int CreateRectVao()
    {
        var vao = Webgl2.CreateVertexArray();
        Webgl2.BindVertexArray(vao);
        BindUnitQuad();
        Webgl2.BindBuffer(ARRAY_BUFFER, _rectVbo);
        var s = sizeof(RectInstance);
        Fa(1, 4, s, Off<RectInstance>(nameof(RectInstance.Rect)));
        Fa(2, 4, s, Off<RectInstance>(nameof(RectInstance.BorderRadius)));
        Fa(3, 4, s, Off<RectInstance>(nameof(RectInstance.BorderSize)));
        Ua(4, s, Off<RectInstance>(nameof(RectInstance.BgColor)));
        Ua(5, s, Off<RectInstance>(nameof(RectInstance.BorderColorTop)));
        Ua(6, s, Off<RectInstance>(nameof(RectInstance.BorderColorRight)));
        Ua(7, s, Off<RectInstance>(nameof(RectInstance.BorderColorBottom)));
        Ua(8, s, Off<RectInstance>(nameof(RectInstance.BorderColorLeft)));
        Ua(9, s, Off<RectInstance>(nameof(RectInstance.ClipIndex)));
        Webgl2.BindVertexArray(0);
        return vao;
    }

    private int CreateGlyphVao()
    {
        var vao = Webgl2.CreateVertexArray();
        Webgl2.BindVertexArray(vao);
        BindUnitQuad();
        Webgl2.BindBuffer(ARRAY_BUFFER, _glyphVbo);
        var s = sizeof(GlyphInstance);
        Fa(1, 4, s, Off<GlyphInstance>(nameof(GlyphInstance.Rect)));
        Fa(2, 4, s, Off<GlyphInstance>(nameof(GlyphInstance.AtlasUV)));
        Ua(3, s, Off<GlyphInstance>(nameof(GlyphInstance.Color)));
        Ua(4, s, Off<GlyphInstance>(nameof(GlyphInstance.ClipIndex)));
        Fa(5, 1, s, Off<GlyphInstance>(nameof(GlyphInstance.Rotation)));
        Webgl2.BindVertexArray(0);
        return vao;
    }

    private int CreateImageVao()
    {
        var vao = Webgl2.CreateVertexArray();
        Webgl2.BindVertexArray(vao);
        BindUnitQuad();
        Webgl2.BindBuffer(ARRAY_BUFFER, _imageVbo);
        var s = sizeof(ImageInstance);
        Fa(1, 4, s, Off<ImageInstance>(nameof(ImageInstance.Rect)));
        Fa(2, 4, s, Off<ImageInstance>(nameof(ImageInstance.SrcUV)));
        Ua(3, s, Off<ImageInstance>(nameof(ImageInstance.Tint)));
        Ua(4, s, Off<ImageInstance>(nameof(ImageInstance.ClipIndex)));
        Fa(5, 1, s, Off<ImageInstance>(nameof(ImageInstance.Rotation)));
        Webgl2.BindVertexArray(0);
        return vao;
    }

    private int CreateShadowVao()
    {
        var vao = Webgl2.CreateVertexArray();
        Webgl2.BindVertexArray(vao);
        BindUnitQuad();
        Webgl2.BindBuffer(ARRAY_BUFFER, _shadowVbo);
        var s = sizeof(ShadowInstance);
        Fa(1, 4, s, Off<ShadowInstance>(nameof(ShadowInstance.OuterRect)));
        Fa(2, 4, s, Off<ShadowInstance>(nameof(ShadowInstance.ShadowRect)));
        Fa(3, 4, s, Off<ShadowInstance>(nameof(ShadowInstance.BorderRadius)));
        Fa(4, 1, s, Off<ShadowInstance>(nameof(ShadowInstance.Sigma)));
        Ua(5, s, Off<ShadowInstance>(nameof(ShadowInstance.Color)));
        Ua(6, s, Off<ShadowInstance>(nameof(ShadowInstance.ClipIndex)));
        Webgl2.BindVertexArray(0);
        return vao;
    }

    private int CreateShapeVao()
    {
        var vao = Webgl2.CreateVertexArray();
        Webgl2.BindVertexArray(vao);
        BindUnitQuad();
        Webgl2.BindBuffer(ARRAY_BUFFER, _shapeVbo);
        var s = sizeof(ShapeInstance);
        Fa(1, 4, s, Off<ShapeInstance>(nameof(ShapeInstance.OuterRect)));
        Fa(2, 4, s, Off<ShapeInstance>(nameof(ShapeInstance.ShapeData)));
        Fa(3, 4, s, Off<ShapeInstance>(nameof(ShapeInstance.ShapeData2)));
        Fa(4, 1, s, Off<ShapeInstance>(nameof(ShapeInstance.HalfWidth)));
        Ua(5, s, Off<ShapeInstance>(nameof(ShapeInstance.Color)));
        Ua(6, s, Off<ShapeInstance>(nameof(ShapeInstance.ShapeType)));
        Ua(7, s, Off<ShapeInstance>(nameof(ShapeInstance.ClipIndex)));
        Ua(8, s, Off<ShapeInstance>(nameof(ShapeInstance.Color2)));
        Ua(9, s, Off<ShapeInstance>(nameof(ShapeInstance.Flags)));
        Webgl2.BindVertexArray(0);
        return vao;
    }

    private void SetupClipUbo()
    {
        _clipUbo = Webgl2.CreateBuffer();
        Webgl2.BindBuffer(UNIFORM_BUFFER, _clipUbo);
        Webgl2.BufferDataSize(UNIFORM_BUFFER, MaxClips * sizeof(Vector4), DYNAMIC_DRAW);
        Webgl2.BindBufferBase(UNIFORM_BUFFER, 0, _clipUbo);
    }

    private void SetupFontAtlas()
    {
        _fontAtlasTex = Webgl2.CreateTexture();
        Webgl2.BindTexture(TEXTURE_2D, _fontAtlasTex);
        Webgl2.PixelStorei(UNPACK_ALIGNMENT, 1);
        var ro = FontBackend.AtlasPixels;
        var span = AsSpan(ro);
        Webgl2.TexImage2DData(TEXTURE_2D, 0, R8, FontBackend.AtlasWidth, FontBackend.AtlasHeight, 0, RED, UNSIGNED_BYTE, span);
        Webgl2.TexParameteri(TEXTURE_2D, TEXTURE_MIN_FILTER, LINEAR);
        Webgl2.TexParameteri(TEXTURE_2D, TEXTURE_MAG_FILTER, LINEAR);
        Webgl2.TexParameteri(TEXTURE_2D, TEXTURE_WRAP_S, CLAMP_TO_EDGE);
        Webgl2.TexParameteri(TEXTURE_2D, TEXTURE_WRAP_T, CLAMP_TO_EDGE);
        FontBackend.ClearDirty();
    }

    // ---- upload hooks ----

    private void Upload<T>(int vbo, ref int cap, T[] data, int count) where T : unmanaged
    {
        if (count == 0) return;
        var elem = Unsafe.SizeOf<T>();
        if (count > cap)
        {
            var newCap = Math.Max(count, cap * 2);
            Webgl2.BindBuffer(ARRAY_BUFFER, vbo);
            Webgl2.BufferDataSize(ARRAY_BUFFER, newCap * elem, DYNAMIC_DRAW);
            cap = newCap;
        }
        Webgl2.BindBuffer(ARRAY_BUFFER, vbo);
        Webgl2.BufferSubData(ARRAY_BUFFER, 0, MemoryMarshal.AsBytes(data.AsSpan(0, count)));
    }

    protected override void UploadRectInstances(RectInstance[] data, int count) => Upload(_rectVbo, ref _rectCap, data, count);
    protected override void UploadGlyphInstances(GlyphInstance[] data, int count) => Upload(_glyphVbo, ref _glyphCap, data, count);
    protected override void UploadImageInstances(ImageInstance[] data, int count) => Upload(_imageVbo, ref _imageCap, data, count);
    protected override void UploadShadowInstances(ShadowInstance[] data, int count) => Upload(_shadowVbo, ref _shadowCap, data, count);
    protected override void UploadShapeInstances(ShapeInstance[] data, int count) => Upload(_shapeVbo, ref _shapeCap, data, count);

    protected override void UploadClips(List<Vector4> clips)
    {
        Webgl2.BindBuffer(UNIFORM_BUFFER, _clipUbo);
        if (clips.Count == 0) return;
        var span = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(clips));
        Webgl2.BufferSubData(UNIFORM_BUFFER, 0, span);
    }

    protected override void UpdateAtlasIfDirty()
    {
        if (!FontBackend.AtlasDirty) return;
        // Re-upload the whole atlas (R8). Atlas changes are rare (new glyphs), so the
        // simplicity is worth more than a sub-rect upload for the first cut.
        Webgl2.BindTexture(TEXTURE_2D, _fontAtlasTex);
        Webgl2.PixelStorei(UNPACK_ALIGNMENT, 1);
        var span = AsSpan(FontBackend.AtlasPixels);
        Webgl2.TexSubImage2D(TEXTURE_2D, 0, 0, 0, FontBackend.AtlasWidth, FontBackend.AtlasHeight, RED, UNSIGNED_BYTE, span);
        FontBackend.ClearDirty();
    }

    // ---- draw ----

    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls)
    {
        UploadProjection();
        var fbW = (int)MathF.Round(Width * DpiScale);
        var fbH = (int)MathF.Round(Height * DpiScale);
        Webgl2.Viewport(0, 0, fbW, fbH);

        if (drawCalls.Count == 0) return;

        Webgl2.Disable(DEPTH_TEST);
        Webgl2.Enable(BLEND);
        Webgl2.BlendFunc(SRC_ALPHA, ONE_MINUS_SRC_ALPHA);
        Webgl2.ActiveTexture(TEXTURE0);

        var bound = (DrawKind)255;

        for (var idx = 0; idx < drawCalls.Count; idx++)
        {
            var call = drawCalls[idx];
            if (call.Kind != bound)
            {
                switch (call.Kind)
                {
                    case DrawKind.Rect: Webgl2.UseProgram(_rectProgram); Webgl2.BindVertexArray(_rectVao); break;
                    case DrawKind.Glyph: Webgl2.UseProgram(_glyphProgram); Webgl2.BindVertexArray(_glyphVao); Webgl2.BindTexture(TEXTURE_2D, _fontAtlasTex); break;
                    case DrawKind.Image: Webgl2.UseProgram(_imageProgram); Webgl2.BindVertexArray(_imageVao); break;
                    case DrawKind.Shadow: Webgl2.UseProgram(_shadowProgram); Webgl2.BindVertexArray(_shadowVao); break;
                    case DrawKind.Shape: Webgl2.UseProgram(_shapeProgram); Webgl2.BindVertexArray(_shapeVao); break;
                }
                bound = call.Kind;
            }

            // No base-instance in WebGL2: rebind this batch's instance pointers at the
            // batch's first-instance byte offset, then draw.
            RebindInstancePointers(call.Kind, call.InstanceStart);
            Webgl2.DrawArraysInstanced(TRIANGLES, 0, 6, call.InstanceCount);
        }

        Webgl2.BindVertexArray(0);
    }

    private void RebindInstancePointers(DrawKind kind, int first)
    {
        switch (kind)
        {
            case DrawKind.Rect:
            {
                Webgl2.BindBuffer(ARRAY_BUFFER, _rectVbo);
                var s = sizeof(RectInstance); var b = first * s;
                Sf(1, 4, s, b + Off<RectInstance>(nameof(RectInstance.Rect)));
                Sf(2, 4, s, b + Off<RectInstance>(nameof(RectInstance.BorderRadius)));
                Sf(3, 4, s, b + Off<RectInstance>(nameof(RectInstance.BorderSize)));
                Su(4, s, b + Off<RectInstance>(nameof(RectInstance.BgColor)));
                Su(5, s, b + Off<RectInstance>(nameof(RectInstance.BorderColorTop)));
                Su(6, s, b + Off<RectInstance>(nameof(RectInstance.BorderColorRight)));
                Su(7, s, b + Off<RectInstance>(nameof(RectInstance.BorderColorBottom)));
                Su(8, s, b + Off<RectInstance>(nameof(RectInstance.BorderColorLeft)));
                Su(9, s, b + Off<RectInstance>(nameof(RectInstance.ClipIndex)));
                break;
            }
            case DrawKind.Glyph:
            {
                Webgl2.BindBuffer(ARRAY_BUFFER, _glyphVbo);
                var s = sizeof(GlyphInstance); var b = first * s;
                Sf(1, 4, s, b + Off<GlyphInstance>(nameof(GlyphInstance.Rect)));
                Sf(2, 4, s, b + Off<GlyphInstance>(nameof(GlyphInstance.AtlasUV)));
                Su(3, s, b + Off<GlyphInstance>(nameof(GlyphInstance.Color)));
                Su(4, s, b + Off<GlyphInstance>(nameof(GlyphInstance.ClipIndex)));
                Sf(5, 1, s, b + Off<GlyphInstance>(nameof(GlyphInstance.Rotation)));
                break;
            }
            case DrawKind.Image:
            {
                Webgl2.BindBuffer(ARRAY_BUFFER, _imageVbo);
                var s = sizeof(ImageInstance); var b = first * s;
                Sf(1, 4, s, b + Off<ImageInstance>(nameof(ImageInstance.Rect)));
                Sf(2, 4, s, b + Off<ImageInstance>(nameof(ImageInstance.SrcUV)));
                Su(3, s, b + Off<ImageInstance>(nameof(ImageInstance.Tint)));
                Su(4, s, b + Off<ImageInstance>(nameof(ImageInstance.ClipIndex)));
                Sf(5, 1, s, b + Off<ImageInstance>(nameof(ImageInstance.Rotation)));
                break;
            }
            case DrawKind.Shadow:
            {
                Webgl2.BindBuffer(ARRAY_BUFFER, _shadowVbo);
                var s = sizeof(ShadowInstance); var b = first * s;
                Sf(1, 4, s, b + Off<ShadowInstance>(nameof(ShadowInstance.OuterRect)));
                Sf(2, 4, s, b + Off<ShadowInstance>(nameof(ShadowInstance.ShadowRect)));
                Sf(3, 4, s, b + Off<ShadowInstance>(nameof(ShadowInstance.BorderRadius)));
                Sf(4, 1, s, b + Off<ShadowInstance>(nameof(ShadowInstance.Sigma)));
                Su(5, s, b + Off<ShadowInstance>(nameof(ShadowInstance.Color)));
                Su(6, s, b + Off<ShadowInstance>(nameof(ShadowInstance.ClipIndex)));
                break;
            }
            default:
            {
                Webgl2.BindBuffer(ARRAY_BUFFER, _shapeVbo);
                var s = sizeof(ShapeInstance); var b = first * s;
                Sf(1, 4, s, b + Off<ShapeInstance>(nameof(ShapeInstance.OuterRect)));
                Sf(2, 4, s, b + Off<ShapeInstance>(nameof(ShapeInstance.ShapeData)));
                Sf(3, 4, s, b + Off<ShapeInstance>(nameof(ShapeInstance.ShapeData2)));
                Sf(4, 1, s, b + Off<ShapeInstance>(nameof(ShapeInstance.HalfWidth)));
                Su(5, s, b + Off<ShapeInstance>(nameof(ShapeInstance.Color)));
                Su(6, s, b + Off<ShapeInstance>(nameof(ShapeInstance.ShapeType)));
                Su(7, s, b + Off<ShapeInstance>(nameof(ShapeInstance.ClipIndex)));
                Su(8, s, b + Off<ShapeInstance>(nameof(ShapeInstance.Color2)));
                Su(9, s, b + Off<ShapeInstance>(nameof(ShapeInstance.Flags)));
                break;
            }
        }
    }

    private static void Sf(uint i, int comps, int stride, int off) => Webgl2.VertexAttribPointer((int)i, comps, FLOAT, 0, stride, off);
    private static void Su(uint i, int stride, int off) => Webgl2.VertexAttribIPointer((int)i, 1, UNSIGNED_INT, stride, off);

    // ---- images: not wired yet ----

    protected override Size GetImageSizeImpl(string imageId) => default;
    protected override uint GetImageTextureId(string imageId) => 0;

    // ---- helpers ----

    // A mutable Span view over read-only atlas memory. We only read it (the JS shim
    // copies via slice() before handing it to WebGL), so aliasing away the readonly
    // is safe here and avoids a managed copy of the whole atlas each upload.
    private static Span<byte> AsSpan(ReadOnlySpan<byte> ro)
        => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in MemoryMarshal.GetReference(ro)), ro.Length);
}
