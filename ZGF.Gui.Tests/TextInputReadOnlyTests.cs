using System.Runtime.InteropServices;
using ZGF.Desktop.Input;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Testing;
using ZGF.Gui.Widgets;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Read-only turns the field into a selectable, copyable text surface rather than a second text
/// view: selection rendering, the clipboard wiring and caret navigation are the editable field's,
/// and only the paths that reach the buffer are cut. So the tests come in pairs — every gesture that
/// must be inert here is also asserted to still edit an ordinary field, which is what proves the
/// suppression is scoped to the flag and not to the control.
/// </summary>
public class TextInputReadOnlyTests
{
    private sealed class FakeClipboard : IClipboard
    {
        public string? Text;
        public void SetText(string text) => Text = text;
        public string? GetText() => Text;
    }

    // Cmd on macOS, Ctrl elsewhere — the same split the controller makes, so the chord tests exercise
    // the real key path on whichever machine runs them.
    private static readonly InputModifiers CommandModifier =
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? InputModifiers.Super : InputModifiers.Control;

    private static GuiTestHarness Field(
        State<string> value, bool readOnly, FakeClipboard clipboard, out TextInputView view,
        TextWrap wrap = TextWrap.NoWrap)
    {
        var h = GuiTestHarness.Create(
            ctx => new TextInput
            {
                Id = "field",
                Value = value,
                AutoFocus = true,
                ReadOnly = readOnly,
                Wrap = wrap,
                CaretColor = 0xFFFF0000,
            }.BuildView(ctx),
            configure: ctx => ctx.AddService<IClipboard>(clipboard));
        view = (TextInputView)h.Get("field");
        return h;
    }

