// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>The types of a diagram, each as a mermaid class definition of its own members, with
/// properties holding diagram types drawn as relations instead, and the members it inherits kept
/// apart by the type declaring them, so the diagrammer can leave out those a base type it draws
/// already shows.</summary>
internal sealed class DiagramTypes(DiagramNames names, DiagramDocs docs)
{
    private const string NewLine = "\n";
    private const string MemberSeparator = NewLine + "    ";

    // The settings compiler-generated members are recognised by, among them automatic properties'
    // backing fields.
    private readonly DecompilerSettings _settings = new();
    private readonly MermaidMembers _members = new(names);

    public DiagramType Build(ITypeDefinition type)
    {
        if (type.Kind == TypeKind.Enum)
            return BuildEnum(type);
        return BuildClass(type);
    }

    private DiagramType BuildEnum(ITypeDefinition type)
    {
        var fields = type
            .GetFields(IsEnumValue)
            .ToArray();
        var values = fields
            .Select(static f => f.Name)
            .Prepend("<<Enumeration>>");
        var id = names.IdOf(type);
        return new DiagramType
        {
            Name = NameUnlessId(type, id),
            Body = Definition(id, Pad(values).TrimEnd(' ')),
            XmlDocs = docs.Collect(type, fields),
        };
    }

    private static bool IsEnumValue(IField field)
    {
        if (!field.IsConst)
            return false;
        if (!field.IsStatic)
            return false;
        return field.Accessibility == Accessibility.Public;
    }

    private DiagramType BuildClass(ITypeDefinition type)
    {
        var id = names.IdOf(type);
        var properties = type
            .GetProperties()
            .ToArray();
        var methods = MethodsOf(type);
        var fields = FieldsOf(type, properties);
        var members = SplitByDeclaringType(properties, methods, fields);
        var documented = fields
            .Concat<IMember>(properties)
            .Concat(methods);
        return new DiagramType
        {
            Name = NameUnlessId(type, id),
            Body = Definition(id, BodyOf(type, members)),
            HasOne = MapHasOne(members, type),
            HasMany = MapHasMany(members, type),
            BaseType = BaseTypeOf(type),
            Interfaces = InterfacesOf(type),
            Inherited = InheritedBy(type, id, members),
            XmlDocs = docs.Collect(type, documented),
        };
    }

    private string? NameUnlessId(IType type, string id)
    {
        var name = names.NameOf(type);
        if (name == id)
            return null;
        return name;
    }

    private static string Definition(string id, string body) => $"class {id} {{{body}}}";

    private static string Pad(IEnumerable<string> lines) => MemberSeparator + string.Join(MemberSeparator, lines) + MemberSeparator;

    /// <summary>The methods <paramref name="type"/> shows: its own and those it inherits, but no
    /// operators, nothing compiler-generated, and nothing object declares or a base type overrides
    /// from it.</summary>
    private static IMethod[] MethodsOf(ITypeDefinition type)
    {
        return type
            .GetMethods(m => IsShownMethod(type, m))
            .ToArray();
    }

    private static bool IsShownMethod(ITypeDefinition type, IMethod method)
    {
        if (method.IsOperator)
            return false;
        if (method.IsCompilerGenerated())
            return false;
        if (method.DeclaringTypeDefinition == type)
            return true;
        if (TypeFacts.IsObject(method.DeclaringType))
            return false;
        return !TypeFacts.OverridesObject(method);
    }

    /// <summary>The fields <paramref name="type"/> shows: none compiler-generated, and none that
    /// looks like the backing field of one of <paramref name="properties"/>.</summary>
    private IField[] FieldsOf(ITypeDefinition type, IProperty[] properties)
    {
        return type
            .GetFields(f => IsShownField(f, properties))
            .ToArray();
    }

    private bool IsShownField(IField field, IProperty[] properties)
    {
        if (IsHidden(field))
            return false;
        return !properties.Any(p => IsBackingField(field, p));
    }

