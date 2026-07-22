using WebPSharp.Container;
using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8ReconstructionTests
{
    private static byte[] Vp8Payload(string asset)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", asset));
        var reader = RiffReader.Create(bytes);
        while (reader.MoveNext())
            if (reader.Current.Id == WebPChunkIds.Vp8)
                return reader.Current.Payload.ToArray();
        throw new InvalidOperationException("no VP8 chunk");
    }

    private static (int max, double mean) Compare(byte[] a, byte[] b)
    {
        var max = 0;
        long sum = 0;
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++)
        {
            var d = Math.Abs(a[i] - b[i]);
            if (d > max) max = d;
            sum += d;
        }
        return (max, (double)sum / n);
    }

    [Fact]
    public void Reconstruction_MatchesDwebpNoFilterReference()
    {
        var decoder = new Vp8Decoder(Vp8Payload("grad_q80.webp"));
        var rgba = decoder.DecodeToRgba();
        var reference = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "grad_q80_nofilter.rgba"));

        Assert.Equal(reference.Length, rgba.Length);
        var (max, mean) = Compare(rgba, reference);
        Assert.True(max <= 1, $"reconstruction differs from dwebp -nofilter: maxDiff={max}, meanDiff={mean:F3}");
    }
}
