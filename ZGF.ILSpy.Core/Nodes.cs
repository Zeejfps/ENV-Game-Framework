using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;
using AssemblyReference = ICSharpCode.Decompiler.Metadata.AssemblyReference;
using MetadataResource = ICSharpCode.Decompiler.Metadata.Resource;

namespace ZGF.ILSpy.Core;

internal sealed class AssemblyNode : BrowseNode
{
    private Dictionary<string, NamespaceNode>? _namespaces;
    private BrowseNode[]? _nested;

    public AssemblyNode(LoadedAssembly assembly, BrowseNode? parent) : base(assembly, parent, NodeKind.Assembly, LabelOf(assembly))
    {
    }

    public override string FullName => Assembly.FullName;

    private protected override bool MayHaveChildren => true;

    public NamespaceNode? FindNamespace(string name)
    {
        _ = Children;
        return Volatile.Read(ref _namespaces)?.GetValueOrDefault(name);
    }

    /// <summary>The row <paramref name="handle"/> names in this assembly's own metadata tables, or null
    /// when it names no table row.</summary>
    public MetadataTarget? FindMetadataRow(EntityHandle handle)
    {
        if (!MetadataTokens.TryGetTableIndex(handle.Kind, out var index))
            return null;
        var metadata = Children
            .OfType<MetadataFolderNode>()
            .First(f => ReferenceEquals(f.File, Assembly.File));
        var table = metadata.FindTable(index);
        if (table == null)
            return null;
        var row = MetadataTokens.GetRowNumber(handle) - 1;
        return new MetadataTarget(table, row);
    }

    private protected override BrowseNode[] CreateChildren()
    {
        var file = Assembly.File;
        var result = new List<BrowseNode> { new MetadataFolderNode(Assembly, this, file, "Metadata") };
        var symbols = Assembly.DebugSymbols;
        if (symbols != null)
            result.Add(new MetadataFolderNode(Assembly, this, symbols.File, symbols.Title));
        result.Add(new ReferencesFolderNode(Assembly, this));
        if (!file.Resources.IsEmpty)
            result.Add(new ResourcesFolderNode(Assembly, this));

        var byNamespace = new Dictionary<string, List<ITypeDefinition>>(StringComparer.Ordinal);
        foreach (var type in Assembly.Module.TopLevelTypeDefinitions)
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
            var node = new NamespaceNode(Assembly, this, name, enclosing, types, forwarded);
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

    private protected override IReadOnlyList<BrowseNode> Nest(IReadOnlyList<BrowseNode> children) => _nested ?? children;

    /// <summary>The assembly's name, version and the framework it was built for. The framework picked
    /// to resolve its references against is left out, so that the label, by which paths find the node,
    /// stays the same when another is picked.</summary>
    private static string LabelOf(LoadedAssembly assembly)
    {
        var details = new[] { assembly.Version?.ToString(), BuiltForName(assembly) }.OfType<string>().ToArray();
        return details.Length == 0 ? assembly.Name : $"{assembly.Name} ({string.Join(", ", details)})";
    }

    private static string? BuiltForName(LoadedAssembly assembly)
    {
        if (assembly.DetectedTargetFramework == null)
            return null;
        return FrameworkIds.ReadableName(assembly.DetectedTargetFramework);
    }
}

/// <summary>The root of a listed file that could not be read as a .NET assembly: labelled with the
/// file's name, without children, and showing why it failed.</summary>
internal sealed class FailedAssemblyNode(LoadedAssembly assembly)
    : BrowseNode(assembly, null, NodeKind.FailedAssembly, System.IO.Path.GetFileName(assembly.FilePath))
{
    public override string FullName => Assembly.FilePath;
}

internal sealed class ReferencesFolderNode(LoadedAssembly assembly, BrowseNode parent)
    : BrowseNode(assembly, parent, NodeKind.ReferencesFolder, "References")
{
    private protected override bool MayHaveChildren => !Assembly.File.AssemblyReferences.IsEmpty;

    /// <summary>The references, sorted by name. What the assembly uses of them is read here, off the UI
    /// thread, so that their rows can tell whether they have children without reading it there.</summary>
    private protected override BrowseNode[] CreateChildren()
    {
        Assembly.ReadReferenceUsage();
        return Assembly.File.AssemblyReferences
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => (BrowseNode)new AssemblyReferenceNode(Assembly, this, r))
            .ToArray();
    }
}

