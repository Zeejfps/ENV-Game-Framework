using ZGF.Gui.Testing;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

/// <summary>Guards the harness keyboard surface: every <see cref="GuiTestHarness.KeyMap"/> entry
/// must decode back to its character via the same <see cref="KeyboardKeyExtensions.ToChar"/> the
/// text-input controller uses, so synthetic typing can't silently drift.</summary>
public class KeyMapRoundTripTests
{
    [Fact]
    public void TypeMap_RoundTripsAgainstKeyboardKeyToChar()
    {
        foreach (var (ch, mapped) in GuiTestHarness.KeyMap)
        {
            Assert.Equal(ch, mapped.Key.ToChar(mapped.Shift));
        }
    }
}
