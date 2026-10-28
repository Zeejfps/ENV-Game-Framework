using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.DocumentPanels;

namespace ZGF.ILSpy.App.Tests;

public sealed class AppStateTests
{
    [Fact]
    public void A_driver_made_without_a_screen_has_none_to_give()
    {
        var d = AppStateDriver.WithoutScreen();

        Assert.Throws<InvalidOperationException>(() => d.Ui);
        Assert.Throws<InvalidOperationException>(d.NextFrame);
    }

    [Fact]
    public void Showing_a_node_in_a_new_tab_asks_the_view_to_dock_it_beside_the_active_tab_and_reveal_the_node()
    {
        var d = AppStateDriver.OpenedWithoutScreen();
        var before = d.AppState.Tabs.Active!;
        var greeter = d.Fixture("Greeter");
        d.TakeViewCommands();

        ShowNode(d.AppState, greeter, inNewTab: true);
        var commands = d.TakeViewCommands();

        var tab = d.AppState.Tabs.Active!;
        Assert.NotSame(before, tab);
        Assert.Equal([new ViewCommand.ShowTab(tab, before), new ViewCommand.RevealNode(greeter)], commands);
    }

    [Fact]
    public void Closing_the_active_tab_with_no_screen_leaves_the_pick_of_the_next_to_the_view()
    {
        var d = AppStateDriver.OpenedWithoutScreen();
        d.Show(d.Fixture("Greeter"), inNewTab: true);
        var greeter = d.AppState.Tabs.Active!;

        var wasActive = CloseTab(d.AppState, greeter);

        Assert.True(wasActive);
        Assert.Null(d.AppState.Tabs.Active);
        Assert.Single(d.AppState.Tabs);
    }

    [Fact]
    public void A_reveal_asked_for_a_node_whose_list_was_cleared_since_is_dropped()
    {
        var d = AppStateDriver.Opened();
        var ns = d.Node(AppStateDriver.FixturesNamespace);
        ShowNode(d.AppState, d.Fixture("Greeter"));

        CloseAllAssemblies(d.AppState);
        d.NextFrame();

        Assert.False(d.Ui.Tree.View.HasSelection);
        Assert.False(d.Ui.Tree.View.IsExpanded(ns));
    }

    [Fact]
    public void Commands_for_nodes_of_a_cleared_list_or_lines_and_rows_a_tab_no_longer_has_are_dropped()
    {
        var d = AppStateDriver.Opened();
        var ns = d.Node(AppStateDriver.FixturesNamespace);
        d.Show(d.Node("Metadata", "Tables").Table("TypeDef"));
        var grid = d.AppState.Tabs.Active!;
        d.Show(d.Fixture("Greeter"), inNewTab: true);
        var code = d.AppState.Tabs.Active!;
        d.NextFrame();
        var lines = code.Document.Lines.Length;
        var rows = grid.Grid!.RowCount;
        CloseAllAssemblies(d.AppState);
        d.NextFrame();
        var lineBefore = d.Ui.Documents.ViewOf(code).Code.PendingReveal;
        var rowBefore = d.Ui.Documents.ViewOf(grid).Grid.PendingReveal;

        d.AppState.EnqueueCommand(new ViewCommand.ExpandNodes([ns]));
        d.AppState.EnqueueCommand(new ViewCommand.RevealLine(code, lines));
        d.AppState.EnqueueCommand(new ViewCommand.RevealRow(grid, rows));
        d.NextFrame();

        Assert.False(d.Ui.Tree.View.IsExpanded(ns));
        Assert.Equal(lineBefore, d.Ui.Documents.ViewOf(code).Code.PendingReveal);
        Assert.Equal(rowBefore, d.Ui.Documents.ViewOf(grid).Grid.PendingReveal);
    }

