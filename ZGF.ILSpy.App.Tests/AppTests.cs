using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class AppTests
{
    private static AppDriver OpenFixtures()
    {
        var app = new AppDriver();
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
}
