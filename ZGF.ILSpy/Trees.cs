using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The browse tree and the analyzer's rows following the open assemblies and analyses.</summary>
public static class Trees
{
    /// <summary>Forgets what the tree had expanded and selected, and the menu's target, once the list
    /// was cleared; rebuilds its roots when the open assemblies changed; and lists the analyzer's rows
    /// again when its roots changed. The roots of the assemblies read from <paramref name="reopening"/>
    /// spin while they are reopened.</summary>
    public static void SyncTrees(UiState uiState, AssemblyList assemblies, AnalyzerTree analyzer, ApiVisibility visibility,
        IReadOnlyCollection<AssemblyPath> reopening)
    {
        ForgetClearedList(uiState, assemblies);
        uiState.Tree.ShowApi(visibility);
        uiState.Tree.Sync(assemblies);
        uiState.Tree.ShowReopening(reopening);
        uiState.Analyzer.Follow(analyzer);
    }

    private static void ForgetClearedList(UiState uiState, AssemblyList assemblies)
    {
        if (!uiState.ShownList.TakeChange(assemblies.Generation))
            return;
        uiState.Tree.Forget();
        uiState.MenuTarget = null;
    }
}
