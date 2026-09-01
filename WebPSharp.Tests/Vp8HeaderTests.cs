using WebPSharp.Container;
using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8HeaderTests
{
    private static byte[] Vp8Payload(string asset)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", asset);
        var bytes = File.ReadAllBytes(path);
        var reader = RiffReader.Create(bytes);
        while (reader.MoveNext())
        {
            if (reader.Current.Id == WebPChunkIds.Vp8)
                return reader.Current.Payload.ToArray();
        }
        throw new InvalidOperationException($"No VP8 chunk in {asset}.");
    }

    [Fact]
    public void ParsesGoldenKeyFrameHeader()
    {
        var decoder = new Vp8Decoder(Vp8Payload("grad_q80.webp"));
        decoder.ParseHeaders();

        Assert.True(decoder.KeyFrame);
        Assert.Equal(0, decoder.Profile);
        Assert.Equal(64, decoder.Width);
        Assert.Equal(48, decoder.Height);
        Assert.Equal(4, decoder.MbWidth);  // ceil(64/16)
        Assert.Equal(3, decoder.MbHeight); // ceil(48/16)
    }

    [Fact]
    public void PartitionsAndQuantAreConsistent()
    {
        var decoder = new Vp8Decoder(Vp8Payload("grad_q80.webp"));
        decoder.ParseHeaders();

        Assert.InRange(decoder.NumParts, 1, 8);
        Assert.Equal(decoder.NumParts, decoder.Partitions.Length);

        // The compressed header must have been read without running the first partition dry,
        // which only happens if the boolean decoder stayed in sync through the whole header.
        Assert.False(decoder.FirstPartition.IsEndOfStream);

        // Dequant steps are in the valid table range for every segment.
        foreach (var m in decoder.Dequant)
        {
            Assert.InRange(m.Y1Dc, 4, 157);
            Assert.InRange(m.Y1Ac, 4, 284);
            Assert.InRange(m.UvDc, 4, 157);
            Assert.True(m.Y2Ac >= 8);
        }
    }

    [Fact]
    public void FilterHeaderIsSane()
    {
        var decoder = new Vp8Decoder(Vp8Payload("grad_q80.webp"));
        decoder.ParseHeaders();
        Assert.InRange(decoder.FilterLevel, 0, 63);
        Assert.InRange(decoder.FilterSharpness, 0, 7);
        Assert.InRange(decoder.FilterType, 0, 2);
    }
}
