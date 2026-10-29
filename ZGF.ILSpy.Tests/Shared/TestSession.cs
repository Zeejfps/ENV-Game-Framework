using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

/// <summary>The browse session the tests of this assembly read trees through, and that the app states
/// they make browse with. A session keeps what it knows of an assembly with the assembly, so tests of
/// lists of their own never share a node.</summary>
internal static class TestSession
{
    public static BrowseSession Shared { get; } = new();
}

/// <summary>The tree of <see cref="TestSession.Shared"/> read as members of what it holds: an assembly's
/// root, a node's children, and the nodes a list's paths, references and ids name.</summary>
internal static class SessionQueries
{
    extension(LoadedAssembly assembly)
    {
        public BrowseNode Root => TestSession.Shared.RootOf(assembly);

        public BrowseNode? FindNode(NodePath path) => TestSession.Shared.Find(assembly, path);
    }

    extension(BrowseNode node)
    {
        /// <summary>The children, built on this thread when nobody built them yet; none when they failed.</summary>
        public IReadOnlyList<BrowseNode> Children => TestSession.Shared.ChildrenOf(node);

        public bool HasChildren => TestSession.Shared.HasChildren(node);

        public ChildrenState ChildrenState => TestSession.Shared.PeekChildren(node);

        public void LoadChildren() => TestSession.Shared.LoadChildren(node);

        public IReadOnlyList<BrowseNode> InnerNamespaces => TestSession.Shared.InnerNamespaces(node);
    }

    extension(AssemblyList list)
    {
        public BrowseNode? FindNode(NodePath path) => NodeLookup.Find(list, TestSession.Shared, path);

        public BrowseNode? FindNode(CodeReference reference) => NodeLookup.Find(list, TestSession.Shared, reference);

        public BrowseNode? FindListedNode(NodePath path) => NodeLookup.FindListed(list, TestSession.Shared, path);

        public BrowseNode? FindNodeById(string idString, IEnumerable<LoadedModule> assemblies) =>
            NodeLookup.FindById(TestSession.Shared, idString, assemblies);

        public MetadataTarget? FindMetadataRow(CodeReference reference) => NodeLookup.FindMetadataRow(list, TestSession.Shared, reference);

        public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default) =>
            AssemblySearch.SearchAsync(TestSession.Shared, list.Assemblies, query, cancellationToken);

        public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, IEnumerable<LoadedModule> assemblies,
            CancellationToken cancellationToken = default) =>
            AssemblySearch.SearchAsync(TestSession.Shared, assemblies, query, cancellationToken);

        public IAsyncEnumerable<IReadOnlyList<SearchResult>> SearchAsync(string text, SearchKinds kinds = SearchKinds.TypesAndMembers,
            CancellationToken cancellationToken = default) =>
            AssemblySearch.SearchAsync(TestSession.Shared, list.Assemblies, SearchQuery.Parse(text, kinds), cancellationToken);
    }
}
