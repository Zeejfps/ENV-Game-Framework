using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class CodeViewTests
{
    private const float ViewHeight = 300f - 2 * CodeViewDriver.Origin;

    private static (CodeViewDriver View, int Line, int Column) GreeterAt(string text)
    {
        var view = CodeViewDriver.Showing(CodeViewDriver.Greeter(), height: 1200f);
        var (line, column) = view.Document.Find(text);
        return (view, line, column);
    }

    private static CodeViewDriver ThreeLines() => CodeViewDriver.Showing(DecompiledDocument.FromText("t", "alpha beta\ngamma delta\nepsilon"));

    [Fact]
    public void Only_the_lines_in_view_are_recorded_in_a_100k_line_document()
    {
        var view = new CodeViewDriver(CodeViewDriver.Numbered(100_000));

        view.Run(2);

        var drawn = view.DrawnText().ToList();
        Assert.Contains(("line 0", CodeViewDriver.Origin), drawn);
        Assert.InRange(drawn.Count, (int)(ViewHeight / CodeViewDriver.Row), (int)(ViewHeight / CodeViewDriver.Row) + 4);
        Assert.InRange(view.Frames.Canvas.GlyphRuns.Count, drawn.Count, drawn.Count * 2);
    }

    [Fact]
    public void Lines_are_drawn_in_the_syntax_colors_of_the_provided_palette()
    {
        var (view, line, column) = GreeterAt("public class Greeter");

        view.Run();
        var dark = view.Frames.Canvas.GlyphRuns.First(r => r.Text == "public").Style.TextColor.Value;
        view.Frames.Run(1f / 60f, ui =>
        {
            ui.State.Theme = UiTheme.Light;
            ui.State.CodeViewPalette = CodeViewPalette.Light;
            ui.CodeView("code", view.Document, view.State);
            ui.State.Theme = UiTheme.Dark;
            ui.State.CodeViewPalette = CodeViewPalette.Dark;
        });
        var light = view.Frames.Canvas.GlyphRuns.First(r => r.Text == "public").Style.TextColor.Value;

        Assert.Equal(SpanKind.Keyword, view.Document.SpanAt(line, column).Kind);
        Assert.Equal(CodeViewPalette.Dark[SpanKind.Keyword], dark);
        Assert.Equal(CodeViewPalette.Light[SpanKind.Keyword], light);
    }

    [Fact]
    public void Tab_reaches_the_code_view_with_a_focus_ring_that_a_click_does_not_show()
    {
        var view = CodeViewDriver.Showing(CodeViewDriver.Numbered(20));
        static bool Ring(CodeViewDriver v) => v.Frames.Canvas.Rects.Any(r => r.Inputs.Style.BorderColor.Top.Value == UiTheme.Dark.FocusRing);
        Assert.False(Ring(view));

        view.Key(KeyboardKey.Tab).Frame();
        Assert.True(Ring(view));
        view.Key(KeyboardKey.A, KeyModifiers.Control);
        view.Key(KeyboardKey.C, KeyModifiers.Control);
        Assert.StartsWith("line 0", view.Frames.Clipboard.Text);

        view.Click(2, 1);
        view.Run();
        Assert.False(Ring(view));
    }

    [Fact]
    public void Hovering_a_reference_underlines_it_with_a_hand_cursor()
    {
        var (view, line, column) = GreeterAt("Greet(2)");

        view.MoveTo(line, column + 1);

        Assert.Equal(MouseCursor.Hand, view.Frames.Cursor);
        var underlined = Assert.Single(view.Frames.Canvas.GlyphRuns, r => r.Underline);
        Assert.Equal("Greet", underlined.Text);
    }

    [Fact]
    public void Clicking_a_reference_navigates_to_it()
    {
        var (view, line, column) = GreeterAt("Greet(2)");

        var result = view.Click(line, column + 2);

        Assert.NotNull(result.Navigate);
        Assert.Equal(view.Document.SpanAt(line, column).Reference, result.Navigate);
        Assert.Equal(CodeReferenceKind.Method, result.Navigate!.Kind);
        Assert.False(result.NavigateInNewTab);
    }

    [Fact]
    public void Ctrl_click_and_middle_click_navigate_in_a_new_tab()
    {
        var (view, line, column) = GreeterAt("Greet(2)");

        var ctrl = view.Click(line, column, modifiers: KeyModifiers.Control);
        view.Frames.Wait(1);
        var middle = view.Click(line, column, PointerButton.Middle);

        Assert.True(ctrl.NavigateInNewTab);
        Assert.Equal(view.Document.SpanAt(line, column).Reference, ctrl.Navigate);
        Assert.True(middle.NavigateInNewTab);
        Assert.Equal(view.Document.SpanAt(line, column).Reference, middle.Navigate);
    }

    [Fact]
    public void Clicking_a_local_highlights_its_occurrences_without_navigating()
    {
        var (view, line, column) = GreeterAt("text = ");

        var result = view.Click(line, column);
        view.Frames.MovePointer(1000f, 1000f).Frame();

        Assert.Null(result.Navigate);
        var occurrence = CodeViewPalette.Dark.Occurrence;
        var expected = view.Document.GetText().Split('\n').Sum(l => CountWord(l, "text"));
        Assert.True(expected >= 3);
        Assert.Equal(expected, view.Frames.Canvas.Rects.Count(r => r.Inputs.Style.BackgroundColor == occurrence));
    }

    [Fact]
    public void Dragging_selects_and_ctrl_c_copies_partial_first_and_last_lines()
    {
        var view = ThreeLines();

        view.MoveTo(0, 6).Press().Frame();
        view.MoveTo(1, 3);
        view.MoveTo(2, 3).Release().Frame();
        view.Key(KeyboardKey.C, KeyModifiers.Control);

        Assert.Equal("beta\ngamma delta\neps", view.Frames.Clipboard.Text);
    }

    [Fact]
    public void Shift_click_extends_and_ctrl_a_selects_everything()
    {
        var view = ThreeLines();

        view.Click(0, 2);
        view.Frames.Wait(1);
        view.Click(1, 5, modifiers: KeyModifiers.Shift);
        var extended = view.State.SelectedText;
        view.Key(KeyboardKey.A, KeyModifiers.Control);

        Assert.Equal("pha beta\ngamma", extended);
        Assert.Equal("alpha beta\ngamma delta\nepsilon", view.State.SelectedText);
    }

    [Fact]
    public void Double_click_selects_a_word_and_triple_click_a_line()
    {
        var view = ThreeLines();

        view.Click(1, 8);
        view.Click(1, 8);
        var word = view.State.SelectedText;
        view.Click(1, 8);
        var line = view.State.SelectedText;

        Assert.Equal("delta", word);
        Assert.Equal("gamma delta\n", line);
    }

    [Fact]
    public void Dragging_past_the_bottom_scrolls_and_keeps_extending_the_selection()
    {
        var view = CodeViewDriver.Showing(CodeViewDriver.Numbered(1000));

        view.MoveTo(0, 0).Press().Frame();
        view.Frames.MovePointer(100f, 400f).Frame(30);
        var requested = view.Frames.UpdateRequested;
        view.Frames.Release().Frame();

        Assert.True(requested);
        Assert.True(view.State.Selection.End.Line > ViewHeight / CodeViewDriver.Row + 5);
        Assert.DoesNotContain(view.DrawnText(), d => d.Text == "line 0");
    }

    [Fact]
    public void Find_counts_matches_and_steps_with_wrap_both_ways()
    {
        var view = CodeViewDriver.Showing(DecompiledDocument.FromText("t", "foo\nbar foo\nbaz\nfoo foo"));
        view.Click(2, 0);

        view.Key(KeyboardKey.F, KeyModifiers.Control);
        view.Frames.Type("foo").Frame();
        var first = (view.Frames.Drew("3 of 4"), view.State.Selection.Start);
        view.Key(KeyboardKey.Enter);
        var second = (view.Frames.Drew("4 of 4"), view.State.Selection.Start);
        view.Key(KeyboardKey.F3);
        var wrapped = (view.Frames.Drew("1 of 4"), view.State.Selection.Start);
        view.Key(KeyboardKey.Enter, KeyModifiers.Shift);
        view.Key(KeyboardKey.F3, KeyModifiers.Shift);
        var back = (view.Frames.Drew("3 of 4"), view.State.Selection.Start);

        Assert.Equal((true, new TextPosition(3, 0)), first);
        Assert.Equal((true, new TextPosition(3, 4)), second);
        Assert.Equal((true, new TextPosition(0, 0)), wrapped);
        Assert.Equal((true, new TextPosition(3, 0)), back);
    }

    [Fact]
    public void Find_scrolls_the_current_match_into_view_and_escape_closes_it()
    {
        var view = CodeViewDriver.Showing(CodeViewDriver.Numbered(1000));
        view.Click(0, 0);

        view.Key(KeyboardKey.F, KeyModifiers.Control);
        view.Frames.Type("line 700").Frame(3);
        var shown = view.DrawnText().Any(d => d.Text == "line 700");
        var counted = view.Frames.Drew("1 of 1");
        view.Key(KeyboardKey.Escape);

        Assert.True(shown);
        Assert.True(counted);
        Assert.False(view.Frames.Drew("1 of 1"));
        Assert.False(view.Frames.WantsTextInput);
    }

    [Fact]
    public void RevealLine_on_the_first_frame_scrolls_the_line_a_third_down_and_flashes_it()
    {
        var view = new CodeViewDriver(CodeViewDriver.Numbered(1000));
        view.State.RevealLine(500);

        view.Run(4);

        var (_, top) = view.DrawnText().Single(d => d.Text == "line 500");
        Assert.InRange(top, CodeViewDriver.Origin + ViewHeight / 3 - CodeViewDriver.Row, CodeViewDriver.Origin + ViewHeight / 3 + CodeViewDriver.Row);
        Assert.True(view.Frames.UpdateRequested);
    }

    [Fact]
    public void A_new_document_resets_selection_and_scroll_but_the_same_one_keeps_them()
    {
        var view = CodeViewDriver.Showing(CodeViewDriver.Numbered(1000));
        view.MoveTo(5, 0).Wheel(-20).Frame(2);
        view.Click(3, 0);
        view.Frames.Wait(1);
        view.Click(3, 2, modifiers: KeyModifiers.Shift);
        var selected = view.State.HasSelection;
        var scrolled = !view.DrawnText().Any(d => d.Text == "line 0");

        view.Document = view.Document;
        view.Run();
        var kept = view.State.HasSelection;
        view.Document = CodeViewDriver.Numbered(1000, "row ");
        view.Run(2);

        Assert.True(selected);
        Assert.True(scrolled);
        Assert.True(kept);
        Assert.False(view.State.HasSelection);
        Assert.Contains(("row 0", CodeViewDriver.Origin), view.DrawnText());
    }

    [Fact]
    public void Keys_scroll_the_focused_view_by_page_and_to_either_end()
    {
        var view = CodeViewDriver.Showing(CodeViewDriver.Numbered(1000));
        view.Click(0, 0);

        var page = (int)MathF.Floor(ViewHeight / CodeViewDriver.Row) - 1;

        view.Key(KeyboardKey.PageDown).Frame();
        var paged = view.DrawnText().Single(d => d.Text == $"line {page}").Top;
        view.Key(KeyboardKey.End).Frame();
        var end = view.DrawnText().Any(d => d.Text == "line 999");

        Assert.Equal(CodeViewDriver.Origin, paged);
        Assert.True(end);
    }

    [Fact]
    public void The_context_menu_goes_to_the_definition_under_the_pointer()
    {
        var (view, line, column) = GreeterAt("Greet(2)");

        view.Click(line, column, PointerButton.Right);
        view.Run();
        var item = view.Frames.Canvas.Texts.Single(t => t.Inputs.Text == "Go to definition").Inputs.Position;
        view.Frames.Click(item.Left + 2f, view.Frames.Height - item.Center.Y);
        var result = view.Last;

        Assert.Equal(view.Document.SpanAt(line, column).Reference, result.Navigate);
        Assert.False(result.NavigateInNewTab);
    }

    [Fact]
    public void The_context_menu_shows_the_reference_or_definition_under_the_pointer_in_metadata()
    {
        foreach (var text in new[] { "Greet(2)", "public class Greeter" })
        {
            var (view, line, column) = GreeterAt(text);
            column += text.IndexOf("Greet", StringComparison.Ordinal);

            view.Click(line, column, PointerButton.Right);
            view.Run();
            var item = view.Frames.Canvas.Texts.Single(t => t.Inputs.Text == "Show in metadata").Inputs.Position;
            view.Frames.Click(item.Left + 2f, view.Frames.Height - item.Center.Y);

            Assert.Equal(view.Document.SpanAt(line, column).Reference, view.Last.ShowInMetadata);
            Assert.Null(view.Last.Navigate);
        }
    }

    private static int CountWord(string line, string word)
    {
        var count = 0;
        for (var at = line.IndexOf(word, StringComparison.Ordinal); at >= 0; at = line.IndexOf(word, at + 1, StringComparison.Ordinal))
        {
            var before = at == 0 || !char.IsLetterOrDigit(line[at - 1]);
            var after = at + word.Length == line.Length || !char.IsLetterOrDigit(line[at + word.Length]);
            if (before && after) count++;
        }
        return count;
    }
}
