using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>A node made from what it stands for, its <see cref="Subject"/>: its label, flags and
/// children all follow from that.</summary>
internal sealed class SubjectNode : BrowseNode, IModuleNode
{
    private SubjectNode(BrowseNode parent, NodeSubject subject, IEntity entity)
        : base(ModuleOf(subject), parent, EntityText.KindOf(entity), EntityText.Label(entity))
    {
        Subject = subject;
        Module = ModuleOf(subject);
        Entity = entity;
        Accessibility = EntityText.Map(entity.Accessibility);
        IsPublicApi = EntityText.IsPublicApi(entity);
        IsCompilerGenerated = parent.IsCompilerGenerated || Members.IsCompilerGenerated(entity);
        Reference = AddressOf(subject).ToReference();
        Token = MetadataTokens.GetToken(entity.MetadataToken);
    }

    public NodeSubject Subject { get; }

    public LoadedModule Module { get; }

    /// <summary>The type or member the node defines.</summary>
    public IEntity Entity { get; }

    /// <summary>The handle the node of the definition is found by.</summary>
    public EntityHandle Handle => AddressOf(Subject).Handle;

    public override string FullName => EntityText.FullName(Entity);

    /// <summary>The node of <paramref name="type"/>, under <paramref name="parent"/>; null for a type with
    /// no metadata token, which the tree does not list.</summary>
    public static SubjectNode? ForType(LoadedModule module, BrowseNode parent, ITypeDefinition type)
    {
        var definition = DefinedType.Of(module, type);
        if (definition == null)
            return null;
        return new SubjectNode(parent, new NodeSubject.TypeDefinition(definition), type);
    }

    /// <summary>The node of <paramref name="member"/>, under <paramref name="parent"/>; null for a member
    /// with no metadata token, which the tree does not list.</summary>
    public static SubjectNode? ForMember(LoadedModule module, BrowseNode parent, IMember member)
    {
        var definition = DefinedMember.Of(module, member);
        if (definition == null)
            return null;
        return new SubjectNode(parent, new NodeSubject.MemberDefinition(definition), member);
    }

    /// <summary>The type or member <paramref name="node"/> defines; null for a node of anything else.</summary>
    public static IEntity? EntityOf(BrowseNode? node)
    {
        if (node is not SubjectNode subject)
            return null;
        return subject.Entity;
    }

    /// <summary>Whether <paramref name="node"/> defines a member, under which only its accessors sit.</summary>
    public static bool IsMember(BrowseNode? node)
    {
        if (node is not SubjectNode subject)
            return false;
        return subject.Subject is NodeSubject.MemberDefinition;
    }

    private static LoadedModule ModuleOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Module,
        NodeSubject.MemberDefinition member => member.Definition.Module,
        _ => throw new UnreachableException(),
    };

    private static NavigationAddress AddressOf(NodeSubject subject) => subject switch
    {
        NodeSubject.TypeDefinition type => type.Definition.Address,
        NodeSubject.MemberDefinition member => member.Definition.Address,
        _ => throw new UnreachableException(),
    };

    internal override bool MayHaveChildren => Subject switch
    {
        NodeSubject.TypeDefinition type => MayListMembers(type.Definition),
        NodeSubject.MemberDefinition member => member.Definition.Symbol is IProperty or IEvent,
        _ => throw new UnreachableException(),
    };

    internal override BrowseNode[] CreateChildren() => Subject switch
    {
        NodeSubject.TypeDefinition type => ListMembers(type.Definition),
        NodeSubject.MemberDefinition member => ListAccessors(member.Definition),
        _ => throw new UnreachableException(),
    };

    /// <summary>Whether the type's metadata lists anything the node would: a base type, an interface,
    /// a nested type, a member, or types that may derive from it.</summary>
    private static bool MayListMembers(DefinedType type)
    {
        var definition = type.Module.File.Metadata.GetTypeDefinition(type.Handle);
        return !definition.BaseType.IsNil || definition.GetInterfaceImplementations().Count > 0 || definition.GetNestedTypes().Length > 0 ||
               DerivedTypesNode.CanHave(type.Symbol) || definition.GetFields().Count > 0 || definition.GetMethods().Count > 0 ||
               definition.GetProperties().Count > 0 || definition.GetEvents().Count > 0;
    }

    /// <summary>The base and derived types, then the nested types, fields, properties, events and methods,
    /// each sorted by name but an enum's fields, which keep their order; an enum's value field and the
    /// accessors are left out.</summary>
    private BrowseNode[] ListMembers(DefinedType definition)
    {
        var type = definition.Symbol;
        var module = definition.Module;
        var result = new List<BrowseNode>();
        if (type.DirectBaseTypes.Any())
            result.Add(new BaseTypesNode(module, this, type));
        if (DerivedTypesNode.CanHave(type))
            result.Add(new DerivedTypesNode(module, this, type));

        foreach (var nested in type.NestedTypes.OrderBy(t => t.Name, NaturalStringComparer.Instance))
            AddIfListed(result, ForType(module, this, nested));

        var fields = type.Fields.Where(f => !(type.Kind == TypeKind.Enum && !f.IsStatic));
        if (type.Kind != TypeKind.Enum)
            fields = fields.OrderBy(f => f.Name, NaturalStringComparer.Instance);
        foreach (var field in fields)
            AddIfListed(result, ForMember(module, this, field));

        foreach (var property in type.Properties.OrderBy(p => p.Name, NaturalStringComparer.Instance))
            AddIfListed(result, ForMember(module, this, property));

        foreach (var @event in type.Events.OrderBy(e => e.Name, NaturalStringComparer.Instance))
            AddIfListed(result, ForMember(module, this, @event));

        foreach (var method in type.Methods.Where(m => !m.IsAccessor).OrderBy(m => m.Name, NaturalStringComparer.Instance))
            AddIfListed(result, ForMember(module, this, method));

        return result.ToArray();
    }

    private BrowseNode[] ListAccessors(DefinedMember definition)
    {
        var result = new List<BrowseNode>();
        foreach (var accessor in Members.Accessors(definition.Symbol))
            AddIfListed(result, ForMember(definition.Module, this, accessor));
        return result.ToArray();
    }

    private static void AddIfListed(List<BrowseNode> nodes, SubjectNode? node)
    {
        if (node != null)
            nodes.Add(node);
    }
}
