using System.Runtime.InteropServices;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Rendering.Metal;

/// <summary>
///     Drives the host-agnostic per-frame Metal loop against an <see cref="IMetalSurface"/>:
///     acquire the next drawable, build a render-pass encoder, hand it to the caller to
///     record draw commands, then present and commit. Identical on macOS and iOS — only
///     the <see cref="IMetalSurface"/> implementation (which window/layer it wraps) differs.
/// </summary>
public sealed class MetalSurfaceRenderer
{
    /// <summary>
    ///     Records draw commands for one frame into the given render command encoder and
    ///     command buffer (both Objective-C object pointers). Called between drawable
    ///     acquisition and presentation.
    /// </summary>
    public delegate void FrameCallback(IntPtr renderCommandEncoder, IntPtr commandBuffer);

    private readonly IMetalSurface _surface;
    private readonly MTLClearColor _clearColor;

    private readonly IntPtr _nextDrawableSel = Sel("nextDrawable");
    private readonly IntPtr _commandBufferSel = Sel("commandBuffer");
    private readonly IntPtr _renderCommandEncoderSel = Sel("renderCommandEncoderWithDescriptor:");
    private readonly IntPtr _endEncodingSel = Sel("endEncoding");
    private readonly IntPtr _presentDrawableSel = Sel("presentDrawable:");
    private readonly IntPtr _commitSel = Sel("commit");
    private readonly IntPtr _textureSel = Sel("texture");

    private bool _capturePending;
    private (int W, int H, byte[] Rgba)? _captureResult;

    /// <summary>Ask the next rendered frame to be copied back to CPU memory (for a screenshot).</summary>
    public void RequestCapture() => _capturePending = true;

    /// <summary>Takes the pixels captured by the last <see cref="RequestCapture"/>, if any. RGBA,
    /// top-row first.</summary>
    public bool TryTakeCapture(out int width, out int height, out byte[] rgba)
    {
        if (_captureResult is { } r)
        {
            width = r.W;
            height = r.H;
            rgba = r.Rgba;
            _captureResult = null;
            return true;
        }
        width = 0;
        height = 0;
        rgba = Array.Empty<byte>();
        return false;
    }

    public MetalSurfaceRenderer(IMetalSurface surface)
        : this(surface, new MTLClearColor(0, 0, 0, 1))
    {
    }

    public MetalSurfaceRenderer(IMetalSurface surface, MTLClearColor clearColor)
    {
        _surface = surface;
        _clearColor = clearColor;
    }

    /// <summary>
    ///     Renders one frame: acquires the surface's next drawable, invokes <paramref name="draw"/>
    ///     to record commands, then presents and commits. A no-op if no drawable is available.
    /// </summary>
    public void RenderFrame(FrameCallback draw)
    {
        var drawable = msg_IntPtr(_surface.Layer, _nextDrawableSel);
        if (drawable == IntPtr.Zero) return;

        var drawableTexture = msg_IntPtr(drawable, _textureSel);
        var commandBuffer = msg_IntPtr(_surface.CommandQueue, _commandBufferSel);
        var passDescriptor = BuildRenderPassDescriptor(drawableTexture);
        var encoder = msg_IntPtr(commandBuffer, _renderCommandEncoderSel, passDescriptor);

        draw(encoder, commandBuffer);

        msg_Void(encoder, _endEncodingSel);

        // Capture: blit the just-rendered drawable into a CPU-readable staging texture on the same
        // command buffer (before present), then read it back once the GPU has finished.
        var capture = _capturePending;
        _capturePending = false;
        var staging = IntPtr.Zero;
        var capW = 0;
        var capH = 0;
        if (capture)
            staging = BlitToStaging(commandBuffer, drawableTexture, out capW, out capH);

        msg_Void_IntPtr(commandBuffer, _presentDrawableSel, drawable);
        msg_Void(commandBuffer, _commitSel);

        if (capture)
        {
            msg_Void(commandBuffer, Sel("waitUntilCompleted"));
            _captureResult = (capW, capH, ReadStaging(staging, capW, capH));
            Release(staging);
        }

        Release(passDescriptor);
    }

    private IntPtr BlitToStaging(IntPtr commandBuffer, IntPtr srcTexture, out int width, out int height)
    {
        width = (int)msg_NUInt(srcTexture, Sel("width"));
        height = (int)msg_NUInt(srcTexture, Sel("height"));

        var desc = msg_IntPtr_NUInt_NUInt_NUInt_Bool(
            Class("MTLTextureDescriptor"),
            Sel("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
            (nuint)MTLPixelFormat.BGRA8Unorm, (nuint)width, (nuint)height, false);
        msg_Void_UInt(desc, Sel("setStorageMode:"), (uint)MTLStorageMode.Shared);
        msg_Void_UInt(desc, Sel("setUsage:"), (uint)MTLTextureUsage.ShaderRead);

        var staging = msg_IntPtr(_surface.Device, Sel("newTextureWithDescriptor:"), desc);

        var blit = msg_IntPtr(commandBuffer, Sel("blitCommandEncoder"));
        var origin = default(MTLOrigin);
        var size = new MTLSize { Width = (nuint)width, Height = (nuint)height, Depth = 1 };
        msg_BlitCopyTexture(
            blit,
            Sel("copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:"),
            srcTexture, 0, 0, origin, size, staging, 0, 0, origin);
        msg_Void(blit, Sel("endEncoding"));
        return staging;
    }

    private static unsafe byte[] ReadStaging(IntPtr staging, int width, int height)
    {
        var bytes = new byte[width * height * 4];
        var region = new MTLRegion
        {
            Origin = default,
            Size = new MTLSize { Width = (nuint)width, Height = (nuint)height, Depth = 1 },
        };
        fixed (byte* p = bytes)
            msg_GetBytes(staging, Sel("getBytes:bytesPerRow:fromRegion:mipmapLevel:"), (IntPtr)p, (nuint)(width * 4), region, 0);

        // The drawable is BGRA8Unorm; swap to RGBA for PNG.
        for (var i = 0; i + 2 < bytes.Length; i += 4)
            (bytes[i], bytes[i + 2]) = (bytes[i + 2], bytes[i]);
        return bytes;
    }

    private IntPtr BuildRenderPassDescriptor(IntPtr drawableTexture)
    {
        var descClass = Class("MTLRenderPassDescriptor");
        var desc = msg_IntPtr(descClass, Sel("renderPassDescriptor"));
        Retain(desc);

        var colorAttachments = msg_IntPtr(desc, Sel("colorAttachments"));
        var color0 = msg_IntPtr_NUInt_NUInt(colorAttachments, Sel("objectAtIndexedSubscript:"), 0, 0);
        msg_Void_IntPtr(color0, Sel("setTexture:"), drawableTexture);
        msg_Void_UInt(color0, Sel("setLoadAction:"), 2);  // MTLLoadActionClear
        msg_Void_UInt(color0, Sel("setStoreAction:"), 1);  // MTLStoreActionStore
        SetClearColor(color0, Sel("setClearColor:"), _clearColor);
        return desc;
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void SetClearColor(IntPtr receiver, IntPtr selector, MTLClearColor color);
}
