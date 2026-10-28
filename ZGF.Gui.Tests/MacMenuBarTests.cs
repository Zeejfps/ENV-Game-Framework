using ZGF.Gui.Hosting;

namespace ZGF.Gui.Tests;

public class MacMenuBarTests
{
    private const ulong Shift = 1UL << 17;
    private const ulong Option = 1UL << 19;
    private const ulong Command = 1UL << 20;

    [Theory]
    [InlineData("Ctrl+O", "o", Command)]
    [InlineData("Ctrl+Shift+E", "e", Command | Shift)]
    [InlineData("Cmd+Alt+H", "h", Command | Option)]
    [InlineData("Ctrl+=", "=", Command)]
    [InlineData("F5", "", 0UL)]
    public void A_shortcut_reads_as_its_key_with_ctrl_standing_for_command(string shortcut, string key, ulong modifiers)
    {
        if (!OperatingSystem.IsMacOS()) return;

        Assert.Equal((key, modifiers), MacMenuBar.KeyEquivalent(shortcut));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Alt+Left")]
    [InlineData("R")]
    [InlineData("Ctrl+PageDown")]
    [InlineData("Hyper+O")]
    public void A_shortcut_a_text_field_needs_or_this_cannot_name_gets_no_key(string? shortcut)
    {
        if (!OperatingSystem.IsMacOS()) return;

        Assert.Equal(("", 0UL), MacMenuBar.KeyEquivalent(shortcut));
    }
}
