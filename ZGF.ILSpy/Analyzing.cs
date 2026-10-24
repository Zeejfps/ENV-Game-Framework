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

    /// <summary>Finds the symbols the open list had analyzed again in the background, opening their
    /// assemblies' files without listing them, and analyzes those still there, in their order, unless
    /// the list was cleared meanwhile. Reports why they could not be found.</summary>
    public static async void RestoreAnalyses(this Workspace w)
    {
        var analyzed = w.PendingAnalyses.Pending;
        if (analyzed == null)
            return;
        var generation = w.Assemblies.Generation;
        var found = await Background.Run(() => w.FindAnalyzed(analyzed));
        if (w.WasListCleared(generation))
            return;
        w.ReportRestoreFailure(found.Error);
        w.AnalyzeAgain(found.Value);
    }

    private static void ReportRestoreFailure(this Workspace w, Exception? error)
    {
        if (error == null)
            return;
        w.ReportError(Strings.For(w.Locale).ErrorRestoreAnalyses, error.ToString());
    }

    private static bool WasListCleared(this Workspace w, int generation) => w.Assemblies.Generation != generation;

    private static List<AnalysisSymbol?> FindAnalyzed(this Workspace w, List<CodeAnchor> analyzed)
    {
        return analyzed.ConvertAll(a => Analysis.Find(w.Assemblies, a));
    }

    private static void AnalyzeAgain(this Workspace w, List<AnalysisSymbol?>? found)
    {
        w.PendingAnalyses.Clear();
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
