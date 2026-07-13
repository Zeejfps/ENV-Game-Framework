using System.Text;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Automation;

/// <summary>
/// Somewhere keystrokes can be sent: a live window (<see cref="GuiDriver"/>) or a headless tree
/// (the test harness). <see cref="Typist"/> scripts against this, so the same typing script can be
/// replayed into a real app or into a test.
/// </summary>
public interface ITypeSink
{
    /// <summary>Send one character, as the OS would commit it — layout and dead keys already resolved.</summary>
    void TypeRune(Rune rune);

    /// <summary>Press and release a physical key. Drives shortcuts and editing gestures, never text.</summary>
    void PressKey(KeyboardKey key, InputModifiers modifiers = InputModifiers.None);
}
