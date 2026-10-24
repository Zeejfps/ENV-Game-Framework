using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class AppTests
{
    private static AppDriver Searching(string text, int moreRowsThan, float width = 1000f)
    {
        var app = AppDriver.Opened(width);
        app.Workspace.OpenSearch();
        app.Run();
        app.Frames.Type(text).Frame().Wait(1);
        app.RunUntil(() => !app.Workspace.Search.IsBusy && app.Workspace.Search.Rows.Count > moreRowsThan);
        return app;
    }

    private static AppDriver ExpandedTree(string until)
    {
        var app = AppDriver.Opened();
        var view = app.Workspace.Tree.View;
        view.SetExpanded(app.Node(), true);
        view.SetExpanded(app.Node(WorkspaceDriver.FixturesNamespace), true);
        view.Refresh();
        app.RunUntil(() => app.Frames.Drew(until));
        return app;
    }

    [Fact]
    public void Double_clicking_a_type_in_the_tree_shows_it_in_one_tab()
    {
        var app = ExpandedTree(until: "Greeter");

        app.DoubleClick("Greeter");
        app.Settle();

        var tab = Assert.Single(app.Workspace.Tabs);
        Assert.Equal("Greeter", tab.Node!.Text);
        Assert.True(app.Workspace.Tree.View.IsExpanded(tab.Node));
    }

    [Fact]
    public void Dragging_a_document_tab_onto_the_assemblies_tab_docks_it_beside_the_assemblies_as_a_tab()
    {
        var app = AppDriver.Opened();
        app.Run(2);
        var shown = app.Workspace.Tabs.Active!;
        var label = app.Workspace.Tree.Label(shown.Node!);
        var tab = app.Frames.TextRects()
            .Where(t => t.Text == label)
            .Select(t => t.Rect)
            .MaxBy(r => r.X);
        var assemblies = app.TextRect("Assemblies");
        var layout = app.Workspace.Layout;
        var from = new PointF(tab.X + tab.Width / 2, tab.Y + tab.Height / 2);
        var aside = new PointF(from.X - 20f, from.Y + 20f);
        var onto = new PointF(assemblies.Right + 4f, assemblies.Y + assemblies.Height / 2);

        app.Frames.Drag(from, aside, onto).Frame().Release().Frame(2);

        Assert.Same(layout.GroupOf(DockPanels.Assemblies), layout.GroupOf(shown.Panel));
        Assert.Equal(shown.Panel, layout.GroupOf(shown.Panel)!.Active);
        Assert.IsType<DockGroup>(layout.Root);
        Assert.Same(shown, app.Workspace.Tabs.Active);
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
        var app = AppDriver.Opened(width);
        var theme = AppThemes.For(AppThemeId.Dark).Ui;
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

        app.MoveTo("1 error").Wait(2).Frame(2);

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
        var app = AppDriver.Opened();
        app.Run();

        app.Click("IL");
        app.Settle();

        Assert.Equal(DecompilationLanguage.IL, app.Workspace.Language);
        Assert.Equal(DecompilationLanguage.IL, app.Workspace.Tabs.Active!.Language);
    }

    [Fact]
    public void The_language_version_dropdown_decompiles_the_active_tab_again_and_hides_for_IL()
    {
        var app = AppDriver.Opened();
        app.Run();

        app.Click("C# 14.0 / VS 2026");
        app.ClickMenuItem("C# 5.0 / VS 2012");
        app.Settle(1);

        Assert.Equal(CSharpVersion.CSharp5, app.Workspace.DecompilerOptions.LanguageVersion);
        Assert.Equal(CSharpVersion.CSharp5, app.Workspace.Tabs.Active!.Options.LanguageVersion);
        Assert.Contains(app.Frames.Canvas.Texts, t => t.Inputs.Text == "C# 5.0 / VS 2012");

        app.Click("IL");
        app.Settle(1);

        Assert.DoesNotContain(app.Frames.Canvas.Texts, t => t.Inputs.Text == "C# 5.0 / VS 2012");
    }

    [Fact]
    public void Every_decompiler_toggle_has_a_label_in_every_language()
    {
        foreach (var (locale, _) in Locales.All)
        {
            var t = Strings.For(locale);
            Assert.All(DecompilerToggle.All, toggle => Assert.False(string.IsNullOrWhiteSpace(DecompilerOptionLabels.Label(t, toggle.Name)), toggle.Name));
        }
    }

    [Fact]
    public void A_decompiler_option_ticked_in_its_panel_shows_in_the_open_document_and_resets()
    {
        var app = AppDriver.Opened(width: 1600f);
        app.Show(app.Fixture("Greeter"), 1);
        FoldOptionGroupsBut(app, nameof(CSharpVersion.CSharp1));
        Assert.DoesNotContain("string.Concat", app.Workspace.Tabs.Active!.Document.GetText());

        app.Click("View");
        app.ClickMenuItem("Decompiler options").Frame();
        app.ClickStartingWith("String concatenation");
        app.Settle(1);

        Assert.False(app.Workspace.DecompilerOptions.IsOn(DecompilerToggle.Find("StringConcat")!));
        Assert.Contains("string.Concat", app.Workspace.Tabs.Active!.Document.GetText());

        app.Click(LucideIcons.RotateCcw);
        app.Settle(1);

        Assert.Equal(DecompilerOptions.Default, app.Workspace.DecompilerOptions);
        Assert.DoesNotContain("string.Concat", app.Workspace.Tabs.Active!.Document.GetText());
    }

    [Fact]
    public void Turning_debug_symbols_off_disassembles_an_open_IL_document_again()
    {
        var app = AppDriver.Opened();
        app.Workspace.SetLanguage(DecompilationLanguage.IL);
        app.Show(app.Fixture("Outline"), 1);
        var named = app.Workspace.Tabs.Active!.Document.GetText();

        app.Workspace.SetDecompilerToggle(DecompilerToggle.UseDebugSymbols, false);
        app.Settle(1);

        Assert.Contains("int32 total", named);
        Assert.DoesNotContain("int32 total", app.Workspace.Tabs.Active!.Document.GetText());
    }

    [Fact]
    public void Options_newer_than_the_version_are_greyed_out_and_groups_fold()
    {
        var app = AppDriver.Opened(width: 1600f);
        app.Workspace.SetLanguageVersion(CSharpVersion.CSharp7);
        FoldOptionGroupsBut(app, nameof(CSharpVersion.CSharp8));
        app.Workspace.OpenDecompilerOptions();
        app.Run();

        app.Click("switch expressions").Frame();
        Assert.Equal(DecompilerOptions.Default with { LanguageVersion = CSharpVersion.CSharp7 }, app.Workspace.DecompilerOptions);
        Assert.Contains(app.Frames.Canvas.Texts, t => t.Inputs.Text.StartsWith("Features newer than"));

        var texts = app.Frames.TextRects().ToList();
        var title = texts.FindIndex(t => t.Text == "C# 8.0 / VS 2019");
        var chevron = texts
            .Take(title)
            .Last(t => t.Text is LucideIcons.ChevronDown or LucideIcons.ChevronRight);
        app.Frames.Click(chevron.Rect).Frame();
        Assert.Contains(nameof(CSharpVersion.CSharp8), app.Workspace.FoldedOptionGroups);
        Assert.DoesNotContain(app.Frames.Canvas.Texts, t => t.Inputs.Text == "switch expressions");
    }

    [Fact]
    public void The_options_toolbar_collapses_and_expands_every_group()
    {
        var app = AppDriver.Opened(width: 1600f);
        app.Workspace.OpenDecompilerOptions();
        app.Run();

        app.ClickLast(LucideIcons.ChevronsDownUp);
        Assert.False(app.Frames.Drew("switch expressions"));
        Assert.True(app.Frames.Drew("C# 8.0 / VS 2019"));

        app.ClickLast(LucideIcons.ChevronsUpDown);
        Assert.Empty(app.Workspace.FoldedOptionGroups);
        Assert.True(app.Frames.Drew("Extension members"));
    }

    [Fact]
    public void A_group_checkbox_sets_every_option_in_it_and_shows_when_only_some_are_on()
    {
        var app = AppDriver.Opened(width: 1600f);
        FoldOptionGroupsBut(app, nameof(CSharpVersion.CSharp8));
        app.Workspace.OpenDecompilerOptions();
        app.Run();
        var csharp8 = DecompilerToggle.All.Where(t => t.IntroducedIn == CSharpVersion.CSharp8).ToArray();

        app.Click("C# 8.0 / VS 2019");
        app.Settle(1);
        Assert.All(csharp8, t => Assert.False(app.Workspace.DecompilerOptions.IsOn(t)));
        Assert.DoesNotContain(nameof(CSharpVersion.CSharp8), app.Workspace.FoldedOptionGroups);

        app.Click("switch expressions").Frame();
        Assert.Equal(csharp8.Length - 1, app.Workspace.DecompilerOptions.Toggles.Count);
        Assert.False(IsBold(app, "switch expressions"));
        Assert.True(IsBold(app, "Async iterators"));
        Assert.True(IsBold(app, "C# 8.0 / VS 2019"));
        Assert.False(IsBold(app, "C# 7.3 / VS 2017.7"));

        app.Click("C# 8.0 / VS 2019");
        app.Settle(1);
        Assert.Equal(DecompilerOptions.Default, app.Workspace.DecompilerOptions);
        Assert.False(IsBold(app, "C# 8.0 / VS 2019"));
    }

    private static bool IsBold(AppDriver app, string text) =>
        app.Frames.Canvas.Texts.Single(t => t.Inputs.Text == text).Inputs.Style.FontWeight.Value == FontWeight.Bold;

    private static void FoldOptionGroupsBut(AppDriver app, string open)
    {
        app.Workspace.FoldedOptionGroups.UnionWith(Enum.GetNames<CSharpVersion>().Append("Other").Append("FSharp"));
        app.Workspace.FoldedOptionGroups.Remove(open);
    }

    [Fact]
    public void Search_results_are_walked_with_the_keyboard_from_the_search_box()
    {
        var app = Searching("Greet", moreRowsThan: 2);

        var search = app.Workspace.Search;
        app.Frames.Key(KeyboardKey.DownArrow).Frame();
        Assert.Same(search.Rows[0], search.SelectedRow);
        app.Frames.Key(KeyboardKey.DownArrow).Frame();
        Assert.Same(search.Rows[1], search.SelectedRow);
        app.Frames.Key(KeyboardKey.Enter).Frame();
        app.Settle();

        Assert.Same(app.Workspace.Search.Rows[1].Node, app.Workspace.Tabs.Active!.Node);
    }

    [Fact]
    public void Clicking_a_search_header_sorts_by_it_then_reverses_it_then_goes_back_to_relevance_keeping_the_selection()
    {
        var app = Searching("Greet", moreRowsThan: 2);
        var search = app.Workspace.Search;
        search.Select(search.Rows[search.Rows.Count - 1]);
        var selected = search.SelectedRow!.Node;
        var names = search.Rows.Select(r => r.Text).ToList();
        Assert.Equal(SearchSort.Relevance, search.Sort);
        Assert.Equal(search.Rows.Select(r => r.Fitness).OrderDescending(), search.Rows.Select(r => r.Fitness));

        app.Click("Name").Frame();
        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase), search.Rows.Select(r => r.Text));
        Assert.Same(selected, search.SelectedRow!.Node);
        Assert.True(app.Frames.Drew(LucideIcons.ChevronUp));

        app.Click("Name").Frame();
        Assert.Equal(names.OrderDescending(StringComparer.OrdinalIgnoreCase), search.Rows.Select(r => r.Text));
        Assert.Same(selected, search.SelectedRow!.Node);
        Assert.True(app.Frames.Drew(LucideIcons.ChevronDown));

        app.Click("Name").Frame();
        Assert.Equal(SearchSort.Relevance, search.Sort);
        Assert.Equal(names, search.Rows.Select(r => r.Text));
        Assert.Same(selected, search.SelectedRow!.Node);

        app.Click("Assembly").Frame();
        Assert.Equal(SearchSort.Assembly, search.Sort);
        Assert.False(search.Descending);
    }

    [Fact]
    public void Search_kinds_are_checked_on_and_off_in_one_open_menu()
    {
        var app = Searching("Greet", moreRowsThan: 0);
        Assert.Contains(app.Workspace.Search.Rows, r => r.Node.Kind == NodeKind.Class);

        app.Click("Types and members").Frame();
        foreach (var kind in new[] { "Types", "Fields", "Properties", "Events" })
            app.ClickMenuItem(kind).Frame();
        Assert.True(app.Frames.Drew("Namespaces"));
        var events = app.LastTextRect("Events");
        var icon = app.LastTextRect(NodeIcons.Glyph(NodeKind.Event));
        Assert.True(icon.Right <= events.X && events.X - icon.Right < 16f, $"the events icon ends at {icon.Right}, its label starts at {events.X}");
        Assert.True(MathF.Abs(icon.Y + icon.Height / 2 - (events.Y + events.Height / 2)) < 4f);
        app.Frames.Key(KeyboardKey.Escape).Frame(2);
        app.RunUntil(() => app.Workspace.Search is { IsBusy: false, Rows.Count: > 0 });

        Assert.Equal(SearchKinds.Methods, app.Workspace.Search.Kinds);
        Assert.All(app.Workspace.Search.Rows, r => Assert.Equal(NodeKind.Method, r.Node.Kind));
        Assert.True(app.Frames.Drew("Methods"));
        Assert.False(app.Frames.Drew("Namespaces"));
    }

    [Fact]
    public void All_kinds_stands_alone_and_comes_back_when_the_last_kind_is_unchecked()
    {
        var app = AppDriver.Opened();
        app.Workspace.OpenSearch();
        app.Run(2);

        app.Click("Types and members").Frame();
        app.ClickMenuItem("All").Frame();
        Assert.Equal(SearchKinds.All, app.Workspace.Search.Kinds);
        Assert.True(app.Frames.Drew("All kinds"));

        app.ClickMenuItem("Resources").Frame();
        Assert.Equal(SearchKinds.Resources, app.Workspace.Search.Kinds);
        app.ClickMenuItem("Namespaces").Frame();
        Assert.True(app.Frames.Drew("Kinds · 2"));

        app.ClickMenuItem("Resources").Frame();
        app.ClickMenuItem("Namespaces").Frame();
        Assert.Equal(SearchKinds.All, app.Workspace.Search.Kinds);
    }

    [Fact]
    public void The_tree_menu_scopes_the_search_to_an_assembly()
    {
        var app = AppDriver.Opened();
        app.Run(2);
        var name = app.Workspace.Assemblies.Assemblies[0].Name;
        var row = app.TextRect(name);

        app.Frames.Click(row.X + 2f, row.Y + row.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem("Scope search to this assembly").Frame(2);

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
        var theme = app.Theme;
        var open = app.TextRect(LucideIcons.FolderOpen);
        var search = app.TextRect(LucideIcons.Search);
        Assert.Equal(open.Y, search.Y, 1f);
        Assert.InRange(search.X - open.Right, 0f, theme.IconButtonSize);
        Assert.True(app.TextRect("C#").X > search.Right);
        Assert.Contains(app.Frames.Canvas.Rects, r => r.Inputs.Style.BorderColor.Top.Value == theme.Border
                                                      && r.Inputs.Position.Left <= open.X && r.Inputs.Position.Left + r.Inputs.Position.Width >= open.Right
                                                      && r.Inputs.Position.Width <= theme.IconButtonSize + 1f);

        app.Click(LucideIcons.FolderOpen).Frame();
        Assert.Equal(["Open assembly"], app.Frames.FilePicker.Titles);

        app.Click(LucideIcons.Search).Frame(2);
        Assert.True(app.Workspace.Layout.Contains(DockPanels.Search));
        Assert.True(app.Frames.Drew("Search…"));
    }

    [Fact]
    public void The_menu_bar_is_set_apart_from_the_toolbar_by_its_color_and_a_line()
    {
        var app = new AppDriver();
        app.Run(2);
        var theme = app.Theme;
        var file = app.TextRect("File");
        var open = app.TextRect(LucideIcons.FolderOpen);
        var rects = app.Frames.Canvas.Rects.Select(r => (r.Inputs.Style.BackgroundColor, Rect: app.Frames.ToUi(r.Inputs.Position))).ToList();

        Assert.Contains(rects, r => r.BackgroundColor == theme.Background && r.Rect.Y <= file.Y && r.Rect.Bottom >= file.Bottom && r.Rect.Bottom <= open.Y);
        Assert.Contains(rects, r => r.BackgroundColor == theme.Separator && r.Rect.Y >= file.Bottom && r.Rect.Bottom <= open.Y);
    }

    [Fact]
    public void Tree_rows_show_assembly_secondary_text_and_an_overlay_on_non_public_types()
    {
        var app = ExpandedTree(until: "Hidden");

        var secondary = NodeText.Secondary(app.Node())!;
        Assert.True(app.TextRect(secondary).X > app.TextRect(app.Workspace.Assemblies.Assemblies[0].Name).Right);
        Assert.True(app.Frames.Drew(LucideIcons.Shield));
    }

    [Fact]
    public void Search_results_are_a_table_of_name_location_and_assembly_that_truncates_at_a_narrow_width()
    {
        var app = Searching("Greeter", moreRowsThan: 0, width: 560f);
        app.Run(2);

        var row = app.Workspace.Search.Rows.First(r => r.Node.Kind == NodeKind.Class);
        var header = app.LastTextRect(LucideIcons.Search);
        var texts = app.Frames.TextRects().ToList();
        var name = texts.Single(t => t.Text == row.Text && t.Rect.Y > header.Bottom).Rect;
        var cells = texts.Where(t => t.Rect.Y == name.Y && t.Rect.X > name.Right).OrderBy(t => t.Rect.X).ToList();

        Assert.True(app.TextRect("Location").X < app.TextRect("Assembly").X);
        Assert.Equal(2, cells.Count);
        Assert.False(app.Frames.Drew(row.Location), $"'{row.Location}' was drawn in full");
        Assert.Contains('…', cells[0].Text);
        Assert.StartsWith(cells[1].Text.TrimEnd('…'), row.Node.Assembly.NameAndVersion);
        Assert.True(cells[1].Rect.Right <= app.Frames.Width, $"the assembly ends at {cells[1].Rect.Right}");
    }

    [Fact]
    public void The_theme_is_picked_from_the_view_menus_theme_submenu()
    {
        var app = new AppDriver();
        app.Run(2);
        Assert.Equal(AppThemeId.Dark, app.Workspace.Theme);

        app.Click("View");
        app.Click("Theme");
        app.Click("Light theme");

        Assert.Equal(AppThemeId.Light, app.Workspace.Theme);
    }

    [Fact]
    public void A_named_color_scheme_is_picked_from_the_theme_submenu_and_draws_the_window()
    {
        var app = new AppDriver();
        app.Run(2);

        app.Click("View");
        app.Click("Theme");
        app.Click("Dracula").Frame();

        Assert.Equal(AppThemeId.Dracula, app.Workspace.Theme);
        Assert.Equal(UiTheme.Dracula.Background, app.Frames.WindowBackground);
    }

    [Fact]
    public void The_decompiler_is_picked_from_the_view_menus_decompiler_submenu()
    {
        var app = AppDriver.Opened();
        app.Run();

        app.Click("View");
        app.Click("Decompiler");
        app.ClickMenuItem("IL");
        app.Settle();

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
