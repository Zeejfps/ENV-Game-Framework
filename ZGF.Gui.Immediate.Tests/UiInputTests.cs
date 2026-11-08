namespace ZGF.Gui.Immediate.Tests;

public sealed class UiInputTests
{
    [Fact]
    public void Typed_characters_inside_and_outside_the_basic_plane_read_back_as_typed()
    {
        var input = new UiInput();

        foreach (var rune in "aé€😀".EnumerateRunes())
            input.Character((uint)rune.Value);
        input.Character(0xD800);
        input.Seal();

        Assert.Equal("aé€😀", input.Text);
    }

    [Fact]
    public void Typing_a_character_allocates_nothing()
    {
        var input = new UiInput();
        input.Character('a');
        input.Character(0x1F600);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 8; i++)
            input.Character(i % 2 == 0 ? 'b' : 0x1F600u);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }
}
