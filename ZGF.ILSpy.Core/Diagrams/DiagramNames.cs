// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>A reference from one type of a diagram to another: the id it points to, and the label it
/// is drawn with, when it has one.</summary>
internal readonly record struct DiagramReference(string To, string? Label);

/// <summary>The ids and display names of the types a diagram draws and refers to. Ids are unique
/// across the diagram and safe in a DOM id; types outside the diagram that it refers to are
/// recorded with their display names.</summary>
internal sealed class DiagramNames
{
    private readonly HashSet<IType> _picked;
    private readonly Dictionary<IType, string> _ids;
    private readonly Dictionary<IType, string> _names = new();

    public DiagramNames(IReadOnlyCollection<ITypeDefinition> picked)
    {
        _picked = new HashSet<IType>(picked);
        _ids = CreateIds(picked);
    }

    /// <summary>The display names of the types outside the diagram it refers to, by id.</summary>
    public Dictionary<string, string> OutsideReferences { get; } = new();

    public bool IsPicked(IType type) => _picked.Contains(type);

    private static Dictionary<IType, string> CreateIds(IEnumerable<ITypeDefinition> types)
    {
        var ids = new Dictionary<IType, string>();
        var groups = types.GroupBy(static t => t.Name);
        foreach (var group in groups)
            AddIds(ids, group);
        return ids;
    }

    // A name only one type has is its id; types sharing a name are numbered.
    private static void AddIds(Dictionary<IType, string> ids, IGrouping<string, ITypeDefinition> group)
    {
        var types = group.ToList();
        if (types.Count == 1)
        {
            ids[types[0]] = Sanitize(group.Key);
            return;
        }
        for (var i = 0; i < types.Count; i++)
            ids[types[i]] = types[i].Name + (i + 1);
    }

    // A type in the main module of an executable is named <Module>.
    private static string Sanitize(string typeName) => typeName.Replace('<', '_').Replace('>', '_');

    public string IdOf(IType type) => Identify(type).Id;

    /// <summary>The id of <paramref name="type"/>, which for a closed generic type is the id of its
    /// open one, returned beside it.</summary>
    private (string Id, IType? OpenGeneric) Identify(IType type)
    {
        var openGeneric = OpenGenericOf(type);
        var identified = openGeneric ?? type;
        if (_ids.TryGetValue(identified, out var id))
            return (id, openGeneric);
        var created = CreateOutsideId(identified);
        _ids[identified] = created;
        return (created, openGeneric);
    }

    private static IType? OpenGenericOf(IType type)
    {
        if (type is not ParameterizedType generic)
            return null;
        if (generic.TypeArguments.Any(static a => a is ITypeParameter))
            return null;
        return generic.GenericType;
    }

    // A type outside the diagram is told apart by its full name, and generic overloads by their
    // type parameters.
    private string CreateOutsideId(IType type)
    {
        var id = Sanitize(type.FullName.Replace('.', '_'));
        if (type.TypeParameterCount == 0)
            return id;
        var parameters = type.TypeParameters.Select(IdOf);
        return id + "_" + string.Join("_", parameters);
    }

    /// <summary>How <paramref name="type"/> reads in the diagram: C# keywords for built-in types, a
    /// nested type after the type declaring it, generic arguments in ❰ ❱ as mermaid can't take
    /// angle brackets.</summary>
    public string NameOf(IType type)
    {
        if (_names.TryGetValue(type, out var name))
            return name;
        var created = CreateName(type);
        _names[type] = created;
        return created;
    }

    private string CreateName(IType type)
    {
        if (type.TypeParameterCount < 1)
            return CreatePlainName(type);
        if (TypeFacts.TryGetNullable(type, out var underlying))
            return NameOf(underlying) + "?";
        var arguments = type.TypeArguments.Select(NameOf);
        return type.Name + "❰" + string.Join(", ", arguments) + "❱";
    }

    private string CreatePlainName(IType type)
    {
        if (type is ArrayType array)
            return NameOf(array.ElementType) + "[]";
        if (type is ByReferenceType reference)
            return "&" + NameOf(reference.ElementType);

        var definition = type.GetDefinition();
        if (definition == null)
            return type.Name;
        if (definition.KnownTypeCode != KnownTypeCode.None)
            return KeywordOf(definition.KnownTypeCode, type);
        if (type.DeclaringType != null)
            return type.DeclaringType.Name + '+' + type.Name;
        return type.Name.Replace('<', '❰').Replace('>', '❱');
    }

    private static string KeywordOf(KnownTypeCode code, IType type)
    {
        var keyword = KnownTypeReference.GetCSharpNameByTypeCode(code);
        if (keyword == null)
            return type.Name;
        return keyword;
    }

    /// <summary>A reference to <paramref name="type"/>, labelled with <paramref name="propertyName"/>
    /// for a property's relation, else with the closed generic type for a base type. Records the type
    /// as an outside reference when the diagram leaves it out.</summary>
    public DiagramReference Refer(IType type, string? propertyName = null)
    {
        var (id, openGeneric) = Identify(type);
        var referred = openGeneric ?? type;
        RecordOutside(id, referred);
        if (propertyName != null)
            return new DiagramReference(id, propertyName);
        if (openGeneric == null)
            return new DiagramReference(id, null);
        return new DiagramReference(id, NameOf(type));
    }

    private void RecordOutside(string id, IType type)
    {
        if (IsPicked(type))
            return;
        OutsideReferences.TryAdd(id, type.Namespace + '.' + NameOf(type));
    }
}
