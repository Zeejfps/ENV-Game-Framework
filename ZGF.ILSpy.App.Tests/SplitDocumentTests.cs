using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy.App.Tests;

public sealed class SplitDocumentTests
{
    [Fact]
    public void A_saved_layout_naming_a_panel_twice_shows_it_once()
    {
        var saved = new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.3f, DockLayout.Group(DockPanels.Assemblies),
                DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("document-1"), DockLayout.Group("document-1"))),
        };

        var layout = DockPanels.Restore(saved);

        var panels = layout.AllGroups().SelectMany(g => g.Panels).ToList();
        Assert.Equal([DockPanels.Assemblies, "document-1"], panels);
    }

    [Fact]
    public void Documents_arranged_in_nested_splits_dock_as_the_arrangement_splits_them()
    {
        var layout = new MainLayout(new DockLayout { Root = DockLayout.Group(DockPanels.Assemblies) });
        var arrangement = new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.6f,
                DockLayout.Split(Direction.Vertical, 0.5f, DockLayout.Group("document-1"), DockLayout.Group("document-2")),
                DockLayout.Group("document-3")),
        };

        layout.DockArranged(["document-1", "document-2", "document-3"], arrangement);

        var beside = FindSplitHolding(layout.Dock.Root, "document-3");
        Assert.NotNull(beside);
        Assert.Equal(Direction.Horizontal, beside.Direction);
        Assert.Equal(["document-1", "document-2"], DockLayout.PanelsIn(beside.First));
        Assert.Equal(["document-3"], DockLayout.PanelsIn(beside.Second));
        Assert.Equal(0.6f, beside.Ratio, 0.01f);
    }

    /// <summary>The split one of whose sides is the group holding only <paramref name="panel"/>.</summary>
    private static DockSplit? FindSplitHolding(DockNode node, string panel)
    {
        if (node is not DockSplit split)
            return null;
        if (IsGroupOf(split.First, panel) || IsGroupOf(split.Second, panel))
            return split;
        return FindSplitHolding(split.First, panel) ?? FindSplitHolding(split.Second, panel);
    }

    private static bool IsGroupOf(DockNode node, string panel) => node is DockGroup group && group.Panels.SequenceEqual([panel]);

    [Fact]
    public void The_first_tab_of_a_run_takes_a_panel_no_saved_document_was_docked_as()
    {
        var saved = new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.3f, DockLayout.Group(DockPanels.Assemblies),
                DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("document-7"), DockLayout.Group("document-1"))),
        };

        var d = new AppStateDriver(new AppSettings { Layout = saved });

        var panel = d.Ui.Documents.PanelOf(Assert.Single(d.AppState.Tabs));
        var panels = d.Ui.Layout.AllGroups().SelectMany(g => g.Panels).ToList();
        Assert.Equal([DockPanels.Assemblies, panel], panels);
        Assert.NotEqual("document-7", panel);
        Assert.NotEqual("document-1", panel);
    }

    [Fact]
    public void Two_tabs_on_one_type_scroll_apart_and_switching_between_them_leaves_the_tree_alone()
    {
        var (app, left, right) = SideBySide();
        var d = app.Driver;

        app.Frames.MovePointer(250f, 400f).Frame().Wheel(-5).Frame(2);
        Assert.True(app.Ui.Documents.ViewOf(left).Code.ScrollY > 0f);
        Assert.Equal(0f, app.Ui.Documents.ViewOf(right).Code.ScrollY);

        app.Frames.Click(750f, 400f).Frame();
        Assert.Same(right, d.AppState.Tabs.Active);
        app.Show(app.Fixture("Greeter"), 2);
        Assert.Equal("Outline", left.Node!.Text);
        Assert.Equal("Greeter", right.Node!.Text);

        app.Frames.Click(250f, 400f).Frame();
        Assert.Same(left, d.AppState.Tabs.Active);
        Assert.Same(right.Node, d.Ui.Tree.View.Selected);
    }

    [Fact]
    public void Only_the_active_documents_tab_is_marked_in_the_accent()
    {
        var (app, left, _) = SideBySide();
        var theme = app.Theme;

        Assert.Equal(theme.Accent, Indicator(app, app.LastTabTitleRect("Outline")));
        Assert.Equal(theme.BorderStrong, Indicator(app, app.TabTitleRect("Outline")));

        app.Frames.Click(250f, 400f).Frame();

        Assert.Same(left, app.AppState.Tabs.Active);
        Assert.Equal(theme.Accent, Indicator(app, app.TabTitleRect("Outline")));
        Assert.Equal(theme.BorderStrong, Indicator(app, app.LastTabTitleRect("Outline")));
    }

    [Fact]
    public void Closing_all_documents_from_the_view_menu_closes_the_tabs_of_every_group()
    {
        var (app, left, right) = SideBySide();
        Assert.NotSame(app.Ui.Layout.GroupOf(app.Ui.Documents.PanelOf(left)), app.Ui.Layout.GroupOf(app.Ui.Documents.PanelOf(right)));

        app.Click("View");
        app.ClickMenuItem("Close all documents").Frame(2);
        app.Settle(2);

        Assert.Empty(app.AppState.Tabs);
    }

    private static (AppDriver App, DocumentTab Left, DocumentTab Right) SideBySide()
    {
        var app = AppDriver.Opened();
        var d = app.Driver;
        var layout = d.Ui.Layout;
        layout.Close(DockPanels.Assemblies);
        d.TurnOffPreviewTabs();
        d.Show(d.Fixture("Outline"));
        var left = d.AppState.Tabs.Active!;
        d.Show(d.Fixture("Outline"), inNewTab: true);
        var right = d.AppState.Tabs.Active!;
        layout.Dock(app.Ui.Documents.PanelOf(right), layout.GroupOf(app.Ui.Documents.PanelOf(left))!, DockEdge.Right, 0.5f);
        app.Run(3);
        return (app, left, right);
    }

    private static uint Indicator(AppDriver app, UiRect title)
    {
        var theme = app.Theme;
        return app.Frames.Canvas.Rects.Select(r => r.Inputs)
            .Where(r => Math.Abs(r.Position.Height - theme.ActiveIndicatorWidth) < 0.01f)
            .Where(r => r.Position.Left <= title.X && r.Position.Left + r.Position.Width >= title.Right)
            .Where(r => app.Frames.Height - r.Position.Top <= title.Y)
            .Select(r => r.Style.BackgroundColor)
            .Last();
    }
}
