// Metal counterpart to GlImageManager: loads PNG images via PngSharp and
// uploads them as RGBA8Unorm MTLTextures with Shared storage. Hands out a
// monotonic uint id so the canvas can batch image draws by texture and look
// up the actual MTLTexture* at draw time.

using System.Numerics;
using System.Runtime.InteropServices;
using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Metal;

public sealed unsafe class MetalImageManager : IDisposable
{
    private readonly IntPtr _device;
    private readonly IntPtr _commandQueue;
    private readonly Dictionary<string, uint> _textureIdByImageId = new();
    private readonly Dictionary<string, SharedImage> _sharedByImageId = new();
    private readonly Dictionary<string, Size> _sizeByImageId = new();
    private readonly Dictionary<uint, IntPtr> _textureByTextureId = new();
    private uint _nextTextureId = 1;

    /// <param name="commandQueue">The queue the canvases draw on, so a shared texture's copy is ordered
    /// after the frames that still sample its previous contents.</param>
    public MetalImageManager(IntPtr device, IntPtr commandQueue)
    {
        _device = device;
        _commandQueue = commandQueue;
    }

    public void LoadImageFromFile(string pathToImageFile)
    {
        if (_textureIdByImageId.ContainsKey(pathToImageFile))
            return;

        var png = Png.DecodeFromFile(pathToImageFile);
        var width = (int)png.Ihdr.Width;
        var height = (int)png.Ihdr.Height;
        var rgba = DecodeToRgba(png);

        var texture = UploadRgbaTexture(width, height, rgba);

        var textureId = _nextTextureId++;
        _textureIdByImageId.Add(pathToImageFile, textureId);
        _sizeByImageId.Add(pathToImageFile, new Size { Width = width, Height = height });
        _textureByTextureId.Add(textureId, texture);
    }

    public uint GetTextureId(string imageId) => _textureIdByImageId[imageId];
    public Size GetImageSize(string imageId) => _sizeByImageId[imageId];
    public int GetImageWidth(string imageId) => (int)_sizeByImageId[imageId].Width;
    public int GetImageHeight(string imageId) => (int)_sizeByImageId[imageId].Height;

    internal IntPtr GetMetalTexture(uint textureId)
        => _textureByTextureId.TryGetValue(textureId, out var t) ? t : IntPtr.Zero;

    public bool HasImage(string imageId) => _textureIdByImageId.ContainsKey(imageId);

    /// <summary>Shared textures are stored top row first, so they are drawn with flipped UVs.</summary>
    public Vector4 GetSrcUv(string imageId) =>
        _sharedByImageId.ContainsKey(imageId) ? new Vector4(0f, 1f, 1f, -1f) : new Vector4(0f, 0f, 1f, 1f);

    public bool SupportsSharedTextures(SharedTextureKind kind) => kind switch
    {
        SharedTextureKind.IOSurface => true,
        SharedTextureKind.D3D11NtHandle => false,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// Copies <paramref name="texture"/> into the image under <paramref name="imageId"/>, creating or
    /// resizing it as needed. Returns once the copy has finished, since the source is only valid for the
    /// call.
    /// </summary>
    public bool CreateOrUpdateSharedTextureImage(string imageId, in SharedTexture texture)
    {
        switch (texture.Kind)
        {
            case SharedTextureKind.IOSurface:
                return CopyIOSurface(imageId, texture.Handle);
            case SharedTextureKind.D3D11NtHandle:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(texture), texture.Kind, null);
        }
    }

