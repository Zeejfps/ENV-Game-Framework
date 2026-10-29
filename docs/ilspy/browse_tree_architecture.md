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
- **Paths** become a sum of segments that name what a node is, not how it is shown; see [Paths](#paths).

### Paths

*Reviewed against the code before work started; the review's findings are folded in below.*

A path today is the listed file and a `(NodeKind, Text)` per step (`NodePath.cs`). That loses the place when a class becomes a record, a field a constant or a property an indexer, since the kind is an icon. Using labels as keys is wrong too:
- a type's label holds its generic parameters' names (`EntityText.cs:76`);
- a member's label leaves its parameter types unqualified, so `M(A.Widget)` and `M(B.Widget)` collide (`EntityText.cs:9`);
- a metadata table's label holds its row count, `02 TypeDef (22)`;
- used types from two namespaces, and methods imported by two types, can share one label under one parent;
- a package folder that holds one folder is merged with it into one node, so the steps to a file change with the package's contents.

**Segments.** A step names the node by identity, kept apart from the label the row shows. `PathSegment` is a closed sum, worked out from the subject by one total function:

| Case | Key | For |
|---|---|---|
| `Namespace(name)` | full name, empty for the global namespace | namespaces |
| `Definition(id)` | XML documentation id: `T:Ns.Calculator`1`, `M:Ns.Calculator`1.Add(`0)`, `F:`, `P:`, `E:` | types, nested types, members, accessors |
| `Folder(role)` | References, Resources, BaseTypes, DerivedTypes, ReferencedTypes, MetadataTables | the fixed folders |
| `Metadata(which)` | the module's own, or its debug metadata | metadata folders, at the root or under the debug directory |
| `MetadataPart(part)` | `Header(Dos \| Coff \| Optional)`, `DataDirectories`, `DebugDirectory`, `Table(TableIndex)`, `Heap(name)`, `DebugEntry(index, type)` | headers, tables, heaps and debug entries, empty tables included |
| `AssemblyReference(name)` | the simple name | references |
| `ModuleReference(name)` | the module's name | module references |
| `ImportedMethod(id)` | the documentation id of the method it calls | imports |
| `UsedType(fullName)` | namespace, nesting and arity: `System.Collections.Generic.List`1` | types used from a reference |
| `UsedMember(id)` | the documentation id of the member as its type declares it | members used of them |
| `ForwardedType(fullName)` | as for used types | forwarded types |
| `BaseType(name)` | the type's reflection name, type arguments included | base types |
| `DerivedType(id)` | the documentation id of the deriving type | derived types |
| `Resource(name)` | the manifest resource's name, or the entry's path in its container | resources, containers and their entries |
| `PackageFolder(path)` / `PackageFile(path)` / `EmbeddedModule(path)` | the entry's full path in the package | package contents |

- **Documentation ids** are what `IdStringProvider` writes and `BrowseSession.FindById` already reads. They tell an event from its backing field (`E:` and `F:`), qualify parameter types, and stay the same when a class becomes a record or a field a constant.
- **A package path is one step, not one per folder.** A path into a package is the package file, then one `PackageFolder`, `PackageFile` or `EmbeddedModule` step keyed by the full entry path, found with `BrowseSession.FindFolderNode` whatever the folders were merged into. Under an `EmbeddedModule` step the module's own steps follow.
- **A step that matches more than one child fails**; it never picks the first.
- **Tokens are no identity.** Within one loaded module a `Definition` step can be found through the session's handle index (`FindById`) instead of walking; a saved path never holds a token.

**Saved places.** A saved tab or bookmark holds a versioned `Path` beside the old `Place`, and is read into `Legacy | Semantic`:
- **`Path` present and well formed, of a known version:** `Semantic`.
- **`Path` present but malformed, or of an unknown version:** the place is reported and dropped. It is never read as the root, as an empty path would be.
- **No `Path`, and a `Place` that reads:** `Legacy`. It is resolved by label and the role its old kind implies, not by the `NodeKind` enum, so legacy places still resolve once `NodeKind` is gone in step 7. A resolved legacy place is written as `Semantic` at the next save.
- **Neither:** no place, as today.

