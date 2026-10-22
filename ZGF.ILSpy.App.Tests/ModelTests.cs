using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy.App.Tests;

public sealed class BrowseTreeTests
{
    [Fact]
    public void A_node_whose_children_are_not_built_shows_a_loading_row_until_they_are()
    {
        var d = new WorkspaceDriver();
        var tree = new BrowseTree(d.Post);
        var root = new AssemblyList().Open(WorkspaceDriver.FixturePath).Root;

        var first = tree.GetChildren(root);

        Assert.Equal([root.LoadingChild], first);
        Assert.Equal("Loading…", tree.Row(root.LoadingChild).Label);
        d.PumpUntil(() => root.TryGetChildren(out _) && tree.GetChildren(root) is not [{ Kind: NodeKind.Loading }]);
        Assert.Contains(tree.GetChildren(root), c => c.Text == WorkspaceDriver.FixturesNamespace);
    }

    [Fact]
    public void Revealing_a_hidden_non_public_node_selects_its_nearest_shown_ancestor()
    {
        var d = new WorkspaceDriver();
        var tree = new BrowseTree(d.Post);
        var ns = new AssemblyList().Open(WorkspaceDriver.FixturePath).Root.Children.Single(c => c.Text == WorkspaceDriver.FixturesNamespace);
        var secret = ns.Children.Single(c => c.Text == "Hidden").Children.Single(c => c.Text == "Secret() : int");
        tree.SetShowNonPublic(false);

        tree.Reveal(secret);
        Assert.Same(ns, tree.View.Selected);

        tree.SetShowNonPublic(true);
        tree.Reveal(secret);
        Assert.Same(secret, tree.View.Selected);
    }

    [Fact]
    public void The_public_children_cache_does_not_keep_closed_assemblies_alive()
    {
        var tree = new BrowseTree(static _ => { });
        tree.SetShowNonPublic(false);
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
        var assembly = list.Open(WorkspaceDriver.FixturePath);
        var root = assembly.Root;
        _ = root.Children;
        Assert.NotEmpty(tree.GetChildren(root));
        list.Close(assembly);
        return new WeakReference(assembly);
    }
}

public sealed class AppSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ilspy-settings-{Guid.NewGuid():N}");

    private string SettingsFile => Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Saved_settings_load_back_and_leave_no_temporary_file()
    {
        var settings = AppSettings.Load(SettingsFile);
        settings.Assemblies = ["a.dll", "b.dll"];
        settings.Language = DecompilationLanguage.IL;
        settings.Decompiler = new DecompilerOptions { LanguageVersion = CSharpVersion.CSharp7_3 };

        Assert.Null(settings.Save());

        var loaded = AppSettings.Load(SettingsFile);
        Assert.Null(loaded.LoadError);
        Assert.Equal(["a.dll", "b.dll"], loaded.Assemblies);
        Assert.Equal(DecompilationLanguage.IL, loaded.Language);
        Assert.Equal(CSharpVersion.CSharp7_3, loaded.Decompiler.LanguageVersion);
        Assert.Equal([SettingsFile], Directory.GetFiles(_directory));
    }

    [Fact]
    public void Window_bounds_round_trip_and_open_the_window_where_it_was_left()
    {
        var settings = AppSettings.Load(SettingsFile);
        settings.Window = new WindowBounds(-1200, 80, 1600, 1000, Maximized: true);
        Assert.Null(settings.Save());

        var loaded = AppSettings.Load(SettingsFile);
        var config = loaded.Window!.Value.ApplyTo(new StartupConfig { WindowWidth = 1400, WindowHeight = 900, WindowTitle = "ILSpy" });

        Assert.Equal(new WindowBounds(-1200, 80, 1600, 1000, true), loaded.Window);
        Assert.Equal((-1200, 80, 1600, 1000, true), (config.WindowX, config.WindowY, config.WindowWidth, config.WindowHeight, config.StartMaximized));
        Assert.Null(AppSettings.Load(Path.Combine(_directory, "missing.json")).Window);
    }

    [Theory]
    [InlineData(10, 20, AppSettings.MinWindowWidth, AppSettings.MinWindowHeight)]
    [InlineData(100_000, 90_000, AppSettings.MaxWindowSize, AppSettings.MaxWindowSize)]
    [InlineData(1200, 800, 1200, 800)]
    public void Saved_window_sizes_load_within_reason(int width, int height, int expectedWidth, int expectedHeight)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsFile, $$"""{ "Window": { "X": 5, "Y": 6, "Width": {{width}}, "Height": {{height}}, "Maximized": false } }""");

        var loaded = AppSettings.Load(SettingsFile);

        Assert.Equal(new WindowBounds(5, 6, expectedWidth, expectedHeight, false), loaded.Window);
    }

    [Fact]
    public void Window_bounds_without_a_size_load_as_none()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsFile, """{ "Window": { "X": 5, "Y": 6, "Width": 0, "Height": 0, "Maximized": false } }""");

        Assert.Null(AppSettings.Load(SettingsFile).Window);
        Assert.Null(AppSettings.Sanitize(default(WindowBounds)));
    }

    [Fact]
    public void A_null_assembly_list_loads_as_empty()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsFile, """{ "Assemblies": null, "DarkMode": false }""");

        var loaded = AppSettings.Load(SettingsFile);

        Assert.Null(loaded.LoadError);
        Assert.Empty(loaded.Assemblies);
        Assert.False(loaded.DarkMode);
    }

    [Fact]
    public void An_unreadable_file_falls_back_to_defaults_and_is_reported()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsFile, """{ "Assemblies": [ "a.dll", """);

        var loaded = AppSettings.Load(SettingsFile);
        var workspace = new Workspace(loaded);

        Assert.Empty(loaded.Assemblies);
        Assert.NotNull(loaded.LoadError);
        var error = Assert.Single(workspace.Errors.Entries);
        Assert.Contains(SettingsFile, error.Details);
    }

    [Fact]
    public void Saving_replaces_the_whole_file()
    {
        var settings = AppSettings.Load(SettingsFile);
        settings.Assemblies = Enumerable.Range(0, 200).Select(i => $"assembly-{i}.dll").ToList();
        settings.Save();
        settings.Assemblies = ["one.dll"];

        settings.Save();

        var json = JsonDocument.Parse(File.ReadAllText(SettingsFile));
        Assert.Equal(1, json.RootElement.GetProperty("Assemblies").GetArrayLength());
    }
}

