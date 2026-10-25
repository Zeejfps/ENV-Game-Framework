using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Analysis;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>Analyzing types and members, and analyzing again what was analyzed in the last run.</summary>
public static class Analysis
{
    /// <summary>Analyzes the type or member <paramref name="node"/> stands for in the analyzer. Returns
    /// the root analyzing it, or null when it cannot be analyzed.</summary>
    public static AnalyzerSymbolNode? AddToAnalyzer(AppState appState, BrowseNode node)
    {
        var symbol = Analyzers.Of(node);
        if (symbol == null)
            return null;
        return AddToAnalyzer(appState, symbol);
    }

    /// <summary>Analyzes <paramref name="symbol"/> in the analyzer. Returns the root analyzing it, the one
    /// already there when it was analyzed before.</summary>
    public static AnalyzerSymbolNode AddToAnalyzer(AppState appState, AnalysisSymbol symbol)
    {
        return appState.Analyzer.AddRoot(symbol);
    }

    /// <summary>Runs the analysis <paramref name="kind"/> stands for over the open assemblies in the
    /// background, unless it already started. When it fails, why is reported.</summary>
    public static void RunAnalysis(AppState appState, AnalyzerKindNode kind)
    {
        kind.StartOnce(appState.Assemblies, appState.Analyzer.NoteFindings, error => ReportAnalysisFailure(appState, kind, error));
    }

    private static void ReportAnalysisFailure(AppState appState, AnalyzerKindNode kind, Exception error)
    {
        var t = Strings.For(appState.Locale);
        var analysis = AnalyzerText.Header(t, kind.Kind);
        appState.ReportError(t.ErrorAnalyze(kind.Subject.Label, analysis), error.ToString());
    }

    /// <summary>Adds a root that shows as loading, in the analyzer, while what <paramref name="reference"/>
    /// points at is found in the background, opening its assembly if it is not, and turns the root into
    /// its analysis, which the view is commanded to show. The root goes when the reference does not
    /// resolve, which is reported. Once the root was removed, as clearing the list removes them all,
    /// what is found is dropped.</summary>
    public static async void AddToAnalyzer(AppState appState, CodeReference reference)
    {
        var root = appState.Analyzer.AddPending(savedAs: null);
        var found = await Background.Run(() => appState.Assemblies.FindNode(reference));
        if (!appState.Analyzer.Contains(root))
            return;
        var node = found.Value;
        if (node == null)
        {
            appState.Analyzer.Remove(root);
            ReportUnresolvedReference(appState, reference, found.Error);
            return;
        }
        AnalyzeFound(appState, root, node);
    }

    private static void AnalyzeFound(AppState appState, AnalyzerPendingNode root, BrowseNode node)
    {
        var symbol = Analyzers.Of(node);
        if (symbol == null)
        {
            appState.Analyzer.Remove(root);
            return;
        }
        ResolveAnalysis(appState, root, symbol);
    }

    /// <summary>Turns <paramref name="root"/> into the analysis of <paramref name="symbol"/>, and commands
    /// the view to show the root that analyzes it.</summary>
    private static void ResolveAnalysis(AppState appState, AnalyzerPendingNode root, AnalysisSymbol symbol)
    {
        var shown = appState.Analyzer.Resolve(root, symbol);
        if (shown == null)
            return;
        appState.EnqueueCommand(new ViewCommand.ShowAnalysis(shown));
    }

    /// <summary>Finds the symbols of the roots restored from the saved analyses in the background,
    /// opening their assemblies' files without listing them, and turns each root into the analysis of
    /// its symbol, or removes it when its symbol is gone, unless the list was cleared meanwhile.
    /// Reports why the symbols could not be found.</summary>
    public static async void RestoreAnalyses(AppState appState)
    {
        var restoring = appState.Analyzer.ListRestoring();
        if (restoring.Count == 0)
            return;
        var analyzed = restoring.ConvertAll(static r => r.SavedAs!.Value);
        var generation = appState.Assemblies.Generation;
        var found = await Background.Run(() => FindAnalyzed(appState, analyzed));
        if (WasListCleared(appState, generation))
            return;
        ReportRestoreFailure(appState, found.Error);
        AnalyzeAgain(appState, restoring, found.Value);
    }

    private static bool WasListCleared(AppState appState, int generation) => appState.Assemblies.Generation != generation;

    private static List<AnalysisSymbol?> FindAnalyzed(AppState appState, List<CodeAnchor> analyzed)
    {
        return analyzed.ConvertAll(a => Analyzers.Find(appState.Assemblies, a));
    }

    private static void ReportRestoreFailure(AppState appState, Exception? error)
    {
        if (error == null)
            return;
        appState.ReportError(Strings.For(appState.Locale).ErrorRestoreAnalyses, error.ToString());
    }

    private static void AnalyzeAgain(AppState appState, List<AnalyzerPendingNode> restoring, List<AnalysisSymbol?>? found)
    {
        for (var i = 0; i < restoring.Count; i++)
        {
            var symbol = FoundAt(found, i);
            AnalyzeAgain(appState, restoring[i], symbol);
        }
    }

    private static AnalysisSymbol? FoundAt(List<AnalysisSymbol?>? found, int index)
    {
        if (found == null)
            return null;
        return found[index];
    }

    private static void AnalyzeAgain(AppState appState, AnalyzerPendingNode root, AnalysisSymbol? symbol)
    {
        if (symbol == null)
        {
            appState.Analyzer.Remove(root);
            return;
        }
        ResolveAnalysis(appState, root, symbol);
    }
}
