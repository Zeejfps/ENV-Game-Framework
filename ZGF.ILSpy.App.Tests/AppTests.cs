using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class AppTests
{
    private static AppDriver OpenFixtures(float width = 1000f)
    {
        var app = new AppDriver(width);
        app.Workspace.Open(WorkspaceDriver.FixturePath);
        for (var i = 0; i < 2000 && app.Workspace.ActiveTab?.Node == null; i++)
        {
            app.Run();
            Thread.Sleep(1);
        }
        app.Driver.Settle();
        return app;
    }

    private static void RunUntil(AppDriver app, Func<bool> done)
    {
        for (var i = 0; i < 5000 && !done(); i++)
        {
            app.Run();
            Thread.Sleep(1);
        }
        Assert.True(done());
    }

    [Fact]
    public void Double_clicking_a_type_in_the_tree_shows_it_in_one_tab()
    {
        var app = OpenFixtures();
        var view = app.Workspace.Tree.View;
        view.SetExpanded(app.Driver.Node(), true);
        view.SetExpanded(app.Driver.Node(WorkspaceDriver.FixturesNamespace), true);
        view.Refresh();
        RunUntil(app, () => app.Frames.Drew("Greeter"));

        app.DoubleClick("Greeter");
        app.Driver.Settle();

        var tab = Assert.Single(app.Workspace.Tabs);
        Assert.Equal("Greeter", tab.Node!.Text);
        Assert.True(view.IsExpanded(tab.Node));
    }

    [Fact]
    public void A_long_error_never_pushes_the_status_bar_off_screen()
    {
        var app = new AppDriver();
        app.Workspace.ReportError(new string('x', 4000), string.Join('\n', Enumerable.Range(0, 500).Select(i => $"at line {i}")));

        app.Run(2);

        Assert.True(app.TextRect("1 error").Right <= app.Frames.Width);
        Assert.True(app.TextRect("0 assemblies").Right <= app.Frames.Width);
    }

    [Fact]
    public void At_a_narrow_width_the_error_indicator_joins_the_right_cluster_and_the_node_name_gives_way()
    {
        const float width = 480f;
        var app = OpenFixtures(width);
        var theme = AppThemes.Dark.Ui;
        app.Run(2);
        var count = app.StatusText("1 assembly");

        app.Workspace.ReportError("Cannot open broken.dll", "details");
        app.Run(2);
        var errors = app.StatusText("1 error");
        var language = app.StatusText("C#");
        var name = app.StatusTexts().MinBy(static r => r.X);

        Assert.Equal(count, app.StatusText("1 assembly"));
        Assert.True(width - count.Right <= theme.ShellPaddingX + 0.5f, $"the cluster ends at {count.Right}");
        Assert.True(errors.Right < language.X && language.X - errors.Right <= 3 * theme.GroupGap, $"'1 error' ends at {errors.Right}, 'C#' starts at {language.X}");
        Assert.True(name.X < errors.X && name.Right <= errors.X - theme.GapTight, $"the node name ends at {name.Right}, the indicator starts at {errors.X}");
    }

    [Fact]
    public void Hovering_the_error_indicator_shows_the_newest_error_in_short()
    {
        var app = new AppDriver();
        app.Workspace.ReportError("Cannot open broken.dll", "first\nsecond\nthird\nfourth");
        app.Run(2);

        app.MoveTo("1 error");
        app.Frames.Wait(2);
        app.Run(2);

        Assert.True(app.Frames.Drew("Cannot open broken.dll — first"));
    }

    [Fact]
    public void Clicking_the_error_indicator_opens_the_errors_to_copy_and_close()
    {
        var app = new AppDriver();
        app.Workspace.ReportError("First failure", "details one");
        app.Workspace.ReportError("Cannot open broken.dll", "System.BadImageFormatException: bad\n   at Somewhere()");
        app.Run(2);

        app.Click("2 errors");
        Assert.True(app.Workspace.Errors.IsOpen);
        app.Run();
        Assert.Equal("Cannot open broken.dll", app.Workspace.Errors.SelectedError!.Title);

        app.Click("Copy");
        Assert.Equal(app.Workspace.Errors.Entries[1].FullText, app.Frames.Clipboard.Text);

        app.Click("First failure");
        app.Click("Copy");
        Assert.StartsWith("First failure", app.Frames.Clipboard.Text);

        app.Click("Close");
        Assert.False(app.Workspace.Errors.IsOpen);
        Assert.Equal(2, app.Workspace.Errors.Entries.Count);
    }

    [Fact]
    public void The_language_control_switches_the_active_tab_to_IL()
    {
        var app = OpenFixtures();
        app.Run();

        app.Click("IL");
        app.Driver.Settle();

        Assert.Equal(DecompilationLanguage.IL, app.Workspace.Language);
        Assert.Equal(DecompilationLanguage.IL, app.Workspace.ActiveTab!.Language);
    }

    [Fact]
    public void Search_results_are_walked_with_the_keyboard_from_the_search_box()
    {
        var app = OpenFixtures();
        app.Workspace.OpenSearch();
        app.Run();
        app.Frames.Type("Greet");
        app.Run();
        app.Frames.Wait(1);
        RunUntil(app, () => app.Workspace.Search is { IsBusy: false, Rows.Count: > 2 });

        app.Frames.Key(KeyboardKey.DownArrow);
        app.Run();
        Assert.Equal(0, app.Workspace.Search.Selected);
        app.Frames.Key(KeyboardKey.DownArrow);
        app.Run();
        Assert.Equal(1, app.Workspace.Search.Selected);
        app.Frames.Key(KeyboardKey.Enter);
        app.Run();
        app.Driver.Settle();

        Assert.Same(app.Workspace.Search.Rows[1].Result.Node, app.Workspace.ActiveTab!.Node);
    }

    [Fact]
    public void The_search_box_draws_its_icon_and_shortcut_hint_inside_the_field()
    {
        var app = new AppDriver();
        app.Run(2);

        var icon = app.TextRect(LucideIcons.Search);
        var placeholder = app.TextRect("Search types and members");
        var hint = app.TextRect("Ctrl+Shift+F");

        Assert.True(icon.Right <= placeholder.X, $"the icon ends at {icon.Right}, the placeholder starts at {placeholder.X}");
        Assert.True(hint.X >= placeholder.X && hint.Right <= app.Frames.Width, $"the hint spans {hint.X}..{hint.Right}");
        Assert.Equal(icon.Y + icon.Height / 2, hint.Y + hint.Height / 2, 1f);
    }

    [Fact]
    public void Tree_rows_show_assembly_secondary_text_and_an_overlay_on_non_public_types()
    {
        var app = OpenFixtures();
        var view = app.Workspace.Tree.View;
        view.SetExpanded(app.Driver.Node(), true);
        view.SetExpanded(app.Driver.Node(WorkspaceDriver.FixturesNamespace), true);
        view.Refresh();
        RunUntil(app, () => app.Frames.Drew("Hidden"));

        var secondary = NodeText.Secondary(app.Driver.Node())!;
        Assert.True(app.TextRect(secondary).X > app.TextRect(app.Workspace.Assemblies.Assemblies[0].Name).Right);
        Assert.True(app.Frames.Drew(LucideIcons.Shield));
    }

    [Fact]
    public void At_a_narrow_width_a_search_result_keeps_its_name_and_its_location_gives_way()
    {
        var app = OpenFixtures(560f);
        app.Workspace.OpenSearch();
        app.Run();
        app.Frames.Type("Greeter");
        app.Run();
        app.Frames.Wait(1);
        RunUntil(app, () => app.Workspace.Search is { IsBusy: false, Rows.Count: > 0 });
        app.Run(2);

        var row = app.Workspace.Search.Rows.First(r => r.Result.Node.Kind == NodeKind.Class);
        var header = app.TextRect("Search");
        var texts = app.Frames.Canvas.Texts.Select(t => t.Inputs).Select(t => (t.Text, Rect: new UiRect(t.Position.Left, app.Frames.Height - t.Position.Top, t.Position.Width, t.Position.Height))).ToList();
        var name = texts.Single(t => t.Text == row.Result.Text && t.Rect.Y > header.Bottom).Rect;
        var location = texts.Single(t => t.Rect.Y == name.Y && t.Rect.X > name.Right);

        Assert.False(app.Frames.Drew(row.Location), $"'{row.Location}' was drawn in full");
        Assert.Contains('…', location.Text);
        Assert.True(location.Rect.Right <= app.Frames.Width, $"the location ends at {location.Rect.Right}");
    }
}
