using JpegSharp.Api;
using JpegSharp.Color;

namespace JpegSharp.Encoder;

// Builds the per-component sample planes for each supported color space: RGB->YCbCr (with
// subsampling), CMYK (Adobe-inverted), YCCK, and direct RGB.
internal sealed partial class BaselineEncoder
{
    private static Component[] BuildColorComponents(JpegImage image, int h, int v)
    {
        var pixelCount = image.Width * image.Height;
        var y = new byte[pixelCount];
        var cbFull = new byte[pixelCount];
        var crFull = new byte[pixelCount];
        ColorConverter.RgbToYCbCr(image.PixelData, y, cbFull, crFull);

        var chromaWidth = ChromaSampler.SubsampledSize(image.Width, h);
        var chromaHeight = ChromaSampler.SubsampledSize(image.Height, v);
        var cb = new byte[chromaWidth * chromaHeight];
        var cr = new byte[chromaWidth * chromaHeight];
        ChromaSampler.Downsample(cbFull, image.Width, image.Height, h, v, cb, chromaWidth, chromaHeight);
        ChromaSampler.Downsample(crFull, image.Width, image.Height, h, v, cr, chromaWidth, chromaHeight);

        return
        [
            new Component { Id = 1, H = h, V = v, QuantId = 0, TableClass = 0, Plane = y, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 2, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cb, PlaneWidth = chromaWidth, PlaneHeight = chromaHeight },
            new Component { Id = 3, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cr, PlaneWidth = chromaWidth, PlaneHeight = chromaHeight },
        ];
    }

    private static Component[] BuildCmykComponents(JpegImage image)
    {
        // Adobe CMYK is stored inverted (255 - value); all four components are full resolution.
        var pixelCount = image.Width * image.Height;
        var components = new Component[4];
        for (var ch = 0; ch < 4; ch++)
        {
            var plane = new byte[pixelCount];
            for (var i = 0; i < pixelCount; i++)
                plane[i] = (byte)(255 - image.PixelData[i * 4 + ch]);
            components[ch] = new Component
            {
                Id = ch + 1, H = 1, V = 1, QuantId = 0, TableClass = 0,
                Plane = plane, PlaneWidth = image.Width, PlaneHeight = image.Height,
            };
        }

        return components;
    }

    private static Component[] BuildRgbDirectComponents(JpegImage image)
    {
        // Store R, G, B directly (no color transform, no subsampling). Component ids 'R','G','B'
        // let decoders without an Adobe marker still recognize the layout.
        var pixelCount = image.Width * image.Height;
        var r = new byte[pixelCount];
        var g = new byte[pixelCount];
        var b = new byte[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            r[i] = image.PixelData[i * 3];
            g[i] = image.PixelData[i * 3 + 1];
            b[i] = image.PixelData[i * 3 + 2];
        }

        return
        [
            new Component { Id = (byte)'R', H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = r, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = (byte)'G', H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = g, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = (byte)'B', H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = b, PlaneWidth = image.Width, PlaneHeight = image.Height },
        ];
    }

    // ----- High-precision (9–16 bit) component builders (full resolution, no subsampling) -----

    private static ushort[] ClonePlane(ushort[] samples) => (ushort[])samples.Clone();

    private Component HighPrecisionComponent(int id, int quantId, int tableClass, ushort[] plane16) =>
        new()
        {
            Id = id, H = 1, V = 1, QuantId = quantId, TableClass = tableClass,
            Plane = [], Plane16 = plane16, PlaneWidth = _width, PlaneHeight = _height,
        };

    private Component[] BuildRgbDirectComponents16(JpegImage16 image)
    {
        // Store R, G, B directly (no color transform). Component ids 'R','G','B' let decoders
        // without an Adobe marker still recognize the layout.
        var pixelCount = image.Width * image.Height;
        var r = new ushort[pixelCount];
        var g = new ushort[pixelCount];
        var b = new ushort[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            r[i] = image.PixelData[i * 3];
            g[i] = image.PixelData[i * 3 + 1];
            b[i] = image.PixelData[i * 3 + 2];
        }

        return
        [
            HighPrecisionComponent('R', 0, 0, r),
            HighPrecisionComponent('G', 0, 0, g),
            HighPrecisionComponent('B', 0, 0, b),
        ];
    }

