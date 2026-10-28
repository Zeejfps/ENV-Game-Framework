using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Decompiling what a tab points at, or reading its grid or picture, in the background, and
/// commanding the view where to stand in it once it arrives.</summary>
public static class Decompilation
{
    /// <summary>Starts decompiling <paramref name="node"/> in <paramref name="tab"/>, or reading its grid,
    /// cancelling what was in flight, then commands the view to stand where <paramref name="arrival"/> says.
    /// When the tab already shows <paramref name="node"/> as the language and options would, only does the latter.</summary>
    public static void LoadIntoTab(AppState appState, DocumentTab tab, BrowseNode node, Arrival arrival = default)
    {
        if (tab.Shows(node, appState.Language, appState.DecompilerOptions))
        {
            Arrive(appState, tab, arrival);
            return;
        }
        var load = tab.StartLoading(node, appState.Language, appState.DecompilerOptions, arrival);
        Load(appState, tab, node, load);
    }

    private static async void Load(AppState appState, DocumentTab tab, BrowseNode node, LoadTicket load)
    {
        var loading = node.LoadContentAsync(tab.Language, tab.Options, load.Token);
        await Background.Wait(loading);
        FinishLoading(appState, tab, node, load, loading);
    }

    private static void FinishLoading(AppState appState, DocumentTab tab, BrowseNode node, LoadTicket load, Task<NodeContent> loading)
    {
        if (!tab.FinishLoading(load))
            return;
        if (loading.IsCanceled)
            return;
        var error = loading.Exception?.GetBaseException();
        tab.ShowLoaded(ContentOf(loading, error));
        var arrival = tab.TakeArrival();
        if (IsFailed(tab, error))
        {
            ReportFailure(appState, tab, node, error);
            Arrive(appState, tab, new Arrival());
            return;
        }
        Arrive(appState, tab, arrival);
    }

    private static TabContent ContentOf(Task<NodeContent> loading, Exception? error)
    {
        if (error != null)
            return new TabContent.Failed(error.Message);
        return loading.Result switch
        {
            GridContent grid => new TabContent.Table(grid.Grid),
            ImageContent image => new TabContent.Picture(image.Image, image.Source),
            DocumentContent document => new TabContent.Code(document.Document),
            _ => throw new InvalidOperationException($"No tab shows {loading.Result.GetType().Name}."),
        };
    }

    private static bool IsFailed(DocumentTab tab, Exception? error)
    {
        if (error != null)
            return true;
        return tab.Document.IsError;
    }

    /// <summary>Reports why <paramref name="node"/> could not be shown, unless its file failed to load,
    /// which its tab tells already.</summary>
    private static void ReportFailure(AppState appState, DocumentTab tab, BrowseNode node, Exception? error)
    {
        if (node.Assembly.HasLoadError)
            return;
        appState.ReportError(new AppProblem.DecompilationFailed(node.FullName, error, tab.Document.GetText()));
    }

    /// <summary>Commands the view to return to the arrival's position, else to reveal the grid's row or the
    /// line its bookmark marks, else the node's definition.</summary>
    private static void Arrive(AppState appState, DocumentTab tab, Arrival arrival)
    {
        var position = arrival.Position;
        if (position != null)
        {
            appState.EnqueueCommand(new ViewCommand.RestoreView(tab, position.Value));
            return;
        }
        if (tab.Grid != null)
        {
            RevealRow(appState, tab, arrival.Row);
            return;
        }
        var line = FindLineToReveal(tab, arrival.Target);
        if (line < 0)
            return;
        appState.EnqueueCommand(new ViewCommand.RevealLine(tab, line));
    }

    private static void RevealRow(AppState appState, DocumentTab tab, int? row)
    {
        if (row == null)
            return;
        appState.EnqueueCommand(new ViewCommand.RevealRow(tab, row.Value));
    }

    /// <summary>The line <paramref name="target"/> marks, else the node's definition, or -1.</summary>
    private static int FindLineToReveal(DocumentTab tab, Bookmark? target)
    {
        var node = tab.Node;
        if (node == null)
            return -1;
        var marked = FindMarkedLine(tab, target, node);
        if (marked >= 0)
            return marked;
        return tab.Document.FindDefinitionLine(node);
    }

    private static int FindMarkedLine(DocumentTab tab, Bookmark? target, BrowseNode node)
    {
        if (target == null)
            return -1;
        return target.FindLine(tab.Document, node.Path);
    }

    /// <summary>Decompiles the node <paramref name="tab"/> shows again if it was shown in another language,
    /// or with other options, than the app state's, keeping the view where it was when the language is the same.</summary>
    public static void DecompileAgainIfOutdated(AppState appState, DocumentTab tab)
    {
        var node = tab.Node;
        if (node == null)
            return;
        if (!node.DependsOnLanguage)
            return;
        if (tab.IsCurrent(appState.Language, appState.DecompilerOptions))
            return;
        var position = FindPositionToKeep(appState, tab);
        LoadIntoTab(appState, tab, node, new Arrival(position));
    }

    private static CodeViewPosition? FindPositionToKeep(AppState appState, DocumentTab tab)
    {
        if (tab.Language != appState.Language)
            return null;
        return tab.Position;
    }
}
