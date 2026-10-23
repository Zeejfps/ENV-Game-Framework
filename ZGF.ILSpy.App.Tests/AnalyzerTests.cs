using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Testing;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class AnalyzerTests
{
    private static BrowseNode First(WorkspaceDriver d) => d.Fixture("Outline").Children.Single(c => c.Text.StartsWith("First()"));

    private static AnalyzerKindNode Expand(WorkspaceDriver d, AnalyzerSymbolNode symbol, string header)
    {
        var analyzer = d.Workspace.Analyzer;
        var kind = analyzer.GetChildren(symbol).OfType<AnalyzerKindNode>().Single(k => k.Kind.Header == header);
        analyzer.View.SetExpanded(kind, true);
        analyzer.GetChildren(kind);
        d.RunUntil(() => kind.IsDone);
        return kind;
    }

    [Fact]
    public void Analyzing_a_member_docks_the_analyzer_and_its_analyses_find_what_uses_it()
    {
        var d = WorkspaceDriver.Opened();

        d.Workspace.Analyze(First(d));
        var root = Assert.Single(d.Workspace.Analyzer.Roots);
        var usedBy = Expand(d, root, "Used By");

        Assert.True(d.Workspace.Layout.Contains(DockPanels.Analyzer));
        Assert.StartsWith("First()", root.Symbol.Label);
        var caller = Assert.IsType<AnalyzerSymbolNode>(Assert.Single(usedBy.Found));
        Assert.StartsWith("Third()", caller.Symbol.Label);
    }

    [Fact]
    public void A_result_opens_its_member_when_activated_and_can_be_analyzed_in_turn()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        d.Workspace.Analyze(First(d));
        var analyzer = d.Workspace.Analyzer;
        var caller = (AnalyzerSymbolNode)Expand(d, analyzer.Roots[0], "Used By").Found[0];
        analyzer.View.Select(caller);
        app.Run(3);

        app.DoubleClick(app.Frames.Canvas.Texts.Last(t => t.Inputs.Text.StartsWith("Third()")).Inputs.Text);
        d.Settle();
        analyzer.Analyze(caller.Symbol);

        Assert.StartsWith("Third()", d.Workspace.Tabs.Active!.Node!.Text);
        Assert.Equal(2, analyzer.Roots.Count);
        Assert.Same(analyzer.Roots[1], analyzer.View.Selected);
    }

    [Fact]
    public void Ctrl_R_analyzes_the_node_selected_in_the_focused_tree()
    {
        var app = AppDriver.Opened();
        var d = app.Driver;
        d.Workspace.Tree.Reveal(First(d));
        app.Run(2);
        var leftmost = app.Frames.TextRects()
            .Where(t => t.Text == First(d).Text)
            .Select(t => t.Rect)
            .MinBy(r => r.X);
        ClickAt(app, leftmost);
        d.Settle();

        app.Frames.Key(KeyboardKey.R, KeyModifiers.Control).Frame(2);

        Assert.StartsWith("First()", Assert.Single(d.Workspace.Analyzer.Roots).Symbol.Label);
    }

    [Fact]
    public void Ctrl_R_in_the_code_analyzes_the_reference_at_the_caret()
    {
        var view = CodeViewDriver.Showing(CodeViewDriver.Outline(), 1200f, frames: 2);
        var (line, column) = view.Document.Find("return First();");
        var row = view.State.Folds.RowOf(line);

        view.Click(row, column + "return First".Length);
        view.Key(KeyboardKey.R, KeyModifiers.Control);

        Assert.Equal("First", view.Last.Analyze?.Name);
    }

    [Fact]
    public void Ctrl_R_in_the_analyzer_analyzes_the_result_it_has_selected()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        ShowFirstCaller(app);
        var third = app.Frames.TextRects().Last(t => t.Text.StartsWith("Third()"));
        ClickAt(app, third.Rect).Frame();

        app.Frames.Key(KeyboardKey.R, KeyModifiers.Control).Frame(2);

        Assert.Equal(2, d.Workspace.Analyzer.Roots.Count);
        Assert.StartsWith("Third()", d.Workspace.Analyzer.Roots[1].Symbol.Label);
    }

    private static void ShowFirstCaller(AppDriver app)
    {
        var d = app.Driver;
        var analyzer = d.Workspace.Analyzer;
        d.Workspace.Analyze(First(d));
        var caller = Expand(d, analyzer.Roots[0], "Used By").Found[0];
        analyzer.View.ScrollIntoView(caller);
        app.Run(3);
    }

    private static UiFrames ClickAt(AppDriver app, UiRect row, PointerButton button = PointerButton.Left) =>
        app.Frames.Click(row.X + 4f, row.Y + row.Height / 2, button);

    [Fact]
    public void A_reference_in_the_code_is_analyzed_once_it_is_found()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Outline"));
        var document = d.Workspace.Tabs.Active!.Document;
        var (line, column) = document.Find("return First();");
        var reference = document.SpanAt(line, column + "return ".Length).Reference!;

        d.Workspace.Analyze(reference);
        d.RunUntil(() => d.Workspace.Analyzer.Roots.Count > 0);

        Assert.StartsWith("First()", d.Workspace.Analyzer.Roots[0].Symbol.Label);
    }

    [Fact]
    public void Removing_a_root_or_clearing_the_assembly_list_empties_the_analyzer()
    {
        var d = WorkspaceDriver.Opened();
        var analyzer = d.Workspace.Analyzer;
        d.Workspace.Analyze(First(d));
        d.Workspace.Analyze(d.Fixture("Greeter"));

        analyzer.Remove(analyzer.Roots[0]);
        var left = analyzer.Roots.Select(r => r.Symbol.Label).ToList();
        d.Workspace.ClearList();

        Assert.Equal(["Greeter"], left);
        Assert.Empty(analyzer.Roots);
    }

    [Fact]
    public void Its_menu_clears_the_analyzer()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        d.Workspace.Analyze(First(d));
        app.Run(3);
        var row = app.Frames.TextRects().Last(t => t.Text.StartsWith("First()")).Rect;

        ClickAt(app, row, PointerButton.Right).Frame();
        app.ClickMenuItem("Clear").Frame(2);

        Assert.Empty(d.Workspace.Analyzer.Roots);
    }

    [Fact]
    public void The_toolbar_expands_and_runs_every_analysis_of_the_roots_then_collapses_them()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        var analyzer = d.Workspace.Analyzer;
        d.Workspace.Analyze(First(d));
        app.Run(3);
        var root = analyzer.Roots[0];
        var kinds = analyzer.GetChildren(root).OfType<AnalyzerKindNode>().ToList();

        app.Click(LucideIcons.ChevronsUpDown).Frame();
        var expanded = kinds.All(k => analyzer.View.IsExpanded(k));
        d.RunUntil(() => kinds.All(k => k.IsDone));
        app.Click(LucideIcons.ChevronsDownUp).Frame();

        Assert.NotEmpty(kinds);
        Assert.True(expanded);
        Assert.False(analyzer.View.IsExpanded(root));
    }

    [Fact]
    public void The_toolbar_analyzes_the_selected_result()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        var analyzer = d.Workspace.Analyzer;
        d.Workspace.Analyze(First(d));
        var caller = (AnalyzerSymbolNode)Expand(d, analyzer.Roots[0], "Used By").Found[0];
        analyzer.View.Select(caller);
        app.Run(3);

        app.Click(LucideIcons.ScanSearch).Frame();

        Assert.Equal(2, analyzer.Roots.Count);
        Assert.StartsWith("Third()", analyzer.Roots[1].Symbol.Label);
    }

    [Fact]
    public void The_toolbar_removes_the_analysis_the_selection_is_in_or_every_analysis()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        var analyzer = d.Workspace.Analyzer;
        d.Workspace.Analyze(First(d));
        var caller = (AnalyzerSymbolNode)Expand(d, analyzer.Roots[0], "Used By").Found[0];
        d.Workspace.Analyze(d.Fixture("Greeter"));
        analyzer.View.Select(caller);
        app.Run(3);

        app.Click(LucideIcons.Trash2).Frame(2);
        var left = analyzer.Roots.Select(r => r.Symbol.Label).ToList();
        app.Click(LucideIcons.ListX).Frame(2);

        Assert.Equal(["Greeter"], left);
        Assert.Empty(analyzer.Roots);
    }

    [Fact]
    public void Delete_in_the_analyzer_removes_the_analysis_the_selection_is_in()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        ShowFirstCaller(app);
        var third = app.Frames.TextRects().Last(t => t.Text.StartsWith("Third()"));
        ClickAt(app, third.Rect).Frame();

        app.Frames.Key(KeyboardKey.Delete).Frame(2);

        Assert.Empty(d.Workspace.Analyzer.Roots);
    }
}
