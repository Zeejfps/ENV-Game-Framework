using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>Types read as diagram cards from the type system, alone, with the types of their assembly
/// they are related to, or as the types deriving from a type. Each read loads the assemblies it needs
/// without listing them: call them off the UI thread.</summary>
public static class TypeCards
{
    /// <summary>The card of the type <paramref name="type"/> points at; null when it points at no type,
    /// or that cannot be resolved.</summary>
    public static TypeCard? Read(AssemblyList list, CodeReference type)
    {
        var definition = list.FindTypeDefinition(type);
        if (definition == null)
            return null;
        return Read(definition);
    }

    /// <summary>The card of the type <paramref name="type"/> points at, then the cards of the types of its
    /// own assembly it is directly related to: those it derives from, implements or holds, and those
    /// deriving from it or implementing it. None when it points at no type.</summary>
    public static List<TypeCard> ReadWithNeighbours(AssemblyList list, CodeReference type, CancellationToken cancellationToken)
    {
        var definition = list.FindTypeDefinition(type);
        if (definition == null)
            return [];
        var cards = new List<TypeCard> { Read(definition) };
        foreach (var neighbour in ListNeighbours(definition))
        {
            cancellationToken.ThrowIfCancellationRequested();
            cards.Add(Read(neighbour));
        }
        return cards;
    }

    /// <summary>The card of the type <paramref name="anchor"/> names; null when its assembly cannot be
    /// read or no longer defines it.</summary>
    public static TypeCard? Read(AssemblyList list, TypeAnchor anchor)
    {
        var definition = list.FindTypeDefinition(anchor);
        if (definition == null)
            return null;
        return Read(definition);
    }

    /// <summary>The cards of the types deriving directly from, or implementing, the type
    /// <paramref name="type"/> points at, among the assemblies <paramref name="list"/> holds now.</summary>
    public static List<TypeCard> ReadDerived(AssemblyList list, CodeReference type, CancellationToken cancellationToken)
    {
        var definition = list.FindTypeDefinition(type);
        if (definition == null)
            return [];
        return DerivedTypes.Find(list, definition, cancellationToken)
            .Where(IsShown)
            .Select(Read)
            .ToList();
    }

    internal static TypeCard Read(ITypeDefinition type)
    {
        return new TypeCard(
            Type: CodeReference.To(type)!,
            Anchor: new TypeAnchor(AssemblyPathOf(type), type.ReflectionName),
            Name: EntityText.Label(type),
            FullName: EntityText.FullName(type),
            AssemblyPath: AssemblyPathOf(type),
            Shape: EntityText.ShapeOf(type),
            Accessibility: EntityText.Map(type.Accessibility),
            Stereotype: StereotypeOf(type),
            Sections: ReadSections(type),
            Inherited: ReadInherited(type),
            Links: ReadLinks(type),
            CanHaveDerived: DerivedTypes.CanHave(type));
    }

    private static AssemblyPath AssemblyPathOf(ITypeDefinition type) => AssemblyPath.Of(type.ParentModule!.MetadataFile!);

    private static TypeStereotype StereotypeOf(ITypeDefinition type)
    {
        if (TypeFacts.IsInterface(type))
            return TypeStereotype.Interface;
        if (type.Kind != TypeKind.Class)
            return TypeStereotype.None;
        if (!type.IsAbstract)
            return TypeStereotype.None;
        if (type.IsSealed)
            return TypeStereotype.Static;
        return TypeStereotype.Abstract;
    }

    private static List<CardSection> ReadSections(ITypeDefinition type)
    {
        var sections = new List<CardSection>();
        AddSection(sections, MemberSection.Fields, ListFields(type));
        AddSection(sections, MemberSection.Properties, Sort(type.Properties));
        AddSection(sections, MemberSection.Methods, Sort(ListMethods(type)));
        AddSection(sections, MemberSection.Events, Sort(type.Events));
        return sections;
    }

    private static void AddSection(List<CardSection> sections, MemberSection kind, IEnumerable<IMember> members)
    {
        var shown = members
            .Where(IsShown)
            .Select(ToCardMember)
            .ToList();
        if (shown.Count == 0)
            return;
        sections.Add(new CardSection(kind, shown));
    }

    /// <summary>The fields as the tree lists them: an enum's values in the order declared, else
    /// sorted; never the field holding an enum's value.</summary>
    private static IEnumerable<IMember> ListFields(ITypeDefinition type)
    {
        if (type.Kind != TypeKind.Enum)
            return Sort(type.Fields);
        return type.Fields.Where(static f => f.IsStatic);
    }

    private static IEnumerable<IMethod> ListMethods(ITypeDefinition type) => type.Methods.Where(static m => !m.IsAccessor);

    private static IEnumerable<IMember> Sort(IEnumerable<IMember> members) => members.OrderBy(static m => m.Name, NaturalStringComparer.Instance);

    private static bool IsShown(IEntity entity) => !Members.IsHidden(entity);

    private static CardMember ToCardMember(IMember member)
    {
        return new CardMember(CodeReference.To(member)!, EntityText.Label(member), EntityText.ShapeOf(member), EntityText.Map(member.Accessibility));
    }

    /// <summary>What <paramref name="type"/> inherits from each of its base classes but object, nearest
    /// first; for an interface, from the interfaces it extends. Base types it inherits nothing from are
    /// left out.</summary>
    private static List<InheritedGroup> ReadInherited(ITypeDefinition type)
    {
        return type.GetNonInterfaceBaseTypes()
            .Where(t => TypeFacts.IsAncestor(type, t))
            .Reverse()
            .Select(ReadInheritedFrom)
            .Where(static g => g.Members.Count > 0)
            .ToList();
    }

