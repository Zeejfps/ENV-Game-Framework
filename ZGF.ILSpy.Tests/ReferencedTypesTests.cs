using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class ReferencedTypesTests
{
    private const string ListLabel = "List<>";
    private const string ListNamespace = "System.Collections.Generic";

    /// <summary>The used type <c>List&lt;T&gt;</c> under the reference the test assembly takes it from.</summary>
    private static BrowseNode UsedList(LoadedAssembly assembly)
    {
        return assembly.Root.Child("References").Children
            .Where(r => r.HasChildren)
            .Select(r => r.Child("Referenced types"))
            .SelectMany(t => t.Children)
            .Single(IsList);
    }

    private static bool IsList(BrowseNode node)
    {
        if (node.Text != ListLabel)
            return false;
        return node.Namespace == ListNamespace;
    }

    [Fact]
    public void A_reference_lists_the_types_used_from_it_with_the_members_used_of_each()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var list = UsedList(assembly);

        Assert.Equal(NodeKind.TypeReference, list.Kind);
        Assert.Equal("System.Collections.Generic.List<>", list.FullName);
        Assert.Equal(NodeKind.MethodReference, list.Child("Add(T) : void").Kind);
        Assert.Equal(NodeKind.TypeReference, list.Child("Enumerator").Kind);
    }

    [Fact]
    public void A_used_member_reads_as_its_type_declares_it_and_an_accessor_as_its_property()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var list = UsedList(assembly);

        var current = list.Child("Enumerator", "Current : T");

        Assert.Equal(NodeKind.PropertyReference, current.Kind);
        Assert.DoesNotContain(list.Children, IsSpecialized);
    }

    private static bool IsSpecialized(BrowseNode node)
    {
        return node.Text.Contains("!0", StringComparison.Ordinal);
    }

    [Fact]
    public void A_used_type_goes_to_where_it_is_defined()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var definition = list.FindNode(UsedList(assembly).Target!);

        Assert.NotNull(definition);
        Assert.Equal(NodeKind.Class, definition.Kind);
        Assert.Equal("List<T>", definition.Text);
    }

    [Fact]
    public void A_reference_with_nothing_used_from_it_has_no_children()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var references = assembly.Root.Child("References").Children;

        Assert.All(references, r => Assert.Equal(r.HasChildren, r.Children.Count == 1));
    }

    [Fact]
    public void The_referenced_types_read_as_each_type_with_what_is_used_of_it_indented_under_it()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var types = UsedList(assembly).Parent!;

        var text = Assert.IsType<DocumentContent>(types.Content()).Document.GetText();

        Assert.Contains("System.Collections.Generic.List<>", text);
        Assert.Contains("    Add(T) : void", text);
    }
}
