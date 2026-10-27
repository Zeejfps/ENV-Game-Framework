namespace ZGF.ILSpy.Core.Comparison;

/// <summary>Which types and members a comparison takes in; never those the compiler made up.</summary>
public enum ApiScope
{
    /// <summary>Only what other assemblies can use.</summary>
    PublicApi,

    /// <summary>Everything the source declared, public or not.</summary>
    AllDeclared,
}

/// <summary>How an entry differs from the left assembly to the right one.</summary>
public enum ApiChange
{
    Unchanged,

    /// <summary>On the right only.</summary>
    Added,

    /// <summary>On the left only.</summary>
    Removed,

    /// <summary>In both, declared differently: its accessibility, modifiers, type, base types, value, or
    /// whether it is obsolete or flags.</summary>
    Changed,
}

/// <summary>A namespace, type or member of either assembly compared, labelled as the tree labels it,
/// with what it points at and how it is declared on each side it is on, and what it holds, merged from
/// both sides and sorted as the tree sorts them.</summary>
public sealed class ApiEntry
{
    public required NodeKind Kind { get; init; }

    public required string Text { get; init; }

    public NodeAccessibility Accessibility { get; init; }

    public required ApiChange Change { get; init; }

    /// <summary>Whether it, or anything it holds, differs.</summary>
    public required bool HasChanges { get; init; }

    public CodeReference? Left { get; init; }

    public CodeReference? Right { get; init; }

    /// <summary>How the left assembly declares it, as C# writes it; null for a namespace or what it lacks.</summary>
    public string? LeftDeclaration { get; init; }

    /// <summary>How the right assembly declares it, as C# writes it; null for a namespace or what it lacks.</summary>
    public string? RightDeclaration { get; init; }

    public required IReadOnlyList<ApiEntry> Children { get; init; }
}

/// <summary>What changed in the API from a left assembly to a right one: their namespaces, merged
/// from both and sorted by name, and how many types and members were added, removed and changed, each
/// counted once with what it holds.</summary>
public sealed class ApiDiff
{
    private ApiDiff(string leftName, string rightName, ApiScope scope, IReadOnlyList<ApiEntry> namespaces)
    {
        LeftName = leftName;
        RightName = rightName;
        Scope = scope;
        Namespaces = namespaces;
        Added = ApiEntries.Count(namespaces, ApiChange.Added);
        Removed = ApiEntries.Count(namespaces, ApiChange.Removed);
        Changed = ApiEntries.Count(namespaces, ApiChange.Changed);
    }

    /// <summary>The left assembly's name and version.</summary>
    public string LeftName { get; }

    /// <summary>The right assembly's name and version.</summary>
    public string RightName { get; }

    public ApiScope Scope { get; }

    public IReadOnlyList<ApiEntry> Namespaces { get; }

    public int Added { get; }

    public int Removed { get; }

    public int Changed { get; }

    /// <summary>Compares the API of <paramref name="left"/> with that of <paramref name="right"/>, taking in
    /// what <paramref name="scope"/> says. Reads both assemblies' types: call it off the UI thread.</summary>
    public static ApiDiff Compare(LoadedAssembly left, LoadedAssembly right, ApiScope scope, CancellationToken cancellationToken)
    {
        var namespaces = ApiEntries.CompareNamespaces(left.Module, right.Module, scope, cancellationToken);
        return new ApiDiff(left.NameAndVersion, right.NameAndVersion, scope, namespaces);
    }
}