internal sealed class AssemblyReferenceNode : BrowseNode
{
    public AssemblyReferenceNode(LoadedAssembly assembly, BrowseNode parent, AssemblyReference reference)
        : base(assembly, parent, NodeKind.AssemblyReference, reference.Name)
    {
        AssemblyReference = reference;
        Target = CodeReference.ToEntity(CodeReferenceKind.Assembly, reference.Name, assembly.FilePath, reference.Handle);
    }

    public AssemblyReference AssemblyReference { get; }

    public override string FullName => AssemblyReference.FullName;

    private protected override bool MayHaveChildren => Assembly.ReferenceUsage.Uses(AssemblyReference.Handle);

    private protected override BrowseNode[] CreateChildren()
    {
        if (!MayHaveChildren)
            return [];
        return [new ReferencedTypesNode(Assembly, this, AssemblyReference.Handle)];
    }
}

internal sealed class ResourcesFolderNode(LoadedAssembly assembly, BrowseNode parent)
    : BrowseNode(assembly, parent, NodeKind.ResourcesFolder, "Resources")
{
    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren() =>
        Assembly.File.Resources
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => ResourceNodes.Create(Assembly, this, r))
            .ToArray();
}

/// <summary>A namespace of an assembly: the types it defines there and those it forwards from
/// there to other assemblies, sorted together by name, then by how many type parameters they have.
/// When the tree nests namespaces, the namespaces nested in it sit among them, each where ILSpy sorts
/// the full names of the types in it.</summary>
internal sealed class NamespaceNode : BrowseNode
{
    private const string NestedSortMark = ".";

    private readonly List<ITypeDefinition> _types;
    private readonly List<ForwardedType> _forwarded;
    private readonly List<NamespaceNode> _inner = new();
    private BrowseNode[]? _nested;

