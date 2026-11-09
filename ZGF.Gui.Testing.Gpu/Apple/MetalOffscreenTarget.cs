using System.Runtime.Versioning;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Testing.Gpu.Apple;

/// <summary>A private-storage BGRA8 render-target texture the size of <see cref="Size"/> in physical pixels,
/// read back through a shared-storage buffer. Its members marshal to the device's render thread, and the
/// device disposes it there if it is still open when the device shuts down.</summary>
[SupportedOSPlatform("macos")]
public sealed unsafe class MetalOffscreenTarget : IDisposable
{
    // The smallest maximum 2D texture dimension of the Apple7+ GPUs MetalContext accepts.
    private const int MaxDimension = 16384;
    private const nuint CommandBufferStatusCompleted = 4;

    private readonly GpuDevice _device;
    private readonly MetalContext _metal;
    private IntPtr _texture;
    private bool _released;

    private MetalOffscreenTarget(GpuDevice device, MetalContext metal, TargetSize size, IntPtr texture)
    {
        _device = device;
        _metal = metal;
        Size = size;
        _texture = texture;
    }

    public TargetSize Size { get; private set; }

    /// <exception cref="ArgumentOutOfRangeException">The size exceeds what the GPU can render to.</exception>
    /// <exception cref="InvalidOperationException">The device is not a Metal device.</exception>
    public static MetalOffscreenTarget Create(GpuDevice device, TargetSize size)
    {
        var metal = device.Metal;
        return device.Run(() =>
        {
            ThrowIfBeyondLimits(size);
            var target = new MetalOffscreenTarget(device, metal, size, NewTexture(metal, size));
            device.Own(target);
            return target;
        });
    }

