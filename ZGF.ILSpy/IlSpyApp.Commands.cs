using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

public sealed partial class IlSpyApp
{
    /// <summary>Runs the commands the features queued for the view, in the order they were queued, dropping
    /// commands about a node, tab, line or row that is gone since.</summary>
    internal static void RunViewCommands(UiState uiState, AppState appState)
    {
        var commands = appState.ViewCommands;
        while (commands.TryDequeue(out var command))
            Run(command, uiState, appState);
    }

    private static void Run(ViewCommand command, UiState uiState, AppState appState)
    {
        switch (command)
        {
            case ViewCommand.ShowTab show:
                ShowTabIfOpen(uiState, appState, show.Tab, show.Beside);
                break;
            case ViewCommand.OpenPanel open:
                OpenPanel(uiState, open.Panel);
                break;
            case ViewCommand.RevealNode reveal:
                RevealIfListed(uiState, appState, reveal.Node);
                break;
            case ViewCommand.ExpandNodes expand:
                ExpandListed(uiState, appState, expand.Nodes);
                break;
            case ViewCommand.ShowAnalysis show:
                ShowAnalysis(uiState, appState, show.Root);
                break;
            case ViewCommand.RestoreView restore:
                RestoreView(uiState, appState, restore.Tab, restore.Position);
                break;
            case ViewCommand.RevealLine reveal:
                RevealLine(uiState, appState, reveal.Tab, reveal.Line);
                break;
            case ViewCommand.RevealRow reveal:
                RevealRow(uiState, appState, reveal.Tab, reveal.Row);
                break;
        }
    }

    private static void ShowTabIfOpen(UiState uiState, AppState appState, DocumentTab tab, DocumentTab? beside)
    {
        if (!appState.Tabs.Contains(tab))
            return;
        ShowTab(uiState, tab, beside);
    }

    private static void RevealIfListed(UiState uiState, AppState appState, BrowseNode node)
    {
        if (!appState.IsListed(node))
            return;
        uiState.Tree.Reveal(node);
    }

    private static void ExpandListed(UiState uiState, AppState appState, IReadOnlyList<BrowseNode> nodes)
    {
        var listed = nodes
            .Where(appState.IsListed)
            .ToList();
        uiState.Tree.Expand(listed);
    }

    /// <summary>Expands <paramref name="root"/> and selects it, unless it was removed meanwhile.</summary>
    private static void ShowAnalysis(UiState uiState, AppState appState, AnalyzerSymbolNode root)
    {
        if (!appState.Analyzer.Contains(root))
            return;
        uiState.Analyzer.Show(root);
    }

    private static void RestoreView(UiState uiState, AppState appState, DocumentTab tab, CodeViewPosition position)
    {
        if (!appState.Tabs.Contains(tab))
            return;
        uiState.Documents.RestoreView(tab, position);
    }

    private static void RevealLine(UiState uiState, AppState appState, DocumentTab tab, int line)
    {
        if (!appState.Tabs.Contains(tab))
            return;
        uiState.Documents.RevealLine(tab, line);
    }

    private static void RevealRow(UiState uiState, AppState appState, DocumentTab tab, int row)
    {
        if (!appState.Tabs.Contains(tab))
            return;
        uiState.Documents.RevealRow(tab, row);
    }

    /// <summary>Runs the commands <paramref name="uiState"/> queued for the app state, in the order queued.</summary>
    internal static void RunAppCommands(UiState uiState, AppState appState)
    {
        var commands = uiState.AppCommands;
        while (commands.TryDequeue(out var command))
            Run(command, uiState, appState);
    }

    private static void Run(AppCommand command, UiState uiState, AppState appState)
    {
        switch (command)
        {
            case AppCommand.OpenTab open:
                OpenTabIn(uiState, appState, open.Panel, open.Focus);
                break;
            case AppCommand.CloseTab close:
                CloseTabAndFocusNext(uiState, appState, close.Tab);
                break;
            case AppCommand.FocusTab focus:
                FocusTab(appState, focus.Tab);
                break;
            case AppCommand.RunAnalysis run:
                RunAnalysis(appState, run.Kind);
                break;
        }
    }

    /// <summary>Opens an empty tab shown in <paramref name="panel"/>, unless the panel was closed meanwhile,
    /// and makes it the active tab when asked to or when the layout focuses its panel.</summary>
    private static void OpenTabIn(UiState uiState, AppState appState, string panel, bool focus)
    {
        if (!uiState.Layout.Contains(panel))
            return;
        var tab = OpenEmptyTab(appState);
        uiState.Documents.Add(tab, panel);
        var becomesActive = focus || uiState.Layout.Focused == panel;
        if (becomesActive)
            FocusTab(appState, tab);
    }

    private static void CloseTabAndFocusNext(UiState uiState, AppState appState, DocumentTab tab)
    {
        var wasActive = CloseTab(appState, tab);
        if (!wasActive)
            return;
        FocusNextTab(uiState);
    }
}
