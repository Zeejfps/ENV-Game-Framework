using System.Reflection.Metadata.Ecma335;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>One way of relating a symbol to others, such as "Used By", as ILSpy's analyzer
/// offers it: its header and its place among the others.</summary>
public sealed class AnalysisKind
{
    internal AnalysisKind(string header, int order, IAnalyzer analyzer, bool sortsResults = false)
    {
        Header = header;
        Order = order;
        Analyzer = analyzer;
        SortsResults = sortsResults;
    }

    /// <summary>ILSpy's name for it, which also keys its translation.</summary>
    public string Header { get; }

    public int Order { get; }

    internal IAnalyzer Analyzer { get; }

    /// <summary>Whether its results read best sorted by label, so come only once all are found.</summary>
    internal bool SortsResults { get; }

    public override string ToString() => Header;
}

/// <summary>A type, member or assembly an analysis is about or found: what a row shows for it and
/// how to go to it.</summary>
public sealed class AnalysisSymbol
{
    internal AnalysisSymbol(ISymbol symbol, NodeKind kind, NodeAccessibility accessibility, string label, string location, CodeReference? reference)
    {
        Symbol = symbol;
        Kind = kind;
        Accessibility = accessibility;
        Label = label;
        Location = location;
        Reference = reference;
    }

    internal ISymbol Symbol { get; }

    public NodeKind Kind { get; }

    public NodeAccessibility Accessibility { get; }

    /// <summary>Its name as the tree would show it.</summary>
    public string Label { get; }

    /// <summary>Where it is: the declaring type, or for a type its namespace, and the assembly.</summary>
    public string Location { get; }

    /// <summary>What to navigate to it by; null when it cannot be shown.</summary>
    public CodeReference? Reference { get; }

    /// <summary>Its assembly file and metadata token, to find it again with
    /// <see cref="Analyzers.Find"/> in a later run; null for an assembly.</summary>
    public CodeAnchor? Definition =>
        Symbol is IEntity { ParentModule.MetadataFile: { } file } entity && !entity.MetadataToken.IsNil
            ? new CodeAnchor(CodeAnchorKind.Definition, file.FileName, MetadataTokens.GetToken(entity.MetadataToken))
            : null;

    public override string ToString() => Label;
}

/// <summary>
/// ILSpy's analyzer over the open assemblies: the analyses that apply to a symbol, and their
/// results, found by ILSpy's own analyzers.
/// </summary>
public static class Analyzers
{
    private static readonly AnalysisKind[] Kinds =
    [
        new("Applied To", 10, new AttributeAppliedToAnalyzer()),
        new("Implemented By", 10, new InheritorsAnalyzer<IEvent>(overrides: false)),
        new("Overridden By", 20, new InheritorsAnalyzer<IEvent>(overrides: true)),
        new("Read By", 10, new FieldAccessAnalyzer(FieldAccessKind.Read)),
        new("Assigned By", 20, new FieldAccessAnalyzer(FieldAccessKind.Write)),
        new("Address Taken By", 30, new FieldAccessAnalyzer(FieldAccessKind.AddressOf)),
        new("Implements", 40, new MemberImplementsInterfaceAnalyzer()),
        new("Overrides", 35, new MemberOverridesAnalyzer()),
        new("Implemented By", 40, new InheritorsAnalyzer<IMethod>(overrides: false)),
        new("Overridden By", 30, new InheritorsAnalyzer<IMethod>(overrides: true)),
        new("Used By", 20, new MethodUsedByAnalyzer()),
        new("Uses", 10, new MethodUsesAnalyzer()),
        new("Used By", 5, new MemberUsedByAnalyzer()),
        new("Implemented By", 10, new InheritorsAnalyzer<IProperty>(overrides: false)),
        new("Overridden By", 20, new InheritorsAnalyzer<IProperty>(overrides: true)),
        new("Exposed By", 40, new TypeExposedByAnalyzer()),
        new("Extension Methods", 50, new TypeExtensionMethodsAnalyzer()),
        new("Instantiated By", 20, new TypeInstantiatedByAnalyzer()),
        new("Used By", 30, new TypeUsedByAnalyzer(), sortsResults: true),
    ];

