using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

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
    /// assembly if it is not, and analyzes it, unless the list was cleared meanwhile.</summary>
    public static async void Analyze(this Workspace w, CodeReference reference)
    {
        var generation = w.Assemblies.Generation;
        var found = await Background.Run(() => w.Assemblies.FindNode(reference));
        if (w.WasListCleared(generation))
            return;
        var node = found.Value;
        if (node == null)
        {
            w.ReportUnresolved(reference, found.Error);
            return;
        }
        w.Analyze(node);
    }

    /// <summary>Finds the symbols of the roots restored from the saved analyses in the background,
    /// opening their assemblies' files without listing them, and turns each root into the analysis of
    /// its symbol, or removes it when its symbol is gone, unless the list was cleared meanwhile.
    /// Reports why the symbols could not be found.</summary>
    public static async void RestoreAnalyses(this Workspace w)
    {
        var restoring = w.Analyzer.ListRestoring();
        if (restoring.Count == 0)
            return;
        var analyzed = restoring.ConvertAll(static r => r.SavedAs!.Value);
        var generation = w.Assemblies.Generation;
        var found = await Background.Run(() => w.FindAnalyzed(analyzed));
        if (w.WasListCleared(generation))
            return;
        w.ReportRestoreFailure(found.Error);
        w.AnalyzeAgain(restoring, found.Value);
    }

    private static bool WasListCleared(this Workspace w, int generation) => w.Assemblies.Generation != generation;

    private static List<AnalysisSymbol?> FindAnalyzed(this Workspace w, List<CodeAnchor> analyzed)
    {
        return analyzed.ConvertAll(a => Analysis.Find(w.Assemblies, a));
    }

    private static void ReportRestoreFailure(this Workspace w, Exception? error)
    {
        if (error == null)
            return;
        w.ReportError(Strings.For(w.Locale).ErrorRestoreAnalyses, error.ToString());
    }

    private static void AnalyzeAgain(this Workspace w, List<AnalyzerPendingNode> restoring, List<AnalysisSymbol?>? found)
    {
        for (var i = 0; i < restoring.Count; i++)
        {
            var symbol = FoundAt(found, i);
            w.AnalyzeAgain(restoring[i], symbol);
        }
    }

    private static AnalysisSymbol? FoundAt(List<AnalysisSymbol?>? found, int index)
    {
        if (found == null)
            return null;
        return found[index];
    }

    private static void AnalyzeAgain(this Workspace w, AnalyzerPendingNode root, AnalysisSymbol? symbol)
    {
        if (symbol == null)
        {
            w.Analyzer.Remove(root);
            return;
        }
        w.Analyzer.Resolve(root, symbol);
    }
}
