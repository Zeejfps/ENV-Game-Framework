using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy.App.Tests;

public sealed class BrowseTreeTests
{
    [Fact]
    public void A_node_whose_children_are_not_built_shows_a_loading_row_until_they_are()
    {
        var d = new AppStateDriver();
        var tree = new BrowseTree();
        var root = new AssemblyList().Open(AppStateDriver.FixturePath).Root;

        var first = tree.GetChildren(root);

        Assert.Equal([root.LoadingChild], first);
        Assert.Equal("Loading…", tree.Row(root.LoadingChild).Label);
        Assert.True(tree.Row(root.LoadingChild).Spinning);
        d.RunUntil(() => root.TryGetChildren(out _) && tree.GetChildren(root) is not [{ Kind: NodeKind.Loading }]);
        Assert.Contains(tree.GetChildren(root), c => c.Text == AppStateDriver.FixturesNamespace);
    }

    [Fact]
    public void An_assembly_spins_while_it_is_reopened()
    {
        var tree = new BrowseTree();
        var assembly = new AssemblyList().Open(AppStateDriver.FixturePath);
        var reopens = new Reopens();
        tree.ShowReopening(reopens.Files);
        var idle = tree.Row(assembly.Root).Spinning;

        reopens.Start(assembly.FilePath);

        Assert.False(idle);
        Assert.True(tree.Row(assembly.Root).Spinning);
        Assert.False(tree.Row(assembly.Root.Child(AppStateDriver.FixturesNamespace)).Spinning);
    }

    [Fact]
    public void Revealing_a_hidden_non_public_node_selects_its_nearest_shown_ancestor()
    {
        var d = new AppStateDriver();
        var tree = new BrowseTree();
        var root = new AssemblyList().Open(AppStateDriver.FixturePath).Root;
        var ns = root.Child(AppStateDriver.FixturesNamespace);
        var secret = ns.Child("Hidden", "Secret() : int");
        tree.ShowApi(ApiVisibility.PublicOnly);

        tree.Reveal(secret);
        Assert.Same(ns, tree.View.Selected);

        tree.ShowApi(ApiVisibility.AllButCompilerGenerated);
        tree.Reveal(secret);
        Assert.Same(secret, tree.View.Selected);
    }

    [Fact]
    public void Compiler_generated_members_show_only_when_everything_is_shown()
    {
        var tree = new BrowseTree();
        var root = new AssemblyList().Open(AppStateDriver.FixturePath).Root;
        var counter = root.Child(AppStateDriver.FixturesNamespace, "Counter");
        var backingField = counter.Child("<Count>k__BackingField : int");

        Assert.True(backingField.IsCompilerGenerated);
        Assert.DoesNotContain(backingField, tree.GetChildren(counter));

        tree.ShowApi(ApiVisibility.All);

        Assert.Contains(backingField, tree.GetChildren(counter));
    }

