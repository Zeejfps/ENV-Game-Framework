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

        /// <summary>What sort of type the node defines; fails the test when it defines none.</summary>
        public TypeShape TypeShape => Assert.IsType<NodeSubject.TypeDefinition>(node.Subject).Definition.Shape;

        /// <summary>What sort of member the node defines; fails the test when it defines none.</summary>
        public MemberShape MemberShape => Assert.IsType<NodeSubject.MemberDefinition>(node.Subject).Definition.Shape;

        /// <summary>What the part of metadata the node stands for lists; fails the test when it stands for none.</summary>
        public MetadataGridRole MetadataRole => Assert.IsType<NodeSubject.MetadataTable>(node.Subject).Value.Role;

        public bool Is(TypeShape shape) => node.Subject is NodeSubject.TypeDefinition type && type.Definition.Shape == shape;

        public bool Is(MemberShape shape) => node.Subject is NodeSubject.MemberDefinition member && member.Definition.Shape == shape;

        public bool Is(MetadataGridRole role) => node.Subject is NodeSubject.MetadataTable table && table.Value.Role == role;

        /// <summary>Whether the node is a resource or a package's file shown as a picture.</summary>
        public bool IsImage => node.Subject switch
        {
            NodeSubject.ResourceEntry entry => entry.Value.IsImage,
            NodeSubject.PackageFile file => file.Value.IsImage,
            _ => false,
        };

        /// <summary>A reference to the type or member the node defines; fails the test when it defines none.</summary>
        public CodeReference Definition => Assert.IsType<CodeReference>(NodeDefinitions.ReferenceOf(node.Subject));

        public NodeAccessibility Accessibility => NodeVisibility.Of(node.Subject).Accessibility;

        public bool IsPublicApi => NodeVisibility.Of(node.Subject).IsPublicApi;

        public bool IsCompilerGenerated => NodeVisibility.Of(node.Subject).IsCompilerGenerated;

        /// <summary>The metadata token of what the node defines; null when it defines nothing.</summary>
        public int? Token => NodeDefinitions.TokenOf(node.Subject);

        /// <summary>Where activating the node goes; fails the test when it opens itself.</summary>
        public NavigationAddress Destination => Assert.IsType<Activation.Navigate>(Activation.Of(node.Subject)).Address;
    }

    extension(AssemblyList list)
    {
        public BrowseNode? FindNode(NodePath path) => NodeLookup.Find(list, TestSession.Shared, path);

        public BrowseNode? FindNode(CodeReference reference) => NodeLookup.Find(list, TestSession.Shared, reference);

        public BrowseNode? FindNode(NavigationAddress address) => NodeLookup.Find(list, TestSession.Shared, address);

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