    public NamespaceNode(LoadedAssembly assembly, BrowseNode parent, string name, NamespaceNode? enclosing, List<ITypeDefinition> types,
        List<ForwardedType> forwarded)
        : base(assembly, parent, NodeKind.Namespace, name.Length == 0 ? "-" : name)
    {
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

    private protected override bool MayHaveChildren => !IsEnclosingOnly;

    private protected override BrowseNode[] CreateChildren()
    {
        var sorted = SortChildren();
        _nested = Interleave(sorted);
        return sorted
            .Select(static c => c.Node)
            .ToArray();
    }

    private List<SortedChild> SortChildren()
    {
        var defined = _types
            .Select(t => new SortedChild(t.Name, t.TypeParameterCount, new TypeNode(Assembly, this, t)));
        var forwarded = _forwarded
            .Select(f => new SortedChild(f.SortName, f.Arity, new ForwardedTypeNode(Assembly, this, f)));
        return defined
            .Concat(forwarded)
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

    private protected override IReadOnlyList<BrowseNode> Nest(IReadOnlyList<BrowseNode> children) => _nested ?? children;

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

/// <summary>A type the assembly forwards to another: named as <c>typeof</c> names an open generic
/// type, and going to the type where it is defined.</summary>
internal sealed class ForwardedTypeNode : BrowseNode
{
    public ForwardedTypeNode(LoadedAssembly assembly, BrowseNode parent, ForwardedType type)
        : base(assembly, parent, NodeKind.ForwardedType, type.Label)
    {
        FullName = type.Namespace.Length == 0 ? type.Label : $"{type.Namespace}.{type.Label}";
        ForwardedTo = type.Destination;
        Target = CodeReference.ToEntity(CodeReferenceKind.Type, type.Label, assembly.FilePath, type.Handle);
    }

    public override string FullName { get; }
}

internal abstract class EntityNode : BrowseNode
{
    private protected EntityNode(LoadedAssembly assembly, BrowseNode parent, IEntity entity)
        : base(assembly, parent, EntityText.KindOf(entity), EntityText.Label(entity))
    {
        Entity = entity;
        Accessibility = EntityText.Map(entity.Accessibility);
        IsPublicApi = EntityText.IsPublicApi(entity);
        IsCompilerGenerated = parent.IsCompilerGenerated || Members.IsCompilerGenerated(entity);
        Reference = CodeReference.To(entity);
        Token = MetadataTokens.GetToken(entity.MetadataToken);
        assembly.Register(entity.MetadataToken, this);
    }

    public IEntity Entity { get; }

    public override string FullName => EntityText.FullName(Entity);
}

internal sealed class TypeNode(LoadedAssembly assembly, BrowseNode parent, ITypeDefinition type)
    : EntityNode(assembly, parent, type)
{
    public ITypeDefinition Type => (ITypeDefinition)Entity;

    private protected override bool MayHaveChildren
    {
        get
        {
            var definition = Assembly.File.Metadata.GetTypeDefinition((TypeDefinitionHandle)Type.MetadataToken);
            return !definition.BaseType.IsNil || definition.GetInterfaceImplementations().Count > 0 || definition.GetNestedTypes().Length > 0 || DerivedTypesNode.CanHave(Type) ||
                   definition.GetFields().Count > 0 || definition.GetMethods().Count > 0 || definition.GetProperties().Count > 0 ||
                   definition.GetEvents().Count > 0;
        }
    }

    private protected override BrowseNode[] CreateChildren()
    {
        var type = Type;
        var result = new List<BrowseNode>();
        if (type.DirectBaseTypes.Any())
            result.Add(new BaseTypesNode(Assembly, this, type));
        if (DerivedTypesNode.CanHave(type))
            result.Add(new DerivedTypesNode(Assembly, this, type));

        foreach (var nested in type.NestedTypes.Where(t => !Members.IsMissing(t)).OrderBy(t => t.Name, NaturalStringComparer.Instance))
            result.Add(new TypeNode(Assembly, this, nested));

        var fields = type.Fields.Where(f => !Members.IsMissing(f) && !(type.Kind == TypeKind.Enum && !f.IsStatic));
        if (type.Kind != TypeKind.Enum)
            fields = fields.OrderBy(f => f.Name, NaturalStringComparer.Instance);
        foreach (var field in fields)
            result.Add(new MemberNode(Assembly, this, field));

        foreach (var property in type.Properties.Where(p => !Members.IsMissing(p)).OrderBy(p => p.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Assembly, this, property));

        foreach (var @event in type.Events.Where(e => !Members.IsMissing(e)).OrderBy(e => e.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Assembly, this, @event));

        foreach (var method in type.Methods.Where(m => !m.IsAccessor && !Members.IsMissing(m)).OrderBy(m => m.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Assembly, this, method));

        return result.ToArray();
    }
}

internal sealed class MemberNode(LoadedAssembly assembly, BrowseNode parent, IMember member)
    : EntityNode(assembly, parent, member)
{
    public IMember Member => (IMember)Entity;

    private protected override bool MayHaveChildren => Member is IProperty or IEvent;

    private protected override BrowseNode[] CreateChildren() =>
        Members.Accessors(Member)
            .Where(a => !a.MetadataToken.IsNil)
            .Select(a => (BrowseNode)new MemberNode(Assembly, this, a))
            .ToArray();
}

internal sealed class BaseTypesNode(LoadedAssembly assembly, BrowseNode parent, ITypeDefinition type)
    : BrowseNode(assembly, parent, NodeKind.BaseTypes, "Base Types")
{
    public ITypeDefinition Type { get; } = type;

    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren() =>
        Type.DirectBaseTypes.Select(t => (BrowseNode)new BaseTypeNode(Assembly, this, t)).ToArray();
}

internal sealed class BaseTypeNode : BrowseNode
{
    public BaseTypeNode(LoadedAssembly assembly, BrowseNode parent, IType type)
        : base(assembly, parent, NodeKind.BaseType, EntityText.TypeName(type))
    {
        Type = type;
        if (type.GetDefinition() is { } definition && CodeReference.To(definition) is { } target)
        {
            Target = target;
            Accessibility = EntityText.Map(definition.Accessibility);
            IsPublicApi = EntityText.IsPublicApi(definition);
        }
    }

    public IType Type { get; }
}

/// <summary>ILSpy's "Derived Types": the types of the open assemblies that name
/// <see cref="Type"/> among their direct base types, found when first expanded.</summary>
internal sealed class DerivedTypesNode(LoadedAssembly assembly, BrowseNode parent, ITypeDefinition type)
    : BrowseNode(assembly, parent, NodeKind.DerivedTypes, "Derived Types")
{
    public ITypeDefinition Type { get; } = type;

    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren() => Find(Assembly, this, Type);

    /// <summary>Whether anything can derive from <paramref name="type"/>: a class that is not sealed,
    /// or an interface.</summary>
    public static bool CanHave(ITypeDefinition type) => !type.IsSealed && type.Kind is TypeKind.Class or TypeKind.Interface;

    /// <summary>The types in scope of <paramref name="type"/> that derive from it directly, as
    /// ILSpy finds them: one whose direct base types include one of the same name and arity.</summary>
    internal static BrowseNode[] Find(LoadedAssembly assembly, BrowseNode parent, ITypeDefinition type)
    {
        var context = new AnalyzerContext { AssemblyList = assembly.List };
        var result = new List<BrowseNode>();
        foreach (var candidate in context.GetScopeOf(type).GetTypesInScope(CancellationToken.None))
        {
            foreach (var baseType in candidate.DirectBaseTypes)
            {
                if (baseType.FullName == type.FullName && baseType.TypeParameterCount == type.TypeParameterCount)
                {
                    result.Add(new DerivedTypeNode(assembly, parent, candidate));
                    break;
                }
            }
        }
        return result.ToArray();
    }
}

/// <summary>A type deriving from the one a <see cref="DerivedTypesNode"/> belongs to: it goes to
/// that type, and expands to the types deriving from it in turn.</summary>
internal sealed class DerivedTypeNode : BrowseNode
{
    public DerivedTypeNode(LoadedAssembly assembly, BrowseNode parent, ITypeDefinition type)
        : base(assembly, parent, NodeKind.DerivedType, EntityText.TypeName(type))
    {
        Type = type;
        Accessibility = EntityText.Map(type.Accessibility);
        IsPublicApi = EntityText.IsPublicApi(type);
        Target = CodeReference.To(type);
    }

    public ITypeDefinition Type { get; }

    private protected override bool MayHaveChildren => !Type.IsSealed;

    private protected override BrowseNode[] CreateChildren() => DerivedTypesNode.Find(Assembly, this, Type);
}

internal sealed class ErrorNode(LoadedAssembly assembly, BrowseNode parent, Exception error)
    : BrowseNode(assembly, parent, NodeKind.Error, $"Error: {error.Message}")
{
    public Exception Error { get; } = error;
}

internal static class Members
{
    private static readonly DecompilerSettings Settings = Core.Settings.Create();

    public static bool IsHidden(IEntity entity) => IsMissing(entity) || IsCompilerGenerated(entity);

    /// <summary>Whether <paramref name="entity"/> has no metadata of its own to show.</summary>
    public static bool IsMissing(IEntity entity) => entity.MetadataToken.IsNil;

    public static bool IsCompilerGenerated(IEntity entity) =>
        entity.ParentModule?.MetadataFile is { } file && CSharpDecompiler.MemberIsHidden(file, entity.MetadataToken, Settings);

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
