using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public class ShortcutTests
{
    private static readonly Shortcut Export = Shortcut.Command(KeyboardKey.E, ShortcutModifiers.Shift);
    private static readonly Shortcut ToggleFold = Shortcut.Key(KeyboardKey.M, ShortcutModifiers.Control).Then(KeyboardKey.M);

    [Fact]
    public void Command_is_written_ctrl_off_macos_and_with_macos_symbols_on_it()
    {
        Assert.Equal("Ctrl+Shift+E", Export.LabelOn(mac: false));
        Assert.Equal("⇧⌘E", Export.LabelOn(mac: true));
        Assert.Equal("Ctrl+M, M", ToggleFold.LabelOn(mac: false));
        Assert.Equal("⌃M, M", ToggleFold.LabelOn(mac: true));
        Assert.Equal("Alt+Left", Shortcut.Key(KeyboardKey.LeftArrow, ShortcutModifiers.Alt).LabelOn(mac: false));
        Assert.Equal("⌥←", Shortcut.Key(KeyboardKey.LeftArrow, ShortcutModifiers.Alt).LabelOn(mac: true));
        Assert.Equal("F5", Shortcut.Key(KeyboardKey.F5).LabelOn(mac: true));
    }

    [Fact]
    public void Command_is_held_as_ctrl_off_macos_and_as_command_on_it()
    {
        var chord = Export.First;

        Assert.True(chord.Matches(KeyboardKey.E, KeyModifiers.Control | KeyModifiers.Shift, mac: false));
        Assert.False(chord.Matches(KeyboardKey.E, KeyModifiers.Super | KeyModifiers.Shift, mac: false));
        Assert.True(chord.Matches(KeyboardKey.E, KeyModifiers.Super | KeyModifiers.Shift, mac: true));
        Assert.False(chord.Matches(KeyboardKey.E, KeyModifiers.Control | KeyModifiers.Shift, mac: true));
    }

    [Fact]
    public void A_chord_takes_exactly_its_modifiers_and_ignores_the_locks()
    {
        var chord = Shortcut.Command(KeyboardKey.E).First;

        Assert.False(chord.Matches(KeyboardKey.E, KeyModifiers.Control | KeyModifiers.Shift, mac: false));
        Assert.True(chord.Matches(KeyboardKey.E, KeyModifiers.Control | KeyModifiers.CapsLock | KeyModifiers.NumLock, mac: false));
    }

    [Fact]
    public void A_second_chord_completes_after_the_first_with_or_without_its_modifiers_held()
    {
        var controlM = new KeyChordPress(KeyboardKey.M, KeyModifiers.Control);
        var m = new KeyChordPress(KeyboardKey.M, KeyModifiers.None);

        Assert.False(ToggleFold.CompletedBy([controlM], mac: true));
        Assert.True(ToggleFold.CompletedBy([controlM, m], mac: true));
        Assert.True(ToggleFold.CompletedBy([controlM, controlM], mac: true));
        Assert.False(ToggleFold.CompletedBy([m, m], mac: true));
    }

    [Fact]
    public void A_press_that_completed_a_chord_does_not_also_begin_it_again()
    {
        var controlM = new KeyChordPress(KeyboardKey.M, KeyModifiers.Control);
        var m = new KeyChordPress(KeyboardKey.M, KeyModifiers.None);

        Assert.False(ToggleFold.CompletedBy([controlM, controlM, controlM], mac: false));
        Assert.True(ToggleFold.CompletedBy([controlM, controlM, controlM, m], mac: false));
    }
}
