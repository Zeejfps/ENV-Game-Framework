using WebPSharp.Api.Exceptions;
using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

// §15: canonical prefix-code length validation at the PrefixCode.BuildCanonicalCodes entry point.
public class PrefixCodeTests
{
    [Fact]
    public void OverSubscribedLengths_Rejected()
    {
        // Kraft sum = 3 * 2^-1 = 1.5 > 1: three length-1 codes cannot coexist.
        Assert.Throws<WebPCorruptException>(
            () => PrefixCode.BuildCanonicalCodes(new[] { 1, 1, 1 }, out _));
    }

    [Fact]
    public void UnderSubscribedMultiSymbolLengths_Rejected()
    {
        // Kraft sum = 2 * 2^-2 = 0.5 < 1 with more than one used symbol: incomplete code.
        Assert.Throws<WebPCorruptException>(
            () => PrefixCode.BuildCanonicalCodes(new[] { 2, 2, 0, 0 }, out _));
    }

    [Fact]
    public void SingleSymbolZeroBitCode_Accepted()
    {
        // Exactly one used symbol is a degenerate zero-bit code and must NOT be rejected as
        // under-subscribed, regardless of the nominal length it carries.
        var lengths = new int[8];
        lengths[3] = 1;

        var codes = PrefixCode.BuildCanonicalCodes(lengths, out var singleSymbol);

        Assert.Equal(3, singleSymbol);
        Assert.NotNull(codes);
    }

    [Fact]
    public void SingleSymbolWithNonUnitLength_Accepted()
    {
        // Even a single symbol carrying length > 1 (Kraft sum 2^-5 < 1) is a valid zero-bit code.
        var lengths = new int[8];
        lengths[5] = 5;

        PrefixCode.BuildCanonicalCodes(lengths, out var singleSymbol);

        Assert.Equal(5, singleSymbol);
    }
}
