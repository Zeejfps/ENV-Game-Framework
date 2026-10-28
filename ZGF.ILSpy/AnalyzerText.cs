using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>The analyzer's words in the interface language.</summary>
internal static class AnalyzerText
{
    public static string Header(Strings t, AnalysisKind kind) => kind.Header switch
    {
        "Applied To" => t.AnalyzerAppliedTo,
        "Implemented By" => t.AnalyzerImplementedBy,
        "Overridden By" => t.AnalyzerOverriddenBy,
        "Read By" => t.AnalyzerReadBy,
        "Assigned By" => t.AnalyzerAssignedBy,
        "Address Taken By" => t.AnalyzerAddressTakenBy,
        "Implements" => t.AnalyzerImplements,
        "Overrides" => t.AnalyzerOverrides,
        "Used By" => t.AnalyzerUsedBy,
        "Uses" => t.AnalyzerUses,
        "Exposed By" => t.AnalyzerExposedBy,
        "Extension Methods" => t.AnalyzerExtensionMethods,
        "Instantiated By" => t.AnalyzerInstantiatedBy,
        _ => kind.Header,
    };
}
