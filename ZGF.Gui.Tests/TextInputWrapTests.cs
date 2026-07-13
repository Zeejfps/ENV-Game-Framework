using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Testing;
using ZGF.KeyboardModule;
using ZGF.Observable;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>
/// A multi-line field breaks lines the way <see cref="TextWrapper"/> does — at word boundaries, with
/// kinsoku for CJK, never inside a surrogate pair — instead of wherever the pixel budget ran out.
/// The synthetic measurer bills every UTF-16 code unit at 8px, so a width of N fits N/8 code units.
/// </summary>
public class TextInputWrapTests
{
    private const string ExtB = "\U00020000"; // CJK Ext-B ideograph, 2 code units

    private static GuiTestHarness Field(State<string> value, float width) =>
        GuiTestHarness.Create(ctx => new Row
        {
            Children =
            [
                new TextInput
                {
                    Id = "field",
                    Value = value,
                    Wrap = TextWrap.Wrap,
                    Width = width,
                    AutoFocus = true,
                },
            ],
        }.BuildView(ctx));

    private static string[] LinesOf(GuiTestHarness h) =>
        h.Render().Texts.Select(t => t.Inputs.Text).ToArray();

    [Fact]
    public void WrapsAtAWordBoundaryNotMidWord()
    {
        var value = new State<string>("hello world");
        using var h = Field(value, 64f);

        Assert.Equal(new[] { "hello ", "world" }, LinesOf(h));
    }

    [Fact]
    public void NeverSplitsASurrogatePairAcrossLines()
    {
        var value = new State<string>(ExtB + ExtB);
        using var h = Field(value, 24f);

        var lines = LinesOf(h);
        Assert.Equal(new[] { ExtB, ExtB }, lines);
        Assert.All(lines, l => Assert.Equal(2, l.Length));
    }

    [Fact]
    public void ClosingPunctuationDoesNotStartALine()
    {
        var value = new State<string>("あ。あ");
        using var h = Field(value, 8f);

        Assert.Equal(new[] { "あ。", "あ" }, LinesOf(h));
    }

    [Fact]
    public void HardNewlineStillStartsANewLine()
    {
        var value = new State<string>("a\nb");
        using var h = Field(value, 400f);

        Assert.Equal(new[] { "a", "b" }, LinesOf(h));
    }

    /// <summary>Up/Down navigate the same visual lines that were drawn: from the end of "world" the
    /// caret rises into "hello " at the same pixel column (40px = after the 5th char).</summary>
    [Fact]
    public void UpArrowMovesByTheWrappedLine()
    {
        var value = new State<string>("hello world");
        using var h = Field(value, 64f);

        h.PressKey(KeyboardKey.UpArrow);
        h.Type("X");

        Assert.Equal("helloX world", value.Value);
    }
}