    /// <summary>Changes the size; the contents are undefined until the next draw.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size exceeds what the GPU can render to.</exception>
    public void Resize(TargetSize size) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        ThrowIfBeyondLimits(size);
        if (size.PixelWidth != Size.PixelWidth || size.PixelHeight != Size.PixelHeight)
        {
            var texture = NewTexture(_metal, size);
            Objc.Release(_texture);
            _texture = texture;
        }
        Size = size;
    });

    /// <summary>Clears the target to <paramref name="clearArgb"/> (straight alpha, stored premultiplied),
    /// lets <paramref name="encode"/> record into one render pass over it, and waits for the GPU to finish.</summary>
    public void Render(uint clearArgb, MetalSurfaceRenderer.FrameCallback encode) => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        var commandBuffer = Required(msg_IntPtr(_metal.CommandQueue, Sel("commandBuffer")), "command buffer");
        var pass = Required(msg_IntPtr(Class("MTLRenderPassDescriptor"), Sel("renderPassDescriptor")), "render pass descriptor");
        var color = Required(msg_IntPtr(msg_IntPtr(pass, Sel("colorAttachments")), Sel("objectAtIndexedSubscript:"), 0), "colour attachment");
        msg_Void_IntPtr(color, Sel("setTexture:"), _texture);
        msg_Void_ULong(color, Sel("setLoadAction:"), (ulong)MTLLoadAction.Clear);
        msg_Void_ULong(color, Sel("setStoreAction:"), (ulong)MTLStoreAction.Store);
        var a = (clearArgb >> 24) / 255.0;
        msg_Void_MTLClearColor(color, Sel("setClearColor:"), new MTLClearColor(
            (byte)(clearArgb >> 16) / 255.0 * a, (byte)(clearArgb >> 8) / 255.0 * a, (byte)clearArgb / 255.0 * a, a));

        var encoder = Required(msg_IntPtr(commandBuffer, Sel("renderCommandEncoderWithDescriptor:"), pass), "render command encoder");
        try
        {
            encode(encoder, commandBuffer);
        }
        finally
        {
            msg_Void(encoder, Sel("endEncoding"));
        }
        Finish(commandBuffer, "drawing into the offscreen target");
    });

    /// <summary>The target's pixels: RGBA, rows top-down, premultiplied alpha.</summary>
    public RgbaImage Read() => _device.Run(() =>
    {
        ObjectDisposedException.ThrowIf(_released, this);
        var width = Size.PixelWidth;
        var height = Size.PixelHeight;
        var rowBytes = (nuint)(width * 4);
        var staging = msg_IntPtr_NUInt_NUInt(_metal.Device, Sel("newBufferWithLength:options:"),
            (nuint)Size.ByteCount, (nuint)MTLResourceOptions.StorageModeShared);
        if (staging == IntPtr.Zero)
            throw new InvalidOperationException($"Metal returned no {Size.ByteCount}-byte readback buffer.");
        try
        {
            var commandBuffer = Required(msg_IntPtr(_metal.CommandQueue, Sel("commandBuffer")), "command buffer");
            var blit = Required(msg_IntPtr(commandBuffer, Sel("blitCommandEncoder")), "blit command encoder");
            msg_BlitCopyTextureToBuffer(blit,
                Sel("copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toBuffer:destinationOffset:destinationBytesPerRow:destinationBytesPerImage:"),
                _texture, 0, 0, default, new MTLSize { Width = (nuint)width, Height = (nuint)height, Depth = 1 },
                staging, 0, rowBytes, rowBytes * (nuint)height);
            msg_Void(blit, Sel("endEncoding"));
            Finish(commandBuffer, "reading the offscreen target");

            var bgra = new ReadOnlySpan<byte>((void*)Required(msg_IntPtr(staging, Sel("contents")), "readback buffer's contents"), Size.ByteCount);
            var rgba = new byte[Size.ByteCount];
            for (var i = 0; i < rgba.Length; i += 4)
            {
                rgba[i] = bgra[i + 2];
                rgba[i + 1] = bgra[i + 1];
                rgba[i + 2] = bgra[i];
                rgba[i + 3] = bgra[i + 3];
            }
            return new RgbaImage(width, height, rgba);
        }
        finally
        {
            Objc.Release(staging);
        }
    });

    private static void ThrowIfBeyondLimits(TargetSize size)
    {
        if (size.PixelWidth > MaxDimension || size.PixelHeight > MaxDimension)
            throw new ArgumentOutOfRangeException(nameof(size),
                $"{size.PixelWidth}x{size.PixelHeight} pixels exceeds the GPU's {MaxDimension}x{MaxDimension} texture limit.");
    }

    private static IntPtr NewTexture(MetalContext metal, TargetSize size)
    {
        var descriptor = Required(msg_IntPtr_NUInt_NUInt_NUInt_Bool(Class("MTLTextureDescriptor"),
            Sel("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
            (nuint)MTLPixelFormat.BGRA8Unorm, (nuint)size.PixelWidth, (nuint)size.PixelHeight, false), "texture descriptor");
        msg_Void_ULong(descriptor, Sel("setStorageMode:"), (ulong)MTLStorageMode.Private);
        msg_Void_ULong(descriptor, Sel("setUsage:"), (ulong)MTLTextureUsage.RenderTarget);
        return Required(msg_IntPtr(metal.Device, Sel("newTextureWithDescriptor:"), descriptor),
            $"{size.PixelWidth}x{size.PixelHeight} BGRA8 render target");
    }

    private static IntPtr Required(IntPtr value, string what) =>
        value != IntPtr.Zero ? value : throw new InvalidOperationException($"Metal returned no {what}.");

    private static void Finish(IntPtr commandBuffer, string doing)
    {
        msg_Void(commandBuffer, Sel("commit"));
        msg_Void(commandBuffer, Sel("waitUntilCompleted"));
        var status = msg_NUInt(commandBuffer, Sel("status"));
        if (status != CommandBufferStatusCompleted)
        {
            var error = msg_IntPtr(commandBuffer, Sel("error"));
            var reason = error == IntPtr.Zero ? "no error reported" : MetalContext.NSString(msg_IntPtr(error, Sel("localizedDescription")));
            throw new InvalidOperationException($"Metal failed {doing}: command buffer status {status}, {reason}.");
        }
    }

    /// <summary>Releases the texture on the render thread. Once the device has stopped accepting work, its
    /// shutdown releases the target instead.</summary>
    public void Dispose() => _device.TryRun(Release);

    private void Release()
    {
        if (_released) return;
        _released = true;
        _device.Disown(this);
        Objc.Release(_texture);
        _texture = IntPtr.Zero;
    }
}
