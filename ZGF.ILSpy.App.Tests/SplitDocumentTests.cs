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

        Wheel(app, 250f, -5);
        Assert.True(left.View.ScrollY > 0f);
        Assert.Equal(0f, right.View.ScrollY);

        Click(app, 750f);
        Assert.Same(right, d.Workspace.ActiveTab);
        d.Show(d.Fixture("Greeter"));
        app.Run(2);
        Assert.Equal("Outline", left.Node!.Text);
        Assert.Equal("Greeter", right.Node!.Text);

        Click(app, 250f);
        Assert.Same(left, d.Workspace.ActiveTab);
        Assert.Same(left.Node, d.Workspace.Tree.View.Selected);
    }

    [Fact]
    public void Only_the_active_documents_tab_is_marked_in_the_accent()
    {
        var (app, left, _) = SideBySide();
        var theme = AppThemes.For(app.Workspace.Theme).Ui;

        Assert.Equal(theme.Accent, Indicator(app, app.LastTextRect("Outline")));
        Assert.Equal(theme.BorderStrong, Indicator(app, app.TextRect("Outline")));

        Click(app, 250f);

        Assert.Same(left, app.Workspace.ActiveTab);
        Assert.Equal(theme.Accent, Indicator(app, app.TextRect("Outline")));
        Assert.Equal(theme.BorderStrong, Indicator(app, app.LastTextRect("Outline")));
    }

    private static (AppDriver App, DocumentTab Left, DocumentTab Right) SideBySide()
    {
        var app = new AppDriver();
        var d = app.Driver;
        d.OpenFixtures();
        var layout = d.Workspace.Layout;
        layout.Close(DockPanels.Assemblies);
        d.Show(d.Fixture("Outline"));
        var left = d.Workspace.ActiveTab!;
        d.Show(d.Fixture("Outline"), inNewTab: true);
        var right = d.Workspace.ActiveTab!;
        layout.Dock(right.Panel, layout.GroupOf(left.Panel)!, DockEdge.Right, 0.5f);
        app.Run(3);
        return (app, left, right);
    }

    private static void Wheel(AppDriver app, float x, double by)
    {
        app.Frames.MovePointer(x, 400f);
        app.Run();
        app.Frames.Wheel(by);
        app.Run(2);
    }

    private static void Click(AppDriver app, float x)
    {
        app.Frames.MovePointer(x, 400f);
        app.Run();
        app.Frames.Press();
        app.Run();
        app.Frames.Release();
        app.Run(2);
    }

    private static uint Indicator(AppDriver app, UiRect title)
    {
        var theme = AppThemes.For(app.Workspace.Theme).Ui;
        return app.Frames.Canvas.Rects.Select(r => r.Inputs)
            .Where(r => Math.Abs(r.Position.Height - theme.ActiveIndicatorWidth) < 0.01f)
            .Where(r => r.Position.Left <= title.X && r.Position.Left + r.Position.Width >= title.Right)
            .Where(r => app.Frames.Height - r.Position.Top <= title.Y)
            .Select(r => r.Style.BackgroundColor)
            .Last();
    }
}
