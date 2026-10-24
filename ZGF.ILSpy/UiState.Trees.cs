using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Analysis;

namespace ZGF.ILSpy;

public sealed partial class UiState
{
    private int _listGeneration;

    /// <summary>The browse tree over the open assemblies, with the view's expansion and selection.</summary>
    public BrowseTree Tree { get; } = new();

    /// <summary>The node the tree's context menu was opened on.</summary>
    public BrowseNode? MenuTarget;

    /// <summary>The analyzer's tree as its view shows it.</summary>
    public AnalyzerRows Analyzer { get; }

    /// <summary>Forgets what the tree had expanded and selected, and the menu's target, once the list
    /// was cleared; rebuilds its roots when the open assemblies changed; and lists the analyzer's rows
    /// again when its roots changed.</summary>
    public void SyncTrees(AssemblyList assemblies, bool showNonPublic)
    {
        ForgetClearedList(assemblies);
        Tree.SetShowNonPublic(showNonPublic);
        Tree.Sync(assemblies);
        Analyzer.Follow();
    }

    private void ForgetClearedList(AssemblyList assemblies)
    {
        var generation = assemblies.Generation;
        if (generation == _listGeneration)
            return;
        _listGeneration = generation;
        Tree.Forget();
        MenuTarget = null;
    }

    /// <summary>Analyzes the type or member <paramref name="node"/> stands for in the analyzer, which
    /// is opened if it is not, and selects its root.</summary>
    public void Analyze(BrowseNode node)
    {
        var root = AddToAnalyzer(_appState, node);
        if (root == null)
            return;
        OpenAnalyzer();
        Analyzer.Show(root);
    }

    /// <summary>Opens the analyzer if it is not, where a root shows as loading while what
    /// <paramref name="reference"/> points at is found and analyzed.</summary>
    public void Analyze(CodeReference reference)
    {
        OpenAnalyzer();
        AddToAnalyzer(_appState, reference);
    }
}
