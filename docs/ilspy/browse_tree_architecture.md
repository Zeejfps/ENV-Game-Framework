# Browse tree architecture

*2026-10-29. Replaces the "Assemblies and nodes" item and the `BrowseNode` follow-up of [ilspy_coding_rules_audit.md](../ilspy_coding_rules_audit.md), and the Core cycles under its Rule 2. Measured against [coding_rules.md](../coding_rules.md).*

## The problem

`BrowseNode` (`ZGF.ILSpy.Core/BrowseNode.cs`) is the base of about 30 node classes, and says what a node is three ways at once: by its subclass, by a `NodeKind` passed to its constructor, and by which of its optional fields are set.

- **Optional fields each set by a few subclasses.** `Reference` and `Token` by entity nodes; `Target` by base type, derived type, assembly reference, forwarded type, type and member reference and imported method nodes; `ForwardedTo`, `Namespace`, `DeclaringTypeName`, `EnclosingNamespace` and `IsEnclosingOnly` by one subclass each. Readers test `Kind`, then read the field that kind happens to set (`NodeText.Secondary`), or force-unwrap it (`TreePaneView.cs:209`).
- **`NodeKind` is set apart from the subclass** and nothing ties the two. It also does four jobs: icons, finding a node again by `(Kind, Text)` (`NodePath.cs:63`), branching in search (`AssemblySearch.cs:275`), and sorting members in a comparison (`ApiEntries.cs`).
- **Values that can be missing are passed on as if they could not.** `CodeReference.To` returns null when an entity has no metadata file or token (`CodeReference.cs:58`); entity, imported method and derived type nodes call it.
- **The base type-tests its own subclasses**: `MetadataGridNode`, `FailedAssemblyNode` and `IResourceData` in `DependsOnLanguage` and `LoadContent`, `IPackageFolderNode` in `FolderInPackage`.
- **Inheritance two deep**: `BrowseNode → EntityNode → TypeNode, MemberNode` and `BrowseNode → MetadataGridNode → ResourcesFileNode`.
- **Cycles in Core.** `BrowseNode.Decompile` calls `NodeDecompiler`, which switches over node subclasses and walks their children. `LoadedModule` holds the node index and the tree's root (`LoadedAssembly.cs:111-112`). `MetadataGrid` holds the node that owns it (`MetadataGrid.cs:63`) to name itself, find sibling tables and resolve links.

Moving each field onto the subclass that has it and turning the type tests into overrides fixes the first two points but leaves the cycles and the inheritance in place, and turns the nullable `CodeReference` into a force-unwrap. The design below goes further.

## The design

### A node is a place in the tree and a subject

```csharp
public sealed class BrowseNode
{
    public NodeSubject Subject { get; }
    public BrowseNode? Parent { get; }
    public NodePath Path { get; }
}
```

The node has no behavior: no decompiling, no content, no children of its own. What it stands for is its subject:

```csharp
public abstract class NodeSubject
{
    private NodeSubject() { }

    public sealed class Module(LoadedModule value) : NodeSubject;
    public sealed class Namespace(NamespaceInfo value) : NodeSubject;
    public sealed class TypeDefinition(DefinedType definition) : NodeSubject;
    public sealed class MemberDefinition(DefinedMember definition) : NodeSubject;
    public sealed class ForwardedType(ForwardedTypeInfo value) : NodeSubject;
    public sealed class AssemblyReference(AssemblyReferenceInfo value) : NodeSubject;
    public sealed class ImportedMethod(ImportedMethodInfo value) : NodeSubject;
    public sealed class BaseType(BaseTypeInfo value) : NodeSubject;
    public sealed class ResourceContainer(ResourceContainerInfo value) : NodeSubject;
    public sealed class ResourceEntry(ResourceEntryInfo value) : NodeSubject;
    public sealed class MetadataTable(MetadataTableInfo value) : NodeSubject;
    public sealed class Package(PackageFolderInfo value) : NodeSubject;
    public sealed class FailedAssembly(LoadedAssembly value) : NodeSubject;
    // and the folders: References, Resources, Base Types, Derived Types, Referenced types, Metadata
}
```

