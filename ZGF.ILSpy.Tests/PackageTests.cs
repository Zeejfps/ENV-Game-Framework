using System.IO.Compression;
using System.Text;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class PackageTests
{
    private const string LibFolder = "lib/net10.0";
    private const string TestAssemblyName = "ZGF.ILSpy.Tests";

    private static string CoreAssemblyPath => typeof(AssemblyList).Assembly.Location;

    [Fact]
    public void A_zip_opens_as_a_package_listed_among_the_files_but_not_the_assemblies()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();

        var package = list.Open(WritePackage(temp));

        Assert.True(package.IsPackage);
        Assert.Equal([package], list.Listed);
        Assert.Empty(list.Assemblies);
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
        var after = TestAssemblyRoot(list.Open(packagePath).Root.Child(LibFolder)).Assembly;

        Assert.NotSame(before, after);
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

    /// <summary>A NuGet package of the test assembly and the assembly it references, beside a native
    /// library and a text file.</summary>
    private static string WritePackage(TempDir temp)
    {
        var path = temp.Write("Sample.1.0.0.nupkg", []);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Update);
        archive.CreateEntryFromFile(Trees.TestAssemblyPath, $"{LibFolder}/ZGF.ILSpy.Tests.dll");
        archive.CreateEntryFromFile(CoreAssemblyPath, $"{LibFolder}/ZGF.ILSpy.Core.dll");
        WriteText(archive, "runtimes/win-x64/native/native.dll", "not an assembly");
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