public sealed class DockPanelsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ilspy-dock-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void A_rearranged_layout_is_saved_and_restored_with_the_settings()
    {
        var file = Path.Combine(_directory, "settings.json");
        var workspace = new Workspace(AppSettings.Load(file));
        var layout = workspace.Layout;
        layout.Float(DockPanels.Assemblies, new UiRect(40f, 50f, 300f, 400f));
        workspace.SaveSettings();

        var next = new Workspace(AppSettings.Load(file));
        var restored = next.Layout;

        Assert.Same(next.ActiveTab, next.FindTab(Assert.Single(Assert.IsType<DockGroup>(restored.Root).Panels)));
        var window = Assert.Single(restored.Windows);
        Assert.Equal(new UiRect(40f, 50f, 300f, 400f), window.Rect);
        Assert.Same(window, restored.WindowOf(restored.GroupOf(DockPanels.Assemblies)!));
    }

    [Fact]
    public void A_saved_layout_keeps_the_views_left_open_and_drops_documents()
    {
        var saved = new DockLayout { Root = DockLayout.Group("document-7", DockPanels.Search, "gone", DockPanels.Bookmarks) };

        var layout = DockPanels.Restore(saved, "document-99", out var placed);
        var fresh = DockPanels.Restore(null, "document-99", out var freshPlaced);

        Assert.True(placed);
        Assert.Equal(["document-99", DockPanels.Search, DockPanels.Bookmarks], Assert.IsType<DockGroup>(layout.Root).Panels);
        Assert.Equal([DockPanels.Assemblies], Assert.IsType<DockGroup>(fresh.Root).Panels);
        Assert.False(freshPlaced);
    }

    [Fact]
    public void Opening_a_search_docks_its_panel_and_closing_the_panel_clears_the_query()
    {
        var workspace = new Workspace(new AppSettings());

        workspace.OpenSearch();
        workspace.Search.Query = "Greeter";

        var search = workspace.Layout.GroupOf(DockPanels.Search);
        Assert.NotNull(search);
        Assert.NotSame(search, workspace.Layout.GroupOf(workspace.ActiveTab!.Panel));

        workspace.Layout.Close(DockPanels.Search);

        Assert.Equal("", workspace.Search.Query);
    }

    [Fact]
    public void The_search_mode_is_saved_and_restored()
    {
        var settings = new AppSettings();
        var workspace = new Workspace(settings);

        workspace.Search.Kinds = SearchKinds.Literals;
        workspace.SaveSettings();

        Assert.Equal(SearchKinds.Literals, new Workspace(settings).Search.Kinds);
    }

    [Fact]
    public void Scoping_the_search_to_a_namespace_keeps_the_terms_typed()
    {
        var driver = new WorkspaceDriver();
        driver.OpenFixtures();
        driver.Workspace.Search.Query = "inassembly:Old Greet";

        driver.Workspace.ScopeSearch(driver.Node(WorkspaceDriver.FixturesNamespace));

        Assert.Equal($"innamespace:{WorkspaceDriver.FixturesNamespace} inassembly:Old Greet ", driver.Workspace.Search.Query);
        Assert.True(driver.Workspace.Layout.Contains(DockPanels.Search));
    }
}

public sealed class FrameworkPickerTests
{
    [Fact]
    public void A_failed_scan_ends_loading_with_an_error_and_reports_it()
    {
        var posted = new ConcurrentQueue<Action>();
        var reported = new List<string>();
        var picker = new FrameworkPickerModel(static () => Strings.En, (title, _) => reported.Add(title), static () => throw new InvalidDataException("no runtime"));

        picker.Open(posted.Enqueue);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (picker.IsLoading && clock.Elapsed < TimeSpan.FromSeconds(10))
        {
            while (posted.TryDequeue(out var action))
                action();
            Thread.Sleep(1);
        }

        Assert.False(picker.IsLoading);
        Assert.Contains("no runtime", picker.Error);
        Assert.Single(reported);
    }
}
