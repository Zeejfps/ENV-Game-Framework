namespace ZGF.Svg.Raster;

/// <summary>
/// Premultiplied source-over compositing surface. Compositing internally in
/// premultiplied space avoids dark fringing where semi-transparent shapes
/// overlap; the final <see cref="WriteTo"/> pass un-premultiplies into the
/// straight-alpha RGBA8 layout the ZGF.Gui image pipeline uploads.
/// </summary>
internal sealed class PixelBlender
{
    private byte[] _premul = [];
    private int _w;
    private int _h;

    public void Begin(int w, int h)
    {
        _w = w;
        _h = h;
        var needed = w * h * 4;
        if (_premul.Length < needed)
            _premul = new byte[needed];
        else
            Array.Clear(_premul, 0, needed);
    }

    public void BlendPixel(int x, int y, uint colorArgb, byte coverage)
    {
        var srcA = (colorArgb >> 24) & 0xFF;
        var a = MulDiv255(srcA, coverage);
        if (a == 0)
            return;

        var srcR = (colorArgb >> 16) & 0xFF;
        var srcG = (colorArgb >> 8) & 0xFF;
        var srcB = colorArgb & 0xFF;

        var i = (y * _w + x) * 4;
        var premul = _premul;
        if (a == 255)
        {
            premul[i] = (byte)srcR;
            premul[i + 1] = (byte)srcG;
            premul[i + 2] = (byte)srcB;
            premul[i + 3] = 255;
            return;
        }

        var inv = 255u - a;
        premul[i] = (byte)(MulDiv255(srcR, a) + MulDiv255(premul[i], inv));
        premul[i + 1] = (byte)(MulDiv255(srcG, a) + MulDiv255(premul[i + 1], inv));
        premul[i + 2] = (byte)(MulDiv255(srcB, a) + MulDiv255(premul[i + 2], inv));
        premul[i + 3] = (byte)(a + MulDiv255(premul[i + 3], inv));
    }

    /// <summary>Un-premultiplies into straight-alpha RGBA8, top-down rows.</summary>
    public void WriteTo(Span<byte> dest)
    {
        var count = _w * _h;
        for (var p = 0; p < count; p++)
        {
            var i = p * 4;
            var a = _premul[i + 3];
            if (a == 0)
            {
                dest[i] = 0;
                dest[i + 1] = 0;
                dest[i + 2] = 0;
                dest[i + 3] = 0;
            }
            else if (a == 255)
            {
                dest[i] = _premul[i];
                dest[i + 1] = _premul[i + 1];
                dest[i + 2] = _premul[i + 2];
                dest[i + 3] = 255;
            }
            else
            {
                dest[i] = (byte)Math.Min(255, (_premul[i] * 255 + a / 2) / a);
                dest[i + 1] = (byte)Math.Min(255, (_premul[i + 1] * 255 + a / 2) / a);
                dest[i + 2] = (byte)Math.Min(255, (_premul[i + 2] * 255 + a / 2) / a);
                dest[i + 3] = a;
            }
        }
    }

    private static uint MulDiv255(uint x, uint y)
    {
        var t = x * y + 128;
        return (t + (t >> 8)) >> 8;
    }
}
