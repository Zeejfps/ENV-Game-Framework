using ZGF.Desktop;
using ZGF.Gui.Hosting;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public class MacMenuBarTests
{
    private const ulong Shift = 1UL << 17;
    private const ulong Option = 1UL << 19;
    private const ulong Command = 1UL << 20;

    public static TheoryData<Shortcut, string, ulong> Bound => new()
    {
        { Shortcut.Command(KeyboardKey.O), "o", Command },
        { Shortcut.Command(KeyboardKey.E, ShortcutModifiers.Shift), "e", Command | Shift },
        { Shortcut.Command(KeyboardKey.H, ShortcutModifiers.Alt), "h", Command | Option },
        { Shortcut.Command(KeyboardKey.Equals), "=", Command },
        { Shortcut.Command(KeyboardKey.LeftBracket), "[", Command },
        { Shortcut.Key(KeyboardKey.F5), "", 0UL },
    };

    [Theory]
    [MemberData(nameof(Bound))]
    public void A_shortcut_with_command_or_a_function_key_is_the_items_key_equivalent(Shortcut shortcut, string key, ulong modifiers)
    {
        if (!OperatingSystem.IsMacOS()) return;

        Assert.Equal((key, modifiers), MacMenuBar.KeyEquivalent(shortcut));
    }

    public static TheoryData<Shortcut?> Unbound => new()
    {
        null,
        Shortcut.Key(KeyboardKey.LeftArrow, ShortcutModifiers.Alt),
        Shortcut.Key(KeyboardKey.Enter),
        Shortcut.Command(KeyboardKey.PageDown),
        Shortcut.Key(KeyboardKey.M, ShortcutModifiers.Control).Then(KeyboardKey.M),
    };

    [Theory]
    [MemberData(nameof(Unbound))]
    public void A_shortcut_a_text_field_needs_or_the_menu_cannot_take_gets_no_key(Shortcut? shortcut)
    {
        if (!OperatingSystem.IsMacOS()) return;

        Assert.Equal(("", 0UL), MacMenuBar.KeyEquivalent(shortcut));
    }
}
