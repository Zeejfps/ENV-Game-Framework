using System.Text;
using ZGF.Desktop.Input;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// A key and the character it produces arrive on separate OS callbacks, so handling the key says
/// nothing on its own about the text that follows. <see cref="KeyClaim"/> is what carries the
/// decision across, and it has to distinguish two outcomes that a single "consumed" flag cannot:
///
/// <para>A shortcut takes the key as a command — press T over the commit list and the tag dialog
/// opens. The dialog focuses its name field synchronously, so the character for that same T lands
/// in a field that did not exist when the key was pressed, and types a stray "t" into it.</para>
///
/// <para>A field being edited takes the key as text — it must stop the key reaching the app's
/// single-key bindings while letting the character through, or typing stops working entirely.</para>
/// </summary>
public class KeyClaimTests
{
    /// <summary>
    /// A field beneath an ancestor shortcut handler, which is the shape of the real app: the commit
    /// list's key controller sits above the dialogs its shortcuts open. The field starts unfocused —
    /// the dialog it stands in has not opened yet when the shortcut key is pressed.
    /// </summary>
    private static GuiTestHarness FieldUnderShortcut(State<string> value, KeyClaim claim) =>
        GuiTestHarness.Create(ctx => new KbmInput
        {
            OnKey = (ref KeyboardKeyEvent e) =>
            {
                if (e.Phase != EventPhase.Bubbling) return;
                if (e.State != InputState.Pressed || e.Key != KeyboardKey.T) return;
                if (claim == KeyClaim.Command) e.Consume();
                else e.ConsumeAsText();
            },
            Child = new Box
            {
                Children =
                [
                    new TextInput
                    {
                        Id = "field",
                        Value = value,
                    },
                ],
            },
        }.BuildView(ctx));

    /// <summary>
    /// Puts the cursor over the tree so the ancestor handler is in the dispatch path at all. Load-
    /// bearing, not cosmetic: that path is built from the hover chain, so with the pointer parked
    /// nowhere the shortcut never fires, nothing claims the key, and a test asserting the character
    /// is suppressed would fail no matter what the claim did.
    /// </summary>
    private static void HoverShortcutHandler(GuiTestHarness h)
    {
        var center = h.Get("field").Position.Center;
        h.MoveTo(center.X, center.Y);
    }

    /// <summary>
    /// The regression. Pressing T fires the shortcut, the dialog opens and focuses its field, and
    /// only then does the OS deliver the character — which must not be typed, because the keystroke
    /// was already spent on the shortcut.
    /// </summary>
    [Fact]
    public void CommandClaim_SuppressesTheCharacterTheKeyProduces()
    {
        var value = new State<string>("");
        using var h = FieldUnderShortcut(value, KeyClaim.Command);

        HoverShortcutHandler(h);
        h.KeyDown(KeyboardKey.T);
        h.ClickOn("field");
        h.SendText(new Rune('t'));

        Assert.Equal("", value.Value);
    }

    /// <summary>The control: same sequence, same consumed key, but claimed as text. Without this
    /// half the suppression would be indistinguishable from "consuming a key breaks typing".</summary>
    [Fact]
    public void TextClaim_LetsTheCharacterThrough()
    {
        var value = new State<string>("");
        using var h = FieldUnderShortcut(value, KeyClaim.Text);

        HoverShortcutHandler(h);
        h.KeyDown(KeyboardKey.T);
        h.ClickOn("field");
        h.SendText(new Rune('t'));

        Assert.Equal("t", value.Value);
    }

    /// <summary>An unclaimed key is the ordinary case and must type. This is also what an IME commit
    /// looks like from here — text with no key of its own — so it pins the default as "deliver".</summary>
    [Fact]
    public void UnclaimedKey_Types()
    {
        var value = new State<string>("");
        using var h = GuiTestHarness.Create(ctx => new TextInput
        {
            Id = "field",
            Value = value,
            AutoFocus = true,
        }.BuildView(ctx));

        h.Type("t");

        Assert.Equal("t", value.Value);
    }

    /// <summary>A command claim is spent on the keystroke that made it. The release clears it, so a
    /// later character — an IME commit, whose own keys the patched GLFW withholds — still arrives.</summary>
    [Fact]
    public void CommandClaim_DoesNotOutliveItsKeystroke()
    {
        var value = new State<string>("");
        using var h = FieldUnderShortcut(value, KeyClaim.Command);

        h.KeyDown(KeyboardKey.T);
        h.KeyUp(KeyboardKey.T);
        h.ClickOn("field");
        h.SendText(new Rune('好'));

        Assert.Equal("好", value.Value);
    }
}
