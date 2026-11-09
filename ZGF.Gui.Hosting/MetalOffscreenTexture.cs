using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Hosting;

/// <summary>A private-storage BGRA8 render-target texture in place of a window's drawable, read back
/// through a shared-storage buffer.</summary>
internal sealed unsafe class MetalOffscreenTexture : IDisposable
{
    private const nuint CommandBufferStatusCompleted = 4;

    private readonly IntPtr _device;
    private readonly IntPtr _commandQueue;
    private IntPtr _texture;

    public MetalOffscreenTexture(IntPtr device, IntPtr commandQueue)
    {
        _device = device;
        _commandQueue = commandQueue;
    }

    public int PixelWidth { get; private set; }
    public int PixelHeight { get; private set; }

    /// <summary>Clears the texture, made or resized to <paramref name="pixelWidth"/> by <paramref name="pixelHeight"/>,
    /// to <paramref name="clear"/>, lets <paramref name="encode"/> record one render pass over it, and waits for the GPU.</summary>
    public void Render(int pixelWidth, int pixelHeight, MTLClearColor clear, MetalSurfaceRenderer.FrameCallback encode)
    {
        Allocate(pixelWidth, pixelHeight);
        var commandBuffer = Required(msg_IntPtr(_commandQueue, Sel("commandBuffer")), "command buffer");
        var pass = Required(msg_IntPtr(Class("MTLRenderPassDescriptor"), Sel("renderPassDescriptor")), "render pass descriptor");
        var color = Required(msg_IntPtr_NUInt_NUInt(msg_IntPtr(pass, Sel("colorAttachments")), Sel("objectAtIndexedSubscript:"), 0, 0), "colour attachment");
        msg_Void_IntPtr(color, Sel("setTexture:"), _texture);
        msg_Void_ULong(color, Sel("setLoadAction:"), (ulong)MTLLoadAction.Clear);
        msg_Void_ULong(color, Sel("setStoreAction:"), (ulong)MTLStoreAction.Store);
        msg_Void_MTLClearColor(color, Sel("setClearColor:"), clear);

        var encoder = Required(msg_IntPtr(commandBuffer, Sel("renderCommandEncoderWithDescriptor:"), pass), "render command encoder");
        try
        {
            encode(encoder, commandBuffer);
        }
        finally
        {
            msg_Void(encoder, Sel("endEncoding"));
        }
        Finish(commandBuffer, "drawing offscreen");
    }

    /// <summary>The pixels last drawn: RGBA, rows top-down, premultiplied alpha; null before the first draw.</summary>
    public SurfaceImage? Read()
    {
        if (_texture == IntPtr.Zero)
            return null;
        var width = PixelWidth;
        var height = PixelHeight;
        var byteCount = width * height * 4;
        var rowBytes = (nuint)(width * 4);
        var staging = msg_IntPtr_NUInt_NUInt(_device, Sel("newBufferWithLength:options:"),
            (nuint)byteCount, (nuint)MTLResourceOptions.StorageModeShared);
        if (staging == IntPtr.Zero)
            throw new InvalidOperationException($"Metal returned no {byteCount}-byte readback buffer.");
        try
        {
            var commandBuffer = Required(msg_IntPtr(_commandQueue, Sel("commandBuffer")), "command buffer");
            var blit = Required(msg_IntPtr(commandBuffer, Sel("blitCommandEncoder")), "blit command encoder");
            msg_BlitCopyTextureToBuffer(blit,
                Sel("copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toBuffer:destinationOffset:destinationBytesPerRow:destinationBytesPerImage:"),
                _texture, 0, 0, default, new MTLSize { Width = (nuint)width, Height = (nuint)height, Depth = 1 },
                staging, 0, rowBytes, rowBytes * (nuint)height);
            msg_Void(blit, Sel("endEncoding"));
            Finish(commandBuffer, "reading an offscreen texture");

            var bgra = new ReadOnlySpan<byte>((void*)Required(msg_IntPtr(staging, Sel("contents")), "readback buffer's contents"), byteCount);
            var rgba = new byte[byteCount];
            for (var i = 0; i < rgba.Length; i += 4)
            {
                rgba[i] = bgra[i + 2];
                rgba[i + 1] = bgra[i + 1];
                rgba[i + 2] = bgra[i];
                rgba[i + 3] = bgra[i + 3];
            }
            return new SurfaceImage(width, height, rgba);
        }
        finally
        {
            Objc.Release(staging);
        }
    }

    private void Allocate(int pixelWidth, int pixelHeight)
    {
        if (_texture != IntPtr.Zero && pixelWidth == PixelWidth && pixelHeight == PixelHeight)
            return;
        var descriptor = Required(msg_IntPtr_NUInt_NUInt_NUInt_Bool(Class("MTLTextureDescriptor"),
            Sel("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
            (nuint)MTLPixelFormat.BGRA8Unorm, (nuint)pixelWidth, (nuint)pixelHeight, false), "texture descriptor");
        msg_Void_ULong(descriptor, Sel("setStorageMode:"), (ulong)MTLStorageMode.Private);
        msg_Void_ULong(descriptor, Sel("setUsage:"), (ulong)MTLTextureUsage.RenderTarget);
        var texture = Required(msg_IntPtr(_device, Sel("newTextureWithDescriptor:"), descriptor),
            $"{pixelWidth}x{pixelHeight} BGRA8 render target");
        Objc.Release(_texture);
        _texture = texture;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    private static IntPtr Required(IntPtr value, string what) =>
        value != IntPtr.Zero ? value : throw new InvalidOperationException($"Metal returned no {what}.");

    private static void Finish(IntPtr commandBuffer, string doing)
    {
        msg_Void(commandBuffer, Sel("commit"));
        msg_Void(commandBuffer, Sel("waitUntilCompleted"));
        var status = msg_NUInt(commandBuffer, Sel("status"));
        if (status != CommandBufferStatusCompleted)
            throw new InvalidOperationException($"Metal failed {doing}: command buffer status {status}.");
    }

    public void Dispose()
    {
        Objc.Release(_texture);
        _texture = IntPtr.Zero;
    }
}
