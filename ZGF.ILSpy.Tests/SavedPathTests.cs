using System.Diagnostics;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class SavedPathTests : IDisposable
{
    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    /// <summary>A record struct, which the fixtures have none of.</summary>
    private readonly record struct Interval(int Start, int End);

    public static IEnumerable<object[]> SavedKinds() =>
        Enum.GetValues<NodeKind>()
            .Where(static k => k is not (NodeKind.Error or NodeKind.Loading))
            .Select(static k => new object[] { k });

    [Theory]
    [MemberData(nameof(SavedKinds))]
    public void A_path_to_a_node_of_each_kind_finds_it_again_in_another_list_that_reopened_its_file(NodeKind kind)
    {
        var node = Sample(kind);
        var reopened = new AssemblyList();
        reopened.Open(node.Path.FilePath);

        var found = reopened.FindNode(node.Path);

        Assert.NotNull(found);
        Assert.NotSame(node, found);
        Assert.Equal((kind, node.Text, node.Path), (found.Kind, found.Text, found.Path));
    }

    [Fact]
    public void A_path_to_a_file_that_failed_finds_nothing_in_a_list_that_does_not_list_it()
    {
        var failed = Sample(NodeKind.FailedAssembly);

        Assert.Null(new AssemblyList().FindNode(failed.Path));
    }

    /// <summary>A node of <paramref name="kind"/>, in a file left in place for another list to open.</summary>
    private BrowseNode Sample(NodeKind kind) => kind switch
    {
        NodeKind.Package or NodeKind.PackageFolder => FirstOf(Open(PackageTests.WritePackage(_temp)), kind),
        NodeKind.Bundle => Open(BundleTests.WriteBundle(_temp)).Root,
        NodeKind.FailedAssembly => Open(_temp.Write("broken.dll", "no assembly")).Root,
        NodeKind.ForwardedType => FirstOf(Open(Path.Combine(RuntimeAssemblies.Directories[0], "System.Runtime.dll")), kind),
        NodeKind.Assembly or NodeKind.ReferencesFolder or NodeKind.AssemblyReference or NodeKind.ResourcesFolder or NodeKind.Resource
            or NodeKind.Namespace or NodeKind.Class or NodeKind.Interface or NodeKind.Struct or NodeKind.Enum or NodeKind.Delegate
            or NodeKind.Record or NodeKind.RecordStruct or NodeKind.BaseTypes or NodeKind.BaseType or NodeKind.Field or NodeKind.Constant
            or NodeKind.Property or NodeKind.Indexer or NodeKind.Event or NodeKind.Method or NodeKind.Constructor or NodeKind.Operator
            or NodeKind.Metadata or NodeKind.MetadataHeader or NodeKind.MetadataDirectory or NodeKind.MetadataTables
            or NodeKind.MetadataTable or NodeKind.MetadataHeap or NodeKind.DerivedTypes or NodeKind.DerivedType or NodeKind.ResourcesFile
            or NodeKind.ResourceImage or NodeKind.ReferencedTypes or NodeKind.TypeReference or NodeKind.MethodReference
            or NodeKind.FieldReference or NodeKind.PropertyReference or NodeKind.EventReference or NodeKind.ModuleReference
            or NodeKind.ImportedMethod => FirstOf(Open(Trees.TestAssemblyPath), kind),
        _ => throw new UnreachableException(),
    };

    private static LoadedAssembly Open(string path) => new AssemblyList().Open(path);

    /// <summary>The first node of <paramref name="kind"/> breadth first from the root of
    /// <paramref name="assembly"/>, not following a reference into the references of the assembly it
    /// resolves to.</summary>
    private static BrowseNode FirstOf(LoadedAssembly assembly, NodeKind kind)
    {
        var queue = new Queue<BrowseNode>([assembly.Root]);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node.Kind == kind)
                return node;
            foreach (var child in node.Children)
            {
                if (node.Kind == NodeKind.AssemblyReference && child.Kind == NodeKind.AssemblyReference) continue;
                queue.Enqueue(child);
            }
        }
        throw new InvalidOperationException($"{assembly.Name} has no node of kind {kind}.");
    }
}
