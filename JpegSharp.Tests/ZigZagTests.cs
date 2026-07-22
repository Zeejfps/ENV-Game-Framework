using JpegSharp.Transforms;
using Xunit;

namespace JpegSharp.Tests;

public class ZigZagTests
{
    // The canonical JPEG zig-zag sequence (ITU-T T.81 Figure A.6):
    // natural-order index visited at each zig-zag position.
    private static readonly int[] ExpectedOrder =
    [
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    ];

    [Fact]
    public void Order_MatchesSpecSequence()
    {
        Assert.Equal(ExpectedOrder, ZigZag.Order.ToArray());
    }

    [Fact]
    public void Order_StartsAtDcAndIsAPermutation()
    {
        Assert.Equal(64, ZigZag.Order.Length);
        Assert.Equal(0, ZigZag.Order[0]);

        var seen = new bool[64];
        foreach (var idx in ZigZag.Order)
        {
            Assert.InRange(idx, 0, 63);
            Assert.False(seen[idx], $"index {idx} appears twice");
            seen[idx] = true;
        }
    }

    [Fact]
    public void InverseOrder_IsInverseOfOrder()
    {
        for (var k = 0; k < 64; k++)
        {
            Assert.Equal(k, ZigZag.InverseOrder[ZigZag.Order[k]]);
            Assert.Equal(k, ZigZag.Order[ZigZag.InverseOrder[k]]);
        }
    }

    [Fact]
    public void Dezigzag_ThenZigzag_RoundTrips()
    {
        Span<short> natural = stackalloc short[64];
        for (short i = 0; i < 64; i++)
            natural[i] = (short)(i * 3 - 50);

        Span<short> zig = stackalloc short[64];
        ZigZag.FromNatural(natural, zig);

        Span<short> back = stackalloc short[64];
        ZigZag.ToNatural(zig, back);

        for (var i = 0; i < 64; i++)
            Assert.Equal(natural[i], back[i]);
    }

    [Fact]
    public void FromNatural_PlacesDcFirst()
    {
        Span<short> natural = stackalloc short[64];
        natural[0] = 1234; // DC term in natural order
        natural[1] = 7;    // AC (row 0, col 1) -> zig position 1

        Span<short> zig = stackalloc short[64];
        ZigZag.FromNatural(natural, zig);

        Assert.Equal(1234, zig[0]);
        Assert.Equal(7, zig[1]);
    }

    [Fact]
    public void ToNatural_ScattersAccordingToOrder()
    {
        // A zig-zag stream that is all zero except position 2 should land at natural index 8.
        Span<short> zig = stackalloc short[64];
        zig[2] = 99;

        Span<short> natural = stackalloc short[64];
        ZigZag.ToNatural(zig, natural);

        Assert.Equal(99, natural[8]);
        for (var i = 0; i < 64; i++)
            if (i != 8)
                Assert.Equal(0, natural[i]);
    }
}