    private bool IsHidden(IEntity entity) =>
        CSharpDecompiler.MemberIsHidden(entity.ParentModule!.MetadataFile, entity.MetadataToken, _settings);

    // A field of a property's type whose name holds the property's, as _name does for Name.
    private static bool IsBackingField(IField field, IProperty property)
    {
        if (!field.ReturnType.Equals(property.ReturnType))
            return false;
        return field.Name.Contains(property.Name, StringComparison.OrdinalIgnoreCase);
    }

    private DeclaredMembers SplitByDeclaringType(IProperty[] properties, IMethod[] methods, IField[] fields)
    {
        var hasOne = properties
            .Where(HoldsOne)
            .ToArray();
        var hasMany = properties
            .Select(ManyRelationOf)
            .OfType<ManyRelation>()
            .ToArray();
        var related = hasMany
            .Select(static r => r.Property)
            .Concat(hasOne)
            .ToHashSet();
        var flat = properties
            .Where(p => !related.Contains(p))
            .ToArray();
        return new DeclaredMembers(
            ByDeclaringType(flat, static p => p),
            ByDeclaringType(hasOne, static p => p),
            ByDeclaringType(hasMany, static r => r.Property),
            ByDeclaringType(methods, static m => m),
            ByDeclaringType(fields, static f => f));
    }

    private static Dictionary<IType, T[]> ByDeclaringType<T>(IEnumerable<T> items, Func<T, IMember> memberOf)
    {
        return items
            .GroupBy(i => memberOf(i).DeclaringType)
            .ToDictionary(static g => g.Key, static g => g.ToArray());
    }

    // A property whose type, or the type it makes nullable, is drawn in the diagram.
    private bool HoldsOne(IProperty property)
    {
        var type = property.ReturnType;
        if (TypeFacts.TryGetNullable(type, out var underlying))
            type = underlying;
        return names.IsPicked(type);
    }

    // A property of a generic enumerable of a type drawn in the diagram.
    private ManyRelation? ManyRelationOf(IProperty property)
    {
        if (!TypeFacts.TryGetElementType(property, out var elementType))
            return null;
        if (!names.IsPicked(elementType))
            return null;
        return new ManyRelation(property, elementType);
    }

    private string BodyOf(ITypeDefinition type, DeclaredMembers members)
    {
        var own = Pad(FlatMembersOf(members, type, ""));
        var annotation = AnnotationOf(type);
        if (annotation == null)
            return own.TrimEnd(' ');
        return own + $"<<{annotation}>>" + NewLine;
    }

    // See https://mermaid.js.org/syntax/classDiagram.html#annotations-on-classes; a static class is
    // abstract and sealed.
    private static string? AnnotationOf(ITypeDefinition type)
    {
        if (TypeFacts.IsInterface(type))
            return "Interface";
        if (!type.IsAbstract)
            return null;
        if (type.IsSealed)
            return "Service";
        return "Abstract";
    }

    private IEnumerable<string> FlatMembersOf(DeclaredMembers members, IType declaring, string prefix)
    {
        var properties = Declared(members.FlatProperties, declaring).Select(p => prefix + _members.FormatProperty(p));
        var methods = Declared(members.Methods, declaring).Select(m => prefix + _members.FormatMethod(m));
        var fields = Declared(members.Fields, declaring).Select(f => prefix + _members.FormatField(f));
        return properties
            .Concat(methods)
            .Concat(fields);
    }

    private static T[] Declared<T>(Dictionary<IType, T[]> byType, IType declaring)
    {
        if (byType.TryGetValue(declaring, out var declared))
            return declared;
        return [];
    }

    /// <summary>The members <paramref name="type"/> inherits, by the id of each base type declaring
    /// them, written as members of <paramref name="id"/> for the diagrammer to add.</summary>
    private Dictionary<string, InheritedMembers> InheritedBy(ITypeDefinition type, string id, DeclaredMembers members)
    {
        var prefix = id + " : ";
        var inherited = new Dictionary<string, InheritedMembers>();
        var ancestors = type
            .GetNonInterfaceBaseTypes()
            .Where(t => TypeFacts.IsAncestor(type, t));
        foreach (var ancestor in ancestors)
            inherited[names.IdOf(ancestor)] = InheritedFrom(members, ancestor, prefix);
        return inherited;
    }

