using WebPSharp.Vp8;

namespace WebPSharp.Tests;

public class Vp8TablesTests
{
    [Fact]
    public void Tables_HaveExpectedSizes()
    {
        Assert.Equal(17, Vp8Tables.Bands.Length);
        Assert.Equal(16, Vp8Tables.Zigzag.Length);
        Assert.Equal(128, Vp8Tables.DcTable.Length);
        Assert.Equal(128, Vp8Tables.AcTable.Length);
        Assert.Equal(4 * 8 * 3 * 11, Vp8Tables.DefaultCoeffProbs.Length);
        Assert.Equal(4 * 8 * 3 * 11, Vp8Tables.CoeffUpdateProbs.Length);
        Assert.Equal(10 * 10 * 9, Vp8Tables.BModeProbs.Length);
    }

    [Fact]
    public void DequantTables_MatchSpecAnchors()
    {
        Assert.Equal(4, Vp8Tables.DcTable[0]);
        Assert.Equal(157, Vp8Tables.DcTable[127]);
        Assert.Equal(4, Vp8Tables.AcTable[0]);
        Assert.Equal(284, Vp8Tables.AcTable[127]);
        // Known mid-table repeats that distinguish the real table from a naive 4,5,6,... sequence.
        Assert.Equal(10, Vp8Tables.DcTable[6]);
        Assert.Equal(10, Vp8Tables.DcTable[7]);
    }

    [Fact]
    public void Zigzag_And_Bands_MatchSpec()
    {
        Assert.Equal(new byte[] { 0, 1, 4, 8, 5, 2, 3, 6, 9, 12, 13, 10, 7, 11, 14, 15 }, Vp8Tables.Zigzag);
        Assert.Equal(7, Vp8Tables.Bands[15]);
        Assert.Equal(6, Vp8Tables.Bands[4]);
    }

    [Fact]
    public void CoeffProbIndex_IsWellFormed()
    {
        Assert.Equal(0, Vp8Tables.CoeffProbIndex(0, 0, 0, 0));
        Assert.Equal(4 * 8 * 3 * 11 - 1, Vp8Tables.CoeffProbIndex(3, 7, 2, 10));
        // All indices are unique and in range.
        var seen = new HashSet<int>();
        for (var t = 0; t < 4; t++)
        for (var b = 0; b < 8; b++)
        for (var c = 0; c < 3; c++)
        for (var p = 0; p < 11; p++)
        {
            var idx = Vp8Tables.CoeffProbIndex(t, b, c, p);
            Assert.InRange(idx, 0, Vp8Tables.DefaultCoeffProbs.Length - 1);
            Assert.True(seen.Add(idx));
        }
    }

    [Fact]
    public void UpdateProbs_AreAllHighProbabilities()
    {
        // Update flags are rarely set, so these probabilities are all large (>=176 in the spec).
        foreach (var p in Vp8Tables.CoeffUpdateProbs)
            Assert.InRange(p, 176, 255);
    }
}
