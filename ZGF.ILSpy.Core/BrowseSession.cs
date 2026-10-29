using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Core;

/// <summary>Where the children of a node stand: not asked for yet, being built, built, or failed to build.</summary>
public abstract record ChildrenState
{
    private ChildrenState()
    {
    }

    public sealed record Unloaded : ChildrenState;

    /// <summary>Being built; <see cref="Load"/> ends <see cref="Ready"/> or <see cref="Failed"/>.</summary>
    public sealed record Running(Task<ChildrenState> Load) : ChildrenState;

    public sealed record Ready(IReadOnlyList<BrowseNode> Children) : ChildrenState;

    public sealed record Failed(Exception Error) : ChildrenState;
}

/// <summary>
/// The browse tree of the assemblies it is asked about: the root of each, the children of each node,
/// built once and handed out whole, and the node of each type and member by its handle. A node keeps
/// its identity for as long as its assembly lives, and what the session holds of an assembly goes when
/// the assembly does. All members are thread-safe.
/// </summary>
public sealed class BrowseSession
{
    private const string NamespaceIdPrefix = "N:";

    private static readonly ChildrenState.Unloaded NotLoaded = new();

    private readonly ConditionalWeakTable<LoadedModule, AssemblyNode> _moduleRoots = new();
    private readonly ConditionalWeakTable<LoadedAssembly, BrowseNode> _otherRoots = new();
    private readonly ConditionalWeakTable<LoadedModule, ConcurrentDictionary<EntityHandle, BrowseNode>> _index = new();
    private readonly ConditionalWeakTable<BrowseNode, ChildSlot> _children = new();

    /// <summary>The root of <paramref name="assembly"/>'s tree. For an assembly read out of a package, it
    /// sits in the package's tree, under the folder holding its file.</summary>
    public BrowseNode RootOf(LoadedAssembly assembly)
    {
        if (assembly is LoadedModule module)
            return RootOf(module);
        return _otherRoots.GetValue(assembly, static a => CreateRoot(a));
    }

    private static BrowseNode CreateRoot(LoadedAssembly assembly) => assembly switch
    {
        FailedAssembly failed => new FailedAssemblyNode(failed),
        LoadedPackage package => new PackageNode(package),
        _ => throw new UnreachableException(),
    };

    internal AssemblyNode RootOf(LoadedModule module)
    {
        if (module.Place == null)
            return _moduleRoots.GetValue(module, static m => new AssemblyNode(m, null));
        return RootInPackage(module, module.Place);
    }

    /// <summary>The root of <paramref name="module"/> under the folder of its package's tree holding its
    /// file: the one that folder lists, else one made there the first time, then kept.</summary>
    private AssemblyNode RootInPackage(LoadedModule module, PackagePlace place)
    {
        if (_moduleRoots.TryGetValue(module, out var placed))
            return placed;
        var folder = FindFolderNode(RootOf(place.Container), place.File.Folder);
        LoadChildren(folder);
        return _moduleRoots.GetValue(module, m => new AssemblyNode(m, folder));
    }

    /// <summary>The node of the folder at <paramref name="folder"/>, a path from the package's root:
    /// the folder's own node, or the node it is merged into; the package's root for its root.</summary>
    private BrowseNode FindFolderNode(BrowseNode packageRoot, string folder)
    {
        var node = packageRoot;
        while (true)
        {
            var next = FindChildFolderHolding(node, folder);
            if (next == null)
                return node;
            if (next.Folder.FullName == folder)
                return next;
            node = next;
        }
    }

    private PackageFolderNode? FindChildFolderHolding(BrowseNode node, string folder)
    {
        foreach (var child in ChildrenOf(node))
        {
            if (child is not PackageFolderNode folderNode)
                continue;
            if (IsWithin(folder, folderNode.Folder.FullName))
                return folderNode;
        }
        return null;
    }

    private static bool IsWithin(string folder, string candidate)
    {
        if (folder == candidate)
            return true;
        return folder.StartsWith(candidate + "/", StringComparison.Ordinal);
    }

    /// <summary>Where <paramref name="node"/>'s children stand now; never blocks or builds them.</summary>
    public ChildrenState PeekChildren(BrowseNode node)
    {
        if (!_children.TryGetValue(node, out var slot))
            return NotLoaded;
        lock (slot.Gate)
            return slot.State;
    }

    /// <summary><see cref="PeekChildren"/>, with the children as the tree lists them when it nests
    /// namespaces: for an assembly, its children but the namespaces nested in others; for a namespace,
    /// the namespaces nested in it and its own children, sorted together by name.</summary>
    public ChildrenState PeekNestedChildren(BrowseNode node)
    {
        var state = PeekChildren(node);
        if (state is not ChildrenState.Ready ready)
            return state;
        return new ChildrenState.Ready(node.Nest(ready.Children));
    }

