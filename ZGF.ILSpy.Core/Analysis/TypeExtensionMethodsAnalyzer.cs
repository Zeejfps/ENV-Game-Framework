// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Finds all extension methods defined for a type.</summary>
internal sealed class TypeExtensionMethodsAnalyzer : IAnalyzer
{
    public AnalysisRun? For(ISymbol? symbol)
    {
        if (symbol is not ITypeDefinition type || type.IsStatic)
            return null;
        return context => Analyze(type, context);
    }

    private static IEnumerable<ISymbol> Analyze(ITypeDefinition analyzed, AnalyzerContext context)
    {
        if (analyzed.ParentModule?.MetadataFile == null)
            yield break;
        foreach (var type in context.GetScopeOf(analyzed).GetTypesInScope(context.CancellationToken))
        {
            if (!type.HasExtensions)
                continue;
            foreach (var method in type.Methods)
            {
                if (!method.IsExtensionMethod)
                    continue;
                var extended = method.Parameters[0].Type.GetDefinition();
                if (extended != null && extended.IsSame(analyzed))
                    yield return method;
            }
        }
    }
}
