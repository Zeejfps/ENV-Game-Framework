using ZGF.Desktop;
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
        d.PumpUntil(() => kind.IsDone);
        return kind;
    }

    [Fact]
    public void Analyzing_a_member_docks_the_analyzer_and_its_analyses_find_what_uses_it()
    {
        var d = new WorkspaceDriver();
        d.OpenFixtures();

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
        var app = new AppDriver(height: 600f);
        var d = app.Driver;
        d.OpenFixtures();
        d.Workspace.Analyze(First(d));
        var analyzer = d.Workspace.Analyzer;
        var caller = (AnalyzerSymbolNode)Expand(d, analyzer.Roots[0], "Used By").Found[0];
        analyzer.View.Select(caller);
        app.Run(3);

        app.DoubleClick(app.Frames.Canvas.Texts.Last(t => t.Inputs.Text.StartsWith("Third()")).Inputs.Text);
        d.Settle();
        analyzer.Analyze(caller.Symbol);

        Assert.StartsWith("Third()", d.Workspace.ActiveTab!.Node!.Text);
        Assert.Equal(2, analyzer.Roots.Count);
        Assert.Same(analyzer.Roots[1], analyzer.View.Selected);
    }

    [Fact]
    public void Ctrl_R_analyzes_the_node_selected_in_the_tree()
    {
        var app = new AppDriver();
        var d = app.Driver;
        d.OpenFixtures();
        d.Workspace.Tree.Reveal(First(d));
        app.Run(2);

        app.Frames.Key(KeyboardKey.R, KeyModifiers.Control);
        app.Run(2);

        Assert.StartsWith("First()", Assert.Single(d.Workspace.Analyzer.Roots).Symbol.Label);
    }

    [Fact]
    public void A_reference_in_the_code_is_analyzed_once_it_is_found()
    {
        var d = new WorkspaceDriver();
        d.OpenFixtures();
        d.Show(d.Fixture("Outline"));
        var document = d.Workspace.ActiveTab!.Document;
        var (line, column) = document.Find("return First();");
        var reference = document.SpanAt(line, column + "return ".Length).Reference!;

        d.Workspace.Analyze(reference);
        d.PumpUntil(() => d.Workspace.Analyzer.Roots.Count > 0);

        Assert.StartsWith("First()", d.Workspace.Analyzer.Roots[0].Symbol.Label);
    }

    [Fact]
    public void Removing_a_root_or_clearing_the_assembly_list_empties_the_analyzer()
    {
        var d = new WorkspaceDriver();
        d.OpenFixtures();
        var analyzer = d.Workspace.Analyzer;
        d.Workspace.Analyze(First(d));
        d.Workspace.Analyze(d.Fixture("Greeter"));

        analyzer.Remove(analyzer.Roots[0]);
        var left = analyzer.Roots.Select(r => r.Symbol.Label).ToList();
        d.Workspace.ClearList();

        Assert.Equal(["Greeter"], left);
        Assert.Empty(analyzer.Roots);
    }
}
