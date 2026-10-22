using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy.App.Tests;

public sealed class DockingTests
{
    [Fact]
    public void A_view_docks_as_a_tab_beside_a_document_and_the_document_stays_the_active_tab()
    {
        var app = new AppDriver();
        var d = app.Driver;
        d.OpenFixtures();
        d.Show(d.Fixture("Greeter"));
        var greeter = d.Workspace.ActiveTab!;
        var layout = d.Workspace.Layout;

        layout.Dock(DockPanels.Assemblies, layout.GroupOf(greeter.Panel)!, DockEdge.Center);
        app.Run(3);

        Assert.Equal([greeter.Panel, DockPanels.Assemblies], layout.GroupOf(greeter.Panel)!.Panels);
        Assert.Same(greeter, d.Workspace.ActiveTab);
        Assert.True(app.Frames.Drew("Greeter"));
        Assert.True(app.Frames.Drew(WorkspaceDriver.FixturesNamespace));
    }

    [Fact]
    public void The_assemblies_view_closes_like_a_tab_and_comes_back_on_the_left_from_the_View_menu()
    {
        var app = new AppDriver();
        var d = app.Driver;
        d.OpenFixtures();
        var layout = d.Workspace.Layout;
        app.Run(2);

        layout.Close(DockPanels.Assemblies);
        app.Run(2);
        var closed = !layout.Contains(DockPanels.Assemblies);
        app.Click("View");
        app.ClickMenuItem("Assemblies");
        app.Run(2);

        Assert.True(closed);
        var split = Assert.IsType<DockSplit>(layout.Root);
        Assert.Same(layout.GroupOf(DockPanels.Assemblies), split.First);
        Assert.True(app.Frames.Drew("0.0.0.0 · net10.0"));
    }

    [Fact]
    public void View_opens_the_search_panel_and_holds_back_and_forward_with_no_navigate_menu()
    {
        var app = new AppDriver();
        var layout = app.Workspace.Layout;
        app.Run(2);
        Assert.False(app.Frames.Drew("Navigate"));

        app.Click("View");
        Assert.True(app.Frames.Drew("Back"));
        Assert.True(app.Frames.Drew("Forward"));
        app.ClickMenuItem("Search");
        app.Run(2);

        Assert.True(layout.Contains(DockPanels.Search));
        Assert.True(app.Frames.Drew("Search…"));
    }

    [Fact]
    public void Closing_the_last_document_frees_its_area_and_the_next_one_opens_right_of_the_views()
    {
        var d = new WorkspaceDriver();
        d.OpenFixtures();
        var layout = d.Workspace.Layout;

        d.Workspace.CloseTab(d.Workspace.ActiveTab!);
        var left = Assert.IsType<DockGroup>(layout.Root).Panels.ToList();
        d.Show(d.Fixture("Greeter"));

        Assert.Equal([DockPanels.Assemblies], left);
        var split = Assert.IsType<DockSplit>(layout.Root);
        Assert.Same(layout.GroupOf(d.Workspace.ActiveTab!.Panel), split.Second);
        Assert.Equal("Greeter", d.Workspace.ActiveTab.Node!.Text);
    }

    [Fact]
    public void With_everything_closed_a_hint_points_to_the_View_menu()
    {
        var app = new AppDriver();
        var w = app.Workspace;
        w.Layout.Close(DockPanels.Assemblies);
        w.CloseTab(w.ActiveTab!);

        app.Run(2);

        Assert.True(app.Frames.Drew("Everything is closed. Open the assemblies from the View menu."));
    }

    [Fact]
    public void Reset_layout_in_the_View_menu_once_confirmed_puts_the_assemblies_left_of_every_open_document_and_closes_the_rest()
    {
        var app = new AppDriver();
        var d = app.Driver;
        d.OpenFixtures();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        var farewell = d.Workspace.ActiveTab!;
        var layout = d.Workspace.Layout;
        layout.Dock(farewell.Panel, layout.Root, DockEdge.Bottom);
        layout.Float(DockPanels.Assemblies, new UiRect(40f, 40f, 300f, 300f));
        d.Workspace.OpenBookmarks();
        app.Run(2);

        app.Click("View");
        app.ClickMenuItem("Reset layout");
        app.Run(2);
        Assert.True(layout.Contains(DockPanels.Bookmarks));
        app.Click("Reset");
        app.Run(2);

        var split = Assert.IsType<DockSplit>(layout.Root);
        Assert.Equal([DockPanels.Assemblies], Assert.IsType<DockGroup>(split.First).Panels);
        var documents = Assert.IsType<DockGroup>(split.Second);
        Assert.Equal(d.Workspace.Tabs.Select(t => t.Panel).Order(), documents.Panels.Order());
        Assert.Equal(farewell.Panel, documents.Active);
        Assert.Same(farewell, d.Workspace.ActiveTab);
        Assert.Empty(layout.Windows);
        Assert.False(layout.Contains(DockPanels.Bookmarks));
    }
}

