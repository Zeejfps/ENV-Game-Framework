using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Tests.Fixtures;

namespace ZGF.ILSpy.Tests;

public sealed class DebugSymbolsTests
{
    [Fact]
    public void The_pdb_beside_the_assembly_is_found_and_read_once()
    {
        var assembly = Trees.TestAssembly;

        var symbols = assembly.DebugSymbols;

        Assert.NotNull(symbols);
        Assert.Equal(Path.ChangeExtension(Trees.TestAssemblyPath, ".pdb"), symbols.SourceFileName);
        Assert.Same(symbols, assembly.DebugSymbols);
    }

    [Fact]
    public void Locals_keep_the_tuple_names_and_dynamic_flags_their_signatures_cannot()
    {
        var symbols = Trees.TestAssembly.DebugSymbols!;

        Assert.True(symbols.TryGetExtraTypeInfo(CircleMethod(nameof(Circle.Spread)), 0, out var tuple));
        Assert.True(symbols.TryGetExtraTypeInfo(CircleMethod(nameof(Circle.Echo)), 0, out var dynamic));

        Assert.Equal(["Low", "High"], tuple.TupleElementNames);
        Assert.Null(tuple.DynamicFlags);
        Assert.True(dynamic.DynamicFlags![0]);
    }

    [Fact]
    public void A_local_without_extra_type_info_has_none()
    {
        var symbols = Trees.TestAssembly.DebugSymbols!;

        Assert.False(symbols.TryGetExtraTypeInfo(CircleMethod(nameof(Circle.Grow)), 0, out _));
        Assert.True(symbols.TryGetName(CircleMethod(nameof(Circle.Grow)), 1, out var name));
        Assert.Equal("step", name);
    }

    [Fact]
    public void A_picked_pdb_is_checked_against_the_build_the_assembly_names()
    {
        using var temp = new TempDir();
        var list = new AssemblyList();
        var test = list.OpenModule(Trees.TestAssemblyPath);
        var sample = list.OpenModule(Trees.SampleAppPath);
        var windows = temp.Write("windows.pdb", Encoding.ASCII.GetBytes("Microsoft C/C++ MSF 7.00\r\n\u001aDS\0\0\0"));
        var text = temp.Write("notes.pdb", "not a pdb");

        Assert.Equal(PdbCheck.Matches, test.CheckPdb(TestPdbPath));
        Assert.Equal(PdbCheck.OtherBuild, sample.CheckPdb(TestPdbPath));
        Assert.Equal(PdbCheck.WindowsPdb, test.CheckPdb(windows));
        Assert.Equal(PdbCheck.Unreadable, test.CheckPdb(text));
        Assert.Equal(PdbCheck.Unreadable, test.CheckPdb(temp.PathOf("missing.pdb")));
    }

    [Fact]
    public void An_assembly_reads_the_pdb_picked_for_it_once_it_is_opened_again()
    {
        using var temp = new TempDir();
        var copy = temp.Copy(Trees.TestAssemblyPath, Path.GetFileName(Trees.TestAssemblyPath));
        var list = new AssemblyList();
        var before = list.OpenModule(copy).DebugSymbols;

        list.Pdbs.Pick(AssemblyPath.Of(copy), TestPdbPath);
        list.Clear();
        var after = list.OpenModule(copy);

        Assert.Null(before);
        Assert.Equal(TestPdbPath, after.DebugSymbols!.SourceFileName);
        Assert.True(after.DebugSymbols.TryGetName(CircleMethod(nameof(Circle.Grow)), 1, out var name));
        Assert.Equal("step", name);
        Assert.Contains(after.Root.Children, c => c.Text == "Debug Metadata (From portable PDB)");
    }

    [Fact]
    public void Closing_an_assembly_forgets_the_pdb_picked_for_it()
    {
        var list = new AssemblyList();
        var test = list.Open(Trees.TestAssemblyPath);
        list.Pdbs.Pick(test.FilePath, TestPdbPath);

        list.Close(test);

        Assert.Empty(list.Pdbs.All);
    }

    private static string TestPdbPath => Path.ChangeExtension(Trees.TestAssemblyPath, ".pdb");

    private static MethodDefinitionHandle CircleMethod(string name)
    {
        var token = typeof(Circle).GetMethod(name)!.MetadataToken;
        return (MethodDefinitionHandle)MetadataTokens.EntityHandle(token);
    }
}
