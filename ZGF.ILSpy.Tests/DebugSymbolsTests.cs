using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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

    private static MethodDefinitionHandle CircleMethod(string name)
    {
        var token = typeof(Circle).GetMethod(name)!.MetadataToken;
        return (MethodDefinitionHandle)MetadataTokens.EntityHandle(token);
    }
}
