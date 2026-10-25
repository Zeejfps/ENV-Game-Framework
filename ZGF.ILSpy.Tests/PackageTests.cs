using System.IO.Compression;
using System.Text;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;

namespace ZGF.ILSpy.Tests;

public sealed class PackageTests
{
    private const string LibFolder = "lib/net10.0";
    private const string TestAssemblyName = "ZGF.ILSpy.Tests";

    private static string CoreAssemblyPath => typeof(AssemblyList).Assembly.Location;

    [Fact]
    public void A_zip_opens_as_a_package_listed_in_place_of_the_assemblies_in_it()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();

        var package = list.Open(WritePackage(temp));

        Assert.True(package.IsPackage);
        Assert.Equal([package], list.Listed);
        Assert.Equal(["ZGF.ILSpy.Core", TestAssemblyName], list.Assemblies.Select(a => a.Name));
        Assert.All(list.Assemblies, a => Assert.Same(package, a.InPackage!.Container));
        Assert.Equal(NodeKind.Package, package.Root.Kind);
        Assert.Equal("Sample.1.0.0.nupkg", package.Root.Text);
        Assert.Equal([LibFolder, "runtimes/win-x64/native", "readme.txt"], package.Root.Children.Select(c => c.Text));
    }

    [Fact]
    public void An_assembly_in_a_package_is_the_root_of_its_tree_under_its_folder_and_known_by_a_path_through_the_package()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));

        var folder = package.Root.Child(LibFolder);
        var root = TestAssemblyRoot(folder);
        var assembly = root.Assembly;

        Assert.Equal(Path.Combine(package.FilePath, "lib", "net10.0", "ZGF.ILSpy.Tests.dll"), assembly.FilePath);
        Assert.Same(package, assembly.InPackage!.Container);
        Assert.Same(root, assembly.Root);
        Assert.Same(folder, root.Parent);
        Assert.Equal(NodeKind.Class, assembly.Calculator().Kind);
    }

    [Fact]
    public void An_assembly_read_out_of_a_package_finds_its_root_before_the_folder_is_opened()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var path = Path.Combine(package.FilePath, "lib", "net10.0", "ZGF.ILSpy.Tests.dll");

        var assembly = list.TryLoad(path)!;

        Assert.Same(package.Root.Child(LibFolder), assembly.Root.Parent);
        Assert.Same(assembly.Root, TestAssemblyRoot(package.Root.Child(LibFolder)));
    }

    [Fact]
    public void A_path_to_a_node_in_a_package_starts_at_the_package_and_finds_the_node_again_in_another_list()
    {
        using var temp = new TempDir();
        var packagePath = WritePackage(temp);
        var first = new AssemblyList();
        var calculator = TestAssemblyRoot(first.Open(packagePath).Root.Child(LibFolder)).Assembly.Calculator();

        var path = calculator.Path;
        var second = new AssemblyList();
        var found = second.FindNode(path);

        Assert.Equal(packagePath, path.FilePath);
        Assert.Equal(new NodePathSegment(NodeKind.PackageFolder, LibFolder), path.Segments[0]);
        Assert.NotNull(found);
        Assert.Equal(calculator.Text, found.Text);
        Assert.True(Assert.Single(second.Listed).IsPackage);
    }

    [Fact]
    public void References_resolve_to_the_assembly_beside_it_in_the_package()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var assembly = TestAssemblyRoot(package.Root.Child(LibFolder)).Assembly;

        var core = assembly.Root.Child("References", "ZGF.ILSpy.Core");
        var resolved = list.FindNode(core.Target!);

        Assert.Equal(Path.Combine(package.FilePath, "lib", "net10.0", "ZGF.ILSpy.Core.dll"), resolved!.Assembly.FilePath);
        Assert.Same(package, resolved.Assembly.InPackage!.Container);
    }

    [Fact]
    public void Following_a_reference_into_a_package_lists_the_package_and_not_the_assembly()
    {
        using var temp = new TempDir();
        var packagePath = WritePackage(temp);
        var calculator = TestAssemblyRoot(new AssemblyList().Open(packagePath).Root.Child(LibFolder)).Assembly.Calculator();

        var list = new AssemblyList();
        var found = list.FindNode(calculator.Reference!);

        Assert.Equal(calculator.Text, found!.Text);
        var listed = Assert.Single(list.Listed);
        Assert.Equal(packagePath, listed.FilePath);
        Assert.Same(listed, found.Assembly.InPackage!.Container);
    }

    [Fact]
    public void Analyzing_a_member_of_an_assembly_in_a_package_finds_its_users_there()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var add = TestAssemblyRoot(package.Root.Child(LibFolder)).Assembly.Calculator().Child("Add(T) : void");
        var symbol = Analyzers.Of(add)!;
        var usedBy = Analyzers.KindsFor(symbol).Single(k => k.Header == "Used By");

        var callers = Analyzers.RunAsync(list, symbol, usedBy)
            .ToBlockingEnumerable()
            .SelectMany(static b => b)
            .ToList();

        var plus = Assert.Single(callers);
        Assert.StartsWith("operator +", plus.Label);
    }

    [Fact]
    public void A_package_named_like_a_referenced_assembly_is_not_taken_for_it()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        list.Open(WriteTextPackage(temp, "ZGF.ILSpy.Core.zip"));
        var assembly = list.Open(Trees.TestAssemblyPath);

        var core = assembly.Root.Child("References", "ZGF.ILSpy.Core");
        var resolved = list.FindNode(core.Target!);

        Assert.Equal(CoreAssemblyPath, resolved!.Assembly.FilePath, ignoreCase: true);
    }

    [Fact]
    public void A_file_of_a_package_that_is_no_assembly_shows_its_contents()
    {
        using var temp = new TempDir();
        var package = new AssemblyList().Open(WritePackage(temp));

        var readme = package.Root.Child("readme.txt");
        var native = package.Root.Child("runtimes/win-x64/native", "native.dll");

        Assert.Equal("Read me", Encoding.UTF8.GetString(readme.ReadResource()));
        Assert.Equal(NodeKind.Resource, native.Kind);
        Assert.Contains("Read me", readme.Decompile(DecompilationLanguage.CSharp).GetText());
    }

    [Theory]
    [InlineData(DecompilationLanguage.CSharp)]
    [InlineData(DecompilationLanguage.IL)]
    public void A_package_and_its_folders_list_what_they_hold(DecompilationLanguage language)
    {
        using var temp = new TempDir();
        var package = new AssemblyList().Open(WritePackage(temp));

        var root = package.Root.Decompile(language);
        var folder = package.Root.Child(LibFolder).Decompile(language);

        Assert.False(root.IsError);
        Assert.Contains($"{LibFolder}/", root.GetText());
        Assert.Contains("readme.txt (7 bytes)", root.GetText());
        Assert.Contains(TestAssemblyName, folder.GetText());
    }

    [Fact]
    public void Closing_a_package_forgets_the_assemblies_read_out_of_it()
    {
        using var temp = new TempDir();
        var packagePath = WritePackage(temp);
        var list = new AssemblyList();
        var before = TestAssemblyRoot(list.Open(packagePath).Root.Child(LibFolder)).Assembly;

        list.Close(list.Listed[0]);
        Assert.Empty(list.Assemblies);
        var after = TestAssemblyRoot(list.Open(packagePath).Root.Child(LibFolder)).Assembly;

        Assert.NotSame(before, after);
    }

    [Fact]
    public async Task Searching_finds_types_in_the_assemblies_of_an_open_package()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));

        var results = new List<SearchResult>();
        await foreach (var batch in list.SearchAsync("Calculator", SearchKinds.Types))
            results.AddRange(batch);

        var calculator = Assert.Single(results, r => r.Text == "Calculator<T>");
        Assert.Same(package, calculator.Node.Assembly.InPackage!.Container);
        Assert.Same(package.Root.Child(LibFolder), calculator.Node.Assembly.Root.Parent);
    }

    [Fact]
    public void An_assembly_in_a_package_takes_its_symbols_from_the_pdb_beside_it_there()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var folder = package.Root.Child(LibFolder);
        var assembly = TestAssemblyRoot(folder).Assembly;
        var core = list.Assemblies.Single(a => a.Name == "ZGF.ILSpy.Core");

        var symbols = assembly.DebugSymbols;

        Assert.Equal(Path.ChangeExtension(assembly.FilePath, ".pdb"), symbols!.SourceFileName);
        Assert.Null(core.DebugSymbols);
    }

    [Fact]
    public void Extracting_a_folder_writes_its_files_at_their_paths_from_it()
    {
        using var temp = new TempDir();
        var package = new AssemblyList().Open(WritePackage(temp));
        var target = temp.PathOf("out");

        var extracted = package.Package!.ExtractFolder(package.Root.Child(LibFolder).PackageFolder!, target);

        var dll = Path.Combine(target, "ZGF.ILSpy.Tests.dll");
        Assert.Empty(extracted.Failures);
        Assert.Equal(3, extracted.Written.Count);
        Assert.Contains(dll, extracted.Written);
        Assert.Equal(File.ReadAllBytes(Trees.TestAssemblyPath), File.ReadAllBytes(dll));
    }

    [Fact]
    public void Extracting_a_package_keeps_its_folders()
    {
        using var temp = new TempDir();
        var package = new AssemblyList().Open(WritePackage(temp));
        var target = temp.PathOf("out");

        package.Package!.ExtractFolder(package.Root.PackageFolder!, target);

        Assert.Equal("Read me", File.ReadAllText(Path.Combine(target, "readme.txt")));
        Assert.True(File.Exists(Path.Combine(target, "runtimes", "win-x64", "native", "native.dll")));
        Assert.True(File.Exists(Path.Combine(target, "lib", "net10.0", "ZGF.ILSpy.Core.dll")));
    }

    [Fact]
    public void A_file_whose_path_leads_out_of_the_folder_is_not_extracted()
    {
        using var temp = new TempDir();
        var path = temp.Write("Escape.zip", []);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            WriteText(archive, "../escaped.txt", "Out");
            WriteText(archive, "kept.txt", "In");
        }
        var package = new AssemblyList().Open(path);
        var target = temp.PathOf("out");

        var extracted = package.Package!.ExtractFolder(package.Root.PackageFolder!, target);

        var failure = Assert.Single(extracted.Failures);
        Assert.Equal("../escaped.txt", failure.File.FullName);
        Assert.Null(failure.Error);
        Assert.False(File.Exists(temp.PathOf("escaped.txt")));
        Assert.Equal([Path.Combine(target, "kept.txt")], extracted.Written);
    }

    [Fact]
    public void A_file_that_is_neither_an_assembly_nor_a_zip_is_listed_as_failed()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();

        var failed = list.Open(temp.Write("notes.dll", "not an assembly"));

        Assert.True(failed.HasLoadError);
        Assert.False(failed.IsPackage);
    }

    private static BrowseNode TestAssemblyRoot(BrowseNode folder)
    {
        return folder.Children.Single(IsTestAssembly);
    }

    private static bool IsTestAssembly(BrowseNode node)
    {
        if (node.Kind != NodeKind.Assembly)
            return false;
        return node.Assembly.Name == TestAssemblyName;
    }

    /// <summary>A NuGet package of the test assembly, with its PDB, and the assembly it references,
    /// without one, beside a native library and a text file.</summary>
    private static string WritePackage(TempDir temp)
    {
        var path = temp.Write("Sample.1.0.0.nupkg", []);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        archive.CreateEntryFromFile(Trees.TestAssemblyPath, $"{LibFolder}/ZGF.ILSpy.Tests.dll");
        archive.CreateEntryFromFile(Path.ChangeExtension(Trees.TestAssemblyPath, ".pdb"), $"{LibFolder}/ZGF.ILSpy.Tests.pdb");
        archive.CreateEntryFromFile(CoreAssemblyPath, $"{LibFolder}/ZGF.ILSpy.Core.dll");
        WriteText(archive, "runtimes/win-x64/native/native.dll", "not an assembly");
        WriteText(archive, "readme.txt", "Read me");
        return path;
    }

    /// <summary>A zip holding nothing but a text file.</summary>
    private static string WriteTextPackage(TempDir temp, string name)
    {
        var path = temp.Write(name, []);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        WriteText(archive, "readme.txt", "Read me");
        return path;
    }

    private static void WriteText(ZipArchive archive, string name, string text)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(text);
    }
}
