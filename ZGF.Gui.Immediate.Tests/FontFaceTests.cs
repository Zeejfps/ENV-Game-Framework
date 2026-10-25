using ZGF.AppUtils;
using ZGF.Fonts;

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

public sealed class LoadedFontsTests
{
    private readonly List<int> _loads = new();

    private FontHandle Load(byte[] data, int faceIndex)
    {
        _loads.Add(data.Length);
        return new FontHandle(_loads.Count);
    }

    [Fact]
    public void Registering_the_same_bytes_again_from_new_arrays_keeps_one_face()
    {
        var fonts = new LoadedFonts();
        byte[] bytes = [0, 1, 0, 0, 7, 7];

        var first = fonts.LoadOnce(new FontFace("Mine", bytes.ToArray()), Load);
        for (var reload = 0; reload < 10; reload++)
            Assert.Equal(first, fonts.LoadOnce(new FontFace("Mine", bytes.ToArray()), Load));

        Assert.Single(_loads);
        Assert.Equal(1, fonts.Count);
    }

    [Fact]
    public void Changed_bytes_and_another_face_of_a_collection_each_load_a_face_of_their_own()
    {
        var fonts = new LoadedFonts();
        byte[] before = [0, 1, 0, 0, 7, 7];
        byte[] after = [0, 1, 0, 0, 7, 8];

        var old = fonts.LoadOnce(new FontFace("Mine", before), Load);
        var changed = fonts.LoadOnce(new FontFace("Mine", after), Load);
        var second = fonts.LoadOnce(new FontFace("Mine", before, FaceIndex: 1), Load);

        Assert.Equal(3, new[] { old, changed, second }.Distinct().Count());
        Assert.Equal(3, fonts.Count);
    }
}
