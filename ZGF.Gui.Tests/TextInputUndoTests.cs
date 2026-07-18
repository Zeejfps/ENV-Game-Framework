using System.Runtime.InteropServices;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Undo/redo lives on the field: it snapshots (text, caret, selection) before each edit and
/// coalesces a contiguous run of same-kind edits into one step, so a single undo reverts a burst of
/// typing rather than one character. The stack is exercised through the public
/// <see cref="TextInputView.Undo"/>/<see cref="TextInputView.Redo"/> — the same methods the
/// controller routes Cmd/Ctrl+Z to — so these assertions don't depend on the platform modifier.
/// </summary>
public class TextInputUndoTests
{
    private static GuiTestHarness Field(State<string> value, out TextInputView view)
    {
        var h = GuiTestHarness.Create(ctx => new TextInput
        {
            Id = "field",
            Value = value,
            AutoFocus = true,
        }.BuildView(ctx));
        view = (TextInputView)h.Get("field");
        return h;
    }

    [Fact]
    public void TypingCoalescesIntoASingleUndo()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("hello");
        Assert.True(view.Undo());

        Assert.Equal("", value.Value);
    }

    [Fact]
    public void RedoReappliesAnUndoneEdit()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("hello");
        view.Undo();
        Assert.True(view.Redo());

        Assert.Equal("hello", value.Value);
    }

    [Fact]
    public void TypingAndDeletingAreSeparateUndoSteps()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("hello");
        h.PressKey(KeyboardKey.Backspace); // "hell"
        h.PressKey(KeyboardKey.Backspace); // "hel" — coalesces with the delete above
        Assert.Equal("hel", value.Value);

        view.Undo(); // undo the delete run
        Assert.Equal("hello", value.Value);

        view.Undo(); // undo the typing run
        Assert.Equal("", value.Value);
    }

    [Fact]
    public void PasteIsItsOwnUndoStep()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("foo");
        view.Enter("bar"); // a multi-rune insert is a paste, not typing → its own step
        Assert.Equal("foobar", value.Value);

        view.Undo();
        Assert.Equal("foo", value.Value);

        view.Undo();
        Assert.Equal("", value.Value);
    }

    [Fact]
    public void UndoRestoresTheCaretToWhereTheEditHappened()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("abc");
        h.PressKey(KeyboardKey.LeftArrow); // caret between 'b' and 'c'
        h.Type("X");                       // "abXc" — a fresh step (caret jumped)
        Assert.Equal("abXc", value.Value);

        view.Undo(); // removes X and puts the caret back where X was typed
        Assert.Equal("abc", value.Value);

        h.Type("Y"); // inserts at the restored caret, not at the end
        Assert.Equal("abYc", value.Value);
    }

    [Fact]
    public void ANewEditClearsTheRedoStack()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("hello");
        view.Undo();          // ""
        h.Type("x");          // branches the timeline
        Assert.False(view.Redo());
        Assert.Equal("x", value.Value);
    }

    [Fact]
    public void SetTextStartsANewDocumentAndDropsHistory()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("hello");
        view.SetText("fresh"); // wholesale replacement — a new document

        Assert.False(view.Undo());
        Assert.Equal("fresh", value.Value);
    }

    [Fact]
    public void UndoAfterImeCommitOverASelectionCanRecoverTheSelectedText()
    {
        var value = new State<string>("");
        using var h = Field(value, out var view);

        h.Type("foo");
        view.SelectAll();
        h.Compose("ni", "你"); // compose over the selection, commit a CJK character
        Assert.Equal("你", value.Value);

        view.Undo(); // undo the commit
        Assert.Equal("", value.Value);

        view.Undo(); // undo the selection-delete the composition performed
        Assert.Equal("foo", value.Value);
    }

    [Fact]
    public void ControllerRoutesTheUndoRedoChord()
    {
        // The controller maps the chord to the view's methods; the modifier is Cmd (Super) on macOS
        // and Ctrl elsewhere, so pick the platform's to exercise the real key path here.
        var mod = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? InputModifiers.Super
            : InputModifiers.Control;

        var value = new State<string>("");
        using var h = Field(value, out _);

        h.Type("hello");
        h.PressKey(KeyboardKey.Z, mod);
        Assert.Equal("", value.Value);

        h.PressKey(KeyboardKey.Z, mod | InputModifiers.Shift); // redo
        Assert.Equal("hello", value.Value);
    }
}
