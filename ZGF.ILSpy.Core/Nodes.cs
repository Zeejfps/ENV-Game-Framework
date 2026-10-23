using System.Reflection;
using System.Reflection.Metadata;
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

    public AssemblyNode(LoadedAssembly assembly) : base(assembly, null, NodeKind.Assembly, LabelOf(assembly))
    {
    }

    public override string FullName => Assembly.FullName;

    private protected override bool MayHaveChildren => true;

    public NamespaceNode? FindNamespace(string name)
    {
        _ = Children;
        return Volatile.Read(ref _namespaces)?.GetValueOrDefault(name);
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
            if (Members.IsHidden(type)) continue;
            if (!byNamespace.TryGetValue(type.Namespace, out var types))
                byNamespace.Add(type.Namespace, types = new List<ITypeDefinition>());
            types.Add(type);
        }

        var namespaces = new Dictionary<string, NamespaceNode>(StringComparer.Ordinal);
        foreach (var name in byNamespace.Keys.Order(NaturalStringComparer.Instance))
        {
            var node = new NamespaceNode(Assembly, this, name, byNamespace[name]);
            namespaces.Add(name, node);
            result.Add(node);
        }
        Volatile.Write(ref _namespaces, namespaces);
        return result.ToArray();
    }

    private static string LabelOf(LoadedAssembly assembly)
    {
        var details = new[] { assembly.Version?.ToString(), assembly.FrameworkName }.OfType<string>().ToArray();
        return details.Length == 0 ? assembly.Name : $"{assembly.Name} ({string.Join(", ", details)})";
    }
}

internal sealed class ReferencesFolderNode(LoadedAssembly assembly, BrowseNode parent)
    : BrowseNode(assembly, parent, NodeKind.ReferencesFolder, "References")
{
    private protected override bool MayHaveChildren => !Assembly.File.AssemblyReferences.IsEmpty;

    private protected override BrowseNode[] CreateChildren() =>
        Assembly.File.AssemblyReferences
            .OrderBy(r => r.Name, NaturalStringComparer.Instance)
            .Select(r => (BrowseNode)new AssemblyReferenceNode(Assembly, this, r))
            .ToArray();
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

internal sealed class NamespaceNode : BrowseNode
{
    private readonly List<ITypeDefinition> _types;

    public NamespaceNode(LoadedAssembly assembly, BrowseNode parent, string name, List<ITypeDefinition> types)
        : base(assembly, parent, NodeKind.Namespace, name.Length == 0 ? "-" : name)
    {
        Name = name;
        _types = types;
        IsPublicApi = types.Exists(EntityText.IsPublicApi);
    }

    public string Name { get; }

    public IEnumerable<ITypeDefinition> Types => _types;

    public override string FullName => Name;

    private protected override bool MayHaveChildren => true;

    private protected override BrowseNode[] CreateChildren() =>
        _types
            .OrderBy(t => t.Name, NaturalStringComparer.Instance)
            .Select(t => (BrowseNode)new TypeNode(Assembly, this, t))
            .ToArray();
}

internal abstract class EntityNode : BrowseNode
{
    private protected EntityNode(LoadedAssembly assembly, BrowseNode parent, IEntity entity)
        : base(assembly, parent, EntityText.KindOf(entity), EntityText.Label(entity))
    {
        Entity = entity;
        Accessibility = EntityText.Map(entity.Accessibility);
        IsPublicApi = EntityText.IsPublicApi(entity);
        Reference = CodeReference.To(entity);
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

        foreach (var nested in type.NestedTypes.Where(t => !Members.IsHidden(t)).OrderBy(t => t.Name, NaturalStringComparer.Instance))
            result.Add(new TypeNode(Assembly, this, nested));

        var fields = type.Fields.Where(f => !Members.IsHidden(f) && !(type.Kind == TypeKind.Enum && !f.IsStatic));
        if (type.Kind != TypeKind.Enum)
            fields = fields.OrderBy(f => f.Name, NaturalStringComparer.Instance);
        foreach (var field in fields)
            result.Add(new MemberNode(Assembly, this, field));

        foreach (var property in type.Properties.Where(p => !Members.IsHidden(p)).OrderBy(p => p.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Assembly, this, property));

        foreach (var @event in type.Events.Where(e => !Members.IsHidden(e)).OrderBy(e => e.Name, NaturalStringComparer.Instance))
            result.Add(new MemberNode(Assembly, this, @event));

        foreach (var method in type.Methods.Where(m => !m.IsAccessor && !Members.IsHidden(m)).OrderBy(m => m.Name, NaturalStringComparer.Instance))
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

    public static bool IsHidden(IEntity entity) =>
        entity.MetadataToken.IsNil
        || entity.ParentModule?.MetadataFile is { } file && CSharpDecompiler.MemberIsHidden(file, entity.MetadataToken, Settings);

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
