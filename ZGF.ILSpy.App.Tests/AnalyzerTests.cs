using System.Reflection;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Testing;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Assemblies;

namespace ZGF.ILSpy.App.Tests;

public sealed class AnalyzerTests
{
    private static BrowseNode First(AppStateDriver d) => d.Fixture("Outline").Children.Single(c => c.Text.StartsWith("First()"));

    private static AnalyzerKindNode Expand(AppStateDriver d, AnalyzerSymbolNode symbol, string header)
    {
        var rows = d.Ui.Analyzer;
        var kind = rows.GetChildren(symbol).OfType<AnalyzerKindNode>().Single(k => k.Kind.Header == header);
        rows.View.SetExpanded(kind, true);
        rows.GetChildren(kind);
        d.RunUntil(() => kind.IsDone);
        return kind;
    }

    [Fact]
    public void Analyzing_a_member_docks_the_analyzer_and_its_analyses_find_what_uses_it()
    {
        var d = AppStateDriver.Opened();

        d.Analyze(First(d));
        var root = Assert.Single(d.AppState.Analyzer.Found());
        var usedBy = Expand(d, root, "Used By");

        Assert.True(d.Ui.Layout.Contains(DockPanels.Analyzer));
        Assert.StartsWith("First()", root.Symbol.Label);
        var caller = Assert.IsType<AnalyzerSymbolNode>(Assert.Single(usedBy.Found));
        Assert.StartsWith("Third()", caller.Symbol.Label);
    }

    [Theory]
    [InlineData("Count", "Increment()")]
    [InlineData("Changed", "Watch(")]
    public void Used_By_a_property_or_event_finds_each_member_using_its_accessors_once(string member, string user)
    {
        var d = AppStateDriver.Opened();
        var node = d.Fixture("Counter").Children.Single(c => c.Text.StartsWith(member + " "));

        d.Analyze(node);
        var usedBy = Expand(d, Assert.Single(d.AppState.Analyzer.Found()), "Used By");

        var found = Assert.IsType<AnalyzerSymbolNode>(Assert.Single(usedBy.Found));
        Assert.StartsWith(user, found.Symbol.Label);
    }

    [Fact]
    public void Applied_To_finds_members_that_carry_a_security_attribute()
    {
        var d = AppStateDriver.Opened();

        d.Analyze(d.Fixture("GuardAttribute"));
        var appliedTo = Expand(d, Assert.Single(d.AppState.Analyzer.Found()), "Applied To");

        var member = Assert.IsType<AnalyzerSymbolNode>(Assert.Single(appliedTo.Found));
        Assert.StartsWith("Enter()", member.Symbol.Label);
    }

    [Fact]
    public void A_result_opens_its_member_when_activated_and_can_be_analyzed_in_turn()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        d.Analyze(First(d));
        var analyzer = d.AppState.Analyzer;
        var caller = (AnalyzerSymbolNode)Expand(d, analyzer.Found()[0], "Used By").Found[0];
        app.Ui.Analyzer.View.Select(caller);
        app.Run(3);

        app.DoubleClick(app.Frames.Canvas.Texts.Last(t => t.Inputs.Text.StartsWith("Third()")).Inputs.Text);
        d.Settle();
        app.Ui.Analyzer.Analyze(caller.Symbol);

