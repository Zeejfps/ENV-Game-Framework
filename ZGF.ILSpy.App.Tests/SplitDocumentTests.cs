using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy.App.Tests;

public sealed class SplitDocumentTests
{
    [Fact]
    public void A_saved_layout_naming_the_new_document_twice_shows_it_once()
    {
        var saved = new DockLayout
        {
            Root = DockLayout.Split(Direction.Horizontal, 0.3f, DockLayout.Group(DockPanels.Assemblies),
                DockLayout.Split(Direction.Horizontal, 0.5f, DockLayout.Group("document-7"), DockLayout.Group("document-1"))),
        };

        var layout = DockPanels.Restore(saved, "document-1", out _);

        var panels = layout.AllGroups().SelectMany(g => g.Panels).ToList();
        Assert.Equal([DockPanels.Assemblies, "document-1"], panels);
    }

    [Fact]
    public void Two_tabs_on_one_type_scroll_apart_and_only_the_one_worked_in_follows_the_tree()
    {
        var (app, left, right) = SideBySide();
        var d = app.Driver;

        app.Frames.MovePointer(250f, 400f).Frame().Wheel(-5).Frame(2);
        Assert.True(app.Ui.ViewOf(left).Code.ScrollY > 0f);
        Assert.Equal(0f, app.Ui.ViewOf(right).Code.ScrollY);

        app.Frames.Click(750f, 400f).Frame();
        Assert.Same(right, d.Workspace.Tabs.Active);
        app.Show(app.Fixture("Greeter"), 2);
        Assert.Equal("Outline", left.Node!.Text);
        Assert.Equal("Greeter", right.Node!.Text);

        app.Frames.Click(250f, 400f).Frame();
        Assert.Same(left, d.Workspace.Tabs.Active);
        Assert.Same(left.Node, d.Workspace.Tree.View.Selected);
    }

    [Fact]
    public void Only_the_active_documents_tab_is_marked_in_the_accent()
    {
        var (app, left, _) = SideBySide();
        var theme = app.Theme;

        Assert.Equal(theme.Accent, Indicator(app, app.LastTextRect("Outline")));
        Assert.Equal(theme.BorderStrong, Indicator(app, app.TextRect("Outline")));

        app.Frames.Click(250f, 400f).Frame();

        Assert.Same(left, app.Workspace.Tabs.Active);
        Assert.Equal(theme.Accent, Indicator(app, app.TextRect("Outline")));
        Assert.Equal(theme.BorderStrong, Indicator(app, app.LastTextRect("Outline")));
    }

    private static (AppDriver App, DocumentTab Left, DocumentTab Right) SideBySide()
    {
        var app = AppDriver.Opened();
        var d = app.Driver;
        var layout = d.Workspace.Layout;
        layout.Close(DockPanels.Assemblies);
        d.Show(d.Fixture("Outline"));
        var left = d.Workspace.Tabs.Active!;
        d.Show(d.Fixture("Outline"), inNewTab: true);
        var right = d.Workspace.Tabs.Active!;
        layout.Dock(right.Panel, layout.GroupOf(left.Panel)!, DockEdge.Right, 0.5f);
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
