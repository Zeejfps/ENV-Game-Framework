using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the members whose code uses any accessor of a property or event, each once.</summary>
internal sealed class MemberUsedByAnalyzer : IAnalyzer
{
    private readonly MethodUsedByAnalyzer _accessorUsedBy = new();

    public AnalysisRun? For(ISymbol? symbol)
    {
        if (symbol is not IMember member || member is not (IProperty or IEvent))
            return null;
        if (!Members.Accessors(member).Any())
            return null;
        return context => Analyze(member, context);
    }

    private IEnumerable<ISymbol> Analyze(IMember member, AnalyzerContext context)
    {
        var accessors = Members.Accessors(member);
        return accessors
            .SelectMany(a => _accessorUsedBy.Analyze(a, context))
            .Distinct();
    }
}
