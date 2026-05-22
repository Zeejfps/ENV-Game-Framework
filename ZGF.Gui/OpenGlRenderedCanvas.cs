using System.Numerics;
using System.Runtime.InteropServices;
using OpenGL.NET;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public sealed unsafe class OpenGlRenderedCanvas : RenderedCanvasBase, IDisposable
{
    private readonly GlSharedResources _shared;

    private uint _rectVao, _glyphVao, _imageVao, _shadowVao;
    private uint _rectInstanceVbo, _glyphInstanceVbo, _imageInstanceVbo, _shadowInstanceVbo;
    private uint _clipUbo;

    private Matrix4x4 _projection;
    private int _atlasUploads;

    public OpenGlRenderedCanvas(
        int width, int height,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        GlSharedResources shared,
        float dpiScale = 1f)
        : base(width, height, fonts, defaultFont, dpiScale)
    {
        _shared = shared;

        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);
        UploadProjection();
        SetupInstanceBuffers();
        SetupClipUbo();
    }

    protected override void OnResize(int width, int height)
    {
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);
        UploadProjection();
    }

    protected override Size GetImageSizeImpl(string imageId) => _shared.ImageManager.GetImageSize(imageId);
    protected override uint GetImageTextureId(string imageId) => _shared.ImageManager.GetTextureId(imageId);

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

    protected override void UploadShadowInstances(ShadowInstance[] data, int count)
    {
        if (count == 0) return;
        glBindBuffer(GL_ARRAY_BUFFER, _shadowInstanceVbo);
        fixed (ShadowInstance* ptr = &data[0])
            glBufferSubData(GL_ARRAY_BUFFER, 0, count * sizeof(ShadowInstance), ptr);
    }

    protected override void UploadClips(List<Vector4> clips)
    {
        glBindBuffer(GL_UNIFORM_BUFFER, _clipUbo);
        if (clips.Count == 0) return;

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
        _shared.UploadAtlasIfDirty(ref _atlasUploads);
        LastFrameUploadCount = _atlasUploads;
    }

    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls)
    {
        // Per-frame setup: the shared shaders' projection uniform is overwritten by
        // every OTHER canvas that calls UploadProjection, so we must re-establish
        // this canvas's projection at the start of every frame. Same logic applies
        // to glViewport: it's per-context, but it's also per-context state we need
        // to set explicitly when this canvas gets time to draw (window/framebuffer
        // resizes don't auto-update viewport on subsequent context-make-currents).
        UploadProjection();
        var fbW = (int)MathF.Round(Width * DpiScale);
        var fbH = (int)MathF.Round(Height * DpiScale);
        glViewport(0, 0, fbW, fbH);
        if (PopupDebugLog.IsOn(PopupDebugLog.Channel.Render))
        {
            PopupDebugLog.Log(PopupDebugLog.Channel.Render,
                $"OpenGlRenderedCanvas.IssueDraws canvas={Width}x{Height} dpi={DpiScale} viewport=({fbW}x{fbH}) drawCalls={drawCalls.Count}");
        }

        if (drawCalls.Count == 0) return;

        glDisable(GL_DEPTH_TEST);
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glActiveTexture(GL_TEXTURE0);

        var atlasTex = _shared.FontAtlasTextureId;
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
                        glUseProgram(_shared.RectShader);
                        glBindVertexArray(_rectVao);
                        break;
                    case DrawKind.Glyph:
                        glUseProgram(_shared.GlyphShader);
                        glBindVertexArray(_glyphVao);
                        glBindTexture(GL_TEXTURE_2D, atlasTex);
                        boundTexture = atlasTex;
                        break;
                    case DrawKind.Image:
                        glUseProgram(_shared.ImageShader);
                        glBindVertexArray(_imageVao);
                        boundTexture = 0;
                        break;
                    case DrawKind.Shadow:
                        glUseProgram(_shared.ShadowShader);
                        glBindVertexArray(_shadowVao);
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
                case DrawKind.Shadow: RebindShadowInstancePointers(call.InstanceStart); break;
            }

            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, call.InstanceCount);
        }

        glBindVertexArray(0);
    }

    private void UploadProjection()
    {
        var p = _projection;
        var ptr = &p.M11;
        glUseProgram(_shared.RectShader);
        glUniformMatrix4fv(_shared.RectProjLoc, 1, false, ptr);
        glUseProgram(_shared.GlyphShader);
        glUniformMatrix4fv(_shared.GlyphProjLoc, 1, false, ptr);
        glUseProgram(_shared.ImageShader);
        glUniformMatrix4fv(_shared.ImageProjLoc, 1, false, ptr);
        glUseProgram(_shared.ShadowShader);
        glUniformMatrix4fv(_shared.ShadowProjLoc, 1, false, ptr);
        AssertNoGlError();
    }

    private void SetupInstanceBuffers()
    {
        _rectInstanceVbo = AllocInstanceVbo(MaxRects * sizeof(RectInstance));
        _glyphInstanceVbo = AllocInstanceVbo(MaxGlyphs * sizeof(GlyphInstance));
        _imageInstanceVbo = AllocInstanceVbo(MaxImages * sizeof(ImageInstance));
        _shadowInstanceVbo = AllocInstanceVbo(MaxShadows * sizeof(ShadowInstance));

        _rectVao = CreateRectVao();
        _glyphVao = CreateGlyphVao();
        _imageVao = CreateImageVao();
        _shadowVao = CreateShadowVao();
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
        glBindBuffer(GL_ARRAY_BUFFER, _shared.UnitQuadVbo);
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
        glBindBuffer(GL_ARRAY_BUFFER, _shared.UnitQuadVbo);
        glVertexAttribPointer(0, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0);
        glEnableVertexAttribArray(0);
        glBindBuffer(GL_ARRAY_BUFFER, _glyphInstanceVbo);
        var stride = sizeof(GlyphInstance);
        AddFloatInstanceAttrib(1, 4, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.Rect)));
        AddFloatInstanceAttrib(2, 4, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.AtlasUV)));
        AddUintInstanceAttrib(3, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.Color)));
        AddUintInstanceAttrib(4, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.ClipIndex)));
        AddFloatInstanceAttrib(5, 1, stride, OffsetOf<GlyphInstance>(nameof(GlyphInstance.Rotation)));
        glBindVertexArray(0);
        AssertNoGlError();
        return vao;
    }

    private uint CreateImageVao()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        glBindVertexArray(vao);
        glBindBuffer(GL_ARRAY_BUFFER, _shared.UnitQuadVbo);
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

    private uint CreateShadowVao()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        glBindVertexArray(vao);
        glBindBuffer(GL_ARRAY_BUFFER, _shared.UnitQuadVbo);
        glVertexAttribPointer(0, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0);
        glEnableVertexAttribArray(0);
        glBindBuffer(GL_ARRAY_BUFFER, _shadowInstanceVbo);
        var stride = sizeof(ShadowInstance);
        AddFloatInstanceAttrib(1, 4, stride, OffsetOf<ShadowInstance>(nameof(ShadowInstance.OuterRect)));
        AddFloatInstanceAttrib(2, 4, stride, OffsetOf<ShadowInstance>(nameof(ShadowInstance.ShadowRect)));
        AddFloatInstanceAttrib(3, 4, stride, OffsetOf<ShadowInstance>(nameof(ShadowInstance.BorderRadius)));
        AddFloatInstanceAttrib(4, 1, stride, OffsetOf<ShadowInstance>(nameof(ShadowInstance.Sigma)));
        AddUintInstanceAttrib(5, stride, OffsetOf<ShadowInstance>(nameof(ShadowInstance.Color)));
        AddUintInstanceAttrib(6, stride, OffsetOf<ShadowInstance>(nameof(ShadowInstance.ClipIndex)));
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
        SetFloatInstancePointer(5, 1, stride, baseBytes + OffsetOf<GlyphInstance>(nameof(GlyphInstance.Rotation)));
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

    private void RebindShadowInstancePointers(int firstInstance)
    {
        glBindBuffer(GL_ARRAY_BUFFER, _shadowInstanceVbo);
        var stride = sizeof(ShadowInstance);
        var baseBytes = firstInstance * stride;
        SetFloatInstancePointer(1, 4, stride, baseBytes + OffsetOf<ShadowInstance>(nameof(ShadowInstance.OuterRect)));
        SetFloatInstancePointer(2, 4, stride, baseBytes + OffsetOf<ShadowInstance>(nameof(ShadowInstance.ShadowRect)));
        SetFloatInstancePointer(3, 4, stride, baseBytes + OffsetOf<ShadowInstance>(nameof(ShadowInstance.BorderRadius)));
        SetFloatInstancePointer(4, 1, stride, baseBytes + OffsetOf<ShadowInstance>(nameof(ShadowInstance.Sigma)));
        SetUintInstancePointer(5, stride, baseBytes + OffsetOf<ShadowInstance>(nameof(ShadowInstance.Color)));
        SetUintInstancePointer(6, stride, baseBytes + OffsetOf<ShadowInstance>(nameof(ShadowInstance.ClipIndex)));
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

    public void Dispose()
    {
        DeleteBuffer(ref _rectInstanceVbo);
        DeleteBuffer(ref _glyphInstanceVbo);
        DeleteBuffer(ref _imageInstanceVbo);
        DeleteBuffer(ref _shadowInstanceVbo);
        DeleteBuffer(ref _clipUbo);
        DeleteVao(ref _rectVao);
        DeleteVao(ref _glyphVao);
        DeleteVao(ref _imageVao);
        DeleteVao(ref _shadowVao);
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
