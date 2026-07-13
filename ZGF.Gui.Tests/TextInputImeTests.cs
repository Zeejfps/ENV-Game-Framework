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
/// CJK is typed through an IME: the user types romaji/pinyin/jamo, the IME shows a composition
/// (preedit) and a candidate list, and text exists only once a candidate is committed. Two things
/// must hold, and both fail silently.
///
/// <para>The composition is not the field's value. It is half-typed pinyin — if it reaches
/// <c>TextValue</c>, whatever is bound to the field (a commit message, a search box) sees garbage.</para>
///
/// <para>While composing, the keys belong to the IME. Enter picks a candidate; if it also reaches the
/// app it submits the commit mid-word. That one is destructive, and it is guarded by an unmerged
/// upstream GLFW patch, so it is pinned here rather than trusted.</para>
/// </summary>
public class TextInputImeTests
{
    private static GuiTestHarness Field(State<string> value, TextWrap wrap = TextWrap.NoWrap) =>
        GuiTestHarness.Create(ctx => new TextInput
        {
            Id = "field",
            Value = value,
            Wrap = wrap,
            AutoFocus = true,
        }.BuildView(ctx));

    /// <summary>Everything the tree actually painted, so a test can tell a rendered composition from
    /// a committed one.</summary>
    private static string DrawnText(GuiTestHarness h) =>
        string.Concat(h.Render().Texts.Select(t => t.Inputs.Text));

