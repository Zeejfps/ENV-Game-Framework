// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Finds the symbols related to an analyzed one in a particular way: its users, its
/// overrides, and so on.</summary>
internal interface IAnalyzer
{
    /// <summary>The analysis of <paramref name="symbol"/>; null when it does not apply to it, or for null.</summary>
    AnalysisRun? For(ISymbol? symbol);
}

/// <summary>An analysis of one symbol, run against the open assemblies.</summary>
internal delegate IEnumerable<ISymbol> AnalysisRun(AnalyzerContext context);

/// <summary>What an analysis runs against: the open assemblies and cancellation.</summary>
internal sealed class AnalyzerContext
{
    private (MetadataFile File, EntityHandle Handle, CodeMappingInfo Mapping)? _mapping;

    public required AssemblyList AssemblyList { get; init; }

    public CancellationToken CancellationToken { get; init; }

    public AnalyzerScope GetScopeOf(IEntity entity) => new(AssemblyList, entity);

    public MethodBodyBlock? GetMethodBody(IMethod method)
    {
        var module = method.ParentModule?.MetadataFile;
        if (!method.HasBody || method.MetadataToken.IsNil || module == null)
            return null;
        return module.ReadBody((MethodDefinitionHandle)method.MetadataToken);
    }

    /// <summary>How C# maps the compiler-generated code around <paramref name="entity"/> to the
    /// methods it came from; the last one asked for is kept.</summary>
    public CodeMappingInfo MappingOf(IEntity entity)
    {
        var file = entity.ParentModule!.MetadataFile!;
        var mapping = _mapping;
        if (mapping == null || mapping.Value.File != file || mapping.Value.Handle != entity.MetadataToken)
        {
            mapping = (file, entity.MetadataToken, CSharpDecompiler.GetCodeMappingInfo(file, entity.MetadataToken));
            _mapping = mapping;
        }
        return mapping.Value.Mapping;
    }
}
