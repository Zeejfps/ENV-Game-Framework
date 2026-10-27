using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class NodePathTests
{
    [Fact]
    public void A_listed_lookup_finds_a_place_only_in_an_assembly_the_list_holds_and_lists_none()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var add = assembly.Calculator().Child("Add(T) : void");
        var empty = new AssemblyList();

        Assert.Null(empty.FindListedNode(add.Path));
        Assert.Empty(empty.Listed);

        var (listing, _) = Trees.OpenTestAssembly();
        Assert.Equal(add.Text, listing.FindListedNode(add.Path)!.Text);
    }

    [Fact]
    public void A_path_finds_the_same_place_in_a_reopened_assembly()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var add = assembly.Calculator().Child("Add(T) : void");
        var (fresh, _) = Trees.OpenTestAssembly();

        var found = fresh.FindNode(add.Path);

        Assert.NotNull(found);
        Assert.NotSame(add, found);
        Assert.Equal(add.Path, found!.Path);
        Assert.Equal(add.Text, found.Text);
        Assert.Contains(found.Assembly, fresh.Assemblies);
    }

    [Fact]
    public void Paths_are_equal_by_place()
    {
        var (_, a) = Trees.OpenTestAssembly();
        var (_, b) = Trees.OpenTestAssembly();

        Assert.Equal(a.Calculator().Path, b.Calculator().Path);
        Assert.Equal(a.Calculator().Path.GetHashCode(), b.Calculator().Path.GetHashCode());
        Assert.NotEqual(a.Calculator().Path, a.Fixtures().Path);
        Assert.Equal(a.Root.Path, b.Root.Path);
    }

    [Fact]
    public void A_path_into_a_file_that_is_gone_finds_nothing()
    {
        using var temp = new TempDir();
        var copy = temp.Copy(Trees.TestAssemblyPath, "fixture.dll");
        var path = new AssemblyList().Open(copy).Calculator().Path;
        File.Delete(copy);

        Assert.Null(new AssemblyList().FindNode(path));
    }

    [Fact]
    public void A_closed_assembly_is_never_listed_again()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        list.Close(assembly);

        Assert.False(list.TryList(assembly));
        var reopened = list.Open(Trees.TestAssemblyPath);
        Assert.False(list.TryList(assembly));

        Assert.NotSame(assembly, reopened);
        Assert.Equal([reopened], list.Assemblies);
    }

    [Fact]
    public void Children_can_be_asked_for_without_building_them()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var root = assembly.Root;

        Assert.False(root.TryGetChildren(out _));
        Assert.Same(root.LoadingChild, root.LoadingChild);
        Assert.Equal(NodeKind.Loading, root.LoadingChild.Kind);
        Assert.Same(root, root.LoadingChild.Parent);

        var built = root.Children;
        Assert.True(root.TryGetChildren(out var children));
        Assert.Same(built, children);
    }
}
