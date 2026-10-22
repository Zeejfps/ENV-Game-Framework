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
    public void The_bookmarks_panel_lists_bookmarks_and_Ctrl_clicking_a_location_goes_to_one()
    {
        var app = new AppDriver(height: 500f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Show(d.Fixture("Greeter"));
        d.Workspace.OpenBookmarks();
        app.Run(4);

        Assert.True(app.Frames.Drew("Bookmark 1"));
        Click(app, "Outline:" + (line + 1));
        d.Settle();
        Assert.Equal("Greeter", tab.Node!.Text);
        Click(app, "Outline:" + (line + 1), KeyModifiers.Control);
        d.Settle();
        app.Run(4);

        Assert.Equal("Outline", tab.Node!.Text);
        Assert.Equal(line, tab.View.FlashLine);
    }

    private static void Click(AppDriver app, string text, KeyModifiers modifiers = KeyModifiers.None)
    {
        var at = app.TextRect(text);
        app.Frames.Wait(1);
        app.Frames.MovePointer(at.X + at.Width / 2, at.Y + at.Height / 2);
        app.Run();
        app.Frames.Press(modifiers);
        app.Run();
        app.Frames.Release(modifiers: modifiers);
        app.Run(2);
    }

    private static (AppDriver App, WorkspaceDriver Driver) ShowBookmark()
    {
        var app = new AppDriver(height: 500f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Workspace.OpenBookmarks();
        app.Run(3);
        return (app, d);
    }

    [Fact]
    public void Hovering_a_location_shows_where_it_points_how_to_follow_it_and_a_link_icon_that_goes_there()
    {
        var (app, d) = ShowBookmark();
        var tab = d.Workspace.ActiveTab!;
        d.Show(d.Fixture("Greeter"));
        app.Run(2);
        var location = app.TextRect(d.Workspace.Bookmarks.Items[0].Location);

        app.Frames.MovePointer(location.X + 2f, location.Y + location.Height / 2);
        for (var i = 0; i < 90; i++)
            app.Run();
        Assert.True(app.Frames.Drew(Strings.En.BookmarksGoToHint));
        var icon = app.TextRect(LucideIcons.ExternalLink);

        app.Frames.MovePointer(icon.X + icon.Width / 2, icon.Y + icon.Height / 2);
        app.Run();
        app.Frames.Press();
        app.Run();
        app.Frames.Release();
        app.Run();
        d.Settle();

        Assert.Equal("Outline", tab.Node!.Text);
    }

    [Fact]
    public void Clicking_a_name_selects_it_to_type_over_and_Enter_keeps_the_name()
    {
        var (app, d) = ShowBookmark();

        Click(app, "Bookmark 1");
        app.Frames.Type("Calls First");
        app.Run(2);
        app.Frames.Key(KeyboardKey.Enter);
        app.Run(2);

        Assert.Equal("Calls First", d.Workspace.Bookmarks.Items[0].Name);
        Assert.Null(d.Workspace.Bookmarks.Renaming);
        Assert.True(app.Frames.Drew("Calls First"));
    }

    [Fact]
    public void Escape_leaves_a_clicked_name_as_it_was()
    {
        var (app, d) = ShowBookmark();

        Click(app, "Bookmark 1");
        app.Frames.Type("Gone");
        app.Run(2);
        app.Frames.Key(KeyboardKey.Escape);
        app.Run(2);

        Assert.Equal("Bookmark 1", d.Workspace.Bookmarks.Items[0].Name);
        Assert.Null(d.Workspace.Bookmarks.Renaming);
    }

    [Fact]
    public void Clicking_a_location_selects_its_text_for_Ctrl_C()
    {
        var (app, d) = ShowBookmark();
        var location = d.Workspace.Bookmarks.Items[0].Location;

        Click(app, location);
        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control);
        app.Run();

        Assert.Equal(location, app.Frames.Clipboard.Text);
        Assert.Null(d.Workspace.Bookmarks.Renaming);
    }

    [Fact]
    public void F2_renames_the_selected_bookmark_in_place_and_Enter_keeps_the_name()
    {
        var app = new AppDriver(height: 500f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Workspace.OpenBookmarks();
        app.Run(3);
        Click(app, d.Workspace.Bookmarks.Items[0].Location);

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

    private string Exported(params string[] lines)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "exported.json");
        var (d, tab, _) = OutlineAt("return First();");
        foreach (var text in lines)
            d.Workspace.ToggleBookmark(tab, tab.Document.Find(text).Line);
        d.Workspace.ExportBookmarks(path);
        Assert.Empty(d.Workspace.Errors.Entries);
        return path;
    }

    [Fact]
    public void Exported_bookmarks_import_at_once_into_a_workspace_with_none()
    {
        var path = Exported("return First();", "foreach");
        var (d, tab, line) = OutlineAt("return First();");

        d.Workspace.ImportBookmarks(path);
        d.Settle();

        Assert.Null(d.Workspace.PendingImport);
        Assert.Equal(["Bookmark 1", "Bookmark 2"], d.Workspace.Bookmarks.Items.Select(b => b.Name));
        Assert.True(d.Workspace.Bookmarks.LinesIn(tab.Document, tab.Location!)[line]);
    }

    [Fact]
    public void Importing_beside_bookmarks_asks_then_merges_only_new_lines_or_replaces()
    {
        var path = Exported("return First();", "foreach");
        var (d, tab, line) = OutlineAt("return First();");
        var bookmarks = d.Workspace.Bookmarks;
        d.Workspace.ToggleBookmark(tab, line);
        bookmarks.StartRename(bookmarks.Items[0]);
        bookmarks.RenameText = "Mine";
        bookmarks.CommitRename();

        d.Workspace.ImportBookmarks(path);
        d.Settle();
        var asked = d.Workspace.PendingImport != null;
        d.Workspace.FinishImport(BookmarkImportMode.Merge);
        var merged = bookmarks.Items.Select(b => b.Name).ToList();
        d.Workspace.ImportBookmarks(path);
        d.Settle();
        d.Workspace.FinishImport(BookmarkImportMode.Replace);

        Assert.True(asked);
        Assert.Equal(["Mine", "Bookmark 2"], merged);
        Assert.Equal(["Bookmark 1", "Bookmark 2"], bookmarks.Items.Select(b => b.Name));
        Assert.Null(d.Workspace.PendingImport);
    }

    [Fact]
    public void A_file_that_is_not_a_bookmarks_file_is_reported_and_changes_nothing()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "other.json");
        File.WriteAllText(path, "{ \"assemblies\": [] }");
        var (d, tab, line) = OutlineAt("return First();");
        d.Workspace.ToggleBookmark(tab, line);

        d.Workspace.ImportBookmarks(path);
        d.Settle();

        Assert.Single(d.Workspace.Bookmarks.Items);
        Assert.Null(d.Workspace.PendingImport);
        Assert.Equal("Cannot import bookmarks from other.json", Assert.Single(d.Workspace.Errors.Entries).Title);
    }

    [Fact]
    public void Import_in_the_panel_picks_a_file_and_the_dialog_merges_it()
    {
        var path = Exported("foreach");
        var app = new AppDriver(height: 500f);
        var (d, tab, line) = OutlineAt("return First();", app.Driver);
        d.Workspace.ToggleBookmark(tab, line);
        d.Workspace.OpenBookmarks();
        app.Run(3);
        app.Frames.FilePicker.Answer = path;

        app.Click(LucideIcons.Download);
        d.Settle();
        app.Run(2);
        var asked = app.Frames.Drew("Import bookmarks");
        app.Click("Merge");
        app.Run(2);

        Assert.True(asked);
        Assert.Equal(2, d.Workspace.Bookmarks.Items.Count);
        Assert.False(app.Frames.Drew("Merge"));
    }

    private const string Fixtures = WorkspaceDriver.FixturesNamespace;

    private static int TokenOf(string method) => typeof(Fixtures.Outline).GetMethod(method)!.MetadataToken;

    private string ILSpyFile(string records)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "ILSpy.Bookmarks.json");
        File.WriteAllText(path, $$"""{ "Version": 1, "Bookmarks": [ {{records}} ] }""");
        return path;
    }

    private static string Record(string name, string kind, int token, int ilOffset, int lineNumber, string treePath, string? file = null) =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["Name"] = name, ["Enabled"] = true, ["FileName"] = file ?? WorkspaceDriver.FixturePath, ["AssemblyFullName"] = "ZGF.ILSpy.App.Tests",
            ["ModuleName"] = "ZGF.ILSpy.App.Tests.dll", ["Token"] = token, ["Kind"] = kind, ["ILOffset"] = ilOffset, ["LineNumber"] = lineNumber,
            ["MemberName"] = Fixtures + ".Outline", ["LocationNodeName"] = Fixtures + ".Outline",
            ["ViewState"] = new Dictionary<string, object?>
            {
                ["Version"] = 1, ["CaretOffset"] = 0, ["VerticalOffset"] = 0.0, ["HorizontalOffset"] = 0.0, ["FoldingChecksum"] = null,
                ["ExpandedFoldings"] = null, ["RenderedLayoutSettings"] = null,
                ["SelectedTreeNodePath"] = new[] { file ?? WorkspaceDriver.FixturePath, Fixtures, treePath },
            },
        });

    [Fact]
    public void Exports_are_written_in_ILSpys_bookmark_format()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "exported.json");
        var (d, tab, line) = OutlineAt("return First();");
        d.Workspace.ToggleBookmark(tab, line);

        d.Workspace.ExportBookmarks(path);
        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var root = json.RootElement;
        var entry = root.GetProperty("Bookmarks")[0];

        Assert.Equal(1, root.GetProperty("Version").GetInt32());
        Assert.Equal(
            ["Name", "Enabled", "FileName", "AssemblyFullName", "ModuleName", "Token", "Kind", "ILOffset", "LineNumber", "MemberName", "LocationNodeName",
                "ViewState", "Place", "Language"],
            entry.EnumerateObject().Select(p => p.Name));
        Assert.Equal("Body", entry.GetProperty("Kind").GetString());
        Assert.Equal((uint)TokenOf("Third"), entry.GetProperty("Token").GetUInt32());
        Assert.Equal(line + 1, entry.GetProperty("LineNumber").GetInt32());
        Assert.Equal(WorkspaceDriver.FixturePath, entry.GetProperty("FileName").GetString());
        Assert.Equal("ZGF.ILSpy.App.Tests.dll", entry.GetProperty("ModuleName").GetString());
        Assert.Equal(Fixtures + ".Outline.Third", entry.GetProperty("MemberName").GetString());
        Assert.Equal(Fixtures + ".Outline", entry.GetProperty("LocationNodeName").GetString());
        Assert.Equal([WorkspaceDriver.FixturePath, Fixtures, Fixtures + ".Outline"],
            entry.GetProperty("ViewState").GetProperty("SelectedTreeNodePath").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void A_file_ILSpy_wrote_imports_with_each_bookmark_placed_by_ILSpys_tree_path()
    {
        var path = ILSpyFile(Record("Bookmark0", "Body", TokenOf("Third"), 0, 99, Fixtures + ".Outline") + "," +
                             Record("Bookmark1", "Line", TokenOf("Third"), 0, 3, Fixtures + ".Outline"));
        var (d, tab, line) = OutlineAt("return First();");

        d.Workspace.ImportBookmarks(path);
        d.Settle();
        var bookmarks = d.Workspace.Bookmarks.Items;
        var marked = d.Workspace.Bookmarks.LinesIn(tab.Document, tab.Location!);

        Assert.Equal(["Bookmark0", "Bookmark1"], bookmarks.Select(b => b.Name));
        Assert.All(bookmarks, b => Assert.Equal(tab.Location, b.Place));
        Assert.True(marked[line]);
        Assert.True(marked[2]);
        Assert.Equal("Bookmark0", Assert.Single(d.Workspace.Bookmarks.Save(), r => r.Kind == BookmarkKind.Body).Name);
    }

    [Fact]
    public void A_bookmark_whose_tree_path_is_stale_is_placed_by_its_token_and_one_with_no_file_is_reported()
    {
        var gone = Record("Gone", "Token", TokenOf("Third"), 0, 1, Fixtures + ".Outline", Path.Combine(_directory, "gone.dll"));
        var path = ILSpyFile(Record("Moved", "Token", TokenOf("Third"), 0, 1, Fixtures + ".Renamed") + "," + gone);
        var d = new WorkspaceDriver();

        d.Workspace.ImportBookmarks(path);
        d.Settle();

        var moved = Assert.Single(d.Workspace.Bookmarks.Items);
        Assert.Equal("Moved", moved.Name);
        Assert.StartsWith("Third()", moved.Place.Text);
        Assert.Equal("Some bookmarks from ILSpy.Bookmarks.json were not found and were left out", Assert.Single(d.Workspace.Errors.Entries).Title);
    }
}
