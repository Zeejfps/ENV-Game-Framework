using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class HistoryTests
{
    private static AppDriver Opened()
    {
        var app = new AppDriver(height: 360f);
        app.Driver.OpenFixtures();
        app.Run(3);
        return app;
    }

    private static void Show(AppDriver app, BrowseNode node)
    {
        app.Driver.Show(node);
        app.Run(4);
    }

    private static void Settle(AppDriver app)
    {
        app.Driver.Settle();
        app.Run(4);
    }

    private static CodeViewPosition ScrollTo(AppDriver app, int topLine, TextPosition anchor, TextPosition caret)
    {
        app.Workspace.ActiveTab!.View.Restore(new CodeViewPosition(topLine, 0f, anchor, caret));
        app.Run(4);
        return app.Workspace.ActiveTab!.View.Position;
    }

    [Fact]
    public void Going_back_and_forward_returns_each_place_to_where_it_was_left()
    {
        var app = Opened();
        Show(app, app.Driver.Fixture("Outline"));
        var outline = ScrollTo(app, 20, new TextPosition(21, 4), new TextPosition(22, 8));
        Show(app, app.Driver.Fixture("Greeter"));
        var greeter = ScrollTo(app, 3, new TextPosition(4, 0), new TextPosition(4, 6));

        app.Workspace.GoBack();
        Settle(app);
        var back = app.Workspace.ActiveTab!.Node!.Text;
        var backPosition = app.Workspace.ActiveTab!.View.Position;
        app.Workspace.GoForward();
        Settle(app);
        var forward = app.Workspace.ActiveTab!;

        Assert.Equal(20, outline.TopLine);
        Assert.Equal("Outline", back);
        Assert.Equal(outline, backPosition);
        Assert.Equal("Greeter", forward.Node!.Text);
        Assert.Equal(greeter, forward.View.Position);
    }

    [Fact]
    public void Going_back_from_a_followed_reference_returns_to_where_it_was_followed_from()
    {
        var app = Opened();
        Show(app, app.Driver.Fixture("Outline"));
        var tab = app.Workspace.ActiveTab!;
        var (line, column) = tab.Document.Find("return First();");
        var reference = tab.Document.SpanAt(line, column + "return ".Length).Reference!;
        var before = ScrollTo(app, 25, new TextPosition(line, column), new TextPosition(line, column + 6));

        app.Workspace.Navigate(reference, inNewTab: false);
        Settle(app);
        var followed = tab.Node!.Text;
        app.Workspace.GoBack();
        Settle(app);

        Assert.StartsWith("First()", followed);
        Assert.Equal("Outline", tab.Node!.Text);
        Assert.Equal(before, tab.View.Position);
    }

    [Fact]
    public void A_position_left_in_another_language_is_not_applied()
    {
        var app = Opened();
        Show(app, app.Driver.Fixture("Outline"));
        ScrollTo(app, 20, new TextPosition(21, 4), new TextPosition(22, 8));
        Show(app, app.Driver.Fixture("Greeter"));

        app.Workspace.SetLanguage(DecompilationLanguage.IL);
        Settle(app);
        app.Workspace.GoBack();
        Settle(app);
        var tab = app.Workspace.ActiveTab!;

        Assert.Equal("Outline", tab.Node!.Text);
        Assert.Equal(DecompilationLanguage.IL, tab.Language);
        Assert.False(tab.View.HasSelection);
        Assert.Equal(0, tab.View.Position.TopLine);
    }
}
