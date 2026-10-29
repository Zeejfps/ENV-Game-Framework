using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal sealed partial class SubjectNode
{
    /// <summary>Whether the type's metadata lists anything the node would: a base type, an interface,
    /// a nested type, a member, or types that may derive from it.</summary>
    private static bool MayListMembers(DefinedType type)
    {
        var definition = type.Module.File.Metadata.GetTypeDefinition(type.Handle);
        return !definition.BaseType.IsNil || definition.GetInterfaceImplementations().Count > 0 || definition.GetNestedTypes().Length > 0 ||
               DerivedTypes.CanHave(type.Symbol) || definition.GetFields().Count > 0 || definition.GetMethods().Count > 0 ||
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
            result.Add(new SubjectNode(this, new NodeSubject.BaseTypes(definition)));
        if (DerivedTypes.CanHave(type))
            result.Add(new SubjectNode(this, new NodeSubject.DerivedTypes(definition)));

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

    private BrowseNode[] ListBaseTypes(DefinedType type) =>
        type.Symbol.DirectBaseTypes
            .Select(t => (BrowseNode)new SubjectNode(this, new NodeSubject.BaseType(BaseTypeInfo.Of(type.Module, t))))
            .ToArray();

    /// <summary>The types in scope of <paramref name="type"/> that derive from it directly, as
    /// <see cref="DerivedTypes"/> finds them, under <paramref name="parent"/>.</summary>
    private static BrowseNode[] FindDerivedTypes(LoadedModule module, BrowseNode parent, ITypeDefinition type)
    {
        var result = new List<BrowseNode>();
        foreach (var derived in DerivedTypes.Find(module.List, type, CancellationToken.None))
        {
            var info = DerivedTypeInfo.Of(module, derived);
            if (info != null)
                result.Add(new SubjectNode(parent, new NodeSubject.DerivedType(info)));
        }
        return result.ToArray();
    }
}