    [Fact]
    public void TypingDoesNotReachAReadOnlyField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: true, new FakeClipboard(), out var view);

        h.Type("XYZ");

        Assert.Equal("hello", value.Value);
        Assert.Equal("hello", view.Text.ToString());
    }

    [Fact]
    public void TypingStillEditsAnOrdinaryField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: false, new FakeClipboard(), out _);

        h.Type("XYZ");

        Assert.Equal("helloXYZ", value.Value);
    }

    [Fact]
    public void PasteDoesNotReachAReadOnlyField()
    {
        var value = new State<string>("hello");
        var clipboard = new FakeClipboard { Text = "pasted" };
        using var h = Field(value, readOnly: true, clipboard, out _);

        h.PressKey(KeyboardKey.V, CommandModifier);

        Assert.Equal("hello", value.Value);
    }

    [Fact]
    public void PasteStillEditsAnOrdinaryField()
    {
        var value = new State<string>("hello");
        var clipboard = new FakeClipboard { Text = "pasted" };
        using var h = Field(value, readOnly: false, clipboard, out _);

        h.PressKey(KeyboardKey.V, CommandModifier);

        Assert.Equal("hellopasted", value.Value);
    }

    [Fact]
    public void BackspaceAndWordDeleteDoNotReachAReadOnlyField()
    {
        var value = new State<string>("hello world");
        using var h = Field(value, readOnly: true, new FakeClipboard(), out var view);

        h.PressKey(KeyboardKey.Backspace);
        h.PressKey(KeyboardKey.Backspace, InputModifiers.Control);
        view.SelectAll();
        h.PressKey(KeyboardKey.Backspace);

        Assert.Equal("hello world", value.Value);
    }

    [Fact]
    public void BackspaceStillEditsAnOrdinaryField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: false, new FakeClipboard(), out _);

        h.PressKey(KeyboardKey.Backspace);

        Assert.Equal("hell", value.Value);
    }

    [Fact]
    public void CutIsInertInAReadOnlyFieldAndDoesNotTouchTheClipboard()
    {
        var value = new State<string>("hello");
        var clipboard = new FakeClipboard();
        using var h = Field(value, readOnly: true, clipboard, out var view);

        view.SelectAll();
        h.PressKey(KeyboardKey.X, CommandModifier);

        Assert.Equal("hello", value.Value);
        // Cut is refused whole rather than degraded to a copy — the chord bubbles untouched, so an
        // app-level Cut binding still sees it.
        Assert.Null(clipboard.Text);
    }

    [Fact]
    public void EnterDoesNotBreakTheLineInAReadOnlyMultiLineField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: true, new FakeClipboard(), out _, wrap: TextWrap.Wrap);

        h.PressKey(KeyboardKey.Enter);

        Assert.Equal("hello", value.Value);
    }

    [Fact]
    public void EnterStillBreaksTheLineInAnOrdinaryMultiLineField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: false, new FakeClipboard(), out _, wrap: TextWrap.Wrap);

        h.PressKey(KeyboardKey.Enter);

        Assert.Equal("hello\n", value.Value);
    }

    [Fact]
    public void ImeCompositionDoesNotReachAReadOnlyField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: true, new FakeClipboard(), out var view);

        h.Compose("ni", "你");

        Assert.Equal("hello", value.Value);
        Assert.False(view.IsComposing);
    }

    [Fact]
    public void ImeCompositionStillReachesAnOrdinaryField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: false, new FakeClipboard(), out _);

        h.Compose("ni", "你");

        Assert.Equal("hello你", value.Value);
    }

    [Fact]
    public void UndoAndRedoAreInertInAReadOnlyField()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: true, new FakeClipboard(), out var view);

        h.Type("X");
        h.PressKey(KeyboardKey.Z, CommandModifier);

        Assert.False(view.Undo());
        Assert.False(view.Redo());
        Assert.Equal("hello", value.Value);
    }

    [Fact]
    public void SelectionAndCopyStillWorkInAReadOnlyField()
    {
        var value = new State<string>("hello");
        var clipboard = new FakeClipboard();
        using var h = Field(value, readOnly: true, clipboard, out var view);

        // The caret sits at the end after the bound value lands, so Shift+Left three times selects
        // the trailing "llo" — a partial selection, which is the whole point over a copy button.
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Shift);
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Shift);
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Shift);
        Assert.Equal("llo", view.GetSelectedText());

        h.PressKey(KeyboardKey.C, CommandModifier);

        Assert.Equal("llo", clipboard.Text);
    }

    [Fact]
    public void SelectAllAndCopyStillWorkInAReadOnlyField()
    {
        var value = new State<string>("hello");
        var clipboard = new FakeClipboard();
        using var h = Field(value, readOnly: true, clipboard, out _);

        h.PressKey(KeyboardKey.A, CommandModifier);
        h.PressKey(KeyboardKey.C, CommandModifier);

        Assert.Equal("hello", clipboard.Text);
    }

    [Fact]
    public void CaretNavigationStillMovesInAReadOnlyField()
    {
        var value = new State<string>("hello world");
        using var h = Field(value, readOnly: true, new FakeClipboard(), out var view);
        h.Layout();

        var atEnd = view.GetCaretRect().Left;

        h.PressKey(KeyboardKey.LeftArrow);
        var afterOneLeft = view.GetCaretRect().Left;
        Assert.True(afterOneLeft < atEnd, $"caret did not move left: {atEnd} -> {afterOneLeft}");

        // Word-jump uses Option on macOS and Ctrl elsewhere; either way it must overshoot a single
        // character step, which is what tells the two navigation paths apart.
        var wordModifier = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? InputModifiers.Alt
            : InputModifiers.Control;
        h.PressKey(KeyboardKey.LeftArrow, wordModifier);
        var afterWordLeft = view.GetCaretRect().Left;
        Assert.True(afterWordLeft < afterOneLeft, $"word jump did not move: {afterOneLeft} -> {afterWordLeft}");

        h.PressKey(KeyboardKey.RightArrow);
        Assert.True(view.GetCaretRect().Left > afterWordLeft);
    }

    [Fact]
    public void DragSelectsTextInAReadOnlyField()
    {
        var value = new State<string>("hello world");
        var clipboard = new FakeClipboard();
        using var h = Field(value, readOnly: true, clipboard, out var view);
        h.Layout();

        // Synthetic metrics are 8px per character and 16px per line, so the first line's band is the
        // 16px under the field's top edge and column N sits at 8N from the leading edge.
        var pos = view.Position;
        var y = pos.Top - 8f;
        h.MoveTo(pos.Left + 1f, y);
        h.Press();
        h.MoveTo(pos.Left + 40f, y);
        h.Release();

        Assert.Equal("hello", view.GetSelectedText());

        h.PressKey(KeyboardKey.C, CommandModifier);
        Assert.Equal("hello", clipboard.Text);
    }

    [Fact]
    public void AReadOnlyFieldDrawsNoCaretButAnOrdinaryOneDoes()
    {
        var value = new State<string>("hello");

        using (var editable = Field(value, readOnly: false, new FakeClipboard(), out _))
        {
            editable.Render();
            Assert.Contains(editable.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFFFF0000);
        }

        using var readOnly = Field(new State<string>("hello"), readOnly: true, new FakeClipboard(), out _);
        readOnly.Render();
        Assert.DoesNotContain(readOnly.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFFFF0000);
    }

    [Fact]
    public void AReadOnlyFieldSelectionStillDraws()
    {
        var value = new State<string>("hello");
        using var h = Field(value, readOnly: true, new FakeClipboard(), out var view);

        view.SelectAll();
        h.Render();

        // The default selection fill — drawn because the field is focused, which is what makes the
        // selection visible at all in a field that shows no caret.
        Assert.Contains(h.Canvas.Rects, r => r.Inputs.Style.BackgroundColor == 0xFF8aadff);
    }

    [Fact]
    public void AReadOnlyFieldDoesNotSwallowTabOrPlainKeys()
    {
        var value = new State<string>("hello");
        var keys = UnconsumedKeysUnderAField(value, readOnly: true);

        // Tab has to keep traversing focus, and a key that types nothing has no business claiming
        // itself out of the app's single-key bindings.
        Assert.Contains(KeyboardKey.Tab, keys);
        Assert.Contains(KeyboardKey.J, keys);
        Assert.Equal("hello", value.Value);
    }

    [Fact]
    public void AnOrdinaryFieldStillClaimsPlainKeysAsText()
    {
        var value = new State<string>("hello");
        var keys = UnconsumedKeysUnderAField(value, readOnly: false);

        // The counterpart: an editable field owns its letter keys, or every keystroke would also fire
        // the app's single-key bindings. Tab still bubbles in both.
        Assert.Contains(KeyboardKey.Tab, keys);
        Assert.DoesNotContain(KeyboardKey.J, keys);
    }

    // Presses Tab and J over a field sitting under an ancestor key handler, and reports which of them
    // reached that ancestor unclaimed.
    private static List<KeyboardKey> UnconsumedKeysUnderAField(State<string> value, bool readOnly)
    {
        var keys = new List<KeyboardKey>();

        using var h = GuiTestHarness.Create(ctx => new KbmInput
        {
            OnKey = (ref KeyboardKeyEvent e) =>
            {
                if (e.Phase != EventPhase.Bubbling) return;
                if (e.State != InputState.Pressed) return;
                if (!e.IsConsumed) keys.Add(e.Key);
            },
            Child = new Box
            {
                Children =
                [
                    new TextInput
                    {
                        Id = "field",
                        Value = value,
                        AutoFocus = true,
                        ReadOnly = readOnly,
                    },
                ],
            },
        }.BuildView(ctx));

        var field = h.Get("field");
        h.MoveTo(field.Position.Center.X, field.Position.Center.Y);
        h.PressKey(KeyboardKey.Tab);
        h.PressKey(KeyboardKey.J);
        return keys;
    }
}
