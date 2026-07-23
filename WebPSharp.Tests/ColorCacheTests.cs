using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class ColorCacheTests
{
    [Fact]
    public void InsertThenLookup_ByComputedIndex_ReturnsColor()
    {
        var cache = new ColorCache(6);
        const uint argb = 0xFF11AA33;
        cache.Insert(argb);
        var index = cache.GetIndex(argb);
        Assert.Equal(argb, cache.Lookup(index));
    }

    [Fact]
    public void GetIndex_IsDeterministicAndInRange()
    {
        var cache = new ColorCache(4); // 16 slots
        for (uint c = 0; c < 1000; c++)
        {
            var argb = 0xFF000000u | (c * 2654435761u);
            var index = cache.GetIndex(argb);
            Assert.InRange(index, 0, 15);
            Assert.Equal(index, cache.GetIndex(argb)); // deterministic
        }
    }

    [Fact]
    public void Insert_Collision_Overwrites()
    {
        var cache = new ColorCache(1); // 2 slots -> frequent collisions
        cache.Insert(0xFF000000);
        cache.Insert(0xFFFFFFFF);
        // Whatever landed in each slot must read back consistently.
        var idx = cache.GetIndex(0xFFFFFFFF);
        cache.Insert(0xFFFFFFFF);
        Assert.Equal(0xFFFFFFFFu, cache.Lookup(idx));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public void Constructor_InvalidBits_Throws(int bits)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ColorCache(bits));
    }

    [Fact]
    public void Size_MatchesBits()
    {
        Assert.Equal(1 << 7, new ColorCache(7).Size);
    }

    // §14 hash golden: pins the 0x1E35A7BD multiplier. Indices hand-computed as
    // (unchecked(argb * 0x1E35A7BDu) >> (32 - 8)) for an 8-bit (256-slot) cache.
    [Theory]
    [InlineData(0xFF000000u, 67)]
    [InlineData(0xFFFFFFFFu, 225)]
    [InlineData(0x11223344u, 138)]
    public void GetIndex_MatchesHandComputedHash_Bits8(uint argb, int expectedIndex)
    {
        var cache = new ColorCache(8);
        Assert.Equal(expectedIndex, cache.GetIndex(argb));

        cache.Insert(argb);
        Assert.Equal(argb, cache.Lookup(expectedIndex));
    }

    [Fact]
    public void Constructor_MaxValidBits_Accepted()
    {
        // Upper bound of the valid 1..11 range must be accepted (11 -> 2048 slots).
        Assert.Equal(1 << 11, new ColorCache(11).Size);
    }

    [Fact]
    public void Constructor_MinValidBits_Accepted()
    {
        Assert.Equal(1 << 1, new ColorCache(1).Size);
    }
}
