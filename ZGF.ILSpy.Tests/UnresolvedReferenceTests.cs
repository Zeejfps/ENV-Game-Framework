using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

/// <summary>The test assembly copied away from ZGF.ILSpy.Core, which it references and which then
/// resolves to nothing.</summary>
public sealed class UnresolvedReferenceTests : IDisposable
{
    private const string Core = "ZGF.ILSpy.Core";

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    /// <summary>A type whose base type ZGF.ILSpy.Core defines.</summary>
    private sealed record Detached : NodeContent;

    private (AssemblyList List, LoadedModule Assembly) OpenAlone()
    {
        var list = new AssemblyList();
        return (list, list.OpenModule(_temp.Copy(Trees.TestAssemblyPath, "ZGF.ILSpy.Tests.dll")));
    }

    private static BrowseNode BaseTypes(LoadedAssembly assembly) =>
        assembly.Root.Child("ZGF.ILSpy.Tests", nameof(UnresolvedReferenceTests), nameof(Detached), "Base Types");

    [Fact]
    public void A_reference_that_resolves_to_no_assembly_lists_only_the_types_used_from_it_and_goes_nowhere()
    {
        var (list, assembly) = OpenAlone();

        var core = assembly.Root.Child("References", Core);

        Assert.IsType<NodeSubject.ReferencedTypes>(Assert.Single(core.Children).Subject);
        Assert.Null(list.FindNode(core.Destination));
        Assert.Contains("// Not found", core.Decompile(DecompilationLanguage.CSharp).GetText());
    }

    [Fact]
    public void A_base_type_that_resolves_to_no_definition_is_listed_by_name_without_a_target()
    {
        var (_, alone) = OpenAlone();
        var (list, beside) = Trees.OpenTestAssembly();

        var unresolved = BaseTypes(alone).Child(nameof(NodeContent));
        var resolved = BaseTypes(beside).Child(nameof(NodeContent));

        Assert.IsType<NodeSubject.BaseType>(unresolved.Subject);
        Assert.IsType<Activation.OpenSelf>(Activation.Of(unresolved.Subject));
        Assert.Equal(NodeAccessibility.None, unresolved.Accessibility);
        Assert.True(unresolved.IsPublicApi);
        Assert.Contains(nameof(NodeContent), unresolved.Decompile(DecompilationLanguage.CSharp).GetText());
        Assert.Equal(NodeAccessibility.Public, resolved.Accessibility);
        Assert.Equal(nameof(NodeContent), Assert.IsAssignableFrom<BrowseNode>(list.FindNode(resolved.Destination)).Text);
    }

    [Fact]
    public void A_type_used_from_an_assembly_that_does_not_resolve_is_listed_with_its_members_and_goes_nowhere()
    {
        var (list, assembly) = OpenAlone();

        var used = assembly.Root.Child("References", Core, "Referenced types", nameof(NodeContent));

        Assert.IsType<NodeSubject.TypeReference>(used.Subject);
        Assert.Equal(typeof(NodeContent).FullName, used.FullName);
        Assert.Null(list.FindNode(used.Destination));
        Assert.NotEmpty(used.Children);
        Assert.All(used.Children, m => Assert.Null(list.FindNode(m.Destination)));
    }
}
