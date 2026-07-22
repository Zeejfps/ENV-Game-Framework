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
}
