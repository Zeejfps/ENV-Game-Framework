using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Caret movement and deletion step by grapheme cluster, not by UTF-16 code unit. Every case here
/// probes the caret indirectly — move it, then type a marker and read the buffer — so the assertions
/// stay on observable text rather than on private indices.
/// </summary>
public class TextInputBoundaryTests
{
    private const string Emoji = "😀";          // 1 rune, 2 code units
    private const string SkinTone = "👍🏽";      // 2 runes (base + modifier), 4 code units, one cluster
    private const string ExtB = "\U00020000";  // CJK Ext-B ideograph, 2 code units

    private static GuiTestHarness Field(State<string> value, TextWrap wrap = TextWrap.NoWrap) =>
        GuiTestHarness.Create(ctx => new TextInput
        {
            Id = "field",
            Value = value,
            Wrap = wrap,
            AutoFocus = true,
        }.BuildView(ctx));

    [Fact]
    public void LeftArrowStepsOverAWholeAstralChar()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type(Emoji);
        h.PressKey(KeyboardKey.LeftArrow);
        h.Type("X");

        Assert.Equal("X" + Emoji, value.Value);
    }

    [Fact]
    public void RightArrowStepsOverAWholeAstralChar()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type(Emoji);
        h.PressKey(KeyboardKey.LeftArrow);
        h.PressKey(KeyboardKey.LeftArrow);  // clamps at 0
        h.PressKey(KeyboardKey.RightArrow); // must land past the pair, not inside it
        h.Type("X");

        Assert.Equal(Emoji + "X", value.Value);
    }

    [Fact]
    public void BackspaceDeletesAWholeAstralChar()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("a" + ExtB);
        h.PressKey(KeyboardKey.Backspace);

        Assert.Equal("a", value.Value);
    }

    [Fact]
    public void BackspaceDeletesAWholeEmojiCluster()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type(SkinTone);
        h.PressKey(KeyboardKey.Backspace);

        Assert.Equal("", value.Value);
    }

    [Fact]
    public void LeftArrowStepsOverAWholeEmojiCluster()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type(SkinTone);
        h.PressKey(KeyboardKey.LeftArrow);
        h.Type("X");

        Assert.Equal("X" + SkinTone, value.Value);
    }

    [Fact]
    public void CtrlLeftMovesOneIdeographAtATime()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("中文");
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Control);
        h.Type("X");

        Assert.Equal("中X文", value.Value);
    }

    [Fact]
    public void CtrlRightMovesOneIdeographAtATime()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("中文");
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Control);
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Control); // caret at 0
        h.PressKey(KeyboardKey.RightArrow, InputModifiers.Control);
        h.Type("X");

        Assert.Equal("中X文", value.Value);
    }

    [Fact]
    public void CtrlBackspaceDeletesOneIdeograph()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("中文");
        h.PressKey(KeyboardKey.Backspace, InputModifiers.Control);

        Assert.Equal("中", value.Value);
    }

    /// <summary>The CJK rule must not leak into Latin: Ctrl+Backspace still eats a whole word.</summary>
    [Fact]
    public void CtrlBackspaceStillDeletesAWholeLatinWord()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("hello world");
        h.PressKey(KeyboardKey.Backspace, InputModifiers.Control);

        Assert.Equal("hello ", value.Value);
    }

    [Fact]
    public void CtrlLeftStillJumpsAWholeLatinWord()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("hello world");
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Control);
        h.Type("X");

        Assert.Equal("hello Xworld", value.Value);
    }

    /// <summary>A Latin word boundary scan must not stop between the halves of an adjacent emoji.</summary>
    [Fact]
    public void CtrlBackspaceLeavesNoOrphanSurrogate()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("hi " + Emoji);
        h.PressKey(KeyboardKey.Backspace, InputModifiers.Control);

        Assert.DoesNotContain(value.Value, c => char.IsSurrogate(c));
    }

    // A field whose canvas measures text at a fixed 8px/char, so a click x maps to a known
    // character index — the mouse-selection tests need that where the caret-key tests don't.
    private static GuiTestHarness MeasuredField(State<string> value) =>
        GuiTestHarness.Create(ctx => new TextInput
        {
            Id = "field",
            Value = value,
            Wrap = TextWrap.NoWrap,
            AutoFocus = true,
        }.BuildView(ctx), measurer: new SyntheticTextMeasurer());

    /// <summary>Double-click selects the word under the cursor — and only the word: typing over it
    /// leaves the trailing space intact (a select-the-word gesture, not select-word-and-separators).</summary>
    [Fact]
    public void DoubleClickSelectsTheWordUnderTheCursor()
    {
        var value = new State<string>("");
        using var h = MeasuredField(value);

        h.Type("hello world");
        h.Layout();

        var field = h.Get("field");
        var y = field.Position.Top - 1f; // hit-test accepts only the top line-height band
        var x = field.Position.Left + 12f; // ~1.5 chars in → inside "hello"
        h.Click(x, y); // place caret
        h.Click(x, y); // second click within the threshold → select the word
        h.Type("X");

        Assert.Equal("X world", value.Value);
    }

    /// <summary>A third click within the threshold escalates to selecting the whole field.</summary>
    [Fact]
    public void TripleClickSelectsEverything()
    {
        var value = new State<string>("");
        using var h = MeasuredField(value);

        h.Type("hello world");
        h.Layout();

        var field = h.Get("field");
        var y = field.Position.Top - 1f; // hit-test accepts only the top line-height band
        var x = field.Position.Left + 12f;
        h.Click(x, y);
        h.Click(x, y);
        h.Click(x, y); // triple-click → select all
        h.Type("X");

        Assert.Equal("X", value.Value);
    }
}
