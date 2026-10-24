using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class HistoryTests
{
    private static AppDriver Opened()
    {
        var app = AppDriver.Opened(height: 360f);
        app.Run(3);
        return app;
    }

    private static void Show(AppDriver app, string type) => app.Show(app.Fixture(type), 4);

    private static CodeViewPosition ScrollTo(AppDriver app, int topLine, TextPosition anchor, TextPosition caret)
    {
        CodeOf(app, app.Workspace.Tabs.Active!).Restore(new CodeViewPosition(topLine, 0f, anchor, caret));
        app.Run(4);
        return CodeOf(app, app.Workspace.Tabs.Active!).Position;
    }

    private static CodeViewState CodeOf(AppDriver app, DocumentTab tab) => app.Ui.ViewOf(tab).Code;

    [Fact]
    public void Going_back_and_forward_returns_each_place_to_where_it_was_left()
    {
        var app = Opened();
        Show(app, "Outline");
        var outline = ScrollTo(app, 20, new TextPosition(21, 4), new TextPosition(22, 8));
        Show(app, "Greeter");
        var greeter = ScrollTo(app, 3, new TextPosition(4, 0), new TextPosition(4, 6));

        app.Workspace.GoBack();
        app.Settle(4);
        var back = app.Workspace.Tabs.Active!.Node!.Text;
        var backPosition = CodeOf(app, app.Workspace.Tabs.Active!).Position;
        app.Workspace.GoForward();
        app.Settle(4);
        var forward = app.Workspace.Tabs.Active!;

        Assert.Equal(20, outline.TopLine);
        Assert.Equal("Outline", back);
        Assert.Equal(outline, backPosition);
        Assert.Equal("Greeter", forward.Node!.Text);
        Assert.Equal(greeter, CodeOf(app, forward).Position);
    }

    [Fact]
    public void Going_back_from_a_followed_reference_returns_to_where_it_was_followed_from()
    {
        var app = Opened();
        Show(app, "Outline");
        var tab = app.Workspace.Tabs.Active!;
        var (line, column) = tab.Document.Find("return First();");
        var reference = tab.Document.SpanAt(line, column + "return ".Length).Reference!;
        var before = ScrollTo(app, 25, new TextPosition(line, column), new TextPosition(line, column + 6));

        app.Workspace.Navigate(reference, inNewTab: false);
        app.Settle(4);
        var followed = tab.Node!.Text;
        app.Workspace.GoBack();
        app.Settle(4);

        Assert.StartsWith("First()", followed);
        Assert.Equal("Outline", tab.Node!.Text);
        Assert.Equal(before, CodeOf(app, tab).Position);
    }

    [Fact]
    public void A_position_left_in_another_language_is_not_applied()
    {
        var app = Opened();
        Show(app, "Outline");
        ScrollTo(app, 20, new TextPosition(21, 4), new TextPosition(22, 8));
        Show(app, "Greeter");

        app.Workspace.SetLanguage(DecompilationLanguage.IL);
        app.Settle(4);
        app.Workspace.GoBack();
        app.Settle(4);
        var tab = app.Workspace.Tabs.Active!;

        Assert.Equal("Outline", tab.Node!.Text);
        Assert.Equal(DecompilationLanguage.IL, tab.Language);
        Assert.False(CodeOf(app, tab).HasSelection);
        Assert.Equal(0, CodeOf(app, tab).Position.TopLine);
    }

    [Fact]
    public void Going_to_an_entry_jumps_over_the_ones_between_and_keeps_them_all()
    {
        var app = Opened();
        var history = app.Workspace.History;
        Show(app, "Outline");
        var first = history.CurrentIndex;
        Show(app, "Greeter");
        Show(app, "Outline");
        var count = history.Entries.Count;

        app.Workspace.GoTo(first);
        app.Settle(4);
        var back = app.Workspace.Tabs.Active!.Node!.Text;

        Assert.Equal("Outline", back);
        Assert.Equal(first, history.CurrentIndex);
        Assert.Equal(count, history.Entries.Count);
        Assert.True(history.CanGoForward);
    }

    [Fact]
    public void The_back_dropdown_lists_earlier_places_nearest_first_and_goes_to_the_one_picked()
    {
        var app = Opened();
        Show(app, "Outline");
        var first = app.Workspace.History.CurrentIndex;
        Show(app, "Greeter");
        Show(app, "Outline");

        ClickBackDropdown(app);
        var greeter = app.Frames.Canvas.Texts.Last(t => t.Inputs.Text == "Greeter").Inputs.Position;
        var outline = app.Frames.Canvas.Texts.Last(t => t.Inputs.Text == "Outline").Inputs.Position;
        app.ClickMenuItem("Outline");
        app.Settle(4);

        Assert.True(greeter.Top > outline.Top);
        Assert.Equal("Outline", app.Workspace.Tabs.Active!.Node!.Text);
        Assert.Equal(first, app.Workspace.History.CurrentIndex);
    }

    [Fact]
    public void Going_somewhere_while_the_back_dropdown_is_open_closes_it()
    {
        var app = Opened();
        Show(app, "Outline");
        Show(app, "Greeter");
        ClickBackDropdown(app);
        var row = app.Frames.Canvas.Texts.Last(t => t.Inputs.Text == "Outline").Inputs.Position;

        app.Workspace.GoBack();
        app.Settle(4);

        Assert.DoesNotContain(app.Frames.Canvas.Texts, t => t.Inputs.Position.Top == row.Top && t.Inputs.Position.Left == row.Left);
        Assert.Equal("Outline", app.Workspace.Tabs.Active!.Node!.Text);
    }

    [Fact]
    public void The_back_dropdown_shows_each_place_with_its_tree_icon()
    {
        var app = Opened();
        var outline = app.Fixture("Outline");
        app.Show(outline, 4);
        Show(app, "Greeter");

        ClickBackDropdown(app);
        var row = app.Frames.Canvas.Texts.Last(t => t.Inputs.Text == "Outline").Inputs.Position;
        var glyph = NodeIcons.Glyph(outline.Kind);

        Assert.Contains(app.Frames.Canvas.Texts, t => t.Inputs.Text == glyph && t.Inputs.Position.Left < row.Left
            && MathF.Abs(t.Inputs.Position.Top - t.Inputs.Position.Height / 2 - (row.Top - row.Height / 2)) < 2f);
    }

    private static void ClickBackDropdown(AppDriver app)
    {
        var icon = app.TextRect(LucideIcons.ArrowLeft);
        var x = icon.X + icon.Width / 2 + app.Theme.IconButtonSize / 2 + app.Theme.DropdownButtonWidth / 2;
        app.Frames.Click(x, icon.Y + icon.Height / 2).Frame();
    }
}