    private bool CopyIOSurface(string imageId, IntPtr surface)
    {
        var width = (int)IOSurface.GetWidth(surface);
        var height = (int)IOSurface.GetHeight(surface);
        var format = IOSurface.GetPixelFormat(surface) switch
        {
            IOSurface.FourCcBgra => MTLPixelFormat.BGRA8Unorm,
            IOSurface.FourCcRgba => MTLPixelFormat.RGBA8Unorm,
            _ => MTLPixelFormat.Invalid,
        };
        if (width <= 0 || height <= 0 || format == MTLPixelFormat.Invalid)
        {
            Console.Error.WriteLine($"[MetalImageManager] shared texture for '{imageId}': unsupported IOSurface {width}x{height} format 0x{IOSurface.GetPixelFormat(surface):X8}");
            return false;
        }

        var pool = objc_autoreleasePoolPush();
        try
        {
            var desc = TextureDescriptor2D(Class("MTLTextureDescriptor"),
                Sel("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
                (nuint)format, (nuint)width, (nuint)height, false);
            msg_Void_UInt(desc, Sel("setUsage:"), (uint)MTLTextureUsage.ShaderRead);
            var source = msg_IntPtr(_device, Sel("newTextureWithDescriptor:iosurface:plane:"), desc, surface, IntPtr.Zero);
            if (source == IntPtr.Zero)
            {
                Console.Error.WriteLine($"[MetalImageManager] shared texture for '{imageId}': newTextureWithDescriptor:iosurface:plane: failed");
                return false;
            }

            try
            {
                var target = TargetFor(imageId, width, height, format);
                var commandBuffer = msg_IntPtr(_commandQueue, Sel("commandBuffer"));
                var blit = msg_IntPtr(commandBuffer, Sel("blitCommandEncoder"));
                var origin = default(MTLOrigin);
                var size = new MTLSize { Width = (nuint)width, Height = (nuint)height, Depth = 1 };
                msg_BlitCopyTexture(
                    blit,
                    Sel("copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:"),
                    source, 0, 0, origin, size, target.Texture, 0, 0, origin);
                msg_Void(blit, Sel("endEncoding"));
                msg_Void(commandBuffer, Sel("commit"));
                msg_Void(commandBuffer, Sel("waitUntilCompleted"));
                return true;
            }
            finally
            {
                Release(source);
            }
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    /// <summary>The texture the image under <paramref name="imageId"/> copies shared frames into, kept
    /// while the size and format hold. Writing it again is safe because copies go through the canvases'
    /// own queue, after the frames that sample it.</summary>
    private SharedImage TargetFor(string imageId, int width, int height, MTLPixelFormat format)
    {
        if (_sharedByImageId.TryGetValue(imageId, out var current) && current.Matches(width, height, format))
            return current;

        RemoveImage(imageId);
        var desc = TextureDescriptor2D(Class("MTLTextureDescriptor"),
            Sel("texture2DDescriptorWithPixelFormat:width:height:mipmapped:"),
            (nuint)format, (nuint)width, (nuint)height, false);
        msg_Void_UInt(desc, Sel("setStorageMode:"), (uint)MTLStorageMode.Private);
        msg_Void_UInt(desc, Sel("setUsage:"), (uint)MTLTextureUsage.ShaderRead);
        var texture = msg_IntPtr(_device, Sel("newTextureWithDescriptor:"), desc);
        if (texture == IntPtr.Zero)
            throw new Exception("Metal: newTextureWithDescriptor returned null.");

        var textureId = _nextTextureId++;
        var created = new SharedImage(texture, width, height, format);
        _sharedByImageId[imageId] = created;
        _textureIdByImageId[imageId] = textureId;
        _sizeByImageId[imageId] = new Size { Width = width, Height = height };
        _textureByTextureId[textureId] = texture;
        return created;
    }

    private sealed record SharedImage(IntPtr Texture, int Width, int Height, MTLPixelFormat Format)
    {
        public bool Matches(int width, int height, MTLPixelFormat format) =>
            Width == width && Height == height && Format == format;
    }

    /// <summary>
    /// Creates or replaces a dynamic image from straight-alpha RGBA8, top-down rows
    /// (flipped here to the shader's bottom-up UV convention). Updates always allocate
    /// a fresh texture and remap the id: an in-flight command buffer may still sample
    /// the old texture, and it retains it, so releasing our reference right away is safe
    /// while mutating it in place would not be.
    /// </summary>
    public bool CreateOrUpdateRgbaImage(string imageId, int width, int height, ReadOnlySpan<byte> rgbaTopDown)
    {
        _sharedByImageId.Remove(imageId);
        var flipped = FlipRows(rgbaTopDown, width, height);
        var texture = UploadRgbaTexture(width, height, flipped);

        if (_textureIdByImageId.TryGetValue(imageId, out var oldTextureId))
        {
            if (_textureByTextureId.Remove(oldTextureId, out var oldTexture))
                Release(oldTexture);
        }

        var textureId = _nextTextureId++;
        _textureIdByImageId[imageId] = textureId;
        _sizeByImageId[imageId] = new Size { Width = width, Height = height };
        _textureByTextureId[textureId] = texture;
        return true;
    }

    public void RemoveImage(string imageId)
    {
        _sharedByImageId.Remove(imageId);
        if (!_textureIdByImageId.Remove(imageId, out var textureId))
            return;
        _sizeByImageId.Remove(imageId);
        if (_textureByTextureId.Remove(textureId, out var texture))
            Release(texture);
    }

    private byte[] _flipScratch = [];

    private ReadOnlySpan<byte> FlipRows(ReadOnlySpan<byte> topDown, int width, int height)
    {
        var rowBytes = width * 4;
        var needed = rowBytes * height;
        if (_flipScratch.Length < needed)
            _flipScratch = new byte[needed];
        for (var y = 0; y < height; y++)
            topDown.Slice(y * rowBytes, rowBytes).CopyTo(_flipScratch.AsSpan((height - 1 - y) * rowBytes, rowBytes));
        return _flipScratch.AsSpan(0, needed);
    }

    private IntPtr UploadRgbaTexture(int width, int height, ReadOnlySpan<byte> rgba)
    {
        // 1) Build an MTLTextureDescriptor (RGBA8Unorm, 2D, ShaderRead, Shared storage).
        var descClass = Class("MTLTextureDescriptor");
        var descSel = Sel("texture2DDescriptorWithPixelFormat:width:height:mipmapped:");

        // Slot fewer args by calling the convenience factory; mipmapped:NO.
        // texture2DDescriptorWithPixelFormat:width:height:mipmapped: takes
        // (NSUInteger pixelFormat, NSUInteger width, NSUInteger height, BOOL mipmapped).
        var desc = TextureDescriptor2D(descClass, descSel, (nuint)MTLPixelFormat.RGBA8Unorm,
            (nuint)width, (nuint)height, false);

        // Shared storage so we can replaceRegion synchronously.
        msg_Void_UInt(desc, Sel("setStorageMode:"), (uint)MTLStorageMode.Shared);
        msg_Void_UInt(desc, Sel("setUsage:"), (uint)MTLTextureUsage.ShaderRead);

        var texture = msg_IntPtr(_device, Sel("newTextureWithDescriptor:"), desc);
        if (texture == IntPtr.Zero)
            throw new Exception("Metal: newTextureWithDescriptor returned null.");

        var region = new MTLRegion
        {
            Origin = new MTLOrigin(),
            Size = new MTLSize { Width = (nuint)width, Height = (nuint)height, Depth = 1 },
        };
        fixed (byte* ptr = rgba)
        {
            msg_Void_MTLRegion_NUInt_IntPtr_NUInt(
                texture, Sel("replaceRegion:mipmapLevel:withBytes:bytesPerRow:"),
                region, 0, (IntPtr)ptr, (nuint)(width * 4));
        }

        return texture;
    }

    // Wrapper because objc_msgSend's signature for this selector requires
    // a 4-arg form we don't have in Objc.cs by default.
    [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr TextureDescriptor2D(
        IntPtr receiver, IntPtr selector,
        nuint pixelFormat, nuint width, nuint height,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)] bool mipmapped);

    private static byte[] DecodeToRgba(IRawPng png)
    {
        var width = (int)png.Ihdr.Width;
        var height = (int)png.Ihdr.Height;
        var src = png.PixelData;
        var bpp = png.Ihdr.GetBytesPerPixel();
        var colorType = png.Ihdr.ColorType;
        var output = new byte[width * height * 4];

        // Flip Y on upload so texture row 0 holds the image's bottom row, matching the
        // bottom-up UV convention the shared canvas_image shader samples with (same as the GL path).
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIndex = (y * width + x) * bpp;
                var dstIndex = ((height - 1 - y) * width + x) * 4;

                byte r = 0, g = 0, b = 0, a = 255;
                switch (colorType)
                {
                    case ColorType.TrueColorWithAlpha:
                        r = src[srcIndex];
                        g = src[srcIndex + 1];
                        b = src[srcIndex + 2];
                        a = src[srcIndex + 3];
                        break;
                    case ColorType.TrueColor:
                        r = src[srcIndex];
                        g = src[srcIndex + 1];
                        b = src[srcIndex + 2];
                        break;
                    case ColorType.GrayscaleWithAlpha:
                        r = g = b = src[srcIndex];
                        a = src[srcIndex + 1];
                        break;
                    case ColorType.Grayscale:
                        r = g = b = src[srcIndex];
                        break;
                    default:
                        throw new NotSupportedException($"PNG ColorType '{colorType}' is not supported.");
                }

                output[dstIndex] = r;
                output[dstIndex + 1] = g;
                output[dstIndex + 2] = b;
                output[dstIndex + 3] = a;
            }
        }

        return output;
    }

    public void Dispose()
    {
        foreach (var (_, texture) in _textureByTextureId)
            Release(texture);
        _textureByTextureId.Clear();
        _sharedByImageId.Clear();
        _textureIdByImageId.Clear();
        _sizeByImageId.Clear();
    }
}

/// <summary>The few <c>IOSurface</c> getters the shared-texture import reads.</summary>
internal static class IOSurface
{
    private const string Lib = "/System/Library/Frameworks/IOSurface.framework/IOSurface";

    public const uint FourCcBgra = 0x42475241; // 'BGRA'
    public const uint FourCcRgba = 0x52474241; // 'RGBA'

    [DllImport(Lib, EntryPoint = "IOSurfaceGetWidth")]
    public static extern nuint GetWidth(IntPtr surface);

    [DllImport(Lib, EntryPoint = "IOSurfaceGetHeight")]
    public static extern nuint GetHeight(IntPtr surface);

    [DllImport(Lib, EntryPoint = "IOSurfaceGetPixelFormat")]
    public static extern uint GetPixelFormat(IntPtr surface);
}