        Assert.StartsWith("Third()", d.AppState.Tabs.Active!.Node!.Text);
        Assert.Equal(2, analyzer.Roots.Count);
        Assert.Same(analyzer.Roots[1], app.Ui.Analyzer.View.Selected);
    }

    [Fact]
    public void Ctrl_R_analyzes_the_node_selected_in_the_focused_tree()
    {
        var app = AppDriver.Opened();
        var d = app.Driver;
        d.Ui.Tree.Reveal(First(d));
        app.Run(2);
        var leftmost = app.Frames.TextRects()
            .Where(t => t.Text == First(d).Text)
            .Select(t => t.Rect)
            .MinBy(r => r.X);
        ClickAt(app, leftmost);
        d.Settle();

        app.Frames.Key(KeyboardKey.R, KeyModifiers.Control).Frame(2);

        Assert.StartsWith("First()", Assert.Single(d.AppState.Analyzer.Found()).Symbol.Label);
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

        Assert.Equal(2, d.AppState.Analyzer.Roots.Count);
        Assert.StartsWith("Third()", d.AppState.Analyzer.Found()[1].Symbol.Label);
    }

    private static void ShowFirstCaller(AppDriver app)
    {
        var d = app.Driver;
        var analyzer = d.AppState.Analyzer;
        d.Analyze(First(d));
        var caller = Expand(d, analyzer.Found()[0], "Used By").Found[0];
        d.Ui.Analyzer.View.ScrollIntoView(caller);
        app.Run(3);
    }

    private static UiFrames ClickAt(AppDriver app, UiRect row, PointerButton button = PointerButton.Left) =>
        app.Frames.Click(row.X + 4f, row.Y + row.Height / 2, button);

    [Fact]
    public void A_reference_in_the_code_is_analyzed_once_it_is_found()
    {
        var d = AppStateDriver.OpenedWithoutScreen();
        d.Show(d.Fixture("Outline"));
        var document = d.AppState.Tabs.Active!.Document;
        var (line, column) = document.Find("return First();");
        var reference = document.SpanAt(line, column + "return ".Length).Reference!;
        d.TakeViewCommands();

        AddToAnalyzer(d.AppState, reference);
        d.Settle();

        var root = Assert.Single(d.AppState.Analyzer.Found());
        Assert.StartsWith("First()", root.Symbol.Label);
        Assert.Equal([new ViewCommand.ShowAnalysis(root)], d.TakeViewCommands());
    }

    private static CodeReference ReferenceTo(AppStateDriver d, string type, string text, int offset = 0)
    {
        d.Show(d.Fixture(type));
        var document = d.AppState.Tabs.Active!.Document;
        var (line, column) = document.Find(text);
        return document.SpanAt(line, column + offset).Reference!;
    }

    private static CodeReference ReferenceToFirst(AppStateDriver d) => ReferenceTo(d, "Outline", "return First();", "return ".Length);

    [Fact]
    public void A_reference_shows_a_loading_root_at_once_that_is_saved_once_it_is_found()
    {
        var d = AppStateDriver.Opened();
        var reference = ReferenceToFirst(d);
        var analyzer = d.AppState.Analyzer;

        d.Analyze(reference);
        var waiting = analyzer.Roots.ToList();
        var savedWhileFinding = analyzer.ListSavedAnalyses();
        var revisionWhileFinding = analyzer.Revision;
        d.Settle();

        Assert.IsType<AnalyzerPendingNode>(Assert.Single(waiting));
        Assert.Empty(savedWhileFinding);
        Assert.StartsWith("First()", Assert.Single(analyzer.Found()).Symbol.Label);
        Assert.Single(analyzer.ListSavedAnalyses());
        Assert.NotEqual(revisionWhileFinding, analyzer.Revision);
    }

    [Fact]
    public void Two_analyses_asked_for_by_reference_both_add_their_roots_in_the_order_asked()
    {
        var d = AppStateDriver.Opened();
        var length = ReferenceTo(d, "Greeter", "Length");
        var greet = ReferenceTo(d, "Greeter", "Greet(2)");

        d.Analyze(length);
        d.Analyze(greet);
        d.Settle();

        var labels = d.AppState.Analyzer.Found().Select(r => r.Symbol.Label).ToList();
        Assert.Equal(2, labels.Count);
        Assert.StartsWith("Length", labels[0]);
        Assert.StartsWith("Greet(", labels[1]);
    }

    [Fact]
    public void A_reference_to_a_symbol_analyzed_already_selects_its_root()
    {
        var d = AppStateDriver.OpenedWithoutScreen();
        var reference = ReferenceToFirst(d);
        var analyzer = d.AppState.Analyzer;
        var first = AddToAnalyzer(d.AppState, First(d));
        AddToAnalyzer(d.AppState, d.Fixture("Greeter"));
        d.TakeViewCommands();

        AddToAnalyzer(d.AppState, reference);
        d.Settle();

        Assert.Equal(2, analyzer.Roots.Count);
        Assert.Same(analyzer.Found()[0], first);
        Assert.Equal([new ViewCommand.ShowAnalysis(first!)], d.TakeViewCommands());
    }

    [Fact]
    public void A_reference_whose_root_was_removed_meanwhile_is_dropped()
    {
        var d = AppStateDriver.Opened();
        var reference = ReferenceToFirst(d);
        var analyzer = d.AppState.Analyzer;

        d.Analyze(reference);
        analyzer.Remove(analyzer.Roots[0]);
        d.Settle();

        Assert.Empty(analyzer.Roots);
    }

    [Fact]
    public void A_reference_that_does_not_resolve_removes_its_root_and_is_reported()
    {
        using var temp = new TempDir();
        var copy = temp.Write("Copy.dll", File.ReadAllBytes(AppStateDriver.FixturePath));
        var d = new AppStateDriver();
        OpenAssembly(d.AppState, copy);
        d.RunUntil(() => d.AppState.Assemblies.Assemblies.Count == 1);
        d.Settle();
        var assembly = d.AppState.Assemblies.Assemblies.Single();
        var outline = assembly.Root.Child(AppStateDriver.FixturesNamespace, "Outline");
        var document = outline.Decompile(DecompilationLanguage.CSharp);
        var (line, column) = document.Find("return First();");
        var reference = document.SpanAt(line, column + "return ".Length).Reference!;
        CloseAssembly(d.AppState, assembly);
        File.Delete(copy);

        d.Analyze(reference);
        d.Settle();

        Assert.Empty(d.AppState.Analyzer.Roots);
        Assert.Equal("Cannot resolve First", Assert.Single(d.AppState.Errors.Entries).Title);
    }

    [Fact]
    public void Removing_a_root_or_clearing_the_assembly_list_empties_the_analyzer()
    {
        var d = AppStateDriver.Opened();
        var analyzer = d.AppState.Analyzer;
        d.Analyze(First(d));
        d.Analyze(d.Fixture("Greeter"));

        analyzer.Remove(analyzer.Roots[0]);
        var left = analyzer.Found().Select(r => r.Symbol.Label).ToList();
        CloseAllAssemblies(d.AppState);

        Assert.Equal(["Greeter"], left);
        Assert.Empty(analyzer.Roots);
    }

    [Fact]
    public void Removing_a_root_leaves_the_analyses_of_results_never_expanded_unlisted()
    {
        var d = AppStateDriver.Opened();
        var analyzer = d.AppState.Analyzer;
        d.Analyze(First(d));
        var root = analyzer.Found()[0];
        var caller = (AnalyzerSymbolNode)Expand(d, root, "Used By").Found[0];

        analyzer.Remove(root);

        Assert.Null(ListedChildren(caller));
    }

    /// <summary>The children <paramref name="node"/> has listed so far, or null; private, as nothing
    /// public reads them without listing them.</summary>
    private static object? ListedChildren(AnalyzerSymbolNode node)
    {
        var field = typeof(AnalyzerSymbolNode).GetField("_children", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return field.GetValue(node);
    }

    [Fact]
    public void Its_menu_clears_the_analyzer()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        d.Analyze(First(d));
        app.Run(3);
        var row = app.Frames.TextRects().Last(t => t.Text.StartsWith("First()")).Rect;

        ClickAt(app, row, PointerButton.Right).Frame();
        app.ClickMenuItem("Clear").Frame(2);

        Assert.Empty(d.AppState.Analyzer.Roots);
    }

    [Fact]
    public void The_toolbar_expands_and_runs_every_analysis_of_the_roots_then_collapses_them()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        var analyzer = d.AppState.Analyzer;
        d.Analyze(First(d));
        app.Run(3);
        var root = analyzer.Roots[0];
        var rows = app.Ui.Analyzer;
        var kinds = rows.GetChildren(root).OfType<AnalyzerKindNode>().ToList();

        app.ClickLast(LucideIcons.ChevronsUpDown).Frame();
        var expanded = kinds.All(k => rows.View.IsExpanded(k));
        d.RunUntil(() => kinds.All(k => k.IsDone));
        app.ClickLast(LucideIcons.ChevronsDownUp).Frame();

        Assert.NotEmpty(kinds);
        Assert.True(expanded);
        Assert.False(rows.View.IsExpanded(root));
    }

    [Fact]
    public void The_toolbar_removes_the_analysis_the_selection_is_in_or_every_analysis()
    {
        var app = AppDriver.Opened(height: 600f);
        var d = app.Driver;
        var analyzer = d.AppState.Analyzer;
        d.Analyze(First(d));
        var caller = (AnalyzerSymbolNode)Expand(d, analyzer.Found()[0], "Used By").Found[0];
        d.Analyze(d.Fixture("Greeter"));
        app.Ui.Analyzer.View.Select(caller);
        app.Run(3);

        app.ClickLast(LucideIcons.Trash2).Frame(2);
        var left = analyzer.Found().Select(r => r.Symbol.Label).ToList();
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

        Assert.Empty(d.AppState.Analyzer.Roots);
    }
}
