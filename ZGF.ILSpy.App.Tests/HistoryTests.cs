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

    [Fact]
    public void Going_to_an_entry_jumps_over_the_ones_between_and_keeps_them_all()
    {
        var app = Opened();
        var history = app.Workspace.History;
        Show(app, app.Driver.Fixture("Outline"));
        var first = history.CurrentIndex;
        Show(app, app.Driver.Fixture("Greeter"));
        Show(app, app.Driver.Fixture("Outline"));
        var count = history.Entries.Count;

        app.Workspace.GoTo(first);
        Settle(app);
        var back = app.Workspace.ActiveTab!.Node!.Text;

        Assert.Equal("Outline", back);
        Assert.Equal(first, history.CurrentIndex);
        Assert.Equal(count, history.Entries.Count);
        Assert.True(history.CanGoForward);
    }

    [Fact]
    public void The_back_dropdown_lists_earlier_places_nearest_first_and_goes_to_the_one_picked()
    {
        var app = Opened();
        Show(app, app.Driver.Fixture("Outline"));
        var first = app.Workspace.History.CurrentIndex;
        Show(app, app.Driver.Fixture("Greeter"));
        Show(app, app.Driver.Fixture("Outline"));

        ClickBackDropdown(app);
        var greeter = app.Frames.Canvas.Texts.Last(t => t.Inputs.Text == "Greeter").Inputs.Position;
        var outline = app.Frames.Canvas.Texts.Last(t => t.Inputs.Text == "Outline").Inputs.Position;
        app.ClickMenuItem("Outline");
        Settle(app);

        Assert.True(greeter.Top > outline.Top);
        Assert.Equal("Outline", app.Workspace.ActiveTab!.Node!.Text);
        Assert.Equal(first, app.Workspace.History.CurrentIndex);
    }

    [Fact]
    public void Going_somewhere_while_the_back_dropdown_is_open_closes_it()
    {
        var app = Opened();
        Show(app, app.Driver.Fixture("Outline"));
        Show(app, app.Driver.Fixture("Greeter"));
        ClickBackDropdown(app);
        var row = app.Frames.Canvas.Texts.Last(t => t.Inputs.Text == "Outline").Inputs.Position;

        app.Workspace.GoBack();
        Settle(app);

        Assert.DoesNotContain(app.Frames.Canvas.Texts, t => t.Inputs.Position.Top == row.Top && t.Inputs.Position.Left == row.Left);
        Assert.Equal("Outline", app.Workspace.ActiveTab!.Node!.Text);
    }

    private static void ClickBackDropdown(AppDriver app)
    {
        var theme = AppThemes.For(app.Workspace.DarkMode).Ui;
        var icon = app.TextRect(LucideIcons.ArrowLeft);
        var x = icon.X + icon.Width / 2 + theme.IconButtonSize / 2 + theme.DropdownButtonWidth / 2;
        app.Frames.MovePointer(x, icon.Y + icon.Height / 2);
        app.Run();
        app.Frames.Press();
        app.Run();
        app.Frames.Release();
        app.Run(2);
    }
}
