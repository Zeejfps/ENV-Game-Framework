using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the members whose code uses any accessor of a property or event, each once.</summary>
internal sealed class MemberUsedByAnalyzer : IAnalyzer
{
    private readonly MethodUsedByAnalyzer _accessorUsedBy = new();

    public bool Show(ISymbol? symbol)
    {
        if (symbol is not (IProperty or IEvent))
            return false;
        return Members.Accessors((IEntity)symbol).Any();
    }

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
    {
        var accessors = Members.Accessors((IEntity)analyzedSymbol);
        return accessors
            .SelectMany(a => _accessorUsedBy.Analyze(a, context))
            .Distinct();
    }
}
