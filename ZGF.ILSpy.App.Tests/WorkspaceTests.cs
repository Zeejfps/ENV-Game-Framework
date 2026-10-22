using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class WorkspaceTests
{
    [Fact]
    public void Opening_happens_in_the_background_and_shows_the_assembly()
    {
        var d = new WorkspaceDriver();

        d.Workspace.Open(WorkspaceDriver.FixturePath);

        Assert.Null(Assert.Single(d.Workspace.Tabs).Location);
        d.RunUntil(() => d.Workspace.ActiveTab?.Node != null);
        Assert.Equal(NodeKind.Assembly, d.Workspace.ActiveTab!.Node!.Kind);
    }

    [Fact]
    public void A_new_tab_opens_in_the_active_tabs_group()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        var greeter = d.Workspace.Tabs[^2];
        var documents = d.Workspace.Layout;
        documents.Dock(d.Workspace.ActiveTab!.Panel, documents.Root, DockEdge.Right);
        d.Workspace.Activate(greeter);

        d.Show(d.Fixture("Outline"), inNewTab: true);

        Assert.Same(documents.GroupOf(greeter.Panel), documents.GroupOf(d.Workspace.ActiveTab!.Panel));
        Assert.Equal("Outline", d.Workspace.ActiveTab.Node!.Text);
    }

    [Fact]
    public void Docking_the_only_tab_beside_itself_moves_it_there_and_leaves_a_new_empty_tab_that_shows_what_is_picked_next()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var greeter = d.Workspace.ActiveTab!;
        var documents = d.Workspace.Layout;

        documents.Dock(greeter.Panel, documents.GroupOf(greeter.Panel)!, DockEdge.Right);
        d.Workspace.SyncActiveTab();

        Assert.Equal(2, d.Workspace.Tabs.Count);
        var empty = d.Workspace.Tabs.Single(t => t != greeter);
        Assert.Null(empty.Location);
        Assert.NotSame(documents.GroupOf(greeter.Panel), documents.GroupOf(empty.Panel));
        Assert.Same(empty, d.Workspace.ActiveTab);

        d.Show(d.Fixture("Farewell"));

        Assert.Equal("Farewell", empty.Node!.Text);
        Assert.Equal("Greeter", greeter.Node!.Text);
    }

    [Fact]
    public void Closing_the_active_tab_activates_the_one_in_front_of_its_group()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        var greeter = d.Workspace.Tabs[^2];

        d.Workspace.CloseTab(d.Workspace.ActiveTab!);

        Assert.Same(greeter, d.Workspace.ActiveTab);
        Assert.DoesNotContain(d.Workspace.Tabs, t => t.Node?.Text == "Farewell");
        Assert.Equal([greeter.Panel], d.Workspace.Layout.GroupOf(greeter.Panel)!.Panels);
    }

    [Fact]
    public void A_file_that_cannot_be_opened_is_reported_as_an_error()
    {
        var d = new WorkspaceDriver();
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.dll");

        d.Workspace.Open(missing);
        d.RunUntil(() => d.Workspace.Errors.Entries.Count > 0);

        var error = Assert.Single(d.Workspace.Errors.Entries);
        Assert.Contains(Path.GetFileName(missing), error.Title);
        Assert.Null(Assert.Single(d.Workspace.Tabs).Location);
    }

    [Fact]
    public void Reloading_does_not_overwrite_a_tab_the_user_pointed_elsewhere_meanwhile()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var farewell = d.Fixture("Farewell");

        d.Workspace.Refresh();
        d.Workspace.Show(farewell);
        d.Settle();

        var tab = Assert.Single(d.Workspace.Tabs);
        Assert.Same(farewell, tab.Node);
    }

    [Fact]
    public void Reloading_twice_lands_every_tab_on_the_newest_assemblies()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);

        d.Workspace.Refresh();
        d.Workspace.Refresh();
        d.Settle();

        Assert.Equal(["Greeter", "Farewell"], d.Workspace.Tabs.Select(t => t.Node!.Text));
        Assert.All(d.Workspace.Tabs, t => Assert.Contains(t.Node!.Assembly, d.Workspace.Assemblies.Assemblies));
        Assert.Single(d.Workspace.Assemblies.Assemblies);
    }

    [Fact]
    public void Reloading_keeps_the_tree_expanded_as_it_was()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var ns = d.Node(WorkspaceDriver.FixturesNamespace);
        d.Workspace.Tree.View.SetExpanded(d.Fixture("Greeter"), true);

        d.Workspace.Refresh();
        d.Settle();

        var view = d.Workspace.Tree.View;
        Assert.NotSame(ns, d.Node(WorkspaceDriver.FixturesNamespace));
        Assert.True(view.IsExpanded(d.Node()));
        Assert.True(view.IsExpanded(d.Node(WorkspaceDriver.FixturesNamespace)));
        Assert.True(view.IsExpanded(d.Fixture("Greeter")));
        Assert.Same(d.Fixture("Greeter"), view.Selected);
    }

    [Fact]
    public void Going_back_after_a_reload_lands_on_the_reloaded_node()
    {
        var d = WorkspaceDriver.Opened();
        var before = d.Fixture("Greeter");
        d.Show(before);
        d.Show(d.Fixture("Farewell"));

        d.Workspace.Refresh();
        d.Settle();
        d.Workspace.GoBack();
        d.Settle();

        var shown = d.Workspace.ActiveTab!.Node!;
        Assert.Empty(d.Workspace.Errors.Entries.Select(e => e.FullText));
        Assert.Equal("Greeter", shown.Text);
        Assert.NotSame(before, shown);
        Assert.Same(d.Fixture("Greeter"), shown);
        Assert.Same(shown, d.Workspace.Tree.View.Selected);
    }

    [Fact]
    public void Going_back_to_what_the_tab_shows_returns_to_where_it_was_left_without_decompiling_again()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        d.Workspace.Activate(d.Workspace.Tabs[0]);
        d.Settle();
        var tab = d.Workspace.ActiveTab!;
        var document = tab.Document;

        d.Workspace.GoBack();

        Assert.False(tab.IsBusy);
        Assert.Same(document, tab.Document);
        Assert.NotNull(tab.View.PendingRestore);
        Assert.Equal(CodeViewState.NoLine, tab.View.PendingReveal);
    }

    [Fact]
    public void A_navigation_result_does_not_override_a_newer_choice()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var document = d.Workspace.ActiveTab!.Document;
        var (line, column) = document.Find("Name;");
        var reference = document.SpanAt(line, column).Reference!;
        var farewell = d.Fixture("Farewell");

        d.Workspace.Navigate(reference, inNewTab: false);
        d.Workspace.Show(farewell);
        d.Workspace.Navigate(reference, inNewTab: true);
        d.Workspace.Activate(d.Workspace.Tabs[0]);
        d.Settle();

        var tab = Assert.Single(d.Workspace.Tabs);
        Assert.Same(farewell, tab.Node);
        Assert.False(d.Workspace.IsResolving);
    }

    [Fact]
    public void A_navigation_lands_in_the_tab_it_was_asked_from()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var document = d.Workspace.ActiveTab!.Document;
        var (line, column) = document.Find("Name;");

        d.Workspace.Navigate(document.SpanAt(line, column).Reference!, inNewTab: false);
        d.Settle();

        var tab = Assert.Single(d.Workspace.Tabs);
        Assert.Equal("Name : string", tab.Node!.Text);
        Assert.Same(tab.Node, d.Workspace.Tree.View.Selected);
    }

    [Fact]
    public void A_document_is_only_scrolled_to_a_definition_it_has()
    {
        var d = WorkspaceDriver.Opened();
        var tab = d.Workspace.ActiveTab!;

        d.Workspace.Show(d.Node(WorkspaceDriver.FixturesNamespace));
        Assert.Equal(CodeViewState.NoLine, tab.View.PendingReveal);
        d.Settle();
        Assert.Equal(CodeViewState.NoLine, tab.View.PendingReveal);

        d.Show(d.Fixture("Greeter"));
        Assert.True(tab.View.PendingReveal >= 0);
        Assert.Equal(tab.Document.FindDefinitionLine(tab.Node!), tab.View.PendingReveal);
    }

    [Fact]
    public void Search_keeps_its_results_and_selection_when_navigation_opens_an_assembly()
    {
        var d = WorkspaceDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        var search = d.Workspace.Search;
        search.Query = "IsLeapYear";
        search.Update(d.Workspace.Assemblies, 0, d.Post, true);
        search.Update(d.Workspace.Assemblies, 1, d.Post, true);
        d.Settle();
        var rows = search.Rows.ToArray();
        Assert.Single(rows);
        search.Select(0);
        var selected = search.SelectedRow!.Node;

        var document = d.Workspace.ActiveTab!.Document;
        var (line, column) = document.Find("Length");
        d.Workspace.Navigate(document.SpanAt(line, column).Reference!, inNewTab: true);
        d.Settle();
        Assert.Equal(2, d.Workspace.Assemblies.Assemblies.Count);
        search.Update(d.Workspace.Assemblies, 2, d.Post, true);
        d.Settle();

        Assert.All(rows, r => Assert.Contains(r, search.Rows));
        Assert.True(search.Rows.Count > rows.Length);
        Assert.Same(selected, search.SelectedRow!.Node);
    }

    [Fact]
    public void Search_drops_hits_in_a_closed_assembly_and_keeps_the_selection()
    {
        var d = WorkspaceDriver.Opened();
        var greeter = d.Fixture("Greeter");
        var document = greeter.Decompile(DecompilationLanguage.CSharp);
        var (line, column) = document.Find("Length");
        Assert.NotNull(d.Workspace.Assemblies.FindNode(document.SpanAt(line, column).Reference!));
        var search = d.Workspace.Search;
        search.Query = "IsLeapYear";
        search.Update(d.Workspace.Assemblies, 0, d.Post, true);
        search.Update(d.Workspace.Assemblies, 1, d.Post, true);
        d.Settle();
        Assert.Contains(search.Rows, r => r.Node.Assembly != greeter.Assembly);
        search.Select(search.Rows.ToList().FindIndex(r => r.Node.Assembly == greeter.Assembly));
        var selected = search.SelectedRow!.Node;

        d.Workspace.Close(d.Workspace.Assemblies.Assemblies.Single(a => a.Name == "System.Private.CoreLib"));
        search.Update(d.Workspace.Assemblies, 2, d.Post, true);
        d.Settle();

        Assert.All(search.Rows, r => Assert.Equal(greeter.Assembly, r.Node.Assembly));
        Assert.Same(selected, search.SelectedRow!.Node);
    }

    [Fact]
    public void A_closed_search_panel_does_not_search()
    {
        var d = WorkspaceDriver.Opened();
        var search = d.Workspace.Search;
        search.Query = "Greet";

        search.Update(d.Workspace.Assemblies, 0, d.Post, false);
        search.Update(d.Workspace.Assemblies, 5, d.Post, false);

        Assert.False(search.IsBusy);
        Assert.Empty(search.Rows);
    }

    [Fact]
    public void Search_selection_moves_by_keys_within_the_results()
    {
        var d = WorkspaceDriver.Opened();
        var search = d.Workspace.Search;
        search.Query = "e";
        search.Update(d.Workspace.Assemblies, 0, d.Post, true);
        search.Update(d.Workspace.Assemblies, 1, d.Post, true);
        d.Settle();
        Assert.True(search.Rows.Count > 3);

        search.Move(1);
        Assert.Equal(0, search.Selected);
        search.Move(2);
        Assert.Equal(2, search.Selected);
        search.Move(-10);
        Assert.Equal(0, search.Selected);
        search.Move(1000);
        Assert.Equal(search.Rows.Count - 1, search.Selected);
    }

    [Fact]
    public void A_tab_whose_place_is_gone_after_a_reload_says_so_and_reports_it()
    {
        var d = new WorkspaceDriver();
        using var temp = new TempDir();
        var copy = temp.Copy(WorkspaceDriver.FixturePath, "fixture.dll");
        d.Workspace.Open(copy);
        d.RunUntil(() => d.Workspace.ActiveTab?.Node != null);
        d.Settle();
        var greeter = d.Workspace.ActiveTab!.Node!.Child(WorkspaceDriver.FixturesNamespace, "Greeter");
        d.Show(greeter);
        File.Delete(copy);

        d.Workspace.Refresh();
        d.Settle();

        var tab = Assert.Single(d.Workspace.Tabs);
        Assert.Null(tab.Node);
        Assert.Equal("Greeter", tab.Title);
        Assert.Contains("gone", tab.Document.GetText());
        Assert.Contains(d.Workspace.Errors.Entries, e => e.Title.Contains(Path.GetFileName(copy)));
        Assert.Contains(d.Workspace.Errors.Entries, e => e.Title.Contains("Greeter"));
    }
}

public sealed class NamespacePageTests
{
    [Fact]
    public void A_type_named_on_its_namespaces_page_opens_when_clicked()
    {
        var app = AppDriver.Opened();
        var d = app.Driver;
        d.Show(d.Node(WorkspaceDriver.FixturesNamespace));
        var tab = d.Workspace.ActiveTab!;
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
