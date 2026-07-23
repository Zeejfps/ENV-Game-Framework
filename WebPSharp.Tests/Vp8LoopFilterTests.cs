using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8LoopFilterTests
{
    // Layout across an edge with step 1: [p3 p2 p1 p0 | q0 q1 q2 q3], q0 at index 4.
    private static byte[] Edge(byte p3, byte p2, byte p1, byte p0, byte q0, byte q1, byte q2, byte q3)
        => new[] { p3, p2, p1, p0, q0, q1, q2, q3 };

    [Fact]
    public void Simple_UniformRegion_Unchanged()
    {
        var d = Edge(128, 128, 128, 128, 128, 128, 128, 128);
        var before = (byte[])d.Clone();
        Vp8LoopFilter.SimpleFilter(d, 4, 1, edgeLimit: 255);
        Assert.Equal(before, d);
    }

    [Fact]
    public void Simple_SharpEdge_SoftensP0AndQ0()
    {
        var d = Edge(200, 200, 200, 200, 100, 100, 100, 100);
        Vp8LoopFilter.SimpleFilter(d, 4, 1, edgeLimit: 255);
        // Hand-computed (RFC common_adjust): p0 200->184, q0 100->116.
        Assert.Equal(184, d[3]);
        Assert.Equal(116, d[4]);
        // Simple filter only touches p0 and q0.
        Assert.Equal(200, d[2]);
        Assert.Equal(100, d[5]);
    }

    [Fact]
    public void Simple_EdgeExceedsLimit_NoChange()
    {
        var d = Edge(200, 200, 200, 200, 100, 100, 100, 100);
        var before = (byte[])d.Clone();
        Vp8LoopFilter.SimpleFilter(d, 4, 1, edgeLimit: 10); // threshold too small
        Assert.Equal(before, d);
    }

    [Fact]
    public void Subblock_UniformRegion_Unchanged()
    {
        var d = Edge(90, 90, 90, 90, 90, 90, 90, 90);
        var before = (byte[])d.Clone();
        Vp8LoopFilter.SubblockFilter(d, 4, 1, hevThreshold: 0, interiorLimit: 30, edgeLimit: 30);
        Assert.Equal(before, d);
    }

    [Fact]
    public void Macroblock_UniformRegion_Unchanged()
    {
        var d = Edge(70, 70, 70, 70, 70, 70, 70, 70);
        var before = (byte[])d.Clone();
        Vp8LoopFilter.MacroblockFilter(d, 4, 1, hevThreshold: 0, interiorLimit: 30, edgeLimit: 30);
        Assert.Equal(before, d);
    }

    [Fact]
    public void Subblock_InteriorLimitBlocksFiltering()
    {
        // A large interior step (p3..p2) should fail the filter mask -> no change.
        var d = Edge(0, 200, 130, 128, 126, 124, 124, 124);
        var before = (byte[])d.Clone();
        Vp8LoopFilter.SubblockFilter(d, 4, 1, hevThreshold: 10, interiorLimit: 5, edgeLimit: 100);
        Assert.Equal(before, d);
    }

    [Fact]
    public void Macroblock_SmoothStep_AdjustsThreePixelsEachSide()
    {
        // A gentle ramp that passes the mask with no high edge variance -> wide filter runs.
        var d = Edge(120, 122, 124, 126, 132, 134, 136, 138);
        Vp8LoopFilter.MacroblockFilter(d, 4, 1, hevThreshold: 100, interiorLimit: 30, edgeLimit: 60);
        // p3 and q3 are never touched by the macroblock filter.
        Assert.Equal(120, d[0]);
        Assert.Equal(138, d[7]);
        // The inner six pixels move toward each other (edge smoothed).
        Assert.True(d[3] > 126, "p0 should increase toward the edge.");
        Assert.True(d[4] < 132, "q0 should decrease toward the edge.");
    }

    [Fact]
    public void VerticalStep_FiltersAcrossStride()
    {
        // Same sharp edge but arranged as a column with step = stride.
        const int stride = 3;
        var d = new byte[8 * stride];
        byte[] col = { 200, 200, 200, 200, 100, 100, 100, 100 };
        for (var i = 0; i < 8; i++) d[i * stride + 1] = col[i];

        Vp8LoopFilter.SimpleFilter(d, 4 * stride + 1, stride, edgeLimit: 255);
        Assert.Equal(184, d[3 * stride + 1]);
        Assert.Equal(116, d[4 * stride + 1]);
    }
}
