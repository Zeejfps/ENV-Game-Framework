using System.Diagnostics;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Comparison;

/// <summary>The entries of a comparison, built by matching each side's namespaces, types and members by
/// <see cref="ApiKeys"/> and telling them apart by <see cref="ApiDeclarations"/>.</summary>
internal static class ApiEntries
{
    private const string GlobalNamespaceText = "-";
    private const string ModuleTypeName = "<Module>";

    private static readonly Dictionary<string, IEntity> None = new();

    /// <summary>The namespaces either module puts types in that <paramref name="scope"/> takes in, sorted by name.</summary>
    public static List<ApiEntry> CompareNamespaces(MetadataModule left, MetadataModule right, ApiScope scope, CancellationToken cancellationToken)
    {
        var leftTypes = ListTypesByNamespace(left, scope);
        var rightTypes = ListTypesByNamespace(right, scope);
        var names = ListPresence(leftTypes.Keys, rightTypes.Keys)
            .OrderBy(static n => n.Name, NaturalStringComparer.Instance)
            .ToList();
        var entries = new List<ApiEntry>();
        foreach (var (name, presence) in names)
        {
            var leftOnes = leftTypes.GetValueOrDefault(name) ?? None;
            var rightOnes = rightTypes.GetValueOrDefault(name) ?? None;
            var children = CompareAll(leftOnes, rightOnes, scope, cancellationToken);
            entries.Add(new ApiEntry.Namespace(NamespaceText(name), presence, children));
        }
        return entries;
    }

    private static List<(string Name, ApiPresence Presence)> ListPresence(IReadOnlyCollection<string> left, IReadOnlyCollection<string> right)
    {
        var rightSet = right.ToHashSet(StringComparer.Ordinal);
        var leftSet = left.ToHashSet(StringComparer.Ordinal);
        var names = new List<(string, ApiPresence)>();
        foreach (var name in left)
            names.Add((name, rightSet.Contains(name) ? ApiPresence.Both : ApiPresence.LeftOnly));
        foreach (var name in right)
        {
            if (!leftSet.Contains(name))
                names.Add((name, ApiPresence.RightOnly));
        }
        return names;
    }

    /// <summary>The top-level types of <paramref name="module"/> that <paramref name="scope"/> takes in, by
    /// namespace, each by its key.</summary>
    private static Dictionary<string, Dictionary<string, IEntity>> ListTypesByNamespace(MetadataModule module, ApiScope scope)
    {
        var byNamespace = new Dictionary<string, Dictionary<string, IEntity>>(StringComparer.Ordinal);
        foreach (var type in module.TopLevelTypeDefinitions)
        {
            if (IsModuleType(type))
                continue;
            if (!IsTakenIn(type, scope))
                continue;
            var types = FindOrAddNamespace(byNamespace, type.Namespace);
            AddByKey(types, type);
        }
        return byNamespace;
    }

    /// <summary>Adds <paramref name="entity"/> under its key, numbered when others took the key before
    /// it, so none is dropped; those that share a key are matched in the order the metadata lists them.</summary>
    private static void AddByKey(Dictionary<string, IEntity> entities, IEntity entity)
    {
        var key = ApiKeys.Of(entity);
        var candidate = key;
        var number = 2;
        while (!entities.TryAdd(candidate, entity))
        {
            candidate = $"{key}#{number}";
            number++;
        }
    }

    /// <summary>Whether <paramref name="type"/> is the type every module has to hold its global members,
    /// which C# never declares.</summary>
    private static bool IsModuleType(ITypeDefinition type)
    {
        if (type.Namespace.Length > 0)
            return false;
        return type.Name == ModuleTypeName;
    }

    /// <summary>Whether <paramref name="scope"/> takes in <paramref name="entity"/>: never what has no
    /// metadata of its own or what the compiler made up.</summary>
    private static bool IsTakenIn(IEntity entity, ApiScope scope)
    {
        if (Members.IsHidden(entity))
            return false;
        if (scope == ApiScope.AllDeclared)
            return true;
        return EntityText.IsPublicApi(entity);
    }

    private static Dictionary<string, IEntity> FindOrAddNamespace(Dictionary<string, Dictionary<string, IEntity>> byNamespace, string name)
    {
        if (byNamespace.TryGetValue(name, out var types))
            return types;
        types = new Dictionary<string, IEntity>(StringComparer.Ordinal);
        byNamespace.Add(name, types);
        return types;
    }

    private static string NamespaceText(string name)
    {
        if (name.Length == 0)
            return GlobalNamespaceText;
        return name;
    }

