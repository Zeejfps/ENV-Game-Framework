using System.Diagnostics;

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

/// <summary>Which of the assemblies compared a namespace is in.</summary>
public enum ApiPresence
{
    LeftOnly,
    RightOnly,
    Both,
}

/// <summary>A type or member as one assembly has it: what it points at, and how it is declared, as C# writes it.</summary>
public sealed record ApiSide(CodeReference Reference, string Declaration);

/// <summary>Which of the assemblies compared a type or member is in, and how each has it.</summary>
public abstract record ApiSides
{
    private ApiSides()
    {
    }

    public sealed record LeftOnly(ApiSide Left) : ApiSides;

    public sealed record RightOnly(ApiSide Right) : ApiSides;

    public sealed record Both(ApiSide Left, ApiSide Right) : ApiSides;
}

/// <summary>A namespace, type or member of either assembly compared, labelled as the tree labels it,
/// and what it holds, merged from both sides and sorted as the tree sorts them.</summary>
public abstract class ApiEntry
{
    private ApiEntry(string text, ApiChange change, IReadOnlyList<ApiEntry> children)
    {
        Text = text;
        Change = change;
        Children = children;
        HasChanges = change != ApiChange.Unchanged || children.Any(static c => c.HasChanges);
    }

    /// <summary>What the entry is: a namespace, or a type or member of some shape.</summary>
    public abstract SymbolShape Shape { get; }

    public string Text { get; }

    public abstract NodeAccessibility Accessibility { get; }

    public ApiChange Change { get; }

    /// <summary>Whether it, or anything it holds, differs.</summary>
    public bool HasChanges { get; }

    public IReadOnlyList<ApiEntry> Children { get; }

    public sealed class Namespace(string text, ApiPresence presence, IReadOnlyList<ApiEntry> children)
        : ApiEntry(text, ChangeOf(presence), children)
    {
        public override SymbolShape Shape => new SymbolShape.Namespace();

        public override NodeAccessibility Accessibility => NodeAccessibility.None;

        public ApiPresence Presence => presence;

        private static ApiChange ChangeOf(ApiPresence presence) =>
            presence switch
            {
                ApiPresence.LeftOnly => ApiChange.Removed,
                ApiPresence.RightOnly => ApiChange.Added,
                ApiPresence.Both => ApiChange.Unchanged,
                _ => throw new UnreachableException(),
            };
    }

    public sealed class Entity(SymbolShape shape, string text, NodeAccessibility accessibility, ApiSides sides, IReadOnlyList<ApiEntry> children)
        : ApiEntry(text, ChangeOf(sides), children)
    {
        public override SymbolShape Shape => shape;

        public override NodeAccessibility Accessibility => accessibility;

        public ApiSides Sides => sides;

        private static ApiChange ChangeOf(ApiSides sides) =>
            sides switch
            {
                ApiSides.LeftOnly => ApiChange.Removed,
                ApiSides.RightOnly => ApiChange.Added,
                ApiSides.Both both => ChangeOf(both),
                _ => throw new UnreachableException(),
            };

        private static ApiChange ChangeOf(ApiSides.Both sides)
        {
            if (sides.Left.Declaration != sides.Right.Declaration)
                return ApiChange.Changed;
            return ApiChange.Unchanged;
        }
    }
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
    public static ApiDiff Compare(LoadedModule left, LoadedModule right, ApiScope scope, CancellationToken cancellationToken)
    {
        var namespaces = ApiEntries.CompareNamespaces(left.MainModule, right.MainModule, scope, cancellationToken);
        return new ApiDiff(left.NameAndVersion, right.NameAndVersion, scope, namespaces);
    }
}
