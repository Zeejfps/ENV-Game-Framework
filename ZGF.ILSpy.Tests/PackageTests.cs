using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
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

        Assert.IsType<LoadedPackage>(package);
        Assert.Equal([package], list.Listed);
        Assert.Equal(["ZGF.ILSpy.Core", TestAssemblyName], list.Assemblies.Select(a => a.Name));
        Assert.All(list.Assemblies, a => Assert.Same(package, ContainerOf(a)));
        Assert.False(Assert.IsType<NodeSubject.Package>(package.Root.Subject).Value.Package.Package.IsBundle);
        Assert.Equal("Sample.1.0.0.nupkg", package.Root.Text);
        Assert.Equal([LibFolder, "runtimes/win-x64/native", "readme.txt"], package.Root.Children.Select(c => c.Text));
    }

    [Fact]
    public void A_zip_entry_named_with_backslashes_is_listed_with_slashes_and_reads_back()
    {
        using var temp = new TempDir();
        var path = temp.Write("Slashes.zip", []);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            WriteText(archive, "docs\\guide.txt", "Guide");
            WriteText(archive, "./readme.txt", "Read me");
        }

        var package = AssemblyPackage.TryRead(AssemblyPath.Of(path))!;

        var guide = package.FindFile("docs/guide.txt")!;
        Assert.Equal("Guide", Encoding.UTF8.GetString(package.ReadFile(guide)));
        Assert.Equal("Read me", Encoding.UTF8.GetString(package.ReadFile(package.FindFile("readme.txt")!)));
    }

    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("..\\evil.txt")]
    [InlineData("docs/../../evil.txt")]
    [InlineData("/evil.txt")]
    [InlineData("\\evil.txt")]
    [InlineData("C:/evil.txt")]
    [InlineData("C:evil.txt")]
    public void A_zip_entry_whose_name_leads_out_of_the_package_is_left_out(string name)
    {
        using var temp = new TempDir();
        var path = temp.Write("Escape.zip", []);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
            WriteText(archive, name, "Evil");
            WriteText(archive, "readme.txt", "Read me");
        }

        var package = AssemblyPackage.TryRead(AssemblyPath.Of(path))!;

        Assert.Equal(["readme.txt"], package.ListFiles().Select(static f => f.FullName));
        Assert.Empty(package.Root.Folders);
    }

    [Fact]
    public void A_reference_into_a_package_file_itself_finds_nothing_rather_than_failing()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var path = WritePackage(temp);
        var type = CodeReference.ToEntity(CodeReferenceKind.Type, "Sample", AssemblyPath.Of(path), MetadataTokens.TypeDefinitionHandle(2));

        Assert.Null(list.FindNode(type));
        Assert.Null(list.FindMetadataRow(type));
        Assert.Null(list.Describe(type));
        Assert.Null(list.FindApiReferencePage(type));
        Assert.Null(list.FindTypeDefinition(type));
        Assert.Null(list.FindMetadataRow(CodeReference.ToAssembly("Sample", AssemblyPath.Of(path))));
    }

    [Fact]
    public void Closing_a_package_reached_through_one_assembly_forgets_the_picks_of_the_others_in_it()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var sibling = package.FilePath.Combine(Path.Combine("lib", "net10.0", "ZGF.ILSpy.Core.dll"));
        list.TargetFrameworks.Pick(sibling, ".NETFramework,Version=v4.8");
        list.Clear();
        var assembly = list.TryLoad(package.FilePath.Combine(Path.Combine("lib", "net10.0", "ZGF.ILSpy.Tests.dll")))!;

        list.Close(assembly);

        Assert.Empty(list.TargetFrameworks.All);
    }

    [Fact]
    public void A_package_read_again_for_an_assembly_in_it_takes_its_place_with_its_assemblies_read_again()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var old = list.Assemblies.ToList();

        var fresh = list.ReadAgain(old[1]);
        var replaced = list.Replace(package, fresh);

        Assert.True(replaced);
        Assert.Equal([fresh], list.Listed);
        Assert.All(list.Assemblies, a => Assert.Same(fresh, ContainerOf(a)));
        Assert.Empty(list.Assemblies.Intersect(old));
        var root = TestAssemblyRoot(fresh.Root.Child(LibFolder));
        Assert.Same(list.Assemblies[1], root.Assembly);
        Assert.Same(root, list.FindNode(old[1].Root.Path));
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

        Assert.Equal(package.FilePath.Combine(Path.Combine("lib", "net10.0", "ZGF.ILSpy.Tests.dll")), assembly.FilePath);
        Assert.Same(package, ContainerOf(assembly));
        Assert.Same(root, assembly.Root);
        Assert.Same(folder, root.Parent);
        Assert.Equal(TypeShape.Class, assembly.Calculator().TypeShape);
    }

    [Fact]
    public void An_assembly_read_out_of_a_package_finds_its_root_before_the_folder_is_opened()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var path = package.FilePath.Combine(Path.Combine("lib", "net10.0", "ZGF.ILSpy.Tests.dll"));

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

        Assert.Equal(packagePath, path.FilePath.Text);
        Assert.Equal(new NodePathSegment.EmbeddedModule($"{LibFolder}/{TestAssemblyName}.dll"), path.Steps[0].Segment);
        Assert.NotNull(found);
        Assert.Equal(calculator.Text, found.Text);
        Assert.IsType<LoadedPackage>(Assert.Single(second.Listed));
    }

    [Fact]
    public void References_resolve_to_the_assembly_beside_it_in_the_package()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var assembly = TestAssemblyRoot(package.Root.Child(LibFolder)).Assembly;

        var core = assembly.Root.Child("References", "ZGF.ILSpy.Core");
        var resolved = list.FindNode(core.Destination);

        Assert.Equal(package.FilePath.Combine(Path.Combine("lib", "net10.0", "ZGF.ILSpy.Core.dll")), resolved!.Assembly.FilePath);
        Assert.Same(package, ContainerOf(resolved.Assembly));
    }

    [Fact]
    public void Following_a_reference_into_a_package_lists_the_package_and_not_the_assembly()
    {
        using var temp = new TempDir();
        var packagePath = WritePackage(temp);
        var calculator = TestAssemblyRoot(new AssemblyList().Open(packagePath).Root.Child(LibFolder)).Assembly.Calculator();

        var list = new AssemblyList();
        var found = list.FindNode(calculator.Definition);

        Assert.Equal(calculator.Text, found!.Text);
        var listed = Assert.Single(list.Listed);
        Assert.Equal(packagePath, listed.FilePath.Text);
        Assert.Same(listed, ContainerOf(found.Assembly));
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
        var resolved = list.FindNode(core.Destination);

        Assert.Equal(CoreAssemblyPath, resolved!.Assembly.FilePath.Text, ignoreCase: true);
    }

    [Fact]
    public void A_file_of_a_package_that_is_no_assembly_shows_its_contents()
    {
        using var temp = new TempDir();
        var package = Assert.IsType<LoadedPackage>(new AssemblyList().Open(WritePackage(temp)));

        var readme = package.Root.Child("readme.txt");
        var native = package.Root.Child("runtimes/win-x64/native", "native.dll");

        Assert.Equal("Read me", Encoding.UTF8.GetString(readme.ReadResource()));
        Assert.IsType<NodeSubject.PackageFile>(native.Subject);
        Assert.False(native.IsImage);
        Assert.Contains("Read me", readme.Decompile(DecompilationLanguage.CSharp).GetText());
    }

    [Theory]
    [InlineData(DecompilationLanguage.CSharp)]
    [InlineData(DecompilationLanguage.IL)]
    public void A_package_and_its_folders_list_what_they_hold(DecompilationLanguage language)
    {
        using var temp = new TempDir();
        var package = Assert.IsType<LoadedPackage>(new AssemblyList().Open(WritePackage(temp)));

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
        Assert.Same(package, ContainerOf(calculator.Node.Assembly));
        Assert.Same(package.Root.Child(LibFolder), calculator.Node.Assembly.Root.Parent);
    }

    [Fact]
    public void An_assembly_in_a_package_takes_its_symbols_from_the_pdb_beside_it_there()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var package = list.Open(WritePackage(temp));
        var folder = package.Root.Child(LibFolder);
        var assembly = Assert.IsType<LoadedModule>(TestAssemblyRoot(folder).Assembly);
        var core = list.Assemblies.Single(a => a.Name == "ZGF.ILSpy.Core");

        var symbols = assembly.DebugSymbols;

        Assert.Equal(Path.ChangeExtension(assembly.FilePath.Text, ".pdb"), symbols!.SourceFileName);
        Assert.Null(core.DebugSymbols);
    }

    [Fact]
    public void Extracting_a_folder_writes_its_files_at_their_paths_from_it()
    {
        using var temp = new TempDir();
        var package = Assert.IsType<LoadedPackage>(new AssemblyList().Open(WritePackage(temp)));
        var target = temp.PathOf("out");

        var extracted = package.Package.ExtractFolder(FolderOf(package.Root.Child(LibFolder)), target);

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
        var package = Assert.IsType<LoadedPackage>(new AssemblyList().Open(WritePackage(temp)));
        var target = temp.PathOf("out");

        package.Package.ExtractFolder(FolderOf(package.Root), target);

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
        var package = Assert.IsType<LoadedPackage>(new AssemblyList().Open(path));
        var target = temp.PathOf("out");

        var extracted = package.Package.ExtractFolder(FolderOf(package.Root), target);

        Assert.Empty(extracted.Failures);
        Assert.False(File.Exists(temp.PathOf("escaped.txt")));
        Assert.Equal([Path.Combine(target, "kept.txt")], extracted.Written);
    }

    [Fact]
    public void A_file_that_is_neither_an_assembly_nor_a_zip_is_listed_as_failed()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();

        var failed = list.Open(temp.Write("notes.dll", "not an assembly"));

        Assert.IsType<FailedAssembly>(failed);
        Assert.IsNotType<LoadedPackage>(failed);
    }

    private static BrowseNode TestAssemblyRoot(BrowseNode folder)
    {
        return folder.Children.Single(IsTestAssembly);
    }

    private static bool IsTestAssembly(BrowseNode node)
    {
        if (node.Subject is not NodeSubject.Module)
            return false;
        return node.Assembly.Name == TestAssemblyName;
    }

    /// <summary>A NuGet package of the test assembly, with its PDB, and the assembly it references,
    /// without one, beside a native library and a text file.</summary>
    /// <summary>The package <paramref name="assembly"/> was read out of; fails the test when it was read from no package.</summary>
    private static LoadedPackage ContainerOf(LoadedAssembly assembly) =>
        Assert.IsType<PackagePlace>(Assert.IsType<LoadedModule>(assembly).Place).Container;

    private static PackageFolder FolderOf(BrowseNode node) => Assert.IsType<PackageFolderPlace>(node.FolderInPackage).Folder;

    internal static string WritePackage(TempDir temp)
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
