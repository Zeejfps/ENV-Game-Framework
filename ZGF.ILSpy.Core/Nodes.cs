using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;
using AssemblyReference = ICSharpCode.Decompiler.Metadata.AssemblyReference;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core;

/// <summary>A node of a module that was read: one under its <see cref="AssemblyNode"/>, showing what
/// <see cref="Module"/> holds.</summary>
internal interface IModuleNode
{
    LoadedModule Module { get; }
}

internal sealed class AssemblyNode : BrowseNode, IModuleNode
{
    private Dictionary<string, NamespaceNode>? _namespaces;
    private BrowseNode[]? _nested;

    public LoadedModule Module { get; }

    public AssemblyNode(LoadedModule assembly, BrowseNode? parent) : base(assembly, parent, NodeKind.Assembly, LabelOf(assembly))
    {
        Module = assembly;
    }

    public override string FullName => Module.FullName;

    internal override bool MayHaveChildren => true;

    /// <summary>The namespace named <paramref name="name"/>, once the children are built; else null.</summary>
    internal NamespaceNode? NamespaceNamed(string name) => Volatile.Read(ref _namespaces)?.GetValueOrDefault(name);

    internal override BrowseNode[] CreateChildren()
    {
        var file = Module.File;
        var result = new List<BrowseNode> { SubjectNode.MetadataFolder(Module, this, file, "Metadata") };
        var symbols = Module.DebugSymbols;
        if (symbols != null)
            result.Add(SubjectNode.MetadataFolder(Module, this, symbols.File, symbols.Title));
        result.Add(SubjectNode.Of(Module, this, new NodeSubject.References(Module)));
        if (!file.Resources.IsEmpty)
            result.Add(SubjectNode.Of(Module, this, new NodeSubject.Resources(Module)));

        var byNamespace = new Dictionary<string, List<ITypeDefinition>>(StringComparer.Ordinal);
        foreach (var type in Module.MainModule.TopLevelTypeDefinitions)
        {
            if (Members.IsMissing(type)) continue;
            if (!byNamespace.TryGetValue(type.Namespace, out var types))
                byNamespace.Add(type.Namespace, types = new List<ITypeDefinition>());
            types.Add(type);
        }
        var forwardedByNamespace = ForwardedTypes.ListByNamespace(file.Metadata);

        var namespaces = new Dictionary<string, NamespaceNode>(StringComparer.Ordinal);
        var names = byNamespace.Keys
            .Union(forwardedByNamespace.Keys)
            .SelectMany(NamespaceNames.AndEnclosing)
            .Distinct()
            .Order(NaturalStringComparer.Instance);
        foreach (var name in names)
        {
            var types = byNamespace.GetValueOrDefault(name, []);
            var forwarded = forwardedByNamespace.GetValueOrDefault(name, []);
            var enclosing = FindEnclosing(namespaces, name);
            var node = new NamespaceNode(Module, this, name, enclosing, types, forwarded);
            if (enclosing != null)
                enclosing.Enclose(node);
            namespaces.Add(name, node);
            result.Add(node);
        }
        Volatile.Write(ref _namespaces, namespaces);
        _nested = LeaveOutNested(result);
        return result.ToArray();
    }

    /// <summary>The namespace <paramref name="name"/> extends by a segment, made already, as names
    /// sort after the names they extend; null for an outermost name.</summary>
    private static NamespaceNode? FindEnclosing(Dictionary<string, NamespaceNode> namespaces, string name)
    {
        var enclosing = NamespaceNames.EnclosingName(name);
        if (enclosing == null)
            return null;
        return namespaces[enclosing];
    }

    /// <summary>The children but the namespaces nested in others.</summary>
    private static BrowseNode[] LeaveOutNested(List<BrowseNode> children)
    {
        return children
            .Where(static c => c.EnclosingNamespace == null)
            .ToArray();
    }

    internal override IReadOnlyList<BrowseNode> Nest(IReadOnlyList<BrowseNode> children) => _nested ?? children;

    /// <summary>The assembly's name, version and the framework it was built for. The framework picked
    /// to resolve its references against is left out, so that the label, by which paths find the node,
    /// stays the same when another is picked.</summary>
    private static string LabelOf(LoadedModule assembly)
    {
        var details = new[] { assembly.Version?.ToString(), BuiltForName(assembly) }.OfType<string>().ToArray();
        return details.Length == 0 ? assembly.Name : $"{assembly.Name} ({string.Join(", ", details)})";
    }

    private static string? BuiltForName(LoadedModule assembly)
    {
        if (assembly.DetectedTargetFramework == null)
            return null;
        return FrameworkIds.ReadableName(assembly.DetectedTargetFramework);
    }
}

/// <summary>The root of a listed file that could not be read as a .NET assembly: labelled with the
/// file's name, without children, and showing why it failed.</summary>
internal sealed class FailedAssemblyNode(FailedAssembly assembly)
    : BrowseNode(assembly, null, NodeKind.FailedAssembly, assembly.FilePath.FileName)
{
    public FailedAssembly Failed { get; } = assembly;

    public override string FullName => Assembly.FilePath.Text;
}