    private Component[] BuildYCbCrComponents16(JpegImage16 image, int h, int v)
    {
        var pixelCount = image.Width * image.Height;
        var maxValue = image.MaxSampleValue;
        var y = new ushort[pixelCount];
        var cbFull = new ushort[pixelCount];
        var crFull = new ushort[pixelCount];
        for (var i = 0; i < pixelCount; i++)
            ColorConverter.RgbToYCbCr(image.PixelData[i * 3], image.PixelData[i * 3 + 1], image.PixelData[i * 3 + 2], maxValue,
                out y[i], out cbFull[i], out crFull[i]);

        var chromaWidth = ChromaSampler.SubsampledSize(image.Width, h);
        var chromaHeight = ChromaSampler.SubsampledSize(image.Height, v);
        var cb = new ushort[chromaWidth * chromaHeight];
        var cr = new ushort[chromaWidth * chromaHeight];
        ChromaSampler.Downsample(cbFull, image.Width, image.Height, h, v, cb, chromaWidth, chromaHeight);
        ChromaSampler.Downsample(crFull, image.Width, image.Height, h, v, cr, chromaWidth, chromaHeight);

        return
        [
            new Component { Id = 1, H = h, V = v, QuantId = 0, TableClass = 0, Plane = [], Plane16 = y, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 2, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = [], Plane16 = cb, PlaneWidth = chromaWidth, PlaneHeight = chromaHeight },
            new Component { Id = 3, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = [], Plane16 = cr, PlaneWidth = chromaWidth, PlaneHeight = chromaHeight },
        ];
    }

    private Component[] BuildCmykComponents16(JpegImage16 image)
    {
        // Adobe CMYK is stored inverted (max - value); all four components are full resolution.
        var pixelCount = image.Width * image.Height;
        var max = image.MaxSampleValue;
        var components = new Component[4];
        for (var ch = 0; ch < 4; ch++)
        {
            var plane = new ushort[pixelCount];
            for (var i = 0; i < pixelCount; i++)
                plane[i] = (ushort)(max - image.PixelData[i * 4 + ch]);
            components[ch] = HighPrecisionComponent(ch + 1, 0, 0, plane);
        }

        return components;
    }

    private Component[] BuildYcckComponents16(JpegImage16 image)
    {
        // Adobe YCCK: YCbCr transform applied to the inverted CMY channels; the inverted K is
        // stored directly. All four components are full resolution.
        var pixelCount = image.Width * image.Height;
        var max = image.MaxSampleValue;
        var y = new ushort[pixelCount];
        var cb = new ushort[pixelCount];
        var cr = new ushort[pixelCount];
        var k = new ushort[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            var r = max - image.PixelData[i * 4];
            var g = max - image.PixelData[i * 4 + 1];
            var b = max - image.PixelData[i * 4 + 2];
            ColorConverter.RgbToYCbCr(r, g, b, max, out y[i], out cb[i], out cr[i]);
            k[i] = (ushort)(max - image.PixelData[i * 4 + 3]);
        }

        return
        [
            HighPrecisionComponent(1, 0, 0, y),
            HighPrecisionComponent(2, 1, 1, cb),
            HighPrecisionComponent(3, 1, 1, cr),
            HighPrecisionComponent(4, 0, 0, k),
        ];
    }

    private static Component[] BuildYcckComponents(JpegImage image)
    {
        // Adobe YCCK: apply the YCbCr transform to the inverted CMY channels, and store the
        // inverted K channel. All four components are full resolution.
        var pixelCount = image.Width * image.Height;
        var y = new byte[pixelCount];
        var cb = new byte[pixelCount];
        var cr = new byte[pixelCount];
        var k = new byte[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            var r = (byte)(255 - image.PixelData[i * 4]);
            var g = (byte)(255 - image.PixelData[i * 4 + 1]);
            var b = (byte)(255 - image.PixelData[i * 4 + 2]);
            ColorConverter.RgbToYCbCr(r, g, b, out y[i], out cb[i], out cr[i]);
            k[i] = (byte)(255 - image.PixelData[i * 4 + 3]);
        }

        return
        [
            new Component { Id = 1, H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = y, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 2, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cb, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 3, H = 1, V = 1, QuantId = 1, TableClass = 1, Plane = cr, PlaneWidth = image.Width, PlaneHeight = image.Height },
            new Component { Id = 4, H = 1, V = 1, QuantId = 0, TableClass = 0, Plane = k, PlaneWidth = image.Width, PlaneHeight = image.Height },
        ];
    }
}