    [Fact]
    public void The_public_children_cache_does_not_keep_closed_assemblies_alive()
    {
        var tree = new BrowseTree();
        tree.ShowApi(ApiVisibility.PublicOnly);
        var assembly = FilterAndClose(tree);

        for (var i = 0; i < 100 && assembly.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(20);
        }

        Assert.False(assembly.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference FilterAndClose(BrowseTree tree)
    {
        var list = new AssemblyList();
        var assembly = list.Open(AppStateDriver.FixturePath);
        var root = assembly.Root;
        _ = root.Children;
        Assert.NotEmpty(tree.GetChildren(root));
        list.Close(assembly);
        return new WeakReference(assembly);
    }
}

public sealed class AppSettingsTests : IDisposable
{
    private readonly TempDir _temp = new();

    private string SettingsFile => _temp.PathOf("settings.json");

    public void Dispose() => _temp.Dispose();

    private string Written(string json) => _temp.Write("settings.json", json);

    [Fact]
    public void Saved_settings_load_back_and_leave_no_temporary_file()
    {
        var settings = AppSettings.Load(SettingsFile).Settings;
        settings.AssemblyLists = [new AssemblyListRecord("Work", ["a.dll", "b.dll"]), new AssemblyListRecord("Play", ["c.dll"])];
        settings.ActiveAssemblyList = "Play";
        settings.Language = DecompilationLanguage.IL;
        settings.Decompiler = new DecompilerOptions { LanguageVersion = CSharpVersion.CSharp7_3 }.With(DecompilerToggle.Find("AlwaysUseBraces")!, false);
        settings.FoldedOptionGroups = ["Other"];

        Assert.IsType<SettingsWrite.Written>(settings.Save());

        var (loaded, problem) = AppSettings.Load(SettingsFile);
        Assert.IsType<SettingsProblem.None>(problem);
        Assert.Equal(["Work", "Play"], loaded.AssemblyLists.Select(static l => l.Name));
        Assert.Equal(["a.dll", "b.dll"], loaded.AssemblyLists[0].Assemblies);
        Assert.Equal("Play", loaded.ActiveAssemblyList);
        Assert.Equal(DecompilationLanguage.IL, loaded.Language);
        Assert.Equal(settings.Decompiler, loaded.Decompiler);
        Assert.Equal(["Other"], loaded.FoldedOptionGroups);
        Assert.Equal([SettingsFile], Directory.GetFiles(_temp.Path));
    }

    [Theory]
    [InlineData("""{ "Decompiler": { "LanguageVersion": "CSharp7" } }""", CSharpVersion.CSharp7)]
    [InlineData("""{ "Decompiler": { "LanguageVersion": "CSharp14", "Toggles": null } }""", CSharpVersion.CSharp14)]
    [InlineData("""{ "Decompiler": { } }""", CSharpVersion.CSharp14)]
    public void Decompiler_options_from_older_settings_files_load_with_defaults_for_what_they_lack(string json, CSharpVersion version)
    {
        var (loaded, problem) = AppSettings.Load(Written(json));

        Assert.IsType<SettingsProblem.None>(problem);
        Assert.Equal(DecompilerOptions.Default with { LanguageVersion = version }, loaded.Decompiler);
        Assert.Empty(loaded.Decompiler.Toggles);
    }

    [Fact]
    public void Window_bounds_round_trip_and_open_the_window_where_it_was_left()
    {
        var settings = AppSettings.Load(SettingsFile).Settings;
        settings.Window = new WindowBounds(-1200, 80, 1600, 1000, Maximized: true);
        Assert.IsType<SettingsWrite.Written>(settings.Save());

        var loaded = AppSettings.Load(SettingsFile).Settings;
        var config = loaded.Window!.Value.ApplyTo(new StartupConfig { WindowWidth = 1400, WindowHeight = 900, WindowTitle = "ILSpy" });

        Assert.Equal(new WindowBounds(-1200, 80, 1600, 1000, true), loaded.Window);
        Assert.Equal((-1200, 80, 1600, 1000, true), (config.WindowX, config.WindowY, config.WindowWidth, config.WindowHeight, config.StartMaximized));
        Assert.Null(AppSettings.Load(_temp.PathOf("missing.json")).Settings.Window);
    }

    [Theory]
    [InlineData(10, 20, AppSettings.MinWindowWidth, AppSettings.MinWindowHeight)]
    [InlineData(100_000, 90_000, AppSettings.MaxWindowSize, AppSettings.MaxWindowSize)]
    [InlineData(1200, 800, 1200, 800)]
    public void Saved_window_sizes_load_within_reason(int width, int height, int expectedWidth, int expectedHeight)
    {
        var loaded = AppSettings.Load(Written($$"""{ "Window": { "X": 5, "Y": 6, "Width": {{width}}, "Height": {{height}}, "Maximized": false } }""")).Settings;

        Assert.Equal(new WindowBounds(5, 6, expectedWidth, expectedHeight, false), loaded.Window);
    }

    [Fact]
    public void The_export_window_reopens_where_it_was_left_in_the_last_run()
    {
        var d = new AppStateDriver(AppSettings.Load(SettingsFile).Settings);
        d.Ui.ExportSetup.Window.Bounds = new WindowBounds(-900, 200, 700, 800, false);
        d.Ui.ExportSetup.Window.Settle(held: false);
        d.NextFrame();
        SaveSettings(d.AppState);

        var next = new AppStateDriver(AppSettings.Load(SettingsFile).Settings);

        Assert.Equal(new WindowBounds(-900, 200, 700, 800, false), next.Ui.ExportSetup.Window.Bounds);
        Assert.Null(AppSettings.Load(Written("""{ "ExportWindow": { "X": 5, "Y": 6, "Width": 0, "Height": 0, "Maximized": false } }""")).Settings.ExportWindow);
    }

    [Fact]
    public void Window_bounds_without_a_size_load_as_none()
    {
        Assert.Null(AppSettings.Load(Written("""{ "Window": { "X": 5, "Y": 6, "Width": 0, "Height": 0, "Maximized": false } }""")).Settings.Window);
        Assert.Null(AppSettings.Sanitize(default(WindowBounds)));
    }

    [Fact]
    public void Null_assembly_lists_load_as_none()
    {
        var (loaded, problem) = AppSettings.Load(Written("""{ "AssemblyLists": null, "Theme": "Monokai" }"""));

        Assert.IsType<SettingsProblem.None>(problem);
        Assert.Empty(loaded.AssemblyLists);
        Assert.Equal("monokai", loaded.Theme);
    }

    [Fact]
    public void Lists_missing_their_name_or_assemblies_load_without_them()
    {
        var (loaded, problem) = AppSettings.Load(Written("""{ "AssemblyLists": [ null, { "Name": null }, { "Name": "Work", "Assemblies": null }, { "Name": "Play", "Assemblies": [ " ", "a.dll" ] } ] }"""));

        Assert.IsType<SettingsProblem.None>(problem);
        Assert.Equal(["Work", "Play"], loaded.AssemblyLists.Select(static l => l.Name));
        Assert.Empty(loaded.AssemblyLists[0].Assemblies);
        Assert.Equal(["a.dll"], loaded.AssemblyLists[1].Assemblies);
    }

    [Fact]
    public void The_one_list_of_an_older_settings_file_becomes_the_default_list_and_is_saved_only_there()
    {
        var loaded = AppSettings.Load(Written("""{ "Assemblies": [ "a.dll", "", "b.dll" ] }""")).Settings;

        var list = Assert.Single(loaded.AssemblyLists);
        Assert.Equal(SavedAssemblyLists.DefaultName, list.Name);
        Assert.Equal(["a.dll", "b.dll"], list.Assemblies);
        Assert.IsType<SettingsWrite.Written>(loaded.Save());
        Assert.False(JsonDocument.Parse(File.ReadAllText(SettingsFile)).RootElement.TryGetProperty("Assemblies", out _));
    }

    [Fact]
    public void The_analyses_of_an_older_settings_file_go_to_the_open_list_and_are_saved_only_there()
    {
        var loaded = AppSettings.Load(Written("""
            { "AssemblyLists": [ { "Name": "Work", "Assemblies": [] }, { "Name": "Play", "Assemblies": [] } ],
              "ActiveAssemblyList": "Play",
              "Analyzed": [ { "Kind": "Definition", "FilePath": "a.dll", "Token": 33554434 } ] }
            """)).Settings;

        Assert.Empty(loaded.AssemblyLists[0].Analyzed!);
        var analyzed = Assert.Single(loaded.AssemblyLists[1].Analyzed!);
        Assert.Equal(new CodeAnchor(CodeAnchorKind.Definition, "a.dll", 33554434), analyzed);
        Assert.IsType<SettingsWrite.Written>(loaded.Save());
        Assert.False(JsonDocument.Parse(File.ReadAllText(SettingsFile)).RootElement.TryGetProperty("Analyzed", out _));
    }

    [Theory]
    [InlineData(false, ApiVisibility.PublicOnly)]
    [InlineData(true, ApiVisibility.AllButCompilerGenerated)]
    public void The_non_public_switch_of_an_older_settings_file_becomes_its_visibility_and_is_saved_only_as_that(bool showNonPublic, ApiVisibility expected)
    {
        var loaded = AppSettings.Load(Written($$"""{ "ShowNonPublic": {{(showNonPublic ? "true" : "false")}} }""")).Settings;

        Assert.Equal(expected, loaded.ApiVisibility);
        Assert.IsType<SettingsWrite.Written>(loaded.Save());
        Assert.False(JsonDocument.Parse(File.ReadAllText(SettingsFile)).RootElement.TryGetProperty("ShowNonPublic", out _));
    }

    [Fact]
    public void An_unreadable_file_falls_back_to_defaults_and_is_reported()
    {
        var (loaded, problem) = AppSettings.Load(Written("""{ "Assemblies": [ "a.dll", """));
        var appState = new AppState(loaded);
        ReportSettingsProblem(appState, problem);

        Assert.Empty(loaded.AssemblyLists);
        Assert.IsType<SettingsProblem.NotJson>(problem);
        var error = Assert.Single(appState.Errors.Entries);
        Assert.Contains(SettingsFile, error.Words().Details);
    }

    [Fact]
    public void An_unreadable_file_is_copied_beside_it_before_the_settings_are_saved_over_it()
    {
        const string broken = """{ "Assemblies": [ "a.dll", """;
        var (loaded, problem) = AppSettings.Load(Written(broken));
        var appState = new AppState(loaded);
        ReportSettingsProblem(appState, problem);

        Assert.IsType<SettingsWrite.Written>(loaded.Save());

        var copy = _temp.PathOf("settings.unreadable.json");
        Assert.Equal(new SettingsKept.CopiedTo(copy), Assert.IsType<SettingsProblem.NotJson>(problem).Kept);
        Assert.Equal(broken, File.ReadAllText(copy));
        Assert.Contains(copy, Assert.Single(appState.Errors.Entries).Words().Details);
        Assert.Equal(JsonValueKind.Object, JsonDocument.Parse(File.ReadAllText(SettingsFile)).RootElement.ValueKind);
    }

    [Fact]
    public void A_file_that_cannot_be_read_or_copied_is_not_saved_over()
    {
        const string held = """{ "Theme": "light" }""";
        var path = Written(held);
        SettingsLoad load;
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            load = AppSettings.Load(path);

        Assert.IsType<SettingsWrite.Written>(load.Settings.Save());

        Assert.IsType<SettingsKept.NotSaved>(Assert.IsType<SettingsProblem.NotRead>(load.Problem).Kept);
        Assert.Equal(held, File.ReadAllText(path));
    }

    [Fact]
    public void A_number_where_the_settings_name_a_language_is_refused()
    {
        var (loaded, problem) = AppSettings.Load(Written("""{ "Locale": 42 }"""));

        Assert.IsType<SettingsProblem.NotJson>(problem);
        Assert.Null(loaded.Locale);
    }

    [Fact]
    public void Every_enum_the_settings_hold_is_read_by_name_alone()
    {
        var enums = typeof(AppSettingsJson).GetProperties()
            .Select(static p => p.PropertyType)
            .Where(static t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(JsonTypeInfo<>))
            .Select(static t => t.GetGenericArguments()[0])
            .Where(static t => t.IsEnum)
            .ToList();

        Assert.NotEmpty(enums);
        Assert.All(enums, type => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize("1", type, AppSettingsJson.Default)));
    }

    [Fact]
    public void A_file_holding_null_falls_back_to_defaults_and_is_reported()
    {
        var (_, problem) = AppSettings.Load(Written("null"));

        Assert.IsType<SettingsProblem.Empty>(problem);
    }

    [Theory]
    [InlineData("""{ "Layout": { "Root": null } }""")]
    [InlineData("""{ "Layout": { "Root": { "kind": "group", "Panels": null } } }""")]
    [InlineData("""{ "Layout": { "Root": { "kind": "group", "Panels": [ null ] } } }""")]
    [InlineData("""{ "Layout": { "Root": { "kind": "split", "First": null } } }""")]
    [InlineData("""{ "Layout": { "Windows": null } }""")]
    [InlineData("""{ "Layout": { "Windows": [ null ] } }""")]
    [InlineData("""{ "Layout": { "Windows": [ { "Root": null } ] } }""")]
    public void A_layout_missing_parts_is_left_out_for_the_panels_to_start_where_they_do(string json)
    {
        var (loaded, problem) = AppSettings.Load(Written($$"""{ "Theme": "light", {{json[1..]}}"""));
        var d = new AppStateDriver(loaded);
        ReportSettingsProblem(d.AppState, problem);

        Assert.IsType<SettingsProblem.LayoutPartsMissing>(problem);
        Assert.Equal("light", loaded.Theme);
        var fresh = new AppStateDriver(new AppSettings());
        Assert.Equal(DockLayout.PanelsIn(fresh.Ui.Layout.Root), DockLayout.PanelsIn(d.Ui.Layout.Root));
        Assert.Empty(d.Ui.Layout.Windows);
        Assert.Contains(d.AppState.Errors.Entries, e => e.Words().Details.Contains(SettingsFile));
    }

    [Fact]
    public void A_layout_with_two_parts_under_one_id_is_left_out()
    {
        var (loaded, problem) = AppSettings.Load(Written("""
            { "Layout": { "Root": { "kind": "split", "Id": 1,
                "First": { "kind": "group", "Id": 2, "Panels": [ "assemblies" ] },
                "Second": { "kind": "group", "Id": 2, "Panels": [ "search" ] } } } }
            """));

        Assert.IsType<SettingsProblem.LayoutPartsRepeated>(problem);
        Assert.Null(loaded.Layout);
    }

    [Fact]
    public void Saving_replaces_the_whole_file()
    {
        var settings = AppSettings.Load(SettingsFile).Settings;
        settings.AssemblyLists = Enumerable.Range(0, 200).Select(i => new AssemblyListRecord($"list-{i}", [])).ToList();
        settings.Save();
        settings.AssemblyLists = [new AssemblyListRecord("one", [])];

        settings.Save();

        var json = JsonDocument.Parse(File.ReadAllText(SettingsFile));
        Assert.Equal(1, json.RootElement.GetProperty("AssemblyLists").GetArrayLength());
    }
}

public sealed class DockPanelsTests
{
    [Fact]
    public void A_rearranged_layout_is_saved_and_restored_with_the_settings()
    {
        using var temp = new TempDir();
        var d = new AppStateDriver(temp.Settings());
        var layout = d.Ui.Layout;
        layout.Float(DockPanels.Assemblies, new UiRect(40f, 50f, 300f, 400f));
        d.NextFrame();
        SaveSettings(d.AppState);

        var next = new AppStateDriver(temp.Settings());
        var restored = next.Ui.Layout;

        Assert.Same(next.AppState.Tabs.Active, next.Ui.Documents.FindTab(Assert.Single(Assert.IsType<DockGroup>(restored.Root).Panels)));
        var window = Assert.Single(restored.Windows);
        Assert.Equal(new UiRect(40f, 50f, 300f, 400f), window.Rect);
        Assert.Same(window, restored.WindowOf(restored.GroupOf(DockPanels.Assemblies)!));
    }