    /// <summary>The type or member <paramref name="node"/> stands for — for a base or derived type
    /// entry the type it names — or null for any other node.</summary>
    public static AnalysisSymbol? Of(BrowseNode node) =>
        node switch
        {
            EntityNode entity => Describe(entity.Entity),
            DerivedTypeNode derived => Describe(derived.Type),
            BaseTypeNode { Type: var type } when type.GetDefinition() is { } definition => Describe(definition),
            _ => null,
        };

    /// <summary>The type or member <paramref name="definition"/> names, loading its assembly's file
    /// without listing it; null when the file or the entity is gone.</summary>
    public static AnalysisSymbol? Find(AssemblyList list, CodeAnchor definition)
    {
        try
        {
            if (list.TryLoadAssembly(definition.FilePath) is not { } assembly) return null;
            return assembly.MainModule.ResolveEntity(MetadataTokens.EntityHandle(definition.Token)) is { } entity ? Describe(entity) : null;
        }
        catch (Exception e) when (e is ArgumentException or BadImageFormatException or InvalidOperationException or IOException)
        {
            return null;
        }
    }

    /// <summary>The analyses that apply to <paramref name="symbol"/>, in ILSpy's order.</summary>
    public static IReadOnlyList<AnalysisKind> KindsFor(AnalysisSymbol symbol) =>
        Kinds.Where(k => k.Analyzer.Show(symbol.Symbol)).OrderBy(static k => k.Order).ToArray();

    /// <summary>The accessors of a property or event, which ILSpy analyzes on their own.</summary>
    public static IReadOnlyList<AnalysisSymbol> AccessorsOf(AnalysisSymbol symbol) =>
        symbol.Symbol is IEntity entity ? Members.Accessors(entity).Select(static m => Describe(m)!).ToArray() : [];

    /// <summary>Runs <paramref name="kind"/> on <paramref name="symbol"/> over the open assemblies on
    /// the thread pool, yielding results in batches as they are found. Results come sorted when the
    /// analysis asks for it, after all are found.</summary>
    public static IAsyncEnumerable<IReadOnlyList<AnalysisSymbol>> RunAsync(AssemblyList list, AnalysisSymbol symbol, AnalysisKind kind,
        CancellationToken cancellationToken = default) =>
        Batches.Stream<AnalysisSymbol>((emit, token) =>
        {
            var found = kind.Analyzer.Analyze(symbol.Symbol, new AnalyzerContext { AssemblyList = list, CancellationToken = token })
                .Select(Describe).OfType<AnalysisSymbol>();
            if (kind.SortsResults)
                found = found.OrderBy(static s => s.Label, StringComparer.OrdinalIgnoreCase);
            foreach (var result in found)
            {
                token.ThrowIfCancellationRequested();
                emit(result);
            }
        }, cancellationToken);

    private static AnalysisSymbol? Describe(ISymbol symbol)
    {
        switch (symbol)
        {
            case IEntity entity:
                return new AnalysisSymbol(entity, EntityText.KindOf(entity), EntityText.Map(entity.Accessibility), EntityText.Label(entity),
                    Where(entity), CodeReference.To(entity));
            case IModule module:
                return new AnalysisSymbol(module, NodeKind.Assembly, NodeAccessibility.Public, module.AssemblyName, module.MetadataFile?.FileName ?? "",
                    module.MetadataFile is { } file ? CodeReference.ToAssembly(module.AssemblyName, file.FileName) : null);
            default:
                return null;
        }
    }

    private static string Where(IEntity entity)
    {
        var container = entity.DeclaringTypeDefinition is { } outer ? EntityText.FullName(outer) : entity is ITypeDefinition type ? type.Namespace : "";
        return entity.ParentModule?.AssemblyName is { } assembly ? container.Length == 0 ? assembly : $"{container} ({assembly})" : container;
    }
}
