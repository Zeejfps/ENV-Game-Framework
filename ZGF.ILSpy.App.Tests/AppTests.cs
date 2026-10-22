using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
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
    public void Dragging_a_document_tab_onto_the_assemblies_tab_docks_it_beside_the_assemblies_as_a_tab()
    {
        var app = OpenFixtures();
        app.Run(2);
        var shown = app.Workspace.ActiveTab!;
        var label = app.Workspace.Tree.Label(shown.Node!);
        var tab = app.Frames.Canvas.Texts.Where(t => t.Inputs.Text == label).Select(t => t.Inputs.Position).MaxBy(p => p.Left)!;
        var (x, y) = (tab.Left + tab.Width / 2, app.Frames.Height - tab.Top + tab.Height / 2);
        var assemblies = app.TextRect("Assemblies");
        var layout = app.Workspace.Layout;

        app.Frames.MovePointer(x, y);
        app.Run();
        app.Frames.Press();
        app.Run();
        app.Frames.MovePointer(x - 20f, y + 20f);
        app.Run();
        app.Frames.MovePointer(assemblies.X + assemblies.Width + 4f, assemblies.Y + assemblies.Height / 2);
        app.Run(2);
        app.Frames.Release();
        app.Run(2);

        Assert.Same(layout.GroupOf(DockPanels.Assemblies), layout.GroupOf(shown.Panel));
        Assert.Equal(shown.Panel, layout.GroupOf(shown.Panel)!.Active);
        Assert.IsType<DockGroup>(layout.Root);
        Assert.Same(shown, app.Workspace.ActiveTab);
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
    public void Clicking_a_search_header_sorts_by_it_and_again_reverses_it_keeping_the_selection()
    {
        var app = OpenFixtures();
        app.Workspace.OpenSearch();
        app.Run();
        app.Frames.Type("Greet");
        app.Run();
        app.Frames.Wait(1);
        RunUntil(app, () => app.Workspace.Search is { IsBusy: false, Rows.Count: > 2 });
        var search = app.Workspace.Search;
        search.Select(search.Rows.Count - 1);
        var selected = search.SelectedRow!.Result.Node;
        var names = search.Rows.Select(r => r.Result.Text).ToList();

        app.Click("Name");
        app.Run();
        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase), search.Rows.Select(r => r.Result.Text));
        Assert.Same(selected, search.SelectedRow!.Result.Node);
        Assert.True(app.Frames.Drew(LucideIcons.ChevronUp));

        app.Click("Name");
        app.Run();
        Assert.Equal(names.OrderDescending(StringComparer.OrdinalIgnoreCase), search.Rows.Select(r => r.Result.Text));
        Assert.Same(selected, search.SelectedRow!.Result.Node);
        Assert.True(app.Frames.Drew(LucideIcons.ChevronDown));

        app.Click("Assembly");
        app.Run();
        Assert.Equal(SearchSort.Assembly, search.Sort);
        Assert.False(search.Descending);
    }

    [Fact]
    public void Search_kinds_are_checked_on_and_off_in_one_open_menu()
    {
        var app = OpenFixtures();
        app.Workspace.OpenSearch();
        app.Run();
        app.Frames.Type("Greet");
        app.Run();
        app.Frames.Wait(1);
        RunUntil(app, () => app.Workspace.Search is { IsBusy: false, Rows.Count: > 0 });
        Assert.Contains(app.Workspace.Search.Rows, r => r.Result.Node.Kind == NodeKind.Class);

        app.Click("Types and members");
        app.Run();
        foreach (var kind in new[] { "Types", "Fields", "Properties", "Events" })
        {
            app.ClickMenuItem(kind);
            app.Run();
        }
        Assert.True(app.Frames.Drew("Namespaces"));
        var events = app.LastTextRect("Events");
        var icon = app.LastTextRect(NodeIcons.Glyph(NodeKind.Event));
        Assert.True(icon.Right <= events.X && events.X - icon.Right < 16f, $"the events icon ends at {icon.Right}, its label starts at {events.X}");
        Assert.True(MathF.Abs(icon.Y + icon.Height / 2 - (events.Y + events.Height / 2)) < 4f);
        app.Frames.Key(KeyboardKey.Escape);
        app.Run(2);
        RunUntil(app, () => app.Workspace.Search is { IsBusy: false, Rows.Count: > 0 });

        Assert.Equal(SearchKinds.Methods, app.Workspace.Search.Kinds);
        Assert.All(app.Workspace.Search.Rows, r => Assert.Equal(NodeKind.Method, r.Result.Node.Kind));
        Assert.True(app.Frames.Drew("Methods"));
        Assert.False(app.Frames.Drew("Namespaces"));
    }

    [Fact]
    public void All_kinds_stands_alone_and_comes_back_when_the_last_kind_is_unchecked()
    {
        var app = OpenFixtures();
        app.Workspace.OpenSearch();
        app.Run(2);

        app.Click("Types and members");
        app.Run();
        app.ClickMenuItem("All");
        app.Run();
        Assert.Equal(SearchKinds.All, app.Workspace.Search.Kinds);
        Assert.True(app.Frames.Drew("All kinds"));

        app.ClickMenuItem("Resources");
        app.Run();
        Assert.Equal(SearchKinds.Resources, app.Workspace.Search.Kinds);
        app.ClickMenuItem("Namespaces");
        app.Run();
        Assert.True(app.Frames.Drew("Kinds · 2"));

        app.ClickMenuItem("Resources");
        app.Run();
        app.ClickMenuItem("Namespaces");
        app.Run();
        Assert.Equal(SearchKinds.All, app.Workspace.Search.Kinds);
    }

    [Fact]
    public void The_tree_menu_scopes_the_search_to_an_assembly()
    {
        var app = OpenFixtures();
        app.Run(2);
        var name = app.Workspace.Assemblies.Assemblies[0].Name;
        var row = app.TextRect(name);

        app.Frames.MovePointer(row.X + 2f, row.Y + row.Height / 2);
        app.Run();
        app.Frames.Press(PointerButton.Right);
        app.Run();
        app.Frames.Release(PointerButton.Right);
        app.Run(2);
        app.ClickMenuItem("Scope search to this assembly");
        app.Run(2);

        Assert.Equal($"inassembly:{name} ", app.Workspace.Search.Query);
        Assert.True(app.Workspace.Layout.Contains(DockPanels.Search));
    }

    [Fact]
    public void The_search_panel_holds_the_search_box_with_its_icon_and_no_shortcut_hint()
    {
        var app = new AppDriver();
        app.Run(2);
        Assert.False(app.Frames.Drew("Search…"));

        app.Workspace.OpenSearch();
        app.Run(2);

        var icon = app.LastTextRect(LucideIcons.Search);
        var placeholder = app.TextRect("Search…");

        Assert.True(icon.Right <= placeholder.X, $"the icon ends at {icon.Right}, the placeholder starts at {placeholder.X}");
        Assert.False(app.Frames.Drew("Ctrl+Shift+F"));
    }

    [Fact]
    public void The_toolbar_opens_an_assembly_and_the_search_panel_from_outlined_buttons_beside_each_other()
    {
        var app = new AppDriver();
        app.Run(2);
        var theme = AppThemes.For(app.Workspace.DarkMode).Ui;
        var open = app.TextRect(LucideIcons.FolderOpen);
        var search = app.TextRect(LucideIcons.Search);
        Assert.Equal(open.Y, search.Y, 1f);
        Assert.InRange(search.X - open.Right, 0f, theme.IconButtonSize);
        Assert.True(app.TextRect("C#").X > search.Right);
        Assert.Contains(app.Frames.Canvas.Rects, r => r.Inputs.Style.BorderColor.Top.Value == theme.Border
                                                      && r.Inputs.Position.Left <= open.X && r.Inputs.Position.Left + r.Inputs.Position.Width >= open.Right
                                                      && r.Inputs.Position.Width <= theme.IconButtonSize + 1f);

        app.Click(LucideIcons.FolderOpen);
        app.Run();
        Assert.Equal(["Open assembly"], app.Frames.FilePicker.Titles);

        app.Click(LucideIcons.Search);
        app.Run(2);
        Assert.True(app.Workspace.Layout.Contains(DockPanels.Search));
        Assert.True(app.Frames.Drew("Search…"));
    }

    [Fact]
    public void The_menu_bar_is_set_apart_from_the_toolbar_by_its_color_and_a_line()
    {
        var app = new AppDriver();
        app.Run(2);
        var theme = AppThemes.For(app.Workspace.DarkMode).Ui;
        var file = app.TextRect("File");
        var open = app.TextRect(LucideIcons.FolderOpen);
        var rects = app.Frames.Canvas.Rects.Select(r => (r.Inputs.Style.BackgroundColor, Rect: new UiRect(r.Inputs.Position.Left,
            app.Frames.Height - r.Inputs.Position.Top, r.Inputs.Position.Width, r.Inputs.Position.Height))).ToList();

        Assert.Contains(rects, r => r.BackgroundColor == theme.Background && r.Rect.Y <= file.Y && r.Rect.Bottom >= file.Bottom && r.Rect.Bottom <= open.Y);
        Assert.Contains(rects, r => r.BackgroundColor == theme.Separator && r.Rect.Y >= file.Bottom && r.Rect.Bottom <= open.Y);
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
    public void Search_results_are_a_table_of_name_location_and_assembly_that_truncates_at_a_narrow_width()
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
        var header = app.LastTextRect(LucideIcons.Search);
        var texts = app.Frames.Canvas.Texts.Select(t => t.Inputs).Select(t => (t.Text, Rect: new UiRect(t.Position.Left, app.Frames.Height - t.Position.Top, t.Position.Width, t.Position.Height))).ToList();
        var name = texts.Single(t => t.Text == row.Result.Text && t.Rect.Y > header.Bottom).Rect;
        var cells = texts.Where(t => t.Rect.Y == name.Y && t.Rect.X > name.Right).OrderBy(t => t.Rect.X).ToList();

        Assert.True(app.TextRect("Location").X < app.TextRect("Assembly").X);
        Assert.Equal(2, cells.Count);
        Assert.False(app.Frames.Drew(row.Location), $"'{row.Location}' was drawn in full");
        Assert.Contains('…', cells[0].Text);
        Assert.StartsWith(cells[1].Text.TrimEnd('…'), row.Assembly);
        Assert.True(cells[1].Rect.Right <= app.Frames.Width, $"the assembly ends at {cells[1].Rect.Right}");
    }

    [Fact]
    public void The_theme_is_picked_from_the_view_menus_theme_submenu()
    {
        var app = new AppDriver();
        app.Run(2);
        Assert.True(app.Workspace.DarkMode);

        app.Click("View");
        app.Click("Theme");
        app.Click("Light theme");

        Assert.False(app.Workspace.DarkMode);
    }

    [Fact]
    public void The_decompiler_is_picked_from_the_view_menus_decompiler_submenu()
    {
        var app = OpenFixtures();
        app.Run();

        app.Click("View");
        app.Click("Decompiler");
        app.ClickMenuItem("IL");
        app.Driver.Settle();

        Assert.Equal(DecompilationLanguage.IL, app.Workspace.Language);
    }

    [Fact]
    public void The_ui_language_is_picked_from_the_view_menus_language_submenu()
    {
        var app = new AppDriver();
        app.Run(2);

        app.Click("View");
        app.Click("Language");
        app.Click("Deutsch");

        Assert.Equal(Localization.Locale.De, app.Workspace.Locale);
    }
}
