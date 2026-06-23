namespace ZGF.Gui.Tests;

// The alignment-mirroring decision behind RTL layout: Start is the leading edge and End the
// trailing edge, so the same Start-aligned label flips from the left to the right when the UI
// base direction is RTL. Center is direction-independent.
public class TextLayoutTests
{
    [Theory]
    [InlineData(TextAlignment.Start, false, TextPlacement.Left)]
    [InlineData(TextAlignment.End, false, TextPlacement.Right)]
    [InlineData(TextAlignment.Start, true, TextPlacement.Right)]
    [InlineData(TextAlignment.End, true, TextPlacement.Left)]
    public void StartAndEnd_FollowBaseDirection(TextAlignment align, bool rtl, TextPlacement expected)
    {
        Assert.Equal(expected, TextLayout.ResolveHorizontal(align, rtl));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Center_IsDirectionIndependent(bool rtl)
    {
        Assert.Equal(TextPlacement.Center, TextLayout.ResolveHorizontal(TextAlignment.Center, rtl));
    }

    [Fact]
    public void Ltr_IsUnchangedFromLegacyStartLeft_EndRight()
    {
        // The pre-RTL behavior: Start drew at the left, End at the right. Preserved under an LTR base.
        Assert.Equal(TextPlacement.Left, TextLayout.ResolveHorizontal(TextAlignment.Start, rtlBase: false));
        Assert.Equal(TextPlacement.Right, TextLayout.ResolveHorizontal(TextAlignment.End, rtlBase: false));
    }
}