    [Fact]
    public void A_row_revealed_before_a_new_view_of_a_tab_is_drawn_stays_to_be_revealed()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Node("Metadata", "Tables").Table("TypeDef"));
        var grid = d.AppState.Tabs.Active!;
        var documents = d.Ui.Documents;
        var panel = documents.PanelOf(grid);
        documents.Remove(grid);

        documents.Add(grid, panel);
        documents.RevealRow(grid, 2);
        d.NextFrame();

        Assert.Equal(2, documents.ViewOf(grid).Grid.PendingReveal);
    }

    [Fact]
    public void Opening_happens_in_the_background_and_shows_the_assembly()
    {
        var d = new AppStateDriver();

        OpenAssembly(d.AppState, AppStateDriver.FixturePath);

        Assert.Null(Assert.Single(d.AppState.Tabs).Location);
        d.RunUntil(() => d.AppState.Tabs.Active?.Node != null);
        Assert.Equal(NodeKind.Assembly, d.AppState.Tabs.Active!.Node!.Kind);
    }

    [Fact]
    public void A_new_tab_opens_in_the_active_tabs_group()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        var greeter = d.AppState.Tabs[^2];
        var farewellPanel = d.Ui.Documents.PanelOf(d.AppState.Tabs.Active!);
        var documents = d.Ui.Layout;
        documents.Dock(farewellPanel, documents.Root, DockEdge.Right);
        ActivateTab(d.Ui, greeter);
        d.NextFrame();

        d.Show(d.Fixture("Outline"), inNewTab: true);

        var outlinePanel = d.Ui.Documents.PanelOf(d.AppState.Tabs.Active!);
        Assert.Same(documents.GroupOf(d.Ui.Documents.PanelOf(greeter)), documents.GroupOf(outlinePanel));
        Assert.Equal("Outline", d.AppState.Tabs.Active.Node!.Text);
    }

    [Fact]
    public void Docking_the_only_tab_beside_itself_moves_it_there_and_leaves_a_new_empty_tab_that_shows_what_is_picked_next()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var greeter = d.AppState.Tabs.Active!;
        var greeterPanel = d.Ui.Documents.PanelOf(greeter);
        var documents = d.Ui.Layout;

        documents.Dock(greeterPanel, documents.GroupOf(greeterPanel)!, DockEdge.Right);
        SyncActiveTab(d.Ui, d.AppState.Tabs.Active);
        d.NextFrame();

        Assert.Equal(2, d.AppState.Tabs.Count);
        var empty = d.AppState.Tabs.Single(t => t != greeter);
        Assert.Null(empty.Location);
        Assert.NotSame(documents.GroupOf(greeterPanel), documents.GroupOf(d.Ui.Documents.PanelOf(empty)));
        Assert.Same(empty, d.AppState.Tabs.Active);

        d.Show(d.Fixture("Farewell"));

        Assert.Equal("Farewell", empty.Node!.Text);
        Assert.Equal("Greeter", greeter.Node!.Text);
    }

    [Fact]
    public void Closing_the_active_tab_activates_the_one_in_front_of_its_group()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        var greeter = d.AppState.Tabs[^2];

        CloseTab(d.AppState, d.AppState.Tabs.Active!);
        d.NextFrame();

        Assert.Same(greeter, d.AppState.Tabs.Active);
        Assert.DoesNotContain(d.AppState.Tabs, t => t.Node?.Text == "Farewell");
        var greeterPanel = d.Ui.Documents.PanelOf(greeter);
        Assert.Equal([greeterPanel], d.Ui.Layout.GroupOf(greeterPanel)!.Panels);
    }

    [Fact]
    public void A_file_that_cannot_be_opened_is_listed_as_failed_and_its_tab_says_why()
    {
        var d = new AppStateDriver();
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.dll");

        OpenAssembly(d.AppState, missing);
        d.RunUntil(() => d.AppState.Tabs.Active?.Node != null);
        d.Settle();

        var failed = Assert.Single(d.AppState.Assemblies.Listed);
        var tab = Assert.Single(d.AppState.Tabs);
        Assert.True(failed.HasLoadError);
        Assert.Same(failed.Root, tab.Node);
        Assert.Contains(Path.GetFileName(missing), tab.Document.GetText());
        Assert.Same(failed.Root, Assert.Single(d.Ui.Tree.Roots));
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void Reloading_does_not_overwrite_a_tab_the_user_pointed_elsewhere_meanwhile()
    {
        var d = AppStateDriver.OpenedWithoutScreen();
        d.Show(d.Fixture("Greeter"));
        var farewell = d.Fixture("Farewell");

        RefreshAssemblies(d.AppState, []);
        ShowNode(d.AppState, farewell);
        d.Settle();

        var tab = Assert.Single(d.AppState.Tabs);
        Assert.Same(farewell, tab.Node);
    }

    [Fact]
    public void A_reload_asks_the_view_to_expand_what_it_had_expanded_and_to_reveal_the_active_tabs_node()
    {
        var d = AppStateDriver.OpenedWithoutScreen();
        d.Show(d.Fixture("Greeter"));
        var expanded = new List<NodePath> { d.Node().Path, d.Node(AppStateDriver.FixturesNamespace).Path };
        d.TakeViewCommands();

        RefreshAssemblies(d.AppState, expanded);
        d.Settle();
        var commands = d.TakeViewCommands();

        var expand = Assert.Single(commands.OfType<ViewCommand.ExpandNodes>());
        Assert.Equal(expanded, expand.Nodes.Select(n => n.Path));
        Assert.Same(d.Node(), expand.Nodes[0]);
        Assert.Contains(new ViewCommand.RevealNode(d.Fixture("Greeter")), commands);
    }

    [Fact]
    public void Reloading_twice_lands_every_tab_on_the_newest_assemblies()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);

        RefreshAssemblies(d.AppState, d.Ui.Tree.ExpandedPaths());
        RefreshAssemblies(d.AppState, d.Ui.Tree.ExpandedPaths());
        d.Settle();

        Assert.Equal(["Greeter", "Farewell"], d.AppState.Tabs.Select(t => t.Node!.Text));
        Assert.All(d.AppState.Tabs, t => Assert.Contains(t.Node!.Assembly, d.AppState.Assemblies.Assemblies));
        Assert.Single(d.AppState.Assemblies.Assemblies);
    }

    [Fact]
    public void Reloading_keeps_the_tree_expanded_as_it_was()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var ns = d.Node(AppStateDriver.FixturesNamespace);
        d.Ui.Tree.View.SetExpanded(d.Fixture("Greeter"), true);

        RefreshAssemblies(d.AppState, d.Ui.Tree.ExpandedPaths());
        d.Settle();

        var view = d.Ui.Tree.View;
        Assert.NotSame(ns, d.Node(AppStateDriver.FixturesNamespace));
        Assert.True(view.IsExpanded(d.Node()));
        Assert.True(view.IsExpanded(d.Node(AppStateDriver.FixturesNamespace)));
        Assert.True(view.IsExpanded(d.Fixture("Greeter")));
        Assert.Same(d.Fixture("Greeter"), view.Selected);
    }

    [Fact]
    public void Going_back_after_a_reload_lands_on_the_reloaded_node()
    {
        var d = AppStateDriver.Opened();
        var before = d.Fixture("Greeter");
        d.Show(before);
        d.Show(d.Fixture("Farewell"));

        RefreshAssemblies(d.AppState, d.Ui.Tree.ExpandedPaths());
        d.Settle();
        GoBack(d.AppState);
        d.Settle();

        var shown = d.AppState.Tabs.Active!.Node!;
        Assert.Empty(d.AppState.Errors.Entries.Select(e => e.Words().FullText));
        Assert.Equal("Greeter", shown.Text);
        Assert.NotSame(before, shown);
        Assert.Same(d.Fixture("Greeter"), shown);
        Assert.Same(shown, d.Ui.Tree.View.Selected);
    }

    [Fact]
    public void Going_back_to_what_the_tab_shows_returns_to_where_it_was_left_without_decompiling_again()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        ActivateTab(d.Ui, d.AppState.Tabs[0]);
        d.Settle();
        var tab = d.AppState.Tabs.Active!;
        var document = tab.Document;

        GoBack(d.AppState);
        d.NextFrame();

        var view = d.Ui.Documents.ViewOf(tab).Code;
        Assert.False(tab.IsBusy);
        Assert.Same(document, tab.Document);
        Assert.NotNull(view.PendingRestore);
        Assert.Equal(CodeViewState.NoLine, view.PendingReveal);
    }

    [Fact]
    public void A_navigation_result_does_not_override_a_newer_choice()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var document = d.AppState.Tabs.Active!.Document;
        var (line, column) = document.Find("Name;");
        var reference = document.SpanAt(line, column).Reference!;
        var farewell = d.Fixture("Farewell");

        Navigate(d.AppState, reference, inNewTab: false);
        ShowNode(d.AppState, farewell);
        Navigate(d.AppState, reference, inNewTab: true);
        ActivateTab(d.Ui, d.AppState.Tabs[0]);
        d.Settle();

        var tab = Assert.Single(d.AppState.Tabs);
        Assert.Same(farewell, tab.Node);
        Assert.False(d.AppState.IsResolving);
    }

    [Fact]
    public void A_navigation_lands_in_the_tab_it_was_asked_from()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var document = d.AppState.Tabs.Active!.Document;
        var (line, column) = document.Find("Name;");

        Navigate(d.AppState, document.SpanAt(line, column).Reference!, inNewTab: false);
        d.Settle();

        var tab = Assert.Single(d.AppState.Tabs);
        Assert.Equal("Name : string", tab.Node!.Text);
        Assert.Same(tab.Node, d.Ui.Tree.View.Selected);
    }

    [Fact]
    public void A_document_is_only_scrolled_to_a_definition_it_has()
    {
        var d = AppStateDriver.Opened();
        var tab = d.AppState.Tabs.Active!;

        var view = d.Ui.Documents.ViewOf(tab).Code;

        ShowNode(d.AppState, d.Node(AppStateDriver.FixturesNamespace));
        d.NextFrame();
        Assert.Equal(CodeViewState.NoLine, view.PendingReveal);
        d.Settle();
        Assert.Equal(CodeViewState.NoLine, view.PendingReveal);

        d.Show(d.Fixture("Greeter"));
        Assert.True(view.PendingReveal >= 0);
        Assert.Equal(tab.Document.FindDefinitionLine(tab.Node!), view.PendingReveal);
    }

    [Fact]
    public void Search_keeps_its_results_and_selection_when_navigation_opens_an_assembly()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var search = d.Ui.Search;
        search.Query = "IsLeapYear";
        search.Update(d.AppState.Assemblies, 0, true);
        search.Update(d.AppState.Assemblies, 1, true);
        d.Settle();
        var rows = search.Rows.ToArray();
        Assert.Single(rows);
        search.Select(search.Rows[0]);
        var selected = search.SelectedRow!.Node;

        var document = d.AppState.Tabs.Active!.Document;
        var (line, column) = document.Find("Length");
        Navigate(d.AppState, document.SpanAt(line, column).Reference!, inNewTab: true);
        d.Settle();
        Assert.Equal(2, d.AppState.Assemblies.Assemblies.Count);
        search.Update(d.AppState.Assemblies, 2, true);
        d.Settle();

        Assert.All(rows, r => Assert.Contains(r, search.Rows));
        Assert.True(search.Rows.Count > rows.Length);
        Assert.Same(selected, search.SelectedRow!.Node);
    }

    [Fact]
    public void Search_drops_hits_in_a_closed_assembly_and_keeps_the_selection()
    {
        var d = AppStateDriver.Opened();
        var greeter = d.Fixture("Greeter");
        var document = greeter.Decompile(DecompilationLanguage.CSharp);
        var (line, column) = document.Find("Length");
        Assert.NotNull(d.AppState.Assemblies.FindNode(document.SpanAt(line, column).Reference!));
        var search = d.Ui.Search;
        search.Query = "IsLeapYear";
        search.Update(d.AppState.Assemblies, 0, true);
        search.Update(d.AppState.Assemblies, 1, true);
        d.Settle();
        Assert.Contains(search.Rows, r => r.Node.Assembly != greeter.Assembly);
        search.Select(search.Rows.First(r => r.Node.Assembly == greeter.Assembly));
        var selected = search.SelectedRow!.Node;

        CloseAssembly(d.AppState, d.AppState.Assemblies.Assemblies.Single(a => a.Name == "System.Private.CoreLib"));
        search.Update(d.AppState.Assemblies, 2, true);
        d.Settle();

        Assert.All(search.Rows, r => Assert.Equal(greeter.Assembly, r.Node.Assembly));
        Assert.Same(selected, search.SelectedRow!.Node);
    }

    [Fact]
    public void A_closed_search_panel_does_not_search()
    {
        var d = AppStateDriver.Opened();
        var search = d.Ui.Search;
        search.Query = "Greet";

        search.Update(d.AppState.Assemblies, 0, false);
        search.Update(d.AppState.Assemblies, 5, false);

        Assert.False(search.IsBusy);
        Assert.Empty(search.Rows);
    }

    [Fact]
    public void A_search_that_fails_is_reported_unless_another_replaced_it()
    {
        var posted = PostedActions.OfThisTest();
        var reported = new List<string>();
        var replaced = new HeldSearch();
        var current = new HeldSearch();
        var streams = new Queue<HeldSearch>([replaced, current]);
        var search = new Search(static () => Strings.En, problem => reported.Add(ProblemTexts.Describe(Strings.En, problem).Title), (_, _, _, _) => streams.Dequeue().Stream());
        var assemblies = new AssemblyList();
        search.Query = "Greet";
        search.Update(assemblies, 0, true);
        search.Update(assemblies, 1, true);

        search.Query = "Farewell";
        search.Update(assemblies, 2, true);
        search.Update(assemblies, 3, true);
        replaced.Fail();
        posted.Run();
        var afterReplaced = reported.Count;
        current.Fail();
        posted.Run();

        Assert.Empty(streams);
        Assert.Equal(0, afterReplaced);
        Assert.Equal(["Search failed"], reported);
    }

    /// <summary>A search's stream that yields nothing until it is told to fail, whatever cancels it.</summary>
    private sealed class HeldSearch
    {
        private readonly TaskCompletionSource<IReadOnlyList<SearchResult>> _next = new();

        public async IAsyncEnumerable<IReadOnlyList<SearchResult>> Stream()
        {
            yield return await _next.Task;
        }

        public void Fail() => _next.SetException(new InvalidOperationException("The search broke."));
    }

    [Fact]
    public void Search_selection_moves_by_keys_within_the_results()
    {
        var d = AppStateDriver.Opened();
        var search = d.Ui.Search;
        search.Query = "e";
        search.Update(d.AppState.Assemblies, 0, true);
        search.Update(d.AppState.Assemblies, 1, true);
        d.Settle();
        Assert.True(search.Rows.Count > 3);

        search.Move(1);
        Assert.Same(search.Rows[0], search.SelectedRow);
        search.Move(2);
        Assert.Same(search.Rows[2], search.SelectedRow);
        search.Move(-10);
        Assert.Same(search.Rows[0], search.SelectedRow);
        search.Move(search.Rows.Count);
        Assert.Same(search.Rows[search.Rows.Count - 1], search.SelectedRow);
    }

    [Fact]
    public void Results_streamed_in_many_batches_end_up_in_sort_order()
    {
        var d = AppStateDriver.Opened();
        OpenAssembly(d.AppState, typeof(object).Assembly.Location);
        d.RunUntil(() => d.AppState.Assemblies.Assemblies.Count == 2);
        var search = d.Ui.Search;
        search.SortBy(SearchSort.Name);
        search.Query = "e";

        search.Update(d.AppState.Assemblies, 0, true);
        search.Update(d.AppState.Assemblies, 1, true);
        d.Settle();

        var sorted = search.Rows
            .OrderBy(r => r.Text, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Location, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Node.Assembly.NameAndVersion, StringComparer.OrdinalIgnoreCase);
        Assert.True(search.Rows.Count > 1000);
        Assert.Equal(sorted, search.Rows);
    }

    [Fact]
    public void A_tab_whose_place_is_gone_after_a_reload_says_so_and_reports_it()
    {
        var d = new AppStateDriver();
        using var temp = new TempDir();
        var copy = temp.Copy(AppStateDriver.FixturePath, "fixture.dll");
        OpenAssembly(d.AppState, copy);
        d.RunUntil(() => d.AppState.Tabs.Active?.Node != null);
        d.Settle();
        var greeter = d.AppState.Tabs.Active!.Node!.Child(AppStateDriver.FixturesNamespace, "Greeter");
        d.Show(greeter);
        File.Delete(copy);

        RefreshAssemblies(d.AppState, d.Ui.Tree.ExpandedPaths());
        d.Settle();

        var tab = Assert.Single(d.AppState.Tabs);
        Assert.Null(tab.Node);
        Assert.Equal("Greeter", tab.Title);
        Assert.Equal(new TabContent.Gone(tab.Location!), tab.Content);
        Assert.Contains("gone", TabPlaceholder.Write(d.Ui.Strings, tab.Content));
        Assert.True(Assert.Single(d.AppState.Assemblies.Listed).HasLoadError);
        Assert.Contains(d.AppState.Errors.Entries, e => e.Words().Title.Contains("Greeter"));
    }
}

public sealed class NamespacePageTests
{
    [Fact]
    public void A_type_named_on_its_namespaces_page_opens_when_clicked()
    {
        var app = AppDriver.Opened();
        var d = app.Driver;
        d.Show(d.Node(AppStateDriver.FixturesNamespace));
        var tab = d.AppState.Tabs.Active!;
        var (line, column) = tab.Document.Find("Greeter");
        var span = tab.Document.SpanAt(line, column);
        app.Run(3);

        var view = app.Frames.Canvas.GlyphRuns.First(r => r.Text == "Greeter");
        app.Frames.Click(view.Origin.X + 4f, app.Frames.Height - view.Origin.Y + 4f);
        d.Settle();

        Assert.False(span.IsDefinition);
        Assert.Equal("Greeter", tab.Node!.Text);
        Assert.Equal(NodeKind.Class, tab.Node.Kind);
    }
}
