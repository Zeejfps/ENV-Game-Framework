// Metal counterpart to GlImageManager: loads PNG images via PngSharp and
// uploads them as RGBA8Unorm MTLTextures with Shared storage. Hands out a
// monotonic uint id so the canvas can batch image draws by texture and look
// up the actual MTLTexture* at draw time.

using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using ZGF.Rendering.Metal;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui;

public sealed unsafe class MetalImageManager : IDisposable
{
    private readonly IntPtr _device;
    private readonly Dictionary<string, uint> _textureIdByImageId = new();
    private readonly Dictionary<string, Size> _sizeByImageId = new();
    private readonly Dictionary<uint, IntPtr> _textureByTextureId = new();
    private uint _nextTextureId = 1;

    public MetalImageManager(IntPtr device)
    {
        _device = device;
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

    private IntPtr UploadRgbaTexture(int width, int height, byte[] rgba)
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
        fixed (byte* ptr = &rgba[0])
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

        // Metal textures are top-down by default — no Y flip (unlike the GL path).
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIndex = (y * width + x) * bpp;
                var dstIndex = (y * width + x) * 4;

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
        _textureIdByImageId.Clear();
        _sizeByImageId.Clear();
    }
}