public sealed class LayoutResetTests
{
    [Fact]
    public void Cancelling_the_reset_question_leaves_the_layout_as_it_was()
    {
        var app = new AppDriver();
        var w = app.Workspace;
        w.OpenBookmarks();
        app.Run(2);

        app.Click("View");
        app.ClickMenuItem("Reset layout");
        app.Run(2);
        Assert.True(w.ConfirmingLayoutReset);
        app.Click("Cancel");
        app.Run(2);

        Assert.False(w.ConfirmingLayoutReset);
        Assert.True(w.Layout.Contains(DockPanels.Bookmarks));
    }
}

public sealed class LayoutPersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ilspy-layout-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static DockSplit? ParentOf(DockNode node, DockNode target) => node switch
    {
        DockSplit split when split.First == target || split.Second == target => split,
        DockSplit split => ParentOf(split.First, target) ?? ParentOf(split.Second, target),
        _ => null,
    };

    [Fact]
    public void Views_docked_below_the_documents_are_still_below_them_after_a_restart()
    {
        var file = Path.Combine(_directory, "settings.json");
        var first = new WorkspaceDriver(AppSettings.Load(file));
        first.OpenFixtures();
        var w = first.Workspace;
        w.OpenAnalyzer();
        w.Layout.Dock(DockPanels.Bookmarks, w.Layout.GroupOf(DockPanels.Analyzer)!, DockEdge.Center);
        w.SaveSettings();

        var next = new WorkspaceDriver(AppSettings.Load(file)).Workspace;
        var layout = next.Layout;
        var documents = layout.GroupOf(next.ActiveTab!.Panel)!;
        var views = layout.GroupOf(DockPanels.Analyzer)!;
        var split = ParentOf(layout.Root, views)!;

        Assert.Same(views, layout.GroupOf(DockPanels.Bookmarks));
        Assert.Equal(Direction.Vertical, split.Direction);
        Assert.Same(documents, split.First);
        Assert.Same(views, split.Second);
        Assert.Single(next.Tabs);
    }

    [Fact]
    public void An_open_search_panel_is_still_open_where_it_was_docked_after_a_restart()
    {
        var file = Path.Combine(_directory, "settings.json");
        var w = new WorkspaceDriver(AppSettings.Load(file)).Workspace;
        w.OpenSearch();
        w.OpenAnalyzer();
        w.Layout.Dock(DockPanels.Search, w.Layout.GroupOf(DockPanels.Analyzer)!, DockEdge.Center);
        w.SaveSettings();

        var next = new WorkspaceDriver(AppSettings.Load(file)).Workspace;

        Assert.True(next.Layout.Contains(DockPanels.Search));
        Assert.Same(next.Layout.GroupOf(DockPanels.Analyzer), next.Layout.GroupOf(DockPanels.Search));
    }

    [Fact]
    public void The_symbols_being_analyzed_are_analyzed_again_after_a_restart()
    {
        var file = Path.Combine(_directory, "settings.json");
        var first = new WorkspaceDriver(AppSettings.Load(file));
        first.OpenFixtures();
        first.Workspace.Analyze(first.Fixture("Outline").Children.Single(c => c.Text.StartsWith("First()")));
        first.Workspace.Analyze(first.Fixture("Greeter"));
        first.Pump();

        var next = new WorkspaceDriver(AppSettings.Load(file));
        next.PumpUntil(() => next.Workspace.Analyzer.Roots.Count == 2);

        Assert.Equal(["First() : int", "Greeter"], next.Workspace.Analyzer.Roots.Select(r => r.Symbol.Label));
    }
}
