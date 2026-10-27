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
        var names = leftTypes.Keys
            .Union(rightTypes.Keys)
            .Order(NaturalStringComparer.Instance)
            .ToList();
        var entries = new List<ApiEntry>();
        foreach (var name in names)
        {
            var leftOnes = leftTypes.GetValueOrDefault(name);
            var rightOnes = rightTypes.GetValueOrDefault(name);
            entries.Add(CompareNamespace(name, leftOnes, rightOnes, scope, cancellationToken));
        }
        return entries;
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
            types.Add(ApiKeys.Of(type), type);
        }
        return byNamespace;
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

    private static ApiEntry CompareNamespace(string name, Dictionary<string, IEntity>? left, Dictionary<string, IEntity>? right, ApiScope scope,
        CancellationToken cancellationToken)
    {
        var leftTypes = left ?? None;
        var rightTypes = right ?? None;
        var children = CompareAll(leftTypes, rightTypes, scope, cancellationToken);
        var change = ChangeOfNamespace(left, right);
        return new ApiEntry
        {
            Kind = NodeKind.Namespace,
            Text = NamespaceText(name),
            Change = change,
            HasChanges = HasChanges(change, children),
            Children = children,
        };
    }

    private static ApiChange ChangeOfNamespace(Dictionary<string, IEntity>? left, Dictionary<string, IEntity>? right)
    {
        if (left == null)
            return ApiChange.Added;
        if (right == null)
            return ApiChange.Removed;
        return ApiChange.Unchanged;
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
        var keys = left.Keys
            .Union(right.Keys)
            .ToList();
        var entries = new List<ApiEntry>(keys.Count);
        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var leftOne = left.GetValueOrDefault(key);
            var rightOne = right.GetValueOrDefault(key);
            entries.Add(Compare(leftOne, rightOne, scope, cancellationToken));
        }
        return entries
            .OrderBy(static e => SortGroup(e.Kind))
            .ThenBy(static e => e.Text, NaturalStringComparer.Instance)
            .ToList();
    }

    /// <summary>The entry of an entity on either side or both, which <paramref name="right"/> labels
    /// when it is there.</summary>
    private static ApiEntry Compare(IEntity? left, IEntity? right, ApiScope scope, CancellationToken cancellationToken)
    {
        var shown = right ?? left!;
        var leftChildren = ListChildren(left, scope);
        var rightChildren = ListChildren(right, scope);
        var children = CompareAll(leftChildren, rightChildren, scope, cancellationToken);
        var leftDeclaration = ApiDeclarations.Of(left);
        var rightDeclaration = ApiDeclarations.Of(right);
        var change = ChangeOf(leftDeclaration, rightDeclaration);
        return new ApiEntry
        {
            Kind = EntityText.KindOf(shown),
            Text = EntityText.Label(shown),
            Accessibility = EntityText.Map(shown.Accessibility),
            Change = change,
            HasChanges = HasChanges(change, children),
            Left = ReferTo(left),
            Right = ReferTo(right),
            LeftDeclaration = leftDeclaration,
            RightDeclaration = rightDeclaration,
            Children = children,
        };
    }

    /// <summary>The nested types and members of <paramref name="entity"/> that <paramref name="scope"/>
    /// takes in, each by its key, without accessors or an enum's value field; none for a member or nothing.</summary>
    private static Dictionary<string, IEntity> ListChildren(IEntity? entity, ApiScope scope)
    {
        if (entity is not ITypeDefinition type)
            return None;
        var children = new Dictionary<string, IEntity>(StringComparer.Ordinal);
        foreach (var member in ListMembers(type))
        {
            if (IsTakenIn(member, scope))
                children.TryAdd(ApiKeys.Of(member), member);
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

    private static ApiChange ChangeOf(string? leftDeclaration, string? rightDeclaration)
    {
        if (leftDeclaration == null)
            return ApiChange.Added;
        if (rightDeclaration == null)
            return ApiChange.Removed;
        if (leftDeclaration != rightDeclaration)
            return ApiChange.Changed;
        return ApiChange.Unchanged;
    }

    private static bool HasChanges(ApiChange change, List<ApiEntry> children)
    {
        if (change != ApiChange.Unchanged)
            return true;
        return children.Exists(static c => c.HasChanges);
    }

    private static CodeReference? ReferTo(IEntity? entity)
    {
        if (entity == null)
            return null;
        return CodeReference.To(entity);
    }

    /// <summary>Where entries of <paramref name="kind"/> sort among a type's: nested types first, then
    /// fields, properties, events and methods.</summary>
    private static int SortGroup(NodeKind kind) =>
        kind switch
        {
            NodeKind.Field or NodeKind.Constant => 1,
            NodeKind.Property or NodeKind.Indexer => 2,
            NodeKind.Event => 3,
            NodeKind.Method or NodeKind.Constructor or NodeKind.Operator => 4,
            _ => 0,
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
        var own = CountOwn(entry, change);
        if (IsAddedOrRemovedEntity(entry))
            return own;
        return own + Count(entry.Children, change);
    }

    private static int CountOwn(ApiEntry entry, ApiChange change)
    {
        if (entry.Kind == NodeKind.Namespace)
            return 0;
        if (entry.Change != change)
            return 0;
        return 1;
    }

    private static bool IsAddedOrRemovedEntity(ApiEntry entry)
    {
        if (entry.Kind == NodeKind.Namespace)
            return false;
        return entry.Change is ApiChange.Added or ApiChange.Removed;
    }
}
