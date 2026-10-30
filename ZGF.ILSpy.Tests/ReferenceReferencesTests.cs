using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class ReferenceReferencesTests
{
    private const string Runtime = "System.Runtime";
    private const string CoreLib = "System.Private.CoreLib";

    /// <summary>The test assembly's reference to System.Runtime.</summary>
    private static BrowseNode RuntimeReference(LoadedAssembly assembly) => assembly.Root.Child("References", Runtime);

    [Fact]
    public void A_reference_lists_the_references_of_the_assembly_it_resolves_to_after_the_types_used_from_it()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var children = RuntimeReference(assembly).Children;

        Assert.IsType<NodeSubject.ReferencedTypes>(children[0].Subject);
        var coreLib = Assert.Single(children, c => c.Text == CoreLib);
        Assert.IsType<NodeSubject.AssemblyReference>(coreLib.Subject);
        Assert.Equal(Runtime, coreLib.Assembly.Name);
    }

    [Fact]
    public void The_assembly_a_reference_resolves_to_is_read_without_being_listed()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        RuntimeReference(assembly).LoadChildren();

        Assert.Equal([assembly], list.Listed);
    }

    [Fact]
    public void A_reference_of_a_reference_holds_what_that_assembly_uses_of_it()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var coreLib = RuntimeReference(assembly).Child(CoreLib);

        var forwarded = coreLib.Child("Referenced types").Children;

        Assert.Contains(forwarded, f => f.Subject is NodeSubject.ForwardedType && f.Text == "Object");
    }

    [Fact]
    public void A_reference_of_a_reference_is_found_again_by_its_path()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var coreLib = RuntimeReference(assembly).Child(CoreLib);

        var found = list.FindNode(coreLib.Path);

        Assert.Same(coreLib, found);
        Assert.Same(assembly.Root, coreLib.Root);
    }

    [Fact]
    public void A_reference_of_a_reference_reads_as_where_it_resolves_from_the_assembly_that_holds_it()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var coreLib = RuntimeReference(assembly).Child(CoreLib);

        var text = Assert.IsType<DocumentContent>(coreLib.Content()).Document.GetText();

        Assert.Contains("Resolves to", text);
        Assert.Contains($"{CoreLib}.dll", text);
    }
}
