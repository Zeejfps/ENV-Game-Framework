using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class LzPrefixTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(100)]
    [InlineData(4096)]
    public void EncodeThenDecode_RoundTripsLength(int value)
    {
        LzPrefix.Encode(value, out var prefixCode, out var extraBits, out var extraValue);

        var writer = new Vp8LBitWriter();
        writer.PutBits((uint)extraValue, extraBits);
        var reader = new Vp8LBitReader(writer.ToArray());

        var decoded = LzPrefix.Decode(prefixCode, ref reader);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void LengthPrefixCodes_StayWithinAlphabet()
    {
        for (var v = 1; v <= 4096; v++)
        {
            LzPrefix.Encode(v, out var prefixCode, out _, out _);
            Assert.InRange(prefixCode, 0, 23); // 24 length symbols
        }
    }

    [Fact]
    public void DistancePrefixCodes_StayWithinAlphabet()
    {
        // Distances can be large; the distance prefix alphabet has 40 symbols.
        for (var v = 1; v <= 1_000_000; v += 997)
        {
            LzPrefix.Encode(v, out var prefixCode, out _, out _);
            Assert.InRange(prefixCode, 0, 39);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(123)]
    public void FullValueRoundTrip_ManyValues(int seed)
    {
        var rng = new Random(seed);
        for (var i = 0; i < 2000; i++)
        {
            var value = rng.Next(1, 500_000);
            LzPrefix.Encode(value, out var prefixCode, out var extraBits, out var extraValue);
            var writer = new Vp8LBitWriter();
            writer.PutBits((uint)extraValue, extraBits);
            var reader = new Vp8LBitReader(writer.ToArray());
            Assert.Equal(value, LzPrefix.Decode(prefixCode, ref reader));
        }
    }
}
