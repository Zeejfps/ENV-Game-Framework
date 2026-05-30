// Metal backend for RenderedCanvasBase. Per-canvas: globals buffer, clip buffer,
// instance buffers, projection, pending command buffer. Shared (via
// MetalSharedResources): pipelines, sampler, unit-quad buffer, atlas texture,
// image manager.

using System.Numerics;
using System.Runtime.InteropServices;
using ZGF.Core.MacOs;
using ZGF.Fonts;
using static ZGF.Core.MacOs.Objc;

namespace ZGF.Gui;

public sealed unsafe class MetalRenderedCanvas : RenderedCanvasBase, IDisposable
{
    private readonly MetalSharedResources _shared;

    private IntPtr _rectInstanceBuffer;
    private IntPtr _glyphInstanceBuffer;
    private IntPtr _imageInstanceBuffer;
    private IntPtr _shadowInstanceBuffer;
    private IntPtr _globalsBuffer;
    private IntPtr _clipBuffer;

    private Matrix4x4 _projection;
    private IntPtr _pendingCommandBuffer;
    private IntPtr _currentEncoder;
    private int _atlasUploads;

    public MetalRenderedCanvas(
        int width, int height,
        FreeTypeFontBackend fonts, FontHandle defaultFont,
        MetalSharedResources shared,
        float dpiScale = 1f)
        : base(width, height, fonts, defaultFont, dpiScale)
    {
        _shared = shared;

        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);

        var device = shared.Device;
        _rectInstanceBuffer = NewSharedBuffer(device, MaxRects * sizeof(RectInstance));
        _glyphInstanceBuffer = NewSharedBuffer(device, MaxGlyphs * sizeof(GlyphInstance));
        _imageInstanceBuffer = NewSharedBuffer(device, MaxImages * sizeof(ImageInstance));
        _shadowInstanceBuffer = NewSharedBuffer(device, MaxShadows * sizeof(ShadowInstance));
        _globalsBuffer = NewSharedBuffer(device, sizeof(Matrix4x4));
        _clipBuffer = NewSharedBuffer(device, MaxClips * sizeof(Vector4));

