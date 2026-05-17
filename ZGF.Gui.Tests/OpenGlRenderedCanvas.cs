using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGL.NET;
using ZGF.Fonts;
using ZGF.Geometry;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public sealed unsafe class OpenGlRenderedCanvas : RenderedCanvasBase, IDisposable
{
    // ---------- GL resources ----------

    private uint _rectShader, _glyphShader, _imageShader;
    private int _rectProjLoc, _glyphProjLoc, _imageProjLoc;
    private int _glyphAtlasLoc, _imageTexLoc;
    private uint _rectVao, _glyphVao, _imageVao;
    private uint _unitQuadVbo;
    private uint _rectInstanceVbo, _glyphInstanceVbo, _imageInstanceVbo;
    private uint _clipUbo;
    private uint _fontAtlasTextureId;

    private Matrix4x4 _projection;

    private readonly GlImageManager _imageManager;

    public OpenGlRenderedCanvas(
        int width, int height,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        GlImageManager imageManager)
        : base(width, height, fonts, defaultFont)
    {
        _imageManager = imageManager;

        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);

        var shadersDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders");
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

        // The ClipRects UBO uses binding point 0, but the shaders can't declare
        // `binding = 0` on GLSL 410 (macOS), so bind it explicitly per-program.
        BindClipBlockToZero(_rectShader);
        BindClipBlockToZero(_glyphShader);
        BindClipBlockToZero(_imageShader);

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

    // ---------- Abstract hook implementations ----------

    protected override void OnResize(int width, int height)
    {
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);
        UploadProjection();
    }

    protected override Size GetImageSizeImpl(string imageId) => _imageManager.GetImageSize(imageId);
    protected override uint GetImageTextureId(string imageId) => _imageManager.GetTextureId(imageId);

    protected override void UploadRectInstances(RectInstance[] data, int count)
    {
        if (count == 0) return;
        glBindBuffer(GL_ARRAY_BUFFER, _rectInstanceVbo);
        fixed (RectInstance* ptr = &data[0])
            glBufferSubData(GL_ARRAY_BUFFER, 0, count * sizeof(RectInstance), ptr);
    }

    protected override void UploadGlyphInstances(GlyphInstance[] data, int count)
    {
        if (count == 0) return;
        glBindBuffer(GL_ARRAY_BUFFER, _glyphInstanceVbo);
        fixed (GlyphInstance* ptr = &data[0])
            glBufferSubData(GL_ARRAY_BUFFER, 0, count * sizeof(GlyphInstance), ptr);
    }

    protected override void UploadImageInstances(ImageInstance[] data, int count)
    {
        if (count == 0) return;
        glBindBuffer(GL_ARRAY_BUFFER, _imageInstanceVbo);
        fixed (ImageInstance* ptr = &data[0])
            glBufferSubData(GL_ARRAY_BUFFER, 0, count * sizeof(ImageInstance), ptr);
    }

    protected override void UploadClips(List<Vector4> clips)
    {
        glBindBuffer(GL_UNIFORM_BUFFER, _clipUbo);
        if (clips.Count == 0) return;

        // List<Vector4> backing array isn't accessible; copy through stackalloc/temp.
        var n = clips.Count;
        if (n <= 256)
        {
            Span<Vector4> tmp = stackalloc Vector4[n];
            for (var i = 0; i < n; i++) tmp[i] = clips[i];
            fixed (Vector4* ptr = &tmp[0])
                glBufferSubData(GL_UNIFORM_BUFFER, 0, n * sizeof(Vector4), ptr);
        }
        else
        {
            var arr = new Vector4[n];
            for (var i = 0; i < n; i++) arr[i] = clips[i];
            fixed (Vector4* ptr = &arr[0])
                glBufferSubData(GL_UNIFORM_BUFFER, 0, n * sizeof(Vector4), ptr);
        }
    }

    protected override void UpdateAtlasIfDirty()
    {
        var fonts = FontBackend;
        if (!fonts.AtlasDirty)
            return;

        var rect = fonts.DirtyRect;
        if (rect.IsEmpty)
        {
            fonts.ClearDirty();
            return;
        }

        var pixels = fonts.AtlasPixels;
        glBindTexture(GL_TEXTURE_2D, _fontAtlasTextureId);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        glPixelStorei(GL_UNPACK_ROW_LENGTH, fonts.AtlasWidth);

        var offset = rect.Y * fonts.AtlasWidth + rect.X;
        fixed (byte* ptr = &MemoryMarshal.GetReference(pixels))
            glTexSubImage2D(GL_TEXTURE_2D, 0, rect.X, rect.Y, rect.Width, rect.Height,
                GL_RED, GL_UNSIGNED_BYTE, ptr + offset);

        glPixelStorei(GL_UNPACK_ROW_LENGTH, 0);
        AssertNoGlError();

        fonts.ClearDirty();
        LastFrameUploadCount++;
    }

    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls)
    {
        if (drawCalls.Count == 0)
            return;

        glDisable(GL_DEPTH_TEST);
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glActiveTexture(GL_TEXTURE0);

        DrawKind boundKind = (DrawKind)255;
        uint boundTexture = 0;

        for (var idx = 0; idx < drawCalls.Count; idx++)
        {
            var call = drawCalls[idx];
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

            switch (call.Kind)
            {
                case DrawKind.Rect: RebindRectInstancePointers(call.InstanceStart); break;
                case DrawKind.Glyph: RebindGlyphInstancePointers(call.InstanceStart); break;
                case DrawKind.Image: RebindImageInstancePointers(call.InstanceStart); break;
            }

            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, call.InstanceCount);
        }

        glBindVertexArray(0);
    }

    // ---------- GL setup helpers ----------

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

    private void SetupUnitQuad()
    {
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
        AddFloatInstanceAttrib(5, 1, stride, OffsetOf<ImageInstance>(nameof(ImageInstance.Rotation)));

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

    private static void SetFloatInstancePointer(uint index, int components, int stride, int offset)
    {
        glVertexAttribPointer(index, components, GL_FLOAT, false, stride, (void*)offset);
    }

    private static void SetUintInstancePointer(uint index, int stride, int offset)
    {
        glVertexAttribIPointer(index, 1, GL_UNSIGNED_INT, stride, (void*)offset);
    }

    private static int OffsetOf<T>(string field) => (int)Marshal.OffsetOf<T>(field);

    // macOS / GL 4.1 has no ARB_base_instance, so we cannot use
    // glDrawArraysInstancedBaseInstance. Instead, before each draw call we
    // re-point the per-instance attributes at (firstInstance * stride) bytes
    // into the instance VBO. The VAO must already be bound; the instance VBO
    // for the kind is bound here.
    private void RebindRectInstancePointers(int firstInstance)
    {
        glBindBuffer(GL_ARRAY_BUFFER, _rectInstanceVbo);
        var stride = sizeof(RectInstance);
        var baseBytes = firstInstance * stride;
        SetFloatInstancePointer(1, 4, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.Rect)));
        SetFloatInstancePointer(2, 4, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.BorderRadius)));
        SetFloatInstancePointer(3, 4, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.BorderSize)));
        SetUintInstancePointer(4, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.BgColor)));
        SetUintInstancePointer(5, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.BorderColorTop)));
        SetUintInstancePointer(6, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.BorderColorRight)));
        SetUintInstancePointer(7, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.BorderColorBottom)));
        SetUintInstancePointer(8, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.BorderColorLeft)));
        SetUintInstancePointer(9, stride, baseBytes + OffsetOf<RectInstance>(nameof(RectInstance.ClipIndex)));
    }

    private void RebindGlyphInstancePointers(int firstInstance)
    {
        glBindBuffer(GL_ARRAY_BUFFER, _glyphInstanceVbo);
        var stride = sizeof(GlyphInstance);
        var baseBytes = firstInstance * stride;
        SetFloatInstancePointer(1, 4, stride, baseBytes + OffsetOf<GlyphInstance>(nameof(GlyphInstance.Rect)));
        SetFloatInstancePointer(2, 4, stride, baseBytes + OffsetOf<GlyphInstance>(nameof(GlyphInstance.AtlasUV)));
        SetUintInstancePointer(3, stride, baseBytes + OffsetOf<GlyphInstance>(nameof(GlyphInstance.Color)));
        SetUintInstancePointer(4, stride, baseBytes + OffsetOf<GlyphInstance>(nameof(GlyphInstance.ClipIndex)));
    }

    private void RebindImageInstancePointers(int firstInstance)
    {
        glBindBuffer(GL_ARRAY_BUFFER, _imageInstanceVbo);
        var stride = sizeof(ImageInstance);
        var baseBytes = firstInstance * stride;
        SetFloatInstancePointer(1, 4, stride, baseBytes + OffsetOf<ImageInstance>(nameof(ImageInstance.Rect)));
        SetFloatInstancePointer(2, 4, stride, baseBytes + OffsetOf<ImageInstance>(nameof(ImageInstance.SrcUV)));
        SetUintInstancePointer(3, stride, baseBytes + OffsetOf<ImageInstance>(nameof(ImageInstance.Tint)));
        SetUintInstancePointer(4, stride, baseBytes + OffsetOf<ImageInstance>(nameof(ImageInstance.ClipIndex)));
        SetFloatInstancePointer(5, 1, stride, baseBytes + OffsetOf<ImageInstance>(nameof(ImageInstance.Rotation)));
    }

    private static void BindClipBlockToZero(uint shader)
    {
        Span<byte> name = stackalloc byte[10]; // "ClipRects\0"
        name[0] = (byte)'C'; name[1] = (byte)'l'; name[2] = (byte)'i'; name[3] = (byte)'p';
        name[4] = (byte)'R'; name[5] = (byte)'e'; name[6] = (byte)'c'; name[7] = (byte)'t';
        name[8] = (byte)'s'; name[9] = 0;
        uint blockIndex;
        fixed (byte* p = name)
            blockIndex = glGetUniformBlockIndex(shader, p);
        if (blockIndex != GL_INVALID_INDEX)
            glUniformBlockBinding(shader, blockIndex, 0);
    }

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
        var fonts = FontBackend;
        var width = fonts.AtlasWidth;
        var height = fonts.AtlasHeight;
        var pixels = fonts.AtlasPixels;

        uint tex;
        glGenTextures(1, &tex);
        glBindTexture(GL_TEXTURE_2D, tex);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        fixed (byte* ptr = &MemoryMarshal.GetReference(pixels))
            glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_R8, width, height, 0, GL_RED, GL_UNSIGNED_BYTE, ptr);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        AssertNoGlError();

        _fontAtlasTextureId = tex;
        fonts.ClearDirty();
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
