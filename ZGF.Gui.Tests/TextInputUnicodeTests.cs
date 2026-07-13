using System.Text;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Text entry must come from the OS text-input event, never from decoding a physical key. Physical
/// keys are layout-independent: on a Russian layout the key at the 'q' position still reports as
/// <see cref="KeyboardKey.Q"/> while the OS commits 'й'. Decoding the key yields 'q' — the bug these
/// tests pin down.
/// </summary>
public class TextInputUnicodeTests
{
    private static GuiTestHarness Field(State<string> value, TextWrap wrap = TextWrap.NoWrap) =>
        GuiTestHarness.Create(ctx => new TextInput
        {
            Id = "field",
            Value = value,
            Wrap = wrap,
            AutoFocus = true,
        }.BuildView(ctx));

    /// <summary>Exactly what GLFW reports for one keypress on a Russian layout: the physical key at
    /// the US 'q' position, plus a committed character that has nothing to do with it.</summary>
    private static void TypeOnRussianLayout(GuiTestHarness h, KeyboardKey physicalKey, char committed)
    {
        h.KeyDown(physicalKey);
        h.SendText(new Rune(committed));
        h.KeyUp(physicalKey);
    }

    [Fact]
    public void TypesCyrillic()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("привет");

        Assert.Equal("привет", value.Value);
    }

    [Fact]
    public void RussianLayout_InsertsCommittedChar_NotThePhysicalKey()
    {
        var value = new State<string>("");
        using var h = Field(value);

        TypeOnRussianLayout(h, KeyboardKey.Q, 'й');
        TypeOnRussianLayout(h, KeyboardKey.W, 'ц');

        Assert.Equal("йц", value.Value);
    }

    /// <summary>The regression guard for the original bug: a physical key alone must insert nothing.
    /// If the controller ever decodes keys into characters again, this yields "q" — and every other
    /// test here starts double-inserting.</summary>
    [Fact]
    public void PhysicalKeyWithoutTextEvent_InsertsNothing()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.PressKey(KeyboardKey.Q);
        h.PressKey(KeyboardKey.Alpha1);
        h.PressKey(KeyboardKey.Space);

        Assert.Equal("", value.Value);
    }

    [Fact]
    public void TypesAscii()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("Fix bug #42!");

        Assert.Equal("Fix bug #42!", value.Value);
    }

    [Fact]
    public void TypesAccentedLatin()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("café über größe");

        Assert.Equal("café über größe", value.Value);
    }

    [Fact]
    public void TypesAstralPlaneChars()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("ship 🚀");

        Assert.Equal("ship 🚀", value.Value);
    }

    /// <summary>An astral character is two UTF-16 code units but one character to the user. Deleting
    /// a code unit at a time would leave an orphaned surrogate — half a character, and not valid
    /// text.</summary>
    [Fact]
    public void BackspaceDeletesWholeAstralChar()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("ship 🚀");
        h.PressKey(KeyboardKey.Backspace);

        Assert.Equal("ship ", value.Value);
    }

    /// <summary>The caret must step over an astral character, never land between its halves — where
    /// the next edit would split it.</summary>
    [Fact]
    public void ArrowKeysStepOverAstralChar()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("a🚀b");
        h.PressKey(KeyboardKey.LeftArrow);   // before 'b'
        h.PressKey(KeyboardKey.LeftArrow);   // before the rocket, not inside it
        h.Type("X");

        Assert.Equal("aX🚀b", value.Value);
    }

    /// <summary>Backspace from just after an astral character removes the whole thing, leaving the
    /// text around it intact.</summary>
    [Fact]
    public void BackspaceOverAstralChar_LeavesNoOrphanSurrogate()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("a🚀b");
        h.PressKey(KeyboardKey.LeftArrow);   // caret between the rocket and 'b'
        h.PressKey(KeyboardKey.Backspace);

        Assert.Equal("ab", value.Value);
    }

    /// <summary>CJK has no spaces, so a word jump that keys off whitespace would treat a whole run as
    /// one word and skip the line.</summary>
    [Fact]
    public void WordJumpStepsThroughCjkRun()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("你好世界");
        h.PressKey(KeyboardKey.LeftArrow, InputModifiers.Control);
        h.Type("X");

        Assert.Equal("你好世X界", value.Value);
    }

    [Fact]
    public void BackspaceDeletesOneCyrillicChar()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("тест");
        h.PressKey(KeyboardKey.Backspace);

        Assert.Equal("тес", value.Value);
    }

    [Fact]
    public void SelectAllThenType_ReplacesCyrillic()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("старый");
        h.PressKey(KeyboardKey.A, InputModifiers.Control);
        h.Type("новый");

        Assert.Equal("новый", value.Value);
    }

    [Fact]
    public void EnterInsertsNewline_OnlyWhenMultiLine()
    {
        var multi = new State<string>("");
        using (var h = Field(multi, TextWrap.Wrap))
        {
            h.Type("одна");
            h.PressKey(KeyboardKey.Enter);
            h.Type("две");
            Assert.Equal("одна\nдве", multi.Value);
        }

        var single = new State<string>("");
        using (var h = Field(single))
        {
            h.Type("одна");
            h.PressKey(KeyboardKey.Enter);
            h.Type("две");
            Assert.Equal("однадве", single.Value);
        }
    }
}
