using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Analyzing types and members, and analyzing again what was analyzed in the last run.</summary>
public static class Analyzing
{
    /// <summary>Analyzes the type or member <paramref name="node"/> stands for in the analyzer. Returns
    /// the root analyzing it, or null when it cannot be analyzed.</summary>
    public static AnalyzerSymbolNode? Analyze(this Workspace w, BrowseNode node)
    {
        var symbol = Analysis.Of(node);
        if (symbol == null)
            return null;
        return w.Analyzer.Analyze(symbol);
    }

    /// <summary>Adds a root that shows as loading, in the analyzer, while what <paramref name="reference"/>
    /// points at is found in the background, opening its assembly if it is not, and turns the root into
    /// its analysis, which the view is commanded to show. The root goes when the reference does not
    /// resolve, which is reported. Once the root was removed, as clearing the list removes them all,
    /// what is found is dropped.</summary>
    public static async void Analyze(this Workspace w, CodeReference reference)
    {
        var root = w.Analyzer.AddPending(savedAs: null);
        var found = await Background.Run(() => w.Assemblies.FindNode(reference));
        if (!w.Analyzer.Contains(root))
            return;
        var node = found.Value;
        if (node == null)
        {
            w.Analyzer.Remove(root);
            w.ReportUnresolved(reference, found.Error);
            return;
        }
        w.AnalyzeFound(root, node);
    }

    private static void AnalyzeFound(this Workspace w, AnalyzerPendingNode root, BrowseNode node)
    {
        var symbol = Analysis.Of(node);
        if (symbol == null)
        {
            w.Analyzer.Remove(root);
            return;
        }
        w.ResolveAnalysis(root, symbol);
    }

    /// <summary>Turns <paramref name="root"/> into the analysis of <paramref name="symbol"/>, and commands
    /// the view to show the root that analyzes it.</summary>
    private static void ResolveAnalysis(this Workspace w, AnalyzerPendingNode root, AnalysisSymbol symbol)
    {
        var shown = w.Analyzer.Resolve(root, symbol);
        if (shown == null)
            return;
        w.EnqueueCommand(new ViewCommand.ShowAnalysis(shown));
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
        w.ResolveAnalysis(root, symbol);
    }
}
