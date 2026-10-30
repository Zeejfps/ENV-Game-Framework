using System.Diagnostics;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

public sealed class SavedPathTests : IDisposable
{
    /// <summary>The kinds builds before versioned paths saved with each step of a place, by name, one for
    /// each sort of node the tree has.</summary>
    private static readonly string[] LegacyKinds =
    [
        "Assembly", "ReferencesFolder", "AssemblyReference", "ResourcesFolder", "Resource", "Namespace", "Class", "Interface", "Struct",
        "Enum", "Delegate", "Record", "RecordStruct", "BaseTypes", "BaseType", "Field", "Constant", "Property", "Indexer", "Event", "Method",
        "Constructor", "Operator", "Metadata", "MetadataHeader", "MetadataDirectory", "MetadataTables", "MetadataTable", "MetadataHeap",
        "DerivedTypes", "DerivedType", "ResourcesFile", "ResourceImage", "FailedAssembly", "ForwardedType", "Package", "PackageFolder",
        "Bundle", "ReferencedTypes", "TypeReference", "MethodReference", "FieldReference", "PropertyReference", "EventReference",
        "ModuleReference", "ImportedMethod",
    ];

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    /// <summary>A record struct, which the fixtures have none of.</summary>
    private readonly record struct Interval(int Start, int End);

    public static IEnumerable<object[]> SavedKinds() => LegacyKinds.Select(static k => new object[] { k });

    [Theory]
    [MemberData(nameof(SavedKinds))]
    public void A_path_to_a_node_of_each_kind_finds_it_again_in_another_list_that_reopened_its_file(string kind)
    {
        var node = Sample(kind);
        var reopened = new AssemblyList();
        reopened.Open(node.Path.FilePath);

        var found = reopened.FindNode(node.Path);

        Assert.NotNull(found);
        Assert.NotSame(node, found);
        Assert.Equal((kind, node.Text, node.Path), (LegacyKindOf(found), found.Text, found.Path));
    }

    [Theory]
    [MemberData(nameof(SavedKinds))]
    public void A_place_saved_before_paths_were_versioned_finds_a_node_of_each_kind_again(string kind)
    {
        var node = Sample(kind);
        var saved = LegacyPathOf(node);
        var reopened = new AssemblyList();
        reopened.Open(node.Path.FilePath);

        var found = reopened.FindNode(saved);

        Assert.True(saved.Names(node));
        Assert.NotNull(found);
        Assert.Equal((kind, node.Text, node.Path), (LegacyKindOf(found), found.Text, found.Path));
    }

    [Fact]
    public void A_path_to_a_file_that_failed_finds_nothing_in_a_list_that_does_not_list_it()
    {
        var failed = Sample("FailedAssembly");

        Assert.Null(new AssemblyList().FindNode(failed.Path));
    }

    /// <summary>A node builds before versioned paths saved as <paramref name="kind"/>, in a file left in
    /// place for another list to open.</summary>
    private BrowseNode Sample(string kind) => kind switch
    {
        "Package" or "PackageFolder" => FirstOf(Open(PackageTests.WritePackage(_temp)), kind),
        "Bundle" => Open(BundleTests.WriteBundle(_temp)).Root,
        "FailedAssembly" => Open(_temp.Write("broken.dll", "no assembly")).Root,
        "ForwardedType" => FirstOf(Open(Path.Combine(RuntimeAssemblies.Directories[0], "System.Runtime.dll")), kind),
        _ => FirstOf(Open(Trees.TestAssemblyPath), kind),
    };

    private static LoadedAssembly Open(string path) => new AssemblyList().Open(path);

    /// <summary>The place builds before versioned paths saved for <paramref name="node"/>: the name of each
    /// node's kind and its label, down from the root.</summary>
    private static NodePath LegacyPathOf(BrowseNode node)
    {
        var steps = new List<LegacyPathStep>();
        var root = node;
        for (; root.Parent != null; root = root.Parent)
            steps.Add(Assert.NotNull(LegacyPathStep.Read(LegacyKindOf(root), root.Text)));
        steps.Reverse();
        return NodePath.FromLegacy(root.Assembly.FilePath, steps);
    }

    /// <summary>The name of the kind builds before versioned paths saved <paramref name="node"/> as.</summary>
    private static string LegacyKindOf(BrowseNode node) => node.Subject switch
    {
        NodeSubject.Module => "Assembly",
        NodeSubject.FailedAssembly => "FailedAssembly",
        NodeSubject.Namespace => "Namespace",
        NodeSubject.TypeDefinition type => type.Definition.Shape.ToString(),
        NodeSubject.MemberDefinition member => member.Definition.Shape.ToString(),
        NodeSubject.References => "ReferencesFolder",
        NodeSubject.AssemblyReference => "AssemblyReference",
        NodeSubject.ReferencedTypes => "ReferencedTypes",
        NodeSubject.TypeReference => "TypeReference",
        NodeSubject.MemberReference member => member.Value.Shape switch
        {
            MemberShape.Field or MemberShape.Constant => "FieldReference",
            MemberShape.Property or MemberShape.Indexer => "PropertyReference",
            MemberShape.Event => "EventReference",
            MemberShape.Method or MemberShape.Constructor or MemberShape.Operator => "MethodReference",
            _ => throw new UnreachableException(),
        },
        NodeSubject.ModuleReference => "ModuleReference",
        NodeSubject.ImportedMethod => "ImportedMethod",
        NodeSubject.ForwardedType => "ForwardedType",
        NodeSubject.BaseTypes => "BaseTypes",
        NodeSubject.BaseType => "BaseType",
        NodeSubject.DerivedTypes => "DerivedTypes",
        NodeSubject.DerivedType => "DerivedType",
        NodeSubject.Resources => "ResourcesFolder",
        NodeSubject.ResourceContainer => "ResourcesFile",
        NodeSubject.ResourceEntry entry => entry.Value.IsImage ? "ResourceImage" : "Resource",
        NodeSubject.Metadata => "Metadata",
        NodeSubject.MetadataTables => "MetadataTables",
        NodeSubject.MetadataTable table => table.Value.Role switch
        {
            MetadataGridRole.Header => "MetadataHeader",
            MetadataGridRole.Directory => "MetadataDirectory",
            MetadataGridRole.Table => "MetadataTable",
            MetadataGridRole.Heap => "MetadataHeap",
            _ => throw new UnreachableException(),
        },
        NodeSubject.Package package => !package.Value.IsRoot ? "PackageFolder" : package.Value.Package.Package.IsBundle ? "Bundle" : "Package",
        NodeSubject.PackageFile file => file.Value.IsImage ? "ResourceImage" : "Resource",
        _ => throw new UnreachableException(),
    };

    /// <summary>The first node saved as <paramref name="kind"/> breadth first from the root of
    /// <paramref name="assembly"/>, not following a reference into the references of the assembly it
    /// resolves to.</summary>
    private static BrowseNode FirstOf(LoadedAssembly assembly, string kind)
    {
        var queue = new Queue<BrowseNode>([assembly.Root]);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (LegacyKindOf(node) == kind)
                return node;
            foreach (var child in node.Children)
            {
                if (node.Subject is NodeSubject.AssemblyReference && child.Subject is NodeSubject.AssemblyReference) continue;
                queue.Enqueue(child);
            }
        }
        throw new InvalidOperationException($"{assembly.Name} has no node saved as {kind}.");
    }
}