    [Fact]
    public void A_saved_layout_keeps_the_views_left_open_and_shows_the_first_tab_in_place_of_documents_not_kept()
    {
        var saved = new DockLayout { Root = DockLayout.Group("document-7", DockPanels.Search, "gone", DockPanels.Bookmarks) };

        var d = new AppStateDriver(new AppSettings { Layout = saved });
        var fresh = DockPanels.Restore(null);

        var panel = d.Ui.Documents.PanelOf(Assert.Single(d.AppState.Tabs));
        Assert.Equal([panel, DockPanels.Search, DockPanels.Bookmarks], Assert.IsType<DockGroup>(d.Ui.Layout.Root).Panels);
        Assert.NotEqual("document-7", panel);
        Assert.Equal([DockPanels.Assemblies], Assert.IsType<DockGroup>(fresh.Root).Panels);
    }

    [Fact]
    public void Opening_a_search_docks_its_panel_and_closing_the_panel_clears_the_query()
    {
        var d = new AppStateDriver();
        var appState = d.AppState;

        OpenSearch(d.Ui);
        d.Ui.Search.Query = "Greeter";

        var search = d.Ui.Layout.GroupOf(DockPanels.Search);
        Assert.NotNull(search);
        Assert.NotSame(search, d.Ui.Layout.GroupOf(d.Ui.Documents.PanelOf(appState.Tabs.Active!)));

        d.Ui.Layout.Close(DockPanels.Search);

        Assert.Equal("", d.Ui.Search.Query);
    }

