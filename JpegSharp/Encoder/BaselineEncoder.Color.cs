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