    private static InheritedGroup ReadInheritedFrom(IType ancestor)
    {
        var definition = ancestor.GetDefinition();
        if (definition == null)
            return new InheritedGroup(EntityText.ShortTypeName(ancestor), null, []);
        var members = definition.Fields
            .Concat<IMember>(definition.Properties)
            .Concat(ListMethods(definition))
            .Concat(definition.Events)
            .Where(IsInherited)
            .OrderBy(static m => m.Name, NaturalStringComparer.Instance)
            .Select(ToCardMember)
            .ToList();
        return new InheritedGroup(EntityText.ShortTypeName(ancestor), AssemblyPathOf(definition), members);
    }

    /// <summary>Whether a type deriving from the one declaring <paramref name="member"/> inherits it: an
    /// instance member it can reach, no constructor, and no override of what object declares.</summary>
    private static bool IsInherited(IMember member)
    {
        if (!IsShown(member))
            return false;
        if (member.IsStatic)
            return false;
        if (member.Accessibility == Accessibility.Private)
            return false;
        if (member is IMethod method)
            return IsInheritedMethod(method);
        return true;
    }

    private static bool IsInheritedMethod(IMethod method)
    {
        if (method.IsConstructor)
            return false;
        return !TypeFacts.OverridesObject(method);
    }

    private static List<TypeLink> ReadLinks(ITypeDefinition type)
    {
        var from = CodeReference.To(type)!;
        return ListRelations(type)
            .Select(r => LinkOf(from, r))
            .OfType<TypeLink>()
            .Distinct()
            .ToList();
    }

    private static TypeLink? LinkOf(CodeReference from, Relation relation)
    {
        var to = CodeReference.To(relation.Target);
        if (to == null)
            return null;
        return new TypeLink(from, to, relation.Kind, relation.Label);
    }

    /// <summary>The types <paramref name="type"/> derives from, implements and holds in its own
    /// properties, but the base types every class, struct or enum has.</summary>
    private static List<Relation> ListRelations(ITypeDefinition type)
    {
        var relations = new List<Relation>();
        AddBases(relations, type);
        AddHeld(relations, type);
        return relations;
    }

    private static void AddBases(List<Relation> relations, ITypeDefinition type)
    {
        foreach (var baseType in type.DirectBaseTypes)
        {
            var definition = baseType.GetDefinition();
            if (definition == null)
                continue;
            if (IsImplicitBase(definition))
                continue;
            relations.Add(new Relation(definition, BaseLinkKindOf(type, definition), null));
        }
    }

    private static bool IsImplicitBase(ITypeDefinition definition)
    {
        return definition.KnownTypeCode is KnownTypeCode.Object or KnownTypeCode.ValueType or KnownTypeCode.Enum;
    }

    /// <summary>An interface implemented by a class or struct, else a type derived from or extended.</summary>
    private static TypeLinkKind BaseLinkKindOf(ITypeDefinition type, ITypeDefinition baseType)
    {
        if (!TypeFacts.IsInterface(baseType))
            return TypeLinkKind.Inherits;
        if (TypeFacts.IsInterface(type))
            return TypeLinkKind.Inherits;
        return TypeLinkKind.Implements;
    }

    private static void AddHeld(List<Relation> relations, ITypeDefinition type)
    {
        foreach (var property in type.Properties)
        {
            if (!IsShown(property))
                continue;
            var held = FindHeld(property);
            if (held != null)
                relations.Add(held.Value);
        }
    }

    /// <summary>What <paramref name="property"/> holds: many of the elements of a generic enumerable,
    /// else one of its type, or of the type it makes nullable. Null when that is no type defined in
    /// metadata, or one built in, such as <c>int</c> or <c>string</c>.</summary>
    private static Relation? FindHeld(IProperty property)
    {
        if (TypeFacts.TryGetElementType(property, out var elementType))
            return HeldRelation(elementType, TypeLinkKind.HoldsMany, property.Name);
        var type = property.ReturnType;
        if (TypeFacts.TryGetNullable(type, out var underlying))
            type = underlying;
        return HeldRelation(type, TypeLinkKind.HoldsOne, property.Name);
    }

    private static Relation? HeldRelation(IType type, TypeLinkKind kind, string label)
    {
        var definition = type.GetDefinition();
        if (definition == null)
            return null;
        if (definition.KnownTypeCode != KnownTypeCode.None)
            return null;
        return new Relation(definition, kind, label);
    }

    /// <summary>The types of <paramref name="type"/>'s own module it is related to, either way: those it
    /// links to and those deriving from it or implementing it, each once, none compiler-generated.</summary>
    private static List<ITypeDefinition> ListNeighbours(ITypeDefinition type)
    {
        var linked = ListRelations(type)
            .Select(static r => r.Target);
        var module = type.ParentModule;
        return linked
            .Concat(DerivedTypes.FindInModule(type))
            .Where(t => t.ParentModule == module)
            .Where(t => !t.Equals(type))
            .Where(IsShown)
            .Distinct()
            .ToList();
    }

    /// <summary>A type <see cref="Target"/> another is related to as <see cref="Kind"/> says, through the
    /// property named <see cref="Label"/> when it holds it.</summary>
    private readonly record struct Relation(ITypeDefinition Target, TypeLinkKind Kind, string? Label);
}