    [Fact]
    public void The_search_mode_is_saved_and_restored()
    {
        var settings = new AppSettings();
        var d = new AppStateDriver(settings);

        d.Ui.Search.Kinds = SearchKinds.Literals;
        d.NextFrame();
        SaveSettings(d.AppState);

        Assert.Equal(SearchKinds.Literals, new AppStateDriver(settings).Ui.Search.Kinds);
    }

    [Fact]
    public void Scoping_the_search_to_a_namespace_keeps_the_terms_typed()
    {
        var driver = AppStateDriver.Opened();
        driver.Ui.Search.Query = "inassembly:Old Greet";

        ScopeSearch(driver.Ui, driver.Node(AppStateDriver.FixturesNamespace));

        Assert.Equal($"innamespace:{AppStateDriver.FixturesNamespace} inassembly:Old Greet ", driver.Ui.Search.Query);
        Assert.True(driver.Ui.Layout.Contains(DockPanels.Search));
    }
}

public sealed class FrameworkPickerTests
{
    [Fact]
    public void A_failed_scan_ends_loading_with_an_error_and_reports_it()
    {
        var posted = PostedActions.OfThisTest();
        var reported = new List<string>();
        var picker = new FrameworkPicker(static () => Strings.En, problem => reported.Add(ProblemTexts.Describe(Strings.En, problem).Title), static () => throw new InvalidDataException("no runtime"));

        picker.Open();
        Poll.Until(() => !picker.IsLoading, posted.Run);

        Assert.False(picker.IsLoading);
        Assert.Contains("no runtime", picker.Error);
        Assert.Single(reported);
    }

