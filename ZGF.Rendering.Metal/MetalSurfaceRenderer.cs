using System.Runtime.InteropServices;
using ZGF.Core.MacOs;
using static ZGF.Core.MacOs.Objc;

namespace ZGF.Metal;

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

        var commandBuffer = msg_IntPtr(_surface.CommandQueue, _commandBufferSel);
        var passDescriptor = BuildRenderPassDescriptor(msg_IntPtr(drawable, _textureSel));
        var encoder = msg_IntPtr(commandBuffer, _renderCommandEncoderSel, passDescriptor);

        draw(encoder, commandBuffer);

        msg_Void(encoder, _endEncodingSel);
        msg_Void_IntPtr(commandBuffer, _presentDrawableSel, drawable);
        msg_Void(commandBuffer, _commitSel);

        Release(passDescriptor);
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
