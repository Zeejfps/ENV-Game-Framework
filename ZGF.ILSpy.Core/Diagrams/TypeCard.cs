namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>What a type is beyond its kind, as its card tags it.</summary>
public enum TypeStereotype
{
    None,
    Abstract,
    Static,
    Interface,
}

/// <summary>The kinds of members a card lists, a section each, in the order the card shows them.</summary>
public enum MemberSection
{
    Fields,
    Properties,
    Methods,
    Events,
}

/// <summary>A member a card lists: what it points at, how the tree labels it, the kind its icon
/// shows, and its declared accessibility.</summary>
public sealed record CardMember(CodeReference Reference, string Text, NodeKind Kind, NodeAccessibility Accessibility)
{
    /// <summary>Whether code of another assembly can reach the member: it is public or protected.</summary>
    public bool IsVisibleOutside => Accessibility is NodeAccessibility.Public or NodeAccessibility.Protected or NodeAccessibility.ProtectedInternal;
}

/// <summary>A card's own members of one kind, sorted as the tree sorts them.</summary>
public sealed record CardSection(MemberSection Kind, IReadOnlyList<CardMember> Members);

/// <summary>The members a card's type inherits from one of its base types, which is named
/// <see cref="From"/> as the type derives from it and defined in the assembly at <see cref="AssemblyPath"/>.</summary>
public sealed record InheritedGroup(string From, string AssemblyPath, IReadOnlyList<CardMember> Members);

/// <summary>A type as a diagram shows it: its name as the tree writes it, the kind its icon shows and
/// its stereotype; its own members by kind, then what it inherits by base type, nearest first; the links
/// from it to the types it derives from, implements and holds in its properties; and whether anything
/// can derive from it. <see cref="AssemblyPath"/> is the file of the assembly that defines it, and
/// <see cref="Anchor"/> names the type so that it is found again in another run.</summary>
public sealed record TypeCard(
    CodeReference Type,
    TypeAnchor Anchor,
    string Name,
    string FullName,
    string AssemblyPath,
    NodeKind Kind,
    NodeAccessibility Accessibility,
    TypeStereotype Stereotype,
    IReadOnlyList<CardSection> Sections,
    IReadOnlyList<InheritedGroup> Inherited,
    IReadOnlyList<TypeLink> Links,
    bool CanHaveDerived);

/// <summary>How one type of a diagram relates to another.</summary>
public enum TypeLinkKind
{
    /// <summary>Derives from the class, or extends the interface.</summary>
    Inherits,

    /// <summary>Implements the interface.</summary>
    Implements,

    /// <summary>Has a property holding one of the type.</summary>
    HoldsOne,

    /// <summary>Has a property holding many of the type.</summary>
    HoldsMany,
}

/// <summary>A link drawn from the type <see cref="From"/> to the type <see cref="To"/>, labelled with the
/// name of the property that holds it, for the links that are held.</summary>
public sealed record TypeLink(CodeReference From, CodeReference To, TypeLinkKind Kind, string? Label = null);