    private InheritedMembers InheritedFrom(DeclaredMembers members, IType ancestor, string prefix)
    {
        var flat = FlatMembersOf(members, ancestor, prefix).ToList();
        string? flatMembers = null;
        if (flat.Count > 0)
            flatMembers = string.Join(NewLine, flat);
        return new InheritedMembers
        {
            FlatMembers = flatMembers,
            HasOne = MapHasOne(members, ancestor),
            HasMany = MapHasMany(members, ancestor),
        };
    }

    private Dictionary<string, string>? MapHasOne(DeclaredMembers members, IType declaring)
    {
        if (!members.HasOne.TryGetValue(declaring, out var properties))
            return null;
        var relations = new Dictionary<string, string>();
        foreach (var property in properties)
            Relate(relations, ReferToOne(property));
        return relations;
    }

    private DiagramReference ReferToOne(IProperty property)
    {
        var type = property.ReturnType;
        var label = property.Name;
        if (property.IsIndexer)
            label += IndexerParametersOf(property);
        if (TypeFacts.TryGetNullable(type, out var underlying))
        {
            type = underlying;
            label += " ?";
        }
        return names.Refer(type, label);
    }

    private static string IndexerParametersOf(IProperty indexer)
    {
        var parameters = indexer.Parameters.Select(static p => p.Type.Name + " " + p.Name);
        return "[" + string.Join(", ", parameters) + "]";
    }

    private Dictionary<string, string>? MapHasMany(DeclaredMembers members, IType declaring)
    {
        if (!members.HasMany.TryGetValue(declaring, out var many))
            return null;
        var relations = new Dictionary<string, string>();
        foreach (var relation in many)
            Relate(relations, names.Refer(relation.ElementType, relation.Property.Name));
        return relations;
    }

    // Relations are keyed by label; a label two properties share keeps the last.
    private static void Relate(Dictionary<string, string> relations, DiagramReference reference)
    {
        relations[reference.Label!] = reference.To;
    }

    /// <summary>The class <paramref name="type"/> derives from, unless that is object.</summary>
    private Dictionary<string, string?>? BaseTypeOf(ITypeDefinition type)
    {
        var baseType = type.DirectBaseTypes.FirstOrDefault(IsBaseClass);
        if (baseType == null)
            return null;
        var reference = names.Refer(baseType);
        return new Dictionary<string, string?> { [reference.To] = reference.Label };
    }

    private static bool IsBaseClass(IType type)
    {
        if (TypeFacts.IsInterface(type))
            return false;
        return !TypeFacts.IsObject(type);
    }

    /// <summary>The interfaces <paramref name="type"/> implements directly, each with the labels of
    /// the closed generic forms it implements.</summary>
    private Dictionary<string, string?[]>? InterfacesOf(ITypeDefinition type)
    {
        var references = type.DirectBaseTypes
            .Where(TypeFacts.IsInterface)
            .Select(i => names.Refer(i))
            .ToList();
        if (references.Count == 0)
            return null;
        return references
            .GroupBy(static r => r.To)
            .ToDictionary(static g => g.Key, LabelsOf);
    }

    private static string?[] LabelsOf(IEnumerable<DiagramReference> references)
    {
        return references
            .Select(static r => r.Label)
            .ToArray();
    }

    private readonly record struct ManyRelation(IProperty Property, IType ElementType);

    /// <summary>A type's members as the diagram splits them, each kind by the type declaring it:
    /// plain properties, properties holding one or many diagram types, methods and fields.</summary>
    private sealed record DeclaredMembers(
        Dictionary<IType, IProperty[]> FlatProperties,
        Dictionary<IType, IProperty[]> HasOne,
        Dictionary<IType, ManyRelation[]> HasMany,
        Dictionary<IType, IMethod[]> Methods,
        Dictionary<IType, IField[]> Fields);
}