    /// <summary>The children of <paramref name="node"/>, built on the thread pool when nobody asked for
    /// them yet. Every caller waits for the same build; cancelling a wait leaves the build running for
    /// the others. Ends <see cref="ChildrenState.Ready"/> or <see cref="ChildrenState.Failed"/>.</summary>
    public Task<ChildrenState> LoadChildrenAsync(BrowseNode node, CancellationToken cancellationToken = default)
    {
        var slot = SlotOf(node);
        Task<ChildrenState> load;
        lock (slot.Gate)
        {
            switch (slot.State)
            {
                case ChildrenState.Ready or ChildrenState.Failed:
                    return Task.FromResult(slot.State);
                case ChildrenState.Running running:
                    load = running.Load;
                    break;
                case ChildrenState.Unloaded:
                    var building = new TaskCompletionSource<ChildrenState>(TaskCreationOptions.RunContinuationsAsynchronously);
                    slot.State = new ChildrenState.Running(building.Task);
                    load = building.Task;
                    _ = Task.Run(() => Finish(node, slot, building));
                    break;
                default:
                    throw new UnreachableException();
            }
        }
        return load.WaitAsync(cancellationToken);
    }

    /// <summary>The children of <paramref name="node"/>, built on this thread when nobody asked for them
    /// yet, else waited for while another builds them. Blocks: call it off the UI thread. Ends
    /// <see cref="ChildrenState.Ready"/> or <see cref="ChildrenState.Failed"/>.</summary>
    public ChildrenState LoadChildren(BrowseNode node)
    {
        var slot = SlotOf(node);
        TaskCompletionSource<ChildrenState>? building = null;
        Task<ChildrenState> load;
        lock (slot.Gate)
        {
            switch (slot.State)
            {
                case ChildrenState.Ready or ChildrenState.Failed:
                    return slot.State;
                case ChildrenState.Running running:
                    load = running.Load;
                    break;
                case ChildrenState.Unloaded:
                    building = new TaskCompletionSource<ChildrenState>(TaskCreationOptions.RunContinuationsAsynchronously);
                    slot.State = new ChildrenState.Running(building.Task);
                    load = building.Task;
                    break;
                default:
                    throw new UnreachableException();
            }
        }
        if (building != null)
            return Finish(node, slot, building);
        return load.GetAwaiter().GetResult();
    }

    /// <summary>The children of <paramref name="node"/>, as <see cref="LoadChildren"/> builds them; none
    /// when they failed to build. Blocks: call it off the UI thread.</summary>
    public IReadOnlyList<BrowseNode> ChildrenOf(BrowseNode node)
    {
        if (LoadChildren(node) is ChildrenState.Ready ready)
            return ready.Children;
        return [];
    }

    /// <summary>Whether <paramref name="node"/> has children, answered without building them where
    /// possible. Children that failed to build count, so that why can be shown.</summary>
    public bool HasChildren(BrowseNode node) => PeekChildren(node) switch
    {
        ChildrenState.Ready ready => ready.Children.Count > 0,
        ChildrenState.Failed => true,
        ChildrenState.Running or ChildrenState.Unloaded => node.MayHaveChildren,
        _ => throw new UnreachableException(),
    };

    /// <summary>The child of <paramref name="parent"/> standing where <paramref name="other"/> stands under
    /// its own parent, of its kind and text: its counterpart under a copy of the same file read again.
    /// Null when there is none, or the children are not built yet; never blocks.</summary>
    public BrowseNode? FindLoadedChildLike(BrowseNode parent, BrowseNode other)
    {
        if (PeekChildren(parent) is not ChildrenState.Ready ready)
            return null;
        foreach (var child in ready.Children)
        {
            if (child.Kind == other.Kind && child.Text == other.Text)
                return child;
        }
        return null;
    }

    private ChildSlot SlotOf(BrowseNode node) => _children.GetValue(node, static _ => new ChildSlot());

    /// <summary>Builds the children, records the nodes they index, then hands them out.</summary>
    private ChildrenState Finish(BrowseNode node, ChildSlot slot, TaskCompletionSource<ChildrenState> building)
    {
        var state = Build(node);
        lock (slot.Gate)
            slot.State = state;
        building.SetResult(state);
        return state;
    }

    private ChildrenState Build(BrowseNode node)
    {
        BrowseNode[] built;
        try
        {
            built = node.CreateChildren();
        }
        catch (Exception e)
        {
            return new ChildrenState.Failed(e);
        }
        foreach (var child in built)
            Register(child);
        return new ChildrenState.Ready(built);
    }

    private void Register(BrowseNode node)
    {
        if (node is SubjectNode subject && subject.DefinedHandle is EntityHandle handle)
            IndexOf(subject.Module).TryAdd(handle, subject);
        if (node is AssemblyNode root)
            _moduleRoots.TryAdd(root.Module, root);
    }

    private ConcurrentDictionary<EntityHandle, BrowseNode> IndexOf(LoadedModule module) =>
        _index.GetValue(module, static _ => new ConcurrentDictionary<EntityHandle, BrowseNode>());

