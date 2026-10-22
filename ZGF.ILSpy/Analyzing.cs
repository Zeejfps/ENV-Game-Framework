using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;

namespace ZGF.ILSpy;

/// <summary>Analyzing types and members, and analyzing again what was analyzed in the last run.</summary>
public static class Analyzing
{
    /// <summary>Analyzes the type or member <paramref name="node"/> stands for in the analyzer, which is
    /// opened if it is not.</summary>
    public static void Analyze(this Workspace w, BrowseNode node)
    {
        var symbol = Analysis.Of(node);
        if (symbol == null)
            return;
        w.OpenAnalyzer();
        w.Analyzer.Analyze(symbol);
    }

    /// <summary>Finds what <paramref name="reference"/> points at in the background, opening its
    /// assembly if it is not, and analyzes it.</summary>
    public static void Analyze(this Workspace w, CodeReference reference)
    {
        w.RunInBackground(() => w.Assemblies.FindNode(reference), (node, error) => w.OnAnalyzeTargetFound(reference, node, error));
    }

    private static void OnAnalyzeTargetFound(this Workspace w, CodeReference reference, BrowseNode? node, Exception? error)
    {
        if (node == null)
        {
            w.ReportUnresolved(reference, error);
            return;
        }
        w.Analyze(node);
    }

    /// <summary>Finds the symbols analyzed in the last run again in the background, opening their
    /// assemblies' files without listing them, and analyzes those still there, in their order.</summary>
    public static void RestoreLastSessionAnalyses(this Workspace w)
    {
        var analyzed = w.LastSessionAnalyses.StartRestoring();
        if (analyzed == null)
            return;
        w.RunInBackground(() => w.FindAnalyzed(analyzed), (found, _) => w.OnAnalyzedFound(found));
    }

    private static List<AnalysisSymbol?> FindAnalyzed(this Workspace w, List<CodeAnchor> analyzed)
    {
        return analyzed.ConvertAll(a => Analysis.Find(w.Assemblies, a));
    }

    private static void OnAnalyzedFound(this Workspace w, List<AnalysisSymbol?>? found)
    {
        w.LastSessionAnalyses.FinishRestoring();
        if (found != null)
            w.AnalyzeAll(found);
        w.LastSave.RecordAnalyzer(w.Analyzer);
    }

    private static void AnalyzeAll(this Workspace w, List<AnalysisSymbol?> symbols)
    {
        foreach (var symbol in symbols)
        {
            if (symbol != null)
                w.Analyzer.Analyze(symbol);
        }
    }
}
