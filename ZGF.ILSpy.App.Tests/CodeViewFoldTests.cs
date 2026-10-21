using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class CodeViewFoldTests
{
    private static CodeViewDriver Outline(float height = 1200f)
    {
        var view = new CodeViewDriver(CodeViewDriver.Outline(), height);
        view.Run();
        view.Run();
        return view;
    }

    private static int Fold(DecompiledDocument document, string startsWith) =>
        document.FoldStartingAt(document.Find(startsWith).Line);

    private static List<string> Numbers(CodeViewDriver view) =>
        view.Frames.Canvas.GlyphRuns.Where(r => r.Origin.X < view.TextLeft - 0.5f && char.IsDigit(r.Text[0]))
            .OrderByDescending(r => r.Origin.Y).Select(r => r.Text).ToList();

    private static List<string> Rows(CodeViewDriver view) =>
        view.Frames.Canvas.GlyphRuns
            .Select(r => (r.Text, r.Origin.X, Top: view.Frames.Height - r.Origin.Y))
            .Where(r => r.X >= view.TextLeft - 0.5f && r.Top >= CodeViewDriver.Origin - 0.5f)
            .GroupBy(r => r.Top).OrderBy(g => g.Key)
            .Select(g => string.Concat(g.OrderBy(r => r.X).Select(r => r.Text))).ToList();

    private static void ClickChevron(CodeViewDriver view, int row)
    {
        var (x, y) = view.ChevronOf(row);
        view.Frames.MovePointer(x, y);
        view.Run();
        view.Frames.Press();
        view.Run();
        view.Frames.Release();
        view.Run();
        view.Frames.Wait(1);
    }

    [Fact]
    public void The_usings_start_collapsed_behind_a_chip_and_line_numbers_stay_real()
    {
        var view = Outline();
        var document = view.Document;

        var numbers = Numbers(view);
        var rows = Rows(view);

        Assert.Equal(["1", "3", "4"], numbers.Take(3));
        Assert.Equal("using System.Collections.Generic;…", rows[0]);
        Assert.DoesNotContain(rows, r => r.Contains("using System.Text;"));
        Assert.True(view.State.IsCollapsed(Fold(document, "using System.Collections.Generic;")));
    }

    [Fact]
    public void Clicking_a_chevron_folds_a_body_onto_its_signature_and_clicking_the_chip_unfolds_it()
    {
        var view = Outline();
        var document = view.Document;
        var first = document.Find("public int First()").Line;
        var row = first - 1;

        var (x, y) = view.ChevronOf(row);
        view.Frames.MovePointer(x, y);
        view.Run();
        var hand = view.Frames.Cursor;
        var lit = view.Frames.Canvas.Lines.Count(l => l.Inputs.Color == UiTheme.Dark.Text);
        ClickChevron(view, row);
        var folded = Rows(view);
        var numbers = Numbers(view);

        Assert.Equal(MouseCursor.Hand, hand);
        Assert.Equal(2, lit);
        Assert.True(view.State.IsCollapsed(Fold(document, "public int First()")));
        Assert.Contains("    public int First(){ … }", folded);
        Assert.DoesNotContain(folded, r => r.Contains("int num = 0;"));
        Assert.Equal((first + 1).ToString(), numbers[row]);
        Assert.Equal((document.Folds[Fold(document, "public int First()")].EndLine + 2).ToString(), numbers[row + 1]);

        view.Click(row, document.Lines[first].Text.Length + 2);

        Assert.False(view.State.IsCollapsed(Fold(document, "public int First()")));
        Assert.Contains(Rows(view), r => r.Contains("int num = 0;"));
    }

    [Fact]
    public void Ctrl_m_m_toggles_the_fold_at_the_caret_and_ctrl_shift_brackets_collapse_and_expand()
    {
        var view = Outline();
        var document = view.Document;
        var (line, column) = document.Find("stringBuilder.Append");
        var second = Fold(document, "public string Second()");
        view.Click(line - 1, column);

        view.Key(KeyboardKey.M, KeyModifiers.Control);
        view.Key(KeyboardKey.M, KeyModifiers.Control);
        var toggled = view.State.IsCollapsed(second);
        view.Key(KeyboardKey.M, KeyModifiers.Control);
        view.Key(KeyboardKey.M);
        var back = view.State.IsCollapsed(second);
        view.Key(KeyboardKey.LeftBracket, KeyModifiers.Control | KeyModifiers.Shift);
        var collapsed = view.State.IsCollapsed(second);
        view.Key(KeyboardKey.LeftBracket, KeyModifiers.Control | KeyModifiers.Shift);
        var outer = view.State.IsCollapsed(Fold(document, "public class Outline"));
        view.Key(KeyboardKey.RightBracket, KeyModifiers.Control | KeyModifiers.Shift);
        view.Key(KeyboardKey.RightBracket, KeyModifiers.Control | KeyModifiers.Shift);

        Assert.True(toggled);
        Assert.False(back);
        Assert.True(collapsed);
        Assert.True(outer);
        Assert.False(view.State.IsCollapsed(second));
        Assert.False(view.State.IsCollapsed(Fold(document, "public class Outline")));
    }

    [Fact]
    public void The_context_menu_collapses_and_expands_everything_and_toggles_the_fold_under_the_pointer()
    {
        var view = Outline();
        var document = view.Document;

        void Pick(int row, string item)
        {
            view.Click(row, 2, PointerButton.Right);
            view.Run();
            var at = view.Frames.Canvas.Texts.Single(t => t.Inputs.Text == item).Inputs.Position;
            view.Frames.MovePointer(at.Left + 2f, view.Frames.Height - at.Center.Y);
            view.Run();
            view.Frames.Press();
            view.Run();
            view.Frames.Release();
            view.Run();
            view.Run();
            view.Frames.Wait(1);
        }

        Pick(0, "Collapse all");
        var all = Enumerable.Range(0, document.Folds.Length).All(view.State.IsCollapsed);
        var rowsCollapsed = Rows(view).Count;
        Pick(0, "Expand all");
        var none = Enumerable.Range(0, document.Folds.Length).All(f => !view.State.IsCollapsed(f));
        var rowsExpanded = Rows(view).Count;
        var third = document.Find("public int Third()").Line;
        Pick(third, "Toggle fold");

        Assert.True(all);
        Assert.Equal(3, rowsCollapsed);
        Assert.True(none);
        Assert.Equal(document.Lines.Count(l => l.Text.Length > 0), rowsExpanded);
        Assert.True(view.State.IsCollapsed(Fold(document, "public int Third()")));
    }

    [Fact]
    public void Copying_across_a_collapsed_fold_copies_what_it_hides()
    {
        var view = Outline();
        var document = view.Document;
        var first = document.Find("public int First()").Line;
        ClickChevron(view, first - 1);

        view.MoveTo(first - 2, 0);
        view.Frames.Press();
        view.Run();
        view.MoveTo(first + 1, 4);
        view.Frames.Release();
        view.Run();
        view.Key(KeyboardKey.C, KeyModifiers.Control);

        var end = document.Folds[Fold(document, "public int First()")].EndLine + 2;
        Assert.Equal(CodeText.Between(document, new TextPosition(first - 1, 0), new TextPosition(end, 4)), view.Frames.Clipboard.Text);
        Assert.Contains("int num = 0;", view.Frames.Clipboard.Text);
    }

    [Fact]
    public void A_find_match_inside_a_collapsed_fold_expands_it_when_it_becomes_current()
    {
        var view = Outline();
        var document = view.Document;
        var second = Fold(document, "public string Second()");
        view.State.SetCollapsed(second, true);
        view.Run();
        view.Click(0, 0);

        view.Key(KeyboardKey.F, KeyModifiers.Control);
        view.Frames.Type("needle");
        view.Run();
        view.Run();

        Assert.False(view.State.IsCollapsed(second));
        Assert.Contains(Rows(view), r => r.Contains("stringBuilder.Append(\"needle\");"));
        Assert.Equal(document.Find("\"needle\"").Line, view.State.Selection.Start.Line);
    }

    [Fact]
    public void RevealLine_into_nested_collapsed_folds_expands_them_all()
    {
        var view = Outline();
        var document = view.Document;
        var inner = Fold(document, "public class Inner");
        var deep = Fold(document, "public int Deep()");
        view.State.SetCollapsed(deep, true);
        view.State.SetCollapsed(inner, true);
        view.Run();

        view.State.RevealLine(document.Find("return 7;").Line);
        view.Run();
        view.Run();

        Assert.False(view.State.IsCollapsed(inner));
        Assert.False(view.State.IsCollapsed(deep));
        Assert.Contains(Rows(view), r => r.Contains("return 7;"));
        Assert.True(view.State.IsCollapsed(Fold(document, "using System.Collections.Generic;")));
    }

    [Fact]
    public void Folding_above_the_view_keeps_the_top_line_in_place()
    {
        var view = Outline(height: 200f);
        var document = view.Document;
        var target = document.Find("public string Second()").Line;
        view.State.Restore(new CodeViewPosition(target, 0f, default, default));
        view.Run();
        view.Run();
        var before = view.State.Position.TopLine;

        view.State.SetCollapsed(Fold(document, "public int First()"), true);
        view.Run();
        view.Run();

        Assert.Equal(target, before);
        Assert.Equal(target, view.State.Position.TopLine);
        Assert.StartsWith("    public string Second()", Rows(view)[0]);
    }

    [Fact]
    public void A_new_document_starts_from_its_own_default_folds()
    {
        var view = Outline();
        view.State.ExpandAll();
        view.Run();

        view.Document = CodeViewDriver.Outline();
        view.Run();

        Assert.True(view.State.IsCollapsed(Fold(view.Document, "using System.Collections.Generic;")));
        Assert.False(view.State.IsCollapsed(Fold(view.Document, "public int First()")));
    }
}