    /// <summary>The node <paramref name="path"/> leads to from <paramref name="item"/>'s root, when it is
    /// the item of the list the path starts at; else, or when nothing is there, null. May build the
    /// children on the way: call it off the UI thread.</summary>
    public BrowseNode? Find(LoadedAssembly item, NodePath path)
    {
        if (!path.FilePath.Equals(item.FilePath))
            return null;
        var node = RootOf(item);
        foreach (var (kind, text) in path.Segments)
        {
            BrowseNode? next = null;
            foreach (var child in ChildrenOf(node))
            {
                if (child.Kind == kind && child.Text == text)
                {
                    next = child;
                    break;
                }
            }
            if (next is null)
                return null;
            node = next;
        }
        return node;
    }

    /// <summary>The node of the type or member <paramref name="handle"/> defines in <paramref name="module"/>;
    /// null for one the tree does not list.</summary>
    internal BrowseNode? Find(LoadedModule module, EntityHandle handle)
    {
        if (handle.IsNil)
            return null;
        var index = IndexOf(module);
        if (index.TryGetValue(handle, out var node))
            return node;
        var parent = FindParent(module, handle);
        if (parent is null)
            return null;
        LoadChildren(parent);
        return index.GetValueOrDefault(handle);
    }

    /// <summary>The node listing the node of <paramref name="handle"/>: a top-level type's namespace, else
    /// the node of the entity that holds it.</summary>
    private BrowseNode? FindParent(LoadedModule module, EntityHandle handle)
    {
        if (handle.Kind != HandleKind.TypeDefinition)
            return Find(module, module.OwnerOf(handle));
        var metadata = module.File.Metadata;
        var type = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
        if (!type.GetDeclaringType().IsNil)
            return Find(module, module.OwnerOf(handle));
        return FindNamespace(module, metadata.GetString(type.Namespace));
    }

    /// <summary>The node of <paramref name="handle"/>, or of the nearest entity holding it that has one,
    /// as a compiler-generated member is held by the type it is generated in.</summary>
    internal BrowseNode? FindOrAncestor(LoadedModule module, EntityHandle handle)
    {
        for (; !handle.IsNil; handle = module.OwnerOf(handle))
        {
            var node = Find(module, handle);
            if (node != null)
                return node;
        }
        return null;
    }

    private NamespaceNode? FindNamespace(LoadedModule module, string name)
    {
        var root = RootOf(module);
        LoadChildren(root);
        return root.NamespaceNamed(name);
    }

    /// <summary>The node of the namespace, type or member <paramref name="idString"/> names in
    /// <paramref name="module"/>, written as XML documentation ids are (<c>N:</c>, <c>T:</c>, <c>M:</c>,
    /// <c>P:</c>, <c>F:</c> or <c>E:</c> and the full name); null when it defines none such, or the id
    /// cannot be read.</summary>
    internal BrowseNode? FindById(LoadedModule module, string idString)
    {
        if (idString.StartsWith(NamespaceIdPrefix, StringComparison.Ordinal))
            return FindNamespace(module, idString.Substring(NamespaceIdPrefix.Length));
        var entity = module.FindDefinedEntity(idString);
        if (entity == null)
            return null;
        return FindOrAncestor(module, entity.MetadataToken);
    }

    /// <summary>The row <paramref name="handle"/> names in <paramref name="module"/>'s own metadata
    /// tables, or null when it names no table row or the tables could not be read.</summary>
    internal MetadataTarget? FindMetadataRow(LoadedModule module, EntityHandle handle)
    {
        if (!MetadataTokens.TryGetTableIndex(handle.Kind, out var index))
            return null;
        var metadata = ChildrenOf(RootOf(module))
            .OfType<MetadataFolderNode>()
            .FirstOrDefault(f => ReferenceEquals(f.File, module.File));
        if (metadata == null)
            return null;
        var table = FindTable(metadata, index);
        if (table == null)
            return null;
        var row = MetadataTokens.GetRowNumber(handle) - 1;
        return new MetadataTarget(table, row);
    }

    /// <summary>The table node and row that <paramref name="address"/>, a link of the grid
    /// <paramref name="grid"/> shows, leads to, among the tables of the metadata the grid lists; null
    /// when <paramref name="grid"/> lists no metadata, or that metadata lists no such table.</summary>
    public MetadataTarget? FindMetadataRow(BrowseNode grid, MetadataAddress address)
    {
        var folder = FolderOf(grid);
        if (folder == null)
            return null;
        var table = FindTable(folder, address.Table);
        if (table == null)
            return null;
        return new MetadataTarget(table, address.Row);
    }

    /// <summary>The "Metadata" folder <paramref name="node"/> belongs to.</summary>
    private static MetadataFolderNode? FolderOf(BrowseNode node)
    {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is MetadataFolderNode folder)
                return folder;
        }
        return null;
    }

    /// <summary>The node listing the rows of <paramref name="table"/> in <paramref name="folder"/>, or null
    /// when it lists none.</summary>
    private MetadataGridNode? FindTable(MetadataFolderNode folder, TableIndex table)
    {
        var tables = ChildrenOf(folder).OfType<MetadataTablesNode>().FirstOrDefault();
        if (tables == null)
            return null;
        return ChildrenOf(tables).OfType<MetadataGridNode>().FirstOrDefault(n => n.Table == table);
    }

    private sealed class ChildSlot
    {
        public readonly Lock Gate = new();
        public ChildrenState State = NotLoaded;
    }
}