/// <summary>A namespace of an assembly: the types it defines there and those it forwards from
/// there to other assemblies, sorted together by name, then by how many type parameters they have.
/// When the tree nests namespaces, the namespaces nested in it sit among them, each where ILSpy sorts
/// the full names of the types in it.</summary>
internal sealed class NamespaceNode : BrowseNode, IModuleNode
{
    private const string NestedSortMark = ".";

    private readonly List<ITypeDefinition> _types;
    private readonly List<ForwardedType> _forwarded;
    private readonly List<NamespaceNode> _inner = new();
    private BrowseNode[]? _nested;

    public LoadedModule Module { get; }

    public NamespaceNode(LoadedModule assembly, BrowseNode parent, string name, NamespaceNode? enclosing, List<ITypeDefinition> types,
        List<ForwardedType> forwarded)
        : base(assembly, parent, NodeKind.Namespace, name.Length == 0 ? "-" : name)
    {
        Module = assembly;
        Name = name;
        _types = types;
        _forwarded = forwarded;
        EnclosingNamespace = enclosing;
        IsEnclosingOnly = types.Count == 0 && forwarded.Count == 0;
        IsPublicApi = forwarded.Count > 0 || types.Exists(EntityText.IsPublicApi);
        IsCompilerGenerated = forwarded.Count == 0 && types.TrueForAll(Members.IsCompilerGenerated);
    }

    public string Name { get; }

    /// <summary>The types the assembly defines in this namespace.</summary>
    public IEnumerable<ITypeDefinition> Types => _types;

    public override string FullName => Name;

    public override IReadOnlyList<BrowseNode> InnerNamespaces => _inner;

    /// <summary>Nests <paramref name="inner"/>, whose name extends this one's by a segment, in this namespace.</summary>
    internal void Enclose(NamespaceNode inner)
    {
        _inner.Add(inner);
    }

    internal override bool MayHaveChildren => !IsEnclosingOnly;

    internal override BrowseNode[] CreateChildren()
    {
        var sorted = SortChildren();
        _nested = Interleave(sorted);
        return sorted
            .Select(static c => c.Node)
            .ToArray();
    }

    private List<SortedChild> SortChildren()
    {
        var children = new List<SortedChild>(_types.Count + _forwarded.Count);
        foreach (var type in _types)
        {
            var node = SubjectNode.ForType(Module, this, type);
            if (node != null)
                children.Add(new SortedChild(type.Name, type.TypeParameterCount, node));
        }
        foreach (var forwarded in _forwarded)
            children.Add(new SortedChild(forwarded.SortName, forwarded.Arity, SubjectNode.Forwarded(Module, this, forwarded)));
        return children
            .OrderBy(static c => c.Name, NaturalStringComparer.Instance)
            .ThenBy(static c => c.Arity)
            .ToList();
    }

    /// <summary>The namespaces nested in this one among <paramref name="own"/>, the children sorted.</summary>
    private BrowseNode[] Interleave(List<SortedChild> own)
    {
        var result = new List<BrowseNode>(own.Count + _inner.Count);
        var inner = new Queue<NamespaceNode>(_inner);
        foreach (var child in own)
        {
            while (inner.Count > 0 && SortsBefore(inner.Peek(), child))
                result.Add(inner.Dequeue());
            result.Add(child.Node);
        }
        result.AddRange(inner);
        return result.ToArray();
    }

    /// <summary>Whether ILSpy, sorting the full names of the types, comes to those in
    /// <paramref name="inner"/> before <paramref name="child"/>.</summary>
    private static bool SortsBefore(NamespaceNode inner, SortedChild child)
    {
        var key = NamespaceNames.LastSegment(inner.Name) + NestedSortMark;
        return NaturalStringComparer.Instance.Compare(key, child.ReflectionName) < 0;
    }

    internal override IReadOnlyList<BrowseNode> Nest(IReadOnlyList<BrowseNode> children) => _nested ?? children;

    /// <summary>A type of the namespace with what it is sorted by: its name without type parameters,
    /// then how many it has.</summary>
    private readonly record struct SortedChild(string Name, int Arity, BrowseNode Node)
    {
        private const string ArityMark = "`";

        /// <summary>The name as its reflection name ends, e.g. <c>List`1</c>.</summary>
        public string ReflectionName
        {
            get
            {
                if (Arity == 0)
                    return Name;
                return Name + ArityMark + Arity;
            }
        }
    }
}

internal static class Members
{
    private static readonly DecompilerSettings Settings = Core.Settings.Create();

    public static bool IsHidden(IEntity entity) => IsMissing(entity) || IsCompilerGenerated(entity);

    /// <summary>Whether <paramref name="entity"/> has no metadata of its own to show.</summary>
    public static bool IsMissing(IEntity entity) => entity.MetadataToken.IsNil;

    public static bool IsCompilerGenerated(IEntity entity)
    {
        var file = entity.ParentModule?.MetadataFile;
        return file != null && CSharpDecompiler.MemberIsHidden(file, entity.MetadataToken, Settings);
    }

    /// <summary>The accessors of a property or event, in ILSpy's order; none for other entities.</summary>
    public static IEnumerable<IMethod> Accessors(IEntity entity)
    {
        IEnumerable<IMethod?> accessors = entity switch
        {
            IProperty property => [property.Getter, property.Setter],
            IEvent @event => [@event.AddAccessor, @event.RemoveAccessor, @event.InvokeAccessor],
            _ => [],
        };
        return accessors.OfType<IMethod>();
    }
}
