using ZGF.AppUtils;

namespace ZGF.Gui.Immediate.Tests;

public sealed class FontFaceTests
{
    private const int TruncatedLength = 2000;
    private const int PrivateUseCodepoint = 0xE061;

    private static byte[] Inter() => EmbeddedAssets.LoadBytes(typeof(ICanvas).Assembly, "Inter-Regular.ttf");

    [Fact]
    public void A_font_tells_which_characters_it_has_glyphs_for_without_being_registered()
    {
        var characters = new FontFace("Inter", Inter()).ReadCharacters();

        Assert.NotNull(characters);
        Assert.True(characters.Contains('A'));
        Assert.False(characters.Contains(PrivateUseCodepoint));
    }

    [Fact]
    public void A_font_cut_short_and_data_that_is_no_font_cannot_be_read()
    {
        var truncated = Inter().AsSpan(0, TruncatedLength).ToArray();
        var markup = "<svg/>"u8.ToArray();

        Assert.Null(new FontFace("Cut", truncated).ReadCharacters());
        Assert.Null(new FontFace("Markup", markup).ReadCharacters());
    }
}
