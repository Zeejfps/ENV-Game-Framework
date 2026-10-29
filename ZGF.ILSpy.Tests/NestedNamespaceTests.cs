using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class NestedNamespaceTests
{
    private const string TestsNamespace = "ZGF.ILSpy.Tests";

    private static IReadOnlyList<BrowseNode> NestedChildren(BrowseNode node)
    {
        node.LoadChildren();
        return Assert.IsType<ChildrenState.Ready>(TestSession.Shared.PeekNestedChildren(node)).Children;
    }

    [Fact]
    public void Every_namespace_a_name_nests_in_is_listed_and_those_without_types_only_enclose_others()
    {
        var root = Trees.TestAssembly.Root;

        var namespaces = root.Children
            .Where(c => c.Kind == NodeKind.Namespace)
            .ToList();

        Assert.Equal(["-", "ZGF", "ZGF.ILSpy", TestsNamespace, Trees.FixturesNamespace], namespaces.Select(n => n.Text));
        Assert.Equal([false, true, true, false, false], namespaces.Select(n => Assert.IsType<NodeSubject.Namespace>(n.Subject).Value.IsEnclosingOnly));
        Assert.Empty(root.Child("ZGF").Children);
        Assert.False(root.Child("ZGF").HasChildren);
    }

    [Fact]
    public void Each_namespace_nests_in_the_one_its_name_extends_by_a_segment()
    {
        var root = Trees.TestAssembly.Root;
        var zgf = root.Child("ZGF");
        var ilspy = root.Child("ZGF.ILSpy");
        var tests = root.Child(TestsNamespace);
        var fixtures = root.Child(Trees.FixturesNamespace);

        Assert.Null(root.Child("-").EnclosingNamespace);
        Assert.Null(zgf.EnclosingNamespace);
        Assert.Same(zgf, ilspy.EnclosingNamespace);
        Assert.Same(ilspy, tests.EnclosingNamespace);
        Assert.Same(tests, fixtures.EnclosingNamespace);
        Assert.Equal([ilspy], zgf.InnerNamespaces);
        Assert.Equal([fixtures], tests.InnerNamespaces);
        Assert.Empty(fixtures.InnerNamespaces);
    }

    [Fact]
    public void Nested_an_assembly_lists_only_its_outermost_namespaces_and_each_namespace_its_inner_ones()
    {
        var root = Trees.TestAssembly.Root;

        var outermost = NestedChildren(root)
            .Where(c => c.Kind == NodeKind.Namespace)
            .Select(c => c.Text);

        Assert.Equal(["-", "ZGF"], outermost);
        Assert.Equal([root.Child("ZGF.ILSpy")], NestedChildren(root.Child("ZGF")));
        Assert.Equal([root.Child(TestsNamespace)], NestedChildren(root.Child("ZGF.ILSpy")));
        Assert.Equal(root.Children.Where(c => c.Kind != NodeKind.Namespace), NestedChildren(root).Where(c => c.Kind != NodeKind.Namespace));
    }

    [Fact]
    public void Nested_a_namespace_lists_its_inner_namespaces_among_its_own_types_by_name()
    {
        var root = Trees.TestAssembly.Root;
        var tests = root.Child(TestsNamespace);
        var fixtures = root.Child(Trees.FixturesNamespace);

        var nested = NestedChildren(tests);
        var at = nested.ToList().IndexOf(fixtures);

        Assert.Equal(tests.Children, nested.Where(c => c != fixtures));
        Assert.True(at > 0);
        Assert.True(NaturalStringComparer.Instance.Compare(nested[at - 1].Text, "Fixtures") < 0);
        Assert.True(NaturalStringComparer.Instance.Compare(nested[at + 1].Text, "Fixtures") > 0);
        Assert.Equal(fixtures.Children, NestedChildren(fixtures));
        Assert.Contains(fixtures.Child("Calculator<T>"), NestedChildren(fixtures));
    }

    [Fact]
    public void A_namespace_that_only_encloses_others_is_found_again_by_its_full_name()
    {
        var ilspy = Trees.TestAssembly.Root.Child("ZGF.ILSpy");
        var (fresh, _) = Trees.OpenTestAssembly();

        var found = fresh.FindNode(ilspy.Path);

        Assert.Equal([new NodePathSegment(NodeKind.Namespace, "ZGF.ILSpy")], ilspy.Path.Segments);
        Assert.NotNull(found);
        Assert.True(Assert.IsType<NodeSubject.Namespace>(found!.Subject).Value.IsEnclosingOnly);
        Assert.Equal("ZGF.ILSpy", found.Text);
        Assert.StartsWith("namespace ZGF.ILSpy", ilspy.Decompile(DecompilationLanguage.CSharp).GetText());
    }

    [Fact]
    public void A_namespace_name_ends_in_its_last_segment()
    {
        Assert.Equal("Generic", NamespaceNames.LastSegment("System.Collections.Generic"));
        Assert.Equal("System", NamespaceNames.LastSegment("System"));
        Assert.Equal(["System.Collections.Generic", "System.Collections", "System"], NamespaceNames.AndEnclosing("System.Collections.Generic"));
        Assert.Equal([""], NamespaceNames.AndEnclosing(""));
    }
}