    [Fact]
    public void Filtering_keeps_only_the_picked_out_assemblies_still_shown()
    {
        var posted = PostedActions.OfThisTest();
        var assemblies = new[]
        {
            new RuntimeAssembly("System.Net.Http", "Microsoft.NETCore.App", "a.dll"),
            new RuntimeAssembly("System.Net.Sockets", "Microsoft.NETCore.App", "b.dll"),
            new RuntimeAssembly("System.Text.Json", "Microsoft.NETCore.App", "c.dll"),
        };
        var picker = new FrameworkPicker(static () => Strings.En, static _ => { }, () => assemblies);
        var list = new AssemblyList();
        picker.Open();
        Poll.Until(() => !picker.IsLoading, posted.Run);
        picker.Update(list);

        picker.Selection.SelectAll(picker.Filtered);
        picker.Filter = "Net";
        picker.Update(list);

        Assert.Equal([assemblies[0], assemblies[1]], picker.ListSelected());
    }

    [Fact]
    public void Assemblies_already_open_are_not_offered()
    {
        var posted = PostedActions.OfThisTest();
        var open = new RuntimeAssembly("Fixtures", "Microsoft.NETCore.App", AppStateDriver.FixturePath);
        var other = new RuntimeAssembly("System.Text.Json", "Microsoft.NETCore.App", "c.dll");
        var picker = new FrameworkPicker(static () => Strings.En, static _ => { }, () => [open, other]);
        var list = new AssemblyList();
        picker.Open();
        Poll.Until(() => !picker.IsLoading, posted.Run);
        picker.Update(list);
        Assert.Equal([open, other], picker.Filtered);

        list.Open(AppStateDriver.FixturePath);
        picker.Update(list);

        Assert.Equal([other], picker.Filtered);
    }
}