    /// <summary>The entities of <paramref name="left"/> and <paramref name="right"/>, matched by key,
    /// sorted as the tree sorts them: types, then fields, properties, events and methods, each by name.</summary>
    private static List<ApiEntry> CompareAll(Dictionary<string, IEntity> left, Dictionary<string, IEntity> right, ApiScope scope,
        CancellationToken cancellationToken)
    {
        var entries = new List<ApiEntry>(left.Count + right.Count);
        foreach (var (key, leftOne) in left)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (right.TryGetValue(key, out var rightOne))
                entries.Add(CompareBoth(leftOne, rightOne, scope, cancellationToken));
            else
                entries.Add(Removed(leftOne, scope, cancellationToken));
        }
        foreach (var (key, rightOne) in right)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!left.ContainsKey(key))
                entries.Add(Added(rightOne, scope, cancellationToken));
        }
        return entries
            .OrderBy(static e => SortGroup(e.Shape))
            .ThenBy(static e => e.Text, NaturalStringComparer.Instance)
            .ToList();
    }

    private static ApiEntry Removed(IEntity left, ApiScope scope, CancellationToken cancellationToken)
    {
        var children = CompareAll(ListChildren(left, scope), None, scope, cancellationToken);
        return EntryOf(left, new ApiSides.LeftOnly(SideOf(left)), children);
    }

    private static ApiEntry Added(IEntity right, ApiScope scope, CancellationToken cancellationToken)
    {
        var children = CompareAll(None, ListChildren(right, scope), scope, cancellationToken);
        return EntryOf(right, new ApiSides.RightOnly(SideOf(right)), children);
    }

    /// <summary>The entry of an entity on both sides, which <paramref name="right"/> labels.</summary>
    private static ApiEntry CompareBoth(IEntity left, IEntity right, ApiScope scope, CancellationToken cancellationToken)
    {
        var children = CompareAll(ListChildren(left, scope), ListChildren(right, scope), scope, cancellationToken);
        return EntryOf(right, new ApiSides.Both(SideOf(left), SideOf(right)), children);
    }

    private static ApiEntry EntryOf(IEntity shown, ApiSides sides, List<ApiEntry> children)
    {
        var shape = EntityText.SymbolShapeOf(shown);
        var accessibility = EntityText.Map(shown.Accessibility);
        return new ApiEntry.Entity(shape, EntityText.Label(shown), accessibility, sides, children);
    }

    private static ApiSide SideOf(IEntity entity) => new(CodeReference.To(entity), ApiDeclarations.Of(entity));

    /// <summary>The nested types and members of <paramref name="entity"/> that <paramref name="scope"/>
    /// takes in, each by its key, without accessors or an enum's value field; none for a member.</summary>
    private static Dictionary<string, IEntity> ListChildren(IEntity entity, ApiScope scope)
    {
        if (entity is not ITypeDefinition type)
            return None;
        var children = new Dictionary<string, IEntity>(StringComparer.Ordinal);
        foreach (var member in ListMembers(type))
        {
            if (IsTakenIn(member, scope))
                AddByKey(children, member);
        }
        return children;
    }

    private static IEnumerable<IEntity> ListMembers(ITypeDefinition type)
    {
        var fields = type.Fields.Where(f => !IsEnumValueField(type, f));
        var methods = type.Methods.Where(static m => !m.IsAccessor);
        return type.NestedTypes
            .Concat<IEntity>(fields)
            .Concat(type.Properties)
            .Concat(type.Events)
            .Concat(methods);
    }

    /// <summary>Whether <paramref name="field"/> holds the value of the enum <paramref name="type"/>.</summary>
    private static bool IsEnumValueField(ITypeDefinition type, IField field)
    {
        if (type.Kind != TypeKind.Enum)
            return false;
        return !field.IsStatic;
    }

    /// <summary>Where entries of <paramref name="shape"/> sort among a type's: nested types first, then
    /// fields, properties, events and methods; namespaces among themselves.</summary>
    private static int SortGroup(SymbolShape shape) =>
        shape switch
        {
            SymbolShape.Member member => member.Shape switch
            {
                MemberShape.Field or MemberShape.Constant => 1,
                MemberShape.Property or MemberShape.Indexer => 2,
                MemberShape.Event => 3,
                MemberShape.Method or MemberShape.Constructor or MemberShape.Operator => 4,
                _ => throw new UnreachableException(),
            },
            SymbolShape.Type or SymbolShape.Namespace or SymbolShape.Assembly => 0,
            _ => throw new UnreachableException(),
        };

    /// <summary>How many types and members under <paramref name="entries"/> changed as <paramref name="change"/>
    /// says, counting an added or removed one alone, not with what it holds.</summary>
    public static int Count(IReadOnlyList<ApiEntry> entries, ApiChange change)
    {
        var count = 0;
        foreach (var entry in entries)
            count += Count(entry, change);
        return count;
    }

    private static int Count(ApiEntry entry, ApiChange change)
    {
        if (entry is not ApiEntry.Entity)
            return Count(entry.Children, change);
        var own = entry.Change == change ? 1 : 0;
        if (entry.Change is ApiChange.Added or ApiChange.Removed)
            return own;
        return own + Count(entry.Children, change);
    }
}
