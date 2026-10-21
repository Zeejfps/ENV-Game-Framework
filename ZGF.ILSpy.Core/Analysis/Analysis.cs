using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>One way of relating a symbol to others, such as "Used By", as ILSpy's analyzer
/// offers it: its header and its place among the others.</summary>
public sealed class AnalysisKind
{
    internal AnalysisKind(string header, int order, IAnalyzer analyzer)
    {
        Header = header;
        Order = order;
        Analyzer = analyzer;
    }

    /// <summary>ILSpy's name for it, which also keys its translation.</summary>
    public string Header { get; }

    public int Order { get; }

    internal IAnalyzer Analyzer { get; }

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

    public override string ToString() => Label;
}

/// <summary>
/// ILSpy's analyzer over the open assemblies: the analyses that apply to a symbol, and their
/// results, found by ILSpy's own analyzers.
/// </summary>
public static class Analysis
{
    private static readonly AnalysisKind[] Kinds =
    [
        new("Applied To", 10, new AttributeAppliedToAnalyzer()),
        new("Implemented By", 10, new EventImplementedByAnalyzer()),
        new("Overridden By", 20, new EventOverriddenByAnalyzer()),
        new("Read By", 10, new ReadByFieldAccessAnalyzer()),
        new("Assigned By", 20, new AssignedByFieldAccessAnalyzer()),
        new("Address Taken By", 30, new AddressTakenByFieldAccessAnalyzer()),
        new("Implements", 40, new MemberImplementsInterfaceAnalyzer()),
        new("Overrides", 35, new MemberOverridesAnalyzer()),
        new("Implemented By", 40, new MethodImplementedByAnalyzer()),
        new("Overridden By", 30, new MethodOverriddenByAnalyzer()),
        new("Used By", 20, new MethodUsedByAnalyzer()),
        new("Uses", 10, new MethodUsesAnalyzer()),
        new("Used By", 20, new MethodVirtualUsedByAnalyzer()),
        new("Implemented By", 10, new PropertyImplementedByAnalyzer()),
        new("Overridden By", 20, new PropertyOverriddenByAnalyzer()),
        new("Exposed By", 40, new TypeExposedByAnalyzer()),
        new("Extension Methods", 50, new TypeExtensionMethodsAnalyzer()),
        new("Instantiated By", 20, new TypeInstantiatedByAnalyzer()),
        new("Used By", 30, new TypeUsedByAnalyzer()),
    ];

    /// <summary>The type or member <paramref name="node"/> stands for, or null for any other node.</summary>
    public static AnalysisSymbol? Of(BrowseNode node) => node is EntityNode entity ? Describe(entity.Entity) : null;

    /// <summary>The analyses that apply to <paramref name="symbol"/>, in ILSpy's order.</summary>
    public static IReadOnlyList<AnalysisKind> KindsFor(AnalysisSymbol symbol) =>
        Kinds.Where(k => k.Analyzer.Show(symbol.Symbol)).OrderBy(static k => k.Order).ToArray();

    /// <summary>The accessors of a property or event, which ILSpy analyzes on their own.</summary>
    public static IReadOnlyList<AnalysisSymbol> AccessorsOf(AnalysisSymbol symbol)
    {
        IEnumerable<IMethod?> accessors = symbol.Symbol switch
        {
            IProperty property => [property.Getter, property.Setter],
            IEvent @event => [@event.AddAccessor, @event.RemoveAccessor, @event.InvokeAccessor],
            _ => [],
        };
        return accessors.OfType<IMethod>().Select(static m => Describe(m)).OfType<AnalysisSymbol>().ToArray();
    }

    /// <summary>Runs <paramref name="kind"/> on <paramref name="symbol"/> over the open assemblies,
    /// yielding results as they are found; call it off the UI thread. Results come sorted when the
    /// analysis asks for it, after all are found.</summary>
    public static IEnumerable<AnalysisSymbol> Run(AssemblyList list, AnalysisSymbol symbol, AnalysisKind kind, CancellationToken cancellationToken)
    {
        var context = new AnalyzerContext { AssemblyList = list, CancellationToken = cancellationToken };
        var found = kind.Analyzer.Analyze(symbol.Symbol, context);
        if (context.SortResults)
            return found.Select(Describe).OfType<AnalysisSymbol>().OrderBy(static s => s.Label, StringComparer.OrdinalIgnoreCase).ToList();
        return Lazy(found);

        IEnumerable<AnalysisSymbol> Lazy(IEnumerable<ISymbol> symbols)
        {
            foreach (var result in symbols)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Describe(result) is { } described)
                    yield return described;
            }
        }
    }

    private static AnalysisSymbol? Describe(ISymbol symbol)
    {
        switch (symbol)
        {
            case ITypeDefinition type:
                return new AnalysisSymbol(type, TypeNode.KindOf(type), EntityText.Map(type.Accessibility), EntityText.Label(type),
                    Where(type.DeclaringTypeDefinition is { } outer ? EntityText.FullName(outer) : type.Namespace, type), ReferenceTo(type));
            case IMember member:
                var declaring = member.DeclaringTypeDefinition;
                return new AnalysisSymbol(member, MemberNode.KindOf(member), EntityText.Map(member.Accessibility), EntityText.Label(member),
                    Where(declaring is null ? "" : EntityText.FullName(declaring), member), ReferenceTo(member));
            case IModule module:
                return new AnalysisSymbol(module, NodeKind.Assembly, NodeAccessibility.Public, module.AssemblyName, module.MetadataFile?.FileName ?? "",
                    module.MetadataFile is { } file ? CodeReference.ToAssembly(module.AssemblyName, file.FileName) : null);
            default:
                return null;
        }
    }

    private static string Where(string container, IEntity entity) =>
        entity.ParentModule?.AssemblyName is { } assembly ? container.Length == 0 ? assembly : $"{container} ({assembly})" : container;

    private static CodeReference? ReferenceTo(IEntity entity)
    {
        var file = entity.ParentModule?.MetadataFile;
        if (file is null || entity.MetadataToken.IsNil) return null;
        return CodeReference.ToEntity(DocumentBuilder.ReferenceKindOf(entity.SymbolKind), entity.Name, file.FileName, entity.MetadataToken);
    }
}
