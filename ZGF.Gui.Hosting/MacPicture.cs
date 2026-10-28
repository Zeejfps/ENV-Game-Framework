using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Gui.Hosting;

/// <summary>Decodes the encoded picture macOS keeps on the pasteboard, a PNG or TIFF as a screenshot
/// puts there, through ImageIO, and draws it into straight-alpha RGBA through CoreGraphics.</summary>
[SupportedOSPlatform("macos")]
internal static class MacPicture
{
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string ImageIO = "/System/Library/Frameworks/ImageIO.framework/ImageIO";

    // kCGImageAlphaPremultipliedLast | kCGBitmapByteOrder32Big: bytes in R, G, B, A order. A bitmap
    // context cannot hold straight alpha, so the pixels are straightened after drawing.
    private const uint RgbaPremultiplied = 1 | (4 << 12);

    /// <summary>The picture <paramref name="encoded"/> holds, or null where ImageIO cannot read it.</summary>
    public static ClipboardImage? Decode(ReadOnlySpan<byte> encoded)
    {
        var data = CreateData(encoded);
        if (data == IntPtr.Zero)
            return null;
        try
        {
            return Decode(data);
        }
        finally
        {
            CFRelease(data);
        }
    }

    /// <summary>The picture the CFData (or NSData, the same object) <paramref name="data"/> holds, or
    /// null where ImageIO cannot read it.</summary>
    public static ClipboardImage? Decode(IntPtr data)
    {
        var source = CGImageSourceCreateWithData(data, IntPtr.Zero);
        if (source == IntPtr.Zero)
            return null;
        try
        {
            var image = CGImageSourceCreateImageAtIndex(source, 0, IntPtr.Zero);
            if (image == IntPtr.Zero)
                return null;
            try
            {
                return Draw(image);
            }
            finally
            {
                CFRelease(image);
            }
        }
        finally
        {
            CFRelease(source);
        }
    }

    private static unsafe ClipboardImage? Draw(IntPtr image)
    {
        var width = CGImageGetWidth(image);
        var height = CGImageGetHeight(image);
        if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
            return null;
        if ((ulong)width * height * 4 > (ulong)Array.MaxLength)
            return null;

        var rgba = new byte[(int)(width * height * 4)];
        var colorSpace = CGColorSpaceCreateWithName(SrgbName());
        try
        {
            fixed (byte* pixels = rgba)
            {
                var context = CGBitmapContextCreate((IntPtr)pixels, width, height, 8, width * 4, colorSpace, RgbaPremultiplied);
                if (context == IntPtr.Zero)
                    return null;
                try
                {
                    CGContextDrawImage(context, new CGRect(0, 0, width, height), image);
                }
                finally
                {
                    CFRelease(context);
                }
            }
        }
        finally
        {
            CFRelease(colorSpace);
        }
        Straighten(rgba);
        return new ClipboardImage((int)width, (int)height, rgba);
    }

    /// <summary>Divides each pixel's color by its alpha, undoing the premultiplying a bitmap context
    /// draws with.</summary>
    internal static void Straighten(Span<byte> rgba)
    {
        for (var i = 0; i < rgba.Length; i += 4)
        {
            var alpha = rgba[i + 3];
            if (alpha is 0 or byte.MaxValue)
                continue;
            for (var c = i; c < i + 3; c++)
                rgba[c] = (byte)Math.Min(byte.MaxValue, (rgba[c] * 255 + alpha / 2) / alpha);
        }
    }

    private static unsafe IntPtr CreateData(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* pointer = bytes)
            return CFDataCreate(IntPtr.Zero, (IntPtr)pointer, bytes.Length);
    }

    // kCGColorSpaceSRGB is a CFStringRef constant, exported as a variable rather than a function.
    private static IntPtr SrgbName() =>
        Marshal.ReadIntPtr(NativeLibrary.GetExport(NativeLibrary.Load(CoreGraphics), "kCGColorSpaceSRGB"));

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CGRect(double x, double y, double width, double height)
    {
        public readonly double X = x;
        public readonly double Y = y;
        public readonly double Width = width;
        public readonly double Height = height;
    }

    [DllImport(CoreFoundation)]
    private static extern IntPtr CFDataCreate(IntPtr allocator, IntPtr bytes, nint length);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr value);

    [DllImport(ImageIO)]
    private static extern IntPtr CGImageSourceCreateWithData(IntPtr data, IntPtr options);

    [DllImport(ImageIO)]
    private static extern IntPtr CGImageSourceCreateImageAtIndex(IntPtr source, nuint index, IntPtr options);

    [DllImport(CoreGraphics)]
    private static extern nuint CGImageGetWidth(IntPtr image);

    [DllImport(CoreGraphics)]
    private static extern nuint CGImageGetHeight(IntPtr image);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGColorSpaceCreateWithName(IntPtr name);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGBitmapContextCreate(
        IntPtr data, nuint width, nuint height, nuint bitsPerComponent, nuint bytesPerRow, IntPtr colorSpace, uint bitmapInfo);

    [DllImport(CoreGraphics)]
    private static extern void CGContextDrawImage(IntPtr context, CGRect rect, IntPtr image);
}
