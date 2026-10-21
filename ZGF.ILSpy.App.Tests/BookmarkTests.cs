using ZGF.Desktop;
using ZGF.KeyboardModule;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy.App.Tests;

public sealed class BookmarkTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ilspy-bookmarks-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static (WorkspaceDriver Driver, DocumentTab Tab, int Line) OutlineAt(string text, WorkspaceDriver? driver = null)
    {
        var d = driver ?? new WorkspaceDriver();
        d.OpenFixtures();
        d.Show(d.Fixture("Outline"));
        var tab = d.Workspace.ActiveTab!;
        return (d, tab, tab.Document.Find(text).Line);
    }

    [Fact]
    public void Toggling_a_statement_line_bookmarks_it_by_its_statement_and_toggling_again_removes_it()
    {
        var (d, tab, line) = OutlineAt("return First();");
        var bookmarks = d.Workspace.Bookmarks;

        d.Workspace.ToggleBookmark(tab, line);
        var bookmark = Assert.Single(bookmarks.Items);
        var marked = bookmarks.LinesIn(tab.Document, tab.Location!);
        d.Workspace.ToggleBookmark(tab, line);

        Assert.Equal("Bookmark 1", bookmark.Name);
        Assert.Equal(CodeAnchorKind.Statement, bookmark.Anchor!.Value.Kind);
        Assert.Equal("Outline:" + (line + 1), bookmark.Location);
        Assert.True(marked[line]);
        Assert.Empty(bookmarks.Items);
    }

    [Fact]
    public void A_bookmark_follows_its_statement_into_IL_and_into_the_methods_own_document()
    {
        var (d, tab, line) = OutlineAt("return First();");
        d.Workspace.ToggleBookmark(tab, line);
        var bookmarks = d.Workspace.Bookmarks;

        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Settle();
        var il = tab.Document.Lines[Assert.Single(bookmarks.LinesIn(tab.Document, tab.Location!)).Key].Text;
        d.Workspace.SetLanguage(DecompilationLanguage.CSharp);
        d.Show(d.Fixture("Outline").Children.Single(c => c.Text.StartsWith("Third()")));
        var own = tab.Document.Lines[Assert.Single(bookmarks.LinesIn(tab.Document, tab.Location!)).Key].Text;

        Assert.StartsWith("IL_", il.TrimStart());
        Assert.Contains("return First();", own);
    }

    [Fact]
    public void A_line_with_nothing_to_anchor_to_is_marked_only_where_it_was_made()
    {
        var (d, tab, line) = OutlineAt("return First();");
        var brace = line - 1;
        Assert.Equal("{", tab.Document.Lines[brace].Text.Trim());
        d.Workspace.ToggleBookmark(tab, brace);
        var bookmark = Assert.Single(d.Workspace.Bookmarks.Items);

        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Settle();

        Assert.Null(bookmark.Anchor);
        Assert.Empty(d.Workspace.Bookmarks.LinesIn(tab.Document, tab.Location!));
    }

    [Fact]
    public void Going_to_a_bookmark_shows_its_place_and_reveals_its_line()
    {
        var app = new AppDriver(height: 360f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Show(d.Fixture("Greeter"));
        app.Run(3);

        d.Workspace.GoTo(d.Workspace.Bookmarks.Items[0]);
        d.Settle();
        app.Run(4);

        Assert.Equal("Outline", tab.Node!.Text);
        Assert.Equal(line, tab.View.FlashLine);
        Assert.Equal("Outline", d.Workspace.History.Current!.Value.Place.Text);
    }

    [Fact]
    public void Next_and_previous_skip_disabled_bookmarks_and_wrap()
    {
        var (d, tab, line) = OutlineAt("return First();");
        var first = tab.Document.Find("foreach").Line;
        d.Workspace.ToggleBookmark(tab, first);
        d.Workspace.ToggleBookmark(tab, line);
        var bookmarks = d.Workspace.Bookmarks;
        bookmarks.SetEnabled(bookmarks.Items[1], false);

        var next = bookmarks.Step(1);
        var again = bookmarks.Step(1);
        var previous = bookmarks.Step(-1);

        Assert.Same(bookmarks.Items[0], next);
        Assert.Same(bookmarks.Items[0], again);
        Assert.Same(bookmarks.Items[0], previous);
        Assert.Equal("Bookmark 2", bookmarks.Items[1].Name);
    }

    [Fact]
    public void Bookmarks_are_saved_with_the_settings_and_found_again_on_the_next_run()
    {
        var path = Path.Combine(_directory, "settings.json");
        var settings = AppSettings.Load(path);
        settings.Locale = Locale.En;
        var (d, tab, line) = OutlineAt("return First();", new WorkspaceDriver(settings));
        d.Workspace.ToggleBookmark(tab, line);
        var bookmarks = d.Workspace.Bookmarks;
        bookmarks.StartRename(bookmarks.Items[0]);
        bookmarks.RenameText = "Calls First";
        bookmarks.CommitRename();
        d.Pump();

        var (next, nextTab, _) = OutlineAt("return First();", new WorkspaceDriver(AppSettings.Load(path)));
        var saved = Assert.Single(next.Workspace.Bookmarks.Items);

        Assert.Equal("Calls First", saved.Name);
        Assert.Equal(tab.Location, saved.Place);
        Assert.Equal(bookmarks.Items[0].Anchor, saved.Anchor);
        Assert.True(next.Workspace.Bookmarks.LinesIn(nextTab.Document, nextTab.Location!)[line]);
    }

    [Fact]
    public void Clicking_the_margin_asks_to_toggle_that_line_and_a_bookmarked_line_is_marked()
    {
        var view = new CodeViewDriver(CodeViewDriver.Outline(), 1200f);
        view.Run();
        view.Run();
        var (x, y) = view.MarkOf(3);
        var line = view.State.Folds.LineAt(3);

        view.Bookmarks = new Dictionary<int, bool> { [view.State.Folds.LineAt(5)] = true };
        view.Frames.MovePointer(x, y);
        view.Run();
        view.Frames.Press();
        var pressed = view.Run();
        view.Frames.Release();
        view.Run();

        Assert.Equal(line, pressed.ToggleBookmark);
        Assert.Equal(2, view.Frames.Canvas.Texts.Count(t => t.Inputs.Text == LucideIcons.Bookmark));
        Assert.False(view.State.HasSelection);
    }

    [Fact]
    public void Without_bookmarks_the_margin_toggles_nothing()
    {
        var view = new CodeViewDriver(CodeViewDriver.Outline(), 1200f);
        view.Run();
        var (x, y) = view.MarkOf(3);

        view.Frames.MovePointer(x, y);
        view.Run();
        view.Frames.Press();
        var pressed = view.Run();
        view.Frames.Release();
        view.Run();

        Assert.Null(pressed.ToggleBookmark);
        Assert.DoesNotContain(view.Frames.Canvas.Texts, t => t.Inputs.Text == LucideIcons.Bookmark);
    }

    [Fact]
    public void The_bookmarks_panel_lists_bookmarks_and_a_double_click_goes_to_one()
    {
        var app = new AppDriver(height: 500f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Show(d.Fixture("Greeter"));
        d.Workspace.OpenBookmarks();
        app.Run(4);

        Assert.True(app.Frames.Drew("Bookmark 1"));
        Assert.True(app.Frames.Drew("Outline:" + (line + 1)));
        app.DoubleClick("Bookmark 1");
        d.Settle();
        app.Run(4);

        Assert.Equal("Outline", tab.Node!.Text);
        Assert.Equal(line, tab.View.FlashLine);
    }

    [Fact]
    public void F2_renames_the_selected_bookmark_in_place_and_Enter_keeps_the_name()
    {
        var app = new AppDriver(height: 500f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Workspace.OpenBookmarks();
        app.Run(3);
        app.Click("Bookmark 1");

        app.Frames.Key(KeyboardKey.F2);
        app.Run(2);
        app.Frames.Type("Calls First");
        app.Run(2);
        app.Frames.Key(KeyboardKey.Enter);
        app.Run(2);

        Assert.Equal("Calls First", d.Workspace.Bookmarks.Items[0].Name);
        Assert.Null(d.Workspace.Bookmarks.Renaming);
        Assert.True(app.Frames.Drew("Calls First"));
    }

    [Fact]
    public void The_bookmarks_panel_is_a_table_whose_header_lines_up_with_its_rows()
    {
        var app = new AppDriver(height: 500f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Workspace.OpenBookmarks();
        app.Run(4);

        Assert.Equal(app.TextRect("Name").X, app.TextRect("Bookmark 1").X);
        Assert.Equal(app.TextRect("Location").X, app.TextRect("Outline:" + (line + 1)).X);
        var module = app.Frames.Canvas.Texts.Single(t => t.Inputs.Text.EndsWith(".dll", StringComparison.Ordinal)).Inputs.Position.Left;
        Assert.Equal(app.TextRect("Module").X, module);
    }

    [Fact]
    public void The_bookmark_column_widths_are_saved_with_the_settings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var settings = AppSettings.Load(path);
        settings.Locale = Locale.En;
        var w = new WorkspaceDriver(settings).Workspace;
        w.BookmarkColumns.Restore([1f, 1f, 4f, 1f]);
        w.SaveSettings();

        var restored = new WorkspaceDriver(AppSettings.Load(path)).Workspace.BookmarkColumns.Weights;

        Assert.Equal([1f, 1f, 4f, 1f], restored);
    }
}
