using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class AssemblyListTests
{
    [Fact]
    public void Opening_the_same_file_twice_returns_the_same_assembly()
    {
        var list = new AssemblyList();

        var first = list.Open(Trees.TestAssemblyPath);
        var second = list.Open(Path.Combine(Path.GetDirectoryName(Trees.TestAssemblyPath)!, ".", Path.GetFileName(Trees.TestAssemblyPath)));

        Assert.Same(first, second);
        Assert.Single(list.Assemblies);
        Assert.Equal("ZGF.ILSpy.Tests", first.Name);
        Assert.Equal(".NETCoreApp,Version=v10.0", first.TargetFramework);
    }

    [Fact]
    public void An_open_cancelled_before_it_lists_the_assembly_leaves_the_list_as_it_was()
    {
        var list = new AssemblyList();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.Throws<OperationCanceledException>(() => list.Open(Trees.TestAssemblyPath, cancelled.Token));

        Assert.Empty(list.Assemblies);
        Assert.Equal(0, list.Version);
    }

    [Fact]
    public void Open_and_close_update_the_snapshot_paths_version_and_listeners()
    {
        var list = new AssemblyList();
        var changes = 0;
        list.Changed += () => changes++;

        var tests = list.Open(Trees.TestAssemblyPath);
        var snapshot = list.Assemblies;
        var core = list.Open(typeof(AssemblyList).Assembly.Location);

        Assert.Single(snapshot);
        Assert.Equal([Trees.TestAssemblyPath, typeof(AssemblyList).Assembly.Location], list.FilePaths);
        Assert.Equal(2, list.Version);

        list.Close(tests);

        Assert.Equal([core], list.Assemblies);
        Assert.Equal(3, changes);
        Assert.Equal(3, list.Version);
    }

    [Fact]
    public void A_closed_assembly_reopens_as_a_new_object()
    {
        var list = new AssemblyList();
        var first = list.Open(Trees.TestAssemblyPath);

        list.Close(first);
        var second = list.Open(Trees.TestAssemblyPath);

        Assert.NotSame(first, second);
        Assert.Equal([second], list.Assemblies);
    }

    [Fact]
    public void Files_that_are_not_assemblies_fail_with_a_load_exception()
    {
        var list = new AssemblyList();
        using var temp = new TempDir();
        var text = temp.Write("text.dll", "not a PE file");

        var notPe = Assert.Throws<AssemblyLoadException>(() => list.Open(text));
        var missing = Assert.Throws<AssemblyLoadException>(() => list.Open(text + ".missing"));

        Assert.Equal(text, notPe.FilePath);
        Assert.Equal(text + ".missing", missing.FilePath);
        Assert.Empty(list.Assemblies);
    }

    [Fact]
    public void Runtime_assemblies_list_the_managed_framework_assemblies()
    {
        var all = RuntimeAssemblies.All;

        Assert.Equal(Path.GetDirectoryName(Trees.CoreLibPath), RuntimeAssemblies.Directories[0]);
        Assert.Contains(all, a => a.Name == "System.Private.CoreLib" && a.Framework == "Microsoft.NETCore.App");
        Assert.Contains(all, a => a.Name == "System.Runtime");
        Assert.DoesNotContain(all, a => a.Name.Equals("coreclr", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(all.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).Select(a => a.Name), all.Select(a => a.Name));
    }

    [Fact]
    public void A_native_build_finds_the_newest_installed_runtime_of_its_major_version()
    {
        using var temp = new TempDir();
        var root = temp.Path;
        string Runtime(string version) =>
            Path.GetDirectoryName(temp.Write(Path.Combine("shared", "Microsoft.NETCore.App", version, "System.Private.CoreLib.dll"), ""))!;
        Runtime("9.0.11");
        var newest = Runtime("10.0.2");
        Runtime("10.0.1");
        Runtime("11.0.0-preview.1");
        Directory.CreateDirectory(temp.PathOf(Path.Combine("shared", "Microsoft.NETCore.App", "10.0.9")));

        Assert.Equal(newest, RuntimeAssemblies.FindInstalledRuntime([Path.Combine(root, "missing"), root], 10));
        Assert.Equal(Path.Combine(root, "shared", "Microsoft.NETCore.App", "11.0.0-preview.1"), RuntimeAssemblies.FindInstalledRuntime([root], 12));
    }

    [Fact]
    public void References_resolve_from_the_runtime_directory_and_next_to_the_assembly()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var references = assembly.Root.Child("References");

        var runtime = list.FindNode(references.Child("System.Runtime").Target!);
        var sibling = list.FindNode(references.Child("ZGF.ILSpy.Core").Target!);

        Assert.Equal(Path.Combine(RuntimeAssemblies.Directories[0], "System.Runtime.dll"), runtime!.Assembly.FilePath, ignoreCase: true);
        Assert.Equal(typeof(AssemblyList).Assembly.Location, sibling!.Assembly.FilePath, ignoreCase: true);
        Assert.Same(runtime.Assembly.Root, runtime);
        Assert.Contains(runtime.Assembly, list.Assemblies);
    }

    [Fact]
    public async Task Concurrent_opens_and_reads_agree_on_one_assembly_per_file()
    {
        var list = new AssemblyList();
        var paths = new[] { Trees.TestAssemblyPath, typeof(AssemblyList).Assembly.Location, Trees.CoreLibPath };

        var opened = await Task.WhenAll(Enumerable.Range(0, 24).Select(i => Task.Run(() =>
        {
            var assembly = list.Open(paths[i % paths.Length]);
            _ = list.Assemblies.Count;
            _ = assembly.Root.Children.Count;
            return assembly;
        })));

        Assert.Equal(3, list.Assemblies.Count);
        foreach (var group in opened.GroupBy(a => a.FilePath))
            Assert.All(group, a => Assert.Same(group.First(), a));
    }
}