        UploadProjectionToBuffer();
    }

    public void EndFrame(IntPtr renderCommandEncoder, IntPtr currentCommandBuffer)
    {
        if (_pendingCommandBuffer != IntPtr.Zero)
        {
            msg_Void(_pendingCommandBuffer, Sel("waitUntilCompleted"));
            Release(_pendingCommandBuffer);
            _pendingCommandBuffer = IntPtr.Zero;
        }

        _currentEncoder = renderCommandEncoder;
        base.EndFrame();
        _currentEncoder = IntPtr.Zero;

        if (currentCommandBuffer != IntPtr.Zero)
        {
            Retain(currentCommandBuffer);
            _pendingCommandBuffer = currentCommandBuffer;
        }
    }

    protected override void OnResize(int width, int height)
    {
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1f, 1f);
        UploadProjectionToBuffer();
    }

    protected override Size GetImageSizeImpl(string imageId) => _shared.ImageManager.GetImageSize(imageId);
    protected override uint GetImageTextureId(string imageId) => _shared.ImageManager.GetTextureId(imageId);

    protected override void UploadRectInstances(RectInstance[] data, int count)
    {
        if (count == 0) return;
        var dst = (RectInstance*)msg_IntPtr(_rectInstanceBuffer, Sel("contents"));
        fixed (RectInstance* src = &data[0])
            Buffer.MemoryCopy(src, dst, MaxRects * sizeof(RectInstance), count * sizeof(RectInstance));
    }

    protected override void UploadGlyphInstances(GlyphInstance[] data, int count)
    {
        if (count == 0) return;
        var dst = (GlyphInstance*)msg_IntPtr(_glyphInstanceBuffer, Sel("contents"));
        fixed (GlyphInstance* src = &data[0])
            Buffer.MemoryCopy(src, dst, MaxGlyphs * sizeof(GlyphInstance), count * sizeof(GlyphInstance));
    }

    protected override void UploadImageInstances(ImageInstance[] data, int count)
    {
        if (count == 0) return;
        var dst = (ImageInstance*)msg_IntPtr(_imageInstanceBuffer, Sel("contents"));
        fixed (ImageInstance* src = &data[0])
            Buffer.MemoryCopy(src, dst, MaxImages * sizeof(ImageInstance), count * sizeof(ImageInstance));
    }

    protected override void UploadShadowInstances(ShadowInstance[] data, int count)
    {
        if (count == 0) return;
        var dst = (ShadowInstance*)msg_IntPtr(_shadowInstanceBuffer, Sel("contents"));
        fixed (ShadowInstance* src = &data[0])
            Buffer.MemoryCopy(src, dst, MaxShadows * sizeof(ShadowInstance), count * sizeof(ShadowInstance));
    }

    protected override void UploadClips(List<Vector4> clips)
    {
        if (clips.Count == 0) return;
        var dst = (Vector4*)msg_IntPtr(_clipBuffer, Sel("contents"));
        for (var i = 0; i < clips.Count; i++) dst[i] = clips[i];
    }

    protected override void UpdateAtlasIfDirty()
    {
        _shared.UploadAtlasIfDirty(ref _atlasUploads);
        LastFrameUploadCount = _atlasUploads;
    }

    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls)
    {
        if (_currentEncoder == IntPtr.Zero || drawCalls.Count == 0)
            return;

        var enc = _currentEncoder;
        DrawKind boundKind = (DrawKind)255;
        IntPtr boundTexture = IntPtr.Zero;

        var setVertexBufferSel = Sel("setVertexBuffer:offset:atIndex:");
        var setFragmentBufferSel = Sel("setFragmentBuffer:offset:atIndex:");
        var setFragmentTextureSel = Sel("setFragmentTexture:atIndex:");
        var setFragmentSamplerSel = Sel("setFragmentSamplerState:atIndex:");
        var setPipelineSel = Sel("setRenderPipelineState:");
        var drawPrimSel = Sel("drawPrimitives:vertexStart:vertexCount:instanceCount:");

        for (var i = 0; i < drawCalls.Count; i++)
        {
            var call = drawCalls[i];

            if (call.Kind != boundKind)
            {
                IntPtr pipeline = call.Kind switch
                {
                    DrawKind.Rect => _shared.RectPipeline,
                    DrawKind.Glyph => _shared.GlyphPipeline,
                    DrawKind.Image => _shared.ImagePipeline,
                    DrawKind.Shadow => _shared.ShadowPipeline,
                    _ => IntPtr.Zero,
                };
                msg_Void_IntPtr(enc, setPipelineSel, pipeline);

                msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, _globalsBuffer, 0, 0);
                msg_Void_IntPtr_NUInt_NUInt(enc, setFragmentBufferSel, _globalsBuffer, 0, 0);
                msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, _clipBuffer, 0, 1);
                msg_Void_IntPtr_NUInt_NUInt(enc, setFragmentBufferSel, _clipBuffer, 0, 1);
                msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, _shared.UnitQuadBuffer, 0, 2);

                switch (call.Kind)
                {
                    case DrawKind.Rect:
                        msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, _rectInstanceBuffer, 0, 3);
                        break;
                    case DrawKind.Glyph:
                        msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, _glyphInstanceBuffer, 0, 3);
                        msg_Void_IntPtr_NUInt(enc, setFragmentTextureSel, _shared.AtlasTexture, 0);
                        msg_Void_IntPtr_NUInt(enc, setFragmentSamplerSel, _shared.SamplerState, 0);
                        boundTexture = _shared.AtlasTexture;
                        break;
                    case DrawKind.Image:
                        msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, _imageInstanceBuffer, 0, 3);
                        msg_Void_IntPtr_NUInt(enc, setFragmentSamplerSel, _shared.SamplerState, 0);
                        boundTexture = IntPtr.Zero;
                        break;
                    case DrawKind.Shadow:
                        msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, _shadowInstanceBuffer, 0, 3);
                        break;
                }

                boundKind = call.Kind;
            }

            if (call.Kind == DrawKind.Image)
            {
                var tex = _shared.ImageManager.GetMetalTexture(call.TextureId);
                if (tex != boundTexture)
                {
                    msg_Void_IntPtr_NUInt(enc, setFragmentTextureSel, tex, 0);
                    boundTexture = tex;
                }
            }

            if (call.InstanceStart > 0)
            {
                var (buf, stride) = call.Kind switch
                {
                    DrawKind.Rect => (_rectInstanceBuffer, sizeof(RectInstance)),
                    DrawKind.Glyph => (_glyphInstanceBuffer, sizeof(GlyphInstance)),
                    DrawKind.Image => (_imageInstanceBuffer, sizeof(ImageInstance)),
                    DrawKind.Shadow => (_shadowInstanceBuffer, sizeof(ShadowInstance)),
                    _ => (IntPtr.Zero, 0),
                };
                msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, buf, (nuint)(call.InstanceStart * stride), 3);
            }

            msg_Void_MTLPrim_NUInt_NUInt_NUInt(enc, drawPrimSel,
                (nuint)MTLPrimitiveType.Triangle, 0, 6, (nuint)call.InstanceCount);

            if (call.InstanceStart > 0)
            {
                var buf = call.Kind switch
                {
                    DrawKind.Rect => _rectInstanceBuffer,
                    DrawKind.Glyph => _glyphInstanceBuffer,
                    DrawKind.Image => _imageInstanceBuffer,
                    DrawKind.Shadow => _shadowInstanceBuffer,
                    _ => IntPtr.Zero,
                };
                msg_Void_IntPtr_NUInt_NUInt(enc, setVertexBufferSel, buf, 0, 3);
            }
        }
    }

    private void UploadProjectionToBuffer()
    {
        if (_globalsBuffer == IntPtr.Zero) return;
        var dst = (Matrix4x4*)msg_IntPtr(_globalsBuffer, Sel("contents"));
        *dst = Matrix4x4.Transpose(_projection);
    }

    // ---------- Vertex descriptors (referenced from MetalSharedResources) ----------

    internal static IntPtr MakeRectVertexDescriptor()
    {
        var desc = MakeVertexDescriptorBase();
        SetAttribute(desc, 0, MTLVertexFormat.Float2, 0, 2);
        var off = 0;
        SetAttribute(desc, 1, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 2, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 3, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 4, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 5, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 6, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 7, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 8, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 9, MTLVertexFormat.UInt, off, 3); off += 4;
        SetLayout(desc, 2, 8, MTLVertexStepFunction.PerVertex);
        SetLayout(desc, 3, sizeof(RectInstance), MTLVertexStepFunction.PerInstance);
        return desc;
    }

    internal static IntPtr MakeGlyphVertexDescriptor()
    {
        var desc = MakeVertexDescriptorBase();
        SetAttribute(desc, 0, MTLVertexFormat.Float2, 0, 2);
        var off = 0;
        SetAttribute(desc, 1, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 2, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 3, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 4, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 5, MTLVertexFormat.Float, off, 3); off += 4;
        SetLayout(desc, 2, 8, MTLVertexStepFunction.PerVertex);
        SetLayout(desc, 3, sizeof(GlyphInstance), MTLVertexStepFunction.PerInstance);
        return desc;
    }

    internal static IntPtr MakeImageVertexDescriptor()
    {
        var desc = MakeVertexDescriptorBase();
        SetAttribute(desc, 0, MTLVertexFormat.Float2, 0, 2);
        var off = 0;
        SetAttribute(desc, 1, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 2, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 3, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 4, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 5, MTLVertexFormat.Float, off, 3); off += 4;
        SetLayout(desc, 2, 8, MTLVertexStepFunction.PerVertex);
        SetLayout(desc, 3, sizeof(ImageInstance), MTLVertexStepFunction.PerInstance);
        return desc;
    }

    internal static IntPtr MakeShadowVertexDescriptor()
    {
        var desc = MakeVertexDescriptorBase();
        SetAttribute(desc, 0, MTLVertexFormat.Float2, 0, 2);
        var off = 0;
        SetAttribute(desc, 1, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 2, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 3, MTLVertexFormat.Float4, off, 3); off += 16;
        SetAttribute(desc, 4, MTLVertexFormat.Float, off, 3); off += 4;
        SetAttribute(desc, 5, MTLVertexFormat.UInt, off, 3); off += 4;
        SetAttribute(desc, 6, MTLVertexFormat.UInt, off, 3); off += 4;
        SetLayout(desc, 2, 8, MTLVertexStepFunction.PerVertex);
        SetLayout(desc, 3, sizeof(ShadowInstance), MTLVertexStepFunction.PerInstance);
        return desc;
    }

    private static IntPtr MakeVertexDescriptorBase() => New(Class("MTLVertexDescriptor"));

    private static void SetAttribute(IntPtr desc, uint index, MTLVertexFormat format, int offset, int bufferIndex)
    {
        var attribs = msg_IntPtr(desc, Sel("attributes"));
        var attrib = msg_IntPtr_NUInt_NUInt(attribs, Sel("objectAtIndexedSubscript:"), index, 0);
        msg_Void_UInt(attrib, Sel("setFormat:"), (uint)format);
        msg_Void_NUInt_NUInt(attrib, Sel("setOffset:"), (nuint)offset, 0);
        msg_Void_NUInt_NUInt(attrib, Sel("setBufferIndex:"), (nuint)bufferIndex, 0);
    }

    private static void SetLayout(IntPtr desc, uint bufferIndex, int stride, MTLVertexStepFunction step)
    {
        var layouts = msg_IntPtr(desc, Sel("layouts"));
        var layout = msg_IntPtr_NUInt_NUInt(layouts, Sel("objectAtIndexedSubscript:"), bufferIndex, 0);
        msg_Void_NUInt_NUInt(layout, Sel("setStride:"), (nuint)stride, 0);
        msg_Void_UInt(layout, Sel("setStepFunction:"), (uint)step);
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr NewBufferLength(IntPtr receiver, IntPtr selector, nuint length, nuint options);

    private static IntPtr NewSharedBuffer(IntPtr device, int byteLen)
    {
        return NewBufferLength(device, Sel("newBufferWithLength:options:"), (nuint)byteLen, (nuint)MTLResourceOptions.StorageModeShared);
    }

    public void Dispose()
    {
        Release(_clipBuffer);
        Release(_globalsBuffer);
        Release(_shadowInstanceBuffer);
        Release(_imageInstanceBuffer);
        Release(_glyphInstanceBuffer);
        Release(_rectInstanceBuffer);
        if (_pendingCommandBuffer != IntPtr.Zero)
        {
            Release(_pendingCommandBuffer);
            _pendingCommandBuffer = IntPtr.Zero;
        }
    }
}