- **Cases are roles, not icons.** Where a case covers variants, such as a type that is a class, struct or enum, or a metadata header, directory, table or heap, that is a smaller closed sum or a fact inside the case, not another case.
- **Payloads are validated where they are built.** `DefinedType` holds the symbol, its handle and the address to navigate to, built together by a function that fails when there is no metadata file or token, so none of them is ever null.
- **A base type keeps every outcome.** Resolved and navigable; resolved with no address; or unresolved. Each keeps the `IType` it shows, with its type arguments.
- **A resource container is one case.** A `.resources` file today is bytes, children and a grid at once (`ResourceNodes.cs:154`); its grid and export are derived from its payload, not from separate flags or interfaces.
- **No separate settable `Content` or `Capabilities` fields** beside the subject. What a node can do is derived from what it is.

C# switches over a class hierarchy are not checked for exhaustiveness. Every switch over `NodeSubject` names each case and throws `UnreachableException` for the rest, and each case has a test.

### What a node does is derived from its subject

```csharp
public abstract record Activation
{
    private Activation() { }

    public sealed record OpenSelf : Activation;
    public sealed record Navigate(NavigationAddress Address) : Activation;
}
```

`ActivationOf(subject)` is one total function. `NavigationAddress` holds only definitions that can be navigated to, not the locals and labels a `CodeReference` can also name.

### The browse session owns the tree

```csharp
public sealed class BrowseSession
{
    public ChildrenState PeekChildren(BrowseNode node);
    public Task<ChildrenState> LoadChildrenAsync(BrowseNode node, CancellationToken cancellationToken);
    public BrowseNode? Find(NodePath path);
    public BrowseNode? Find(NavigationAddress address);
}

public abstract record ChildrenState
{
    public sealed record Unloaded : ChildrenState;
    public sealed record Running(Task<ChildrenState> Load) : ChildrenState;
    public sealed record Ready(IReadOnlyList<BrowseNode> Children) : ChildrenState;
    public sealed record Failed(Exception Error) : ChildrenState;
}
```

- **The session owns the roots, the index from handles to nodes, and the per-node child state.** These move out of `LoadedModule` and `BrowseNode`.
- **Children are loaded once and handed out whole.** Cancelling one waiter does not cancel the load others wait on. A node keeps its identity for as long as its module is loaded. The "Loading…" and error rows are drawn from `ChildrenState`; they are not nodes.
- **The session is its own object, passed to what needs it.** It does not hang off `AssemblyList`, which is already the hub everything routes through.

### Content and decompiling take requests, not nodes

```csharp
public sealed class NodeContentLoader
{
    public Task<NodeContent> LoadAsync(BrowseNode node, ContentOptions options, CancellationToken cancellationToken);
}

public abstract record DocumentRequest
{
    public sealed record ModulePreview(LoadedModule Module) : DocumentRequest;
    public sealed record Type(DefinedType Definition) : DocumentRequest;
    public sealed record Member(DefinedMember Definition) : DocumentRequest;
    public sealed record Summary(SummaryInfo Summary) : DocumentRequest;
    public sealed record Resource(ResourceEntryInfo Resource) : DocumentRequest;
    public sealed record Failure(LoadedAssembly Assembly) : DocumentRequest;
}

public static class NodeDecompiler
{
    public static DecompiledDocument Decompile(DocumentRequest request, ContentOptions options, CancellationToken cancellationToken);
}
```

- **The loader turns a subject into a grid, a picture or a document request, in one function.** `DocumentRequest` overlaps `NodeSubject`; that is acceptable only while exactly one function maps one to the other.
- **The decompiler never sees a tree node.** Summaries that list children, such as a folder's, are built by the loader from the session's children and passed in as data.
- **Exporting a whole module or namespace stays a separate request from previewing it**, as it is today (`NodeDecompiler.cs:119`).
- **`MetadataGrid` no longer holds its node.** It returns metadata addresses, which the session resolves.

### `NodeKind` leaves Core

