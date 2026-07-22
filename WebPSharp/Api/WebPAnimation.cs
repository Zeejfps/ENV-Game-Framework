namespace WebPSharp.Api;

/// <summary>
/// An animated WebP: a fixed-size canvas, a global background color and loop count, and an ordered
/// list of <see cref="WebPFrame"/>s. Provides <see cref="RenderFrames"/> to composite the frames
/// into full-canvas images honoring blend and disposal.
/// </summary>
public sealed class WebPAnimation
{
    /// <summary>Creates an empty animation with the given canvas size.</summary>
    /// <param name="width">Canvas width in pixels (positive).</param>
    /// <param name="height">Canvas height in pixels (positive).</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is not positive.</exception>
    public WebPAnimation(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
    }

    /// <summary>The canvas width in pixels.</summary>
    public int Width { get; }

    /// <summary>The canvas height in pixels.</summary>
    public int Height { get; }

    /// <summary>The number of times the animation loops; 0 means infinite.</summary>
    public int LoopCount { get; set; }

    /// <summary>The canvas background color in 0xAARRGGBB order.</summary>
    public uint BackgroundColor { get; set; }

    /// <summary>The animation frames in display order.</summary>
    public IList<WebPFrame> Frames { get; } = new List<WebPFrame>();

    /// <summary>Optional metadata (ICC, EXIF, XMP, unknown chunks).</summary>
    public WebPMetadata? Metadata { get; set; }

    /// <summary>
    /// Composites the frames onto the canvas, returning one full-canvas RGBA buffer per frame with
    /// blend and disposal applied.
    /// </summary>
    /// <returns>One <c>Width*Height*4</c> RGBA buffer per frame.</returns>
    public IReadOnlyList<byte[]> RenderFrames()
    {
        var canvas = new byte[Width * Height * 4];
        FillRect(canvas, 0, 0, Width, Height, BackgroundColor);

        var result = new List<byte[]>(Frames.Count);
        foreach (var frame in Frames)
        {
            Blit(canvas, frame);
            result.Add((byte[])canvas.Clone());
            if (frame.Disposal == WebPDisposalMethod.Background)
                FillRect(canvas, frame.X, frame.Y, frame.Width, frame.Height, BackgroundColor);
        }
        return result;
    }

    private void Blit(byte[] canvas, WebPFrame frame)
    {
        var img = frame.Image;
        var src = img.PixelData;
        var comps = img.ComponentCount;

        for (var fy = 0; fy < img.Height; fy++)
        {
            var cy = frame.Y + fy;
            if (cy >= Height)
                break;
            for (var fx = 0; fx < img.Width; fx++)
            {
                var cx = frame.X + fx;
                if (cx >= Width)
                    break;

                var srcOff = (fy * img.Width + fx) * comps;
                var sr = src[srcOff];
                var sg = src[srcOff + 1];
                var sb = src[srcOff + 2];
                var sa = comps == 4 ? src[srcOff + 3] : (byte)255;

                var dstOff = (cy * Width + cx) * 4;
                if (frame.Blend == WebPBlendMethod.Source || sa == 255)
                {
                    canvas[dstOff] = sr;
                    canvas[dstOff + 1] = sg;
                    canvas[dstOff + 2] = sb;
                    canvas[dstOff + 3] = sa;
                }
                else if (sa != 0)
                {
                    // Straight-alpha "source over" blend.
                    var da = canvas[dstOff + 3];
                    var outA = sa + da * (255 - sa) / 255;
                    if (outA == 0)
                    {
                        canvas[dstOff] = canvas[dstOff + 1] = canvas[dstOff + 2] = canvas[dstOff + 3] = 0;
                    }
                    else
                    {
                        canvas[dstOff] = (byte)((sr * sa + canvas[dstOff] * da * (255 - sa) / 255) / outA);
                        canvas[dstOff + 1] = (byte)((sg * sa + canvas[dstOff + 1] * da * (255 - sa) / 255) / outA);
                        canvas[dstOff + 2] = (byte)((sb * sa + canvas[dstOff + 2] * da * (255 - sa) / 255) / outA);
                        canvas[dstOff + 3] = (byte)outA;
                    }
                }
            }
        }
    }

    private void FillRect(byte[] canvas, int x, int y, int w, int h, uint argb)
    {
        var r = (byte)(argb >> 16);
        var g = (byte)(argb >> 8);
        var b = (byte)argb;
        var a = (byte)(argb >> 24);
        for (var yy = y; yy < y + h && yy < Height; yy++)
        {
            for (var xx = x; xx < x + w && xx < Width; xx++)
            {
                var off = (yy * Width + xx) * 4;
                canvas[off] = r;
                canvas[off + 1] = g;
                canvas[off + 2] = b;
                canvas[off + 3] = a;
            }
        }
    }
}
