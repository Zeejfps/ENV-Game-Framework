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
            .SelectMany(r => r.Children)
            .Where(c => c.Subject is NodeSubject.ReferencedTypes)
            .SelectMany(t => t.Children)
            .Single(IsList);
    }

    private static bool IsList(BrowseNode node)
    {
        if (node.Text != ListLabel)
            return false;
        return node.Subject is NodeSubject.TypeReference type && type.Value.Namespace == ListNamespace;
    }

    [Fact]
    public void A_reference_lists_the_types_used_from_it_with_the_members_used_of_each()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var list = UsedList(assembly);

        Assert.IsType<NodeSubject.TypeReference>(list.Subject);
        Assert.Equal("System.Collections.Generic.List<>", list.FullName);
        Assert.Equal(MemberShape.Method, Assert.IsType<NodeSubject.MemberReference>(list.Child("Add(T) : void").Subject).Value.Shape);
        Assert.IsType<NodeSubject.TypeReference>(list.Child("Enumerator").Subject);
    }

    [Fact]
    public void A_used_member_reads_as_its_type_declares_it_and_an_accessor_as_its_property()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var list = UsedList(assembly);

        var current = list.Child("Enumerator", "Current : T");

        Assert.Equal(MemberShape.Property, Assert.IsType<NodeSubject.MemberReference>(current.Subject).Value.Shape);
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

        var definition = list.FindNode(UsedList(assembly).Destination);

        Assert.NotNull(definition);
        Assert.Equal(TypeShape.Class, definition.TypeShape);
        Assert.Equal("List<T>", definition.Text);
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
