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
}