    /// <summary>
    /// A text field beneath an ancestor key handler, which is the shape of the real app: the commit
    /// box sits inside the root view that carries <c>AppKeybindController</c>. The handler must be on
    /// an ancestor <em>view</em> — <see cref="KbmInput"/> attaches to its child's view, so it wraps a
    /// <see cref="Box"/> rather than the field itself.
    /// </summary>
    private static GuiTestHarness FieldUnderKeybinds(State<string> value, List<KeyboardKey> keysSeen) =>
        GuiTestHarness.Create(ctx => new KbmInput
        {
            OnKey = (ref KeyboardKeyEvent e) =>
            {
                if (e.State == InputState.Pressed)
                    keysSeen.Add(e.Key);
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
                    },
                ],
            },
        }.BuildView(ctx));

    /// <summary>
    /// Puts the cursor on the field. Load-bearing, not cosmetic: the dispatch path through the
    /// ancestors is built from the hover chain, so with the pointer parked nowhere no ancestor
    /// handler is in it — and a test asserting that keys do <em>not</em> reach the app would pass no
    /// matter what the field did. <see cref="KeysAfterComposing_ReachTheAppAgain"/> is the control
    /// that keeps these honest.
    /// </summary>
    private static void HoverField(GuiTestHarness h) => h.ClickOn("field");

    [Fact]
    public void Composing_DoesNotChangeTheValue()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.SendComposition("n");
        h.SendComposition("ni");
        h.SendComposition("ni'hao");

        Assert.Equal("", value.Value);
    }

    [Fact]
    public void Commit_InsertsTheCommittedText_NotTheComposition()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Compose("ni'hao", "你好");

        Assert.Equal("你好", value.Value);
    }

    /// <summary>Cancel and commit end the composition identically — an empty preedit either way. The
    /// difference is only that no text follows a cancel, so ending must never insert the preedit: do
    /// that and a cancel types the raw pinyin, and a commit types it twice.</summary>
    [Fact]
    public void Cancel_InsertsNothing()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.SendComposition("ni'hao");
        h.EndComposition();

        Assert.Equal("", value.Value);
    }

    [Fact]
    public void Composition_IsVisibleWhileComposing_ThenReplacedByTheCommit()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.SendComposition("ni'hao");
        Assert.Contains("ni'hao", DrawnText(h));

        h.EndComposition();
        h.SendText(new System.Text.Rune('你'));
        h.SendText(new System.Text.Rune('好'));

        var drawn = DrawnText(h);
        Assert.Contains("你好", drawn);
        Assert.DoesNotContain("ni'hao", drawn);
    }

    /// <summary>The destructive one. Enter selects a candidate; it must not also submit the commit.</summary>
    [Fact]
    public void EnterWhileComposing_DoesNotReachTheApp()
    {
        var value = new State<string>("");
        var keysSeen = new List<KeyboardKey>();
        using var h = FieldUnderKeybinds(value, keysSeen);
        HoverField(h);

        h.SendComposition("ni'hao");
        h.PressKey(KeyboardKey.Enter);

        Assert.DoesNotContain(KeyboardKey.Enter, keysSeen);
    }

    [Fact]
    public void KeysWhileComposing_DoNotReachTheApp()
    {
        var value = new State<string>("");
        var keysSeen = new List<KeyboardKey>();
        using var h = FieldUnderKeybinds(value, keysSeen);
        HoverField(h);

        h.SendComposition("ni");
        h.PressKey(KeyboardKey.Space);
        h.PressKey(KeyboardKey.Escape);
        h.PressKey(KeyboardKey.DownArrow);
        h.PressKey(KeyboardKey.Alpha1);

        Assert.Empty(keysSeen);
    }

    /// <summary>Once the composition is over the field is a normal field again — the gate must not latch.</summary>
    [Fact]
    public void KeysAfterComposing_ReachTheAppAgain()
    {
        var value = new State<string>("");
        var keysSeen = new List<KeyboardKey>();
        using var h = FieldUnderKeybinds(value, keysSeen);
        HoverField(h);

        h.SendComposition("ni");
        h.EndComposition();
        h.PressKey(KeyboardKey.Enter);

        Assert.Contains(KeyboardKey.Enter, keysSeen);
    }

    [Fact]
    public void KeysWhileComposing_DoNotEditTheBuffer()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("abc");
        h.SendComposition("ni");
        h.PressKey(KeyboardKey.Backspace);
        h.PressKey(KeyboardKey.LeftArrow);
        h.EndComposition();

        Assert.Equal("abc", value.Value);
    }

    [Fact]
    public void Composition_EntersAtTheCaret_NotTheEnd()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("ab");
        h.PressKey(KeyboardKey.LeftArrow);
        h.Compose("ni", "你");

        Assert.Equal("a你b", value.Value);
    }

    [Fact]
    public void Composition_ReplacesTheSelection()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("old");
        h.PressKey(KeyboardKey.A, InputModifiers.Control);
        h.Compose("ni'hao", "你好");

        Assert.Equal("你好", value.Value);
    }

    /// <summary>Replacing a selection is a real edit even though the composition is not, so the value
    /// has to follow it. Miss this and cancelling leaves the field rendering empty while whatever is
    /// bound to it still holds the text that was replaced.</summary>
    [Fact]
    public void ComposingOverASelection_ClearsTheValue_EvenIfCancelled()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.Type("old");
        h.PressKey(KeyboardKey.A, InputModifiers.Control);
        h.SendComposition("ni");

        Assert.Equal("", value.Value);

        h.EndComposition();

        Assert.Equal("", value.Value);
        Assert.DoesNotContain("old", DrawnText(h));
    }

    /// <summary>A composition must not outlive the field it was typed into.</summary>
    [Fact]
    public void Blur_DropsTheComposition()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.SendComposition("ni'hao");
        h.Click(400, 400);

        Assert.Equal("", value.Value);
        Assert.DoesNotContain("ni'hao", DrawnText(h));
    }

    /// <summary>Korean composes in place: each jamo replaces the last, and a syllable commits when the
    /// next one starts — so commits and compositions interleave rather than alternating cleanly.</summary>
    [Fact]
    public void Korean_InterleavedCommitAndComposition()
    {
        var value = new State<string>("");
        using var h = Field(value);

        h.SendComposition("ㄱ");
        h.SendComposition("가");
        h.EndComposition();
        h.SendText(new System.Text.Rune('가'));

        h.SendComposition("ㄴ");
        h.SendComposition("나");
        h.EndComposition();
        h.SendText(new System.Text.Rune('나'));

        Assert.Equal("가나", value.Value);
    }

    /// <summary>The preedit carries clause boundaries so they can be underlined. Astral characters take
    /// two UTF-16 chars but one code point, so block offsets have to survive the conversion.</summary>
    [Fact]
    public void PreeditBlocks_SurviveAstralCharacters()
    {
        var value = new State<string>("");
        using var h = Field(value);

        const string composition = "𠮷野家";
        h.SendComposition(composition, caret: composition.Length,
            blocks: [new PreeditBlock(0, 2), new PreeditBlock(2, 2)], focusedBlock: 1);

        Assert.Contains(composition, DrawnText(h));
        Assert.Equal("", value.Value);
    }

    /// <summary>Preedit is purely additive: a keyboard that never composes must be untouched by it.</summary>
    [Fact]
    public void LatinTyping_IsUnaffected()
    {
        var value = new State<string>("");
        var keysSeen = new List<KeyboardKey>();
        using var h = FieldUnderKeybinds(value, keysSeen);
        HoverField(h);

        h.Type("fix bug");
        h.PressKey(KeyboardKey.Enter);

        Assert.Equal("fix bug", value.Value);
        Assert.Contains(KeyboardKey.Enter, keysSeen);
    }
}