**Writing.** Write `Path`, and until step 7 also `Place`, generated from the resolved node, so builds from before this change still load the tabs and bookmarks. They drop a tab without a `Place` (`SettingsFile.cs:177`). A bookmark whose node was not found keeps its `Place` and `Path` exactly as read. Step 7 decides when `Place` stops being written.

**Lookups that use paths:**
- `BrowseSession.Find(LoadedAssembly, NodePath)` matches children by segment.
- `FindLoadedChildLike`, which finds a node's counterpart in a reloaded assembly and is used to keep expansion and selection, matches by segment too.
- `NavigationHistory` keeps the `NodeIcon` it showed rather than reading an icon off the path's kind.
- `ILSpyTree.Locate` finds ILSpy's bookmarks by ILSpy's own node names, which are untouched. Its fallback to the bookmark's token resolves a token without checking it (`ILSpyTree.FindEntity`), so after a rebuild it can land on another entity. It now takes the entity only when its full name is the bookmark's recorded member name.

**Where it plugs in:**
- **`NodePath` and `NodePathSegment`** (`ZGF.ILSpy.Core/NodePath.cs`): the segments change. `NodePath.Of(node)` builds them from `node.Subject`.
- **`BrowseSession.Find` and `FindLoadedChildLike`** (`ZGF.ILSpy.Core/BrowseSession.cs`): match by segment.
- **The settings file** (`ZGF.ILSpy/SettingsFile.cs`): `NodePathSegmentJson` and `SettingsJson.SegmentsOf` read `Place`, in `NodeDocumentJson` and `BookmarkJson`. The new versioned `Path` reader belongs here, at the boundary.
- **Records:** `NodeDocumentRecord.Place` (`DocumentRecords.cs`), `BookmarkRecord.Place` (`BookmarkFile.cs`), and their writers `SavedDocuments.cs:157` and `BookmarkList.cs:62`.
- **History icons:** `HistoryEntry.Node` (`NavigationHistory.cs`) and its icon in `ToolbarView.cs:192`, which is the last use of `NodeIcons.Of(NodeKind)`.
- **Tests:**
  - `SavedPathTests` builds today's paths from live nodes, so it is no fixture of the old format.
  - `SavedPlacesTests` loads the old format from literal JSON, for ten kinds of place.
  - `NodePathTests`, `ILSpyTreeTests` and `BookmarkTests` cover the rest. `BookmarkTests` has a stale tree path falling back to the token.

**Tests to add:**
- old-format JSON frozen as fixtures for every kind of place, tab and bookmark alike;
- collisions: overloads differing only in a parameter's namespace, same-named used types from two namespaces, same-named imports from two types, an event and its backing field, two debug entries of one type;
- rebuild changes: a table's row count, a renamed generic parameter, a class turned record, a field turned constant;
- a package whose folder merging changes;
- a malformed `Path`, an unknown version, and both formats present (`Path` wins);
- expansion and selection kept across a reload;
- `Locate`'s token fallback refusing an entity of another name.

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
4. **Done.** **Replace subclasses with subjects**, one family at a time, each payload validated as it is built: entities, then references, resources, metadata and packages.
5. **Done.** **Move the consumers** (UI, analyzers, search, comparison) onto subjects, and derive icons at the UI edge. The UI names `NodeKind` only for saved places, through `NodeIcons.Of(NodeKind)` and the settings file, which step 6 takes up.
6. **Done.** **Version paths**, as [Paths](#paths) sets out: the segments, the versioned reader with old places resolved by label and role, writing both formats until step 7, lookups by segment, `ILSpyTree.Locate`'s checked token fallback, and the tests listed there.
7. **Delete `NodeKind` from Core** and the compatibility members left over: `BrowseNode.Kind`, which only `SavedPlaces.LegacyPlaceOf` reads to write `Place`; `NodeIcons.Of(NodeKind)`, which only `PresentationTests` calls; and writing `Place`. Add the dependency test to CI.

## Risks

- **Node identity.** Search, navigation and the tree rely on the same entity being the same node object; the session's index must keep that across concurrent loads.
- **When things resolve.** Validating payloads when nodes are built can move work, and failures, earlier than they happen today.
- **Children handed out half-built** if the index and the child lists are not published together.
- **Saved state from older versions** failing to find its node.