- **Icons:** the UI derives an icon key from the subject and the facts in it.
- **Search and comparison** switch on the subject or the symbol, not on a kind.
- **Paths** become a sum of semantic segments: entity signature, namespace name, resource path, metadata table, package path, and structural folders. Tokens speed up lookup within one loaded generation of a module; they are not identity across rebuilds.
- **Old paths still load.** Saved tabs, bookmarks, navigation history and expanded nodes store `(Kind, Text)` paths today (`SavedDocuments`, `DocumentRecords`, `BookmarkFile`, `NavigationHistory`, `ILSpyTree.Locate`). They are read by a versioned reader that maps them to the new segments, each kind of record with a test that an old path still finds its node. Without that, users lose their open tabs and bookmarks without a word.

### The dependency graph

```text
UI → BrowseSession, NodeContentLoader
NodeContentLoader → BrowseSession, NodeDecompiler, MetadataGrid, ResourceImage
BrowseSession → NodeSubject, LoadedModule (reading only)
NodeDecompiler, MetadataGrid → domain data (LoadedModule, type system)
```

No arrow points back up. Check this in CI with an architecture test over the assembly, so a new back edge fails the build. No such test exists yet; it is part of the work.

## How consumers read

```csharp
// NodeText.Secondary
node.Subject switch
{
    NodeSubject.ForwardedType f => "→ " + f.Value.Destination,
    NodeSubject.TypeReference r => EmptyToNull(r.Value.Namespace),
    NodeSubject.ImportedMethod m => m.Value.DeclaringTypeName,
    NodeSubject.Module m => EmptyToNull(Identity(m.Value)),
    NodeSubject.Namespace or NodeSubject.TypeDefinition or ... => null,
    _ => throw new UnreachableException(),
};

// TreePaneView context menu
if (node.Subject is NodeSubject.TypeDefinition type)
    DiagramItems(ui, appState, type.Definition.Address);
if (ActivationOf(node.Subject) is Activation.Navigate jump && ui.MenuItem(t.TreeGoToTarget))
    Navigate(appState, jump.Address, inNewTab: false);

// BrowseTree hiding
if (node.Subject is NodeSubject.MetadataTable table && table.Value.RowCount == 0)
    return !_showEmptyTables;
if (node.Subject is NodeSubject.Namespace ns)
    return IsNamespaceHidden(ns.Value);
return IsApiHidden(VisibilityOf(node.Subject));
```

Nesting namespaces must keep today's behavior: a namespace is hidden only when it and everything nested in it is, and its enclosing namespace is known apart from the parent it is drawn under (`BrowseTree.cs:225`).

## Order of work

1. **Done.** **Pin down today's behavior in tests**: node identity across threads; nested namespaces; assemblies read out of packages; unresolved references and base types; resource fallbacks and BAML export; preview versus whole-module export; every kind of saved path finding its node.
2. **Done.** **Take content out of the node.** Add `NodeContentLoader` and `DocumentRequest`, make `NodeDecompiler` take requests, and drop `MetadataGrid`'s owner. This breaks the `BrowseNode → NodeDecompiler → subclasses` cycle.
3. **Done.** **Add `BrowseSession`.** Move the roots, the node index and child loading out of `LoadedModule` and `BrowseNode`.
4. **In progress.** **Replace subclasses with subjects**, one family at a time, each payload validated as it is built: entities, then references, resources, metadata and packages.
   - **Done:** entities, references, resources, metadata.
   - **Left:** packages, and the module, namespace and failed-file roots.
5. **Move the consumers** (UI, analyzers, search, comparison) onto subjects, and derive icons at the UI edge.
6. **Version paths.** Add the new segments and the reader for old ones, and check bookmarks, saved tabs, expansion and `ILSpyTree.Locate`'s path-then-token lookup (`ILSpyTree.cs:64`).
7. **Delete `NodeKind` from Core** and the compatibility members left over, and add the dependency test to CI.

## Risks

- **Node identity.** Search, navigation and the tree rely on the same entity being the same node object; the session's index must keep that across concurrent loads.
- **When things resolve.** Validating payloads when nodes are built can move work, and failures, earlier than they happen today.
- **Children handed out half-built** if the index and the child lists are not published together.
- **Saved state from older versions** failing to find its node.
