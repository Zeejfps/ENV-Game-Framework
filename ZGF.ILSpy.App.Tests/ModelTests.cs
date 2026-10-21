using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ZGF.ILSpy.Core;

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

        Assert.Null(settings.Save());

        var loaded = AppSettings.Load(SettingsFile);
        Assert.Null(loaded.LoadError);
        Assert.Equal(["a.dll", "b.dll"], loaded.Assemblies);
        Assert.Equal(DecompilationLanguage.IL, loaded.Language);
        Assert.Equal([SettingsFile], Directory.GetFiles(_directory));
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

public sealed class FrameworkPickerTests
{
    [Fact]
    public void A_failed_scan_ends_loading_with_an_error_and_reports_it()
    {
        var posted = new ConcurrentQueue<Action>();
        var reported = new List<string>();
        var picker = new FrameworkPickerModel((title, _) => reported.Add(title), static () => throw new InvalidDataException("no runtime"));

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
