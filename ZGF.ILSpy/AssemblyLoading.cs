using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Opening, closing and reloading the assemblies in the list.</summary>
public static class AssemblyLoading
{
    /// <summary>Opens the files in the background, then shows the last one opened.</summary>
    public static void Open(this Workspace w, IEnumerable<string> paths)
    {
        var files = paths.ToArray();
        if (files.Length == 0)
            return;
        w.OpenInBackground(files);
    }

    private static async void OpenInBackground(this Workspace w, string[] files)
    {
        var ticket = w.Intent.Begin();
        var token = w.Opens.Token;
        var opened = await Background.Run(() => w.OpenFiles(files, token));
        w.ShowOpened(opened, ticket);
    }

    private static void ShowOpened(this Workspace w, Outcome<Opened> outcome, int ticket)
    {
        if (outcome.Error is OperationCanceledException)
            return;
        var opened = outcome.Value;
        if (opened == null)
        {
            w.ReportError(Strings.For(w.Locale).ErrorOpenAssemblies, outcome.Error!.ToString());
            return;
        }
        w.ReportAll(opened.Errors);
        if (opened.Last == null)
            return;
        if (w.Intent.IsStale(ticket))
            return;
        w.Show(opened.Last.Root);
    }

    public static void Open(this Workspace w, string path) => w.Open([path]);

    public static void Close(this Workspace w, LoadedAssembly assembly) => w.Assemblies.Close(assembly);

    public static void ClearList(this Workspace w)
    {
        w.Intent.Begin();
        w.Opens.CancelAll();
        w.Reload.Stop();
        w.PendingAnalyses.Clear();
        w.ClearAssemblies();
    }

    internal static void ClearAssemblies(this Workspace w)
    {
        w.Assemblies.Clear();
        w.Tree.View.ClearSelection();
        w.Tree.CollapseAll();
        w.Analyzer.Clear();
        w.MenuTarget = null;
    }

    /// <summary>Reloads every assembly of the open list from disk in the background, then expands the
    /// tree as it was and shows each tab's place again, decompiled from the fresh files.</summary>
    public static async void Refresh(this Workspace w)
    {
        w.RememberOpenAssemblies();
        var intent = w.Intent.Begin();
        var ticket = w.Reload.Start();

        var paths = w.Lists.ActivePaths.ToList();
        var expanded = w.Tree.ExpandedPaths();
        var tabs = w.ReleaseShownTabs();
        var places = tabs.ConvertAll(t => t.Location);
        w.ClearAssemblies();

        var reloaded = await Background.Run(() => w.ReopenFiles(paths, expanded, places, ticket.Token));
        w.FinishReload(reloaded, ticket, intent, tabs);
    }

    private static List<ReloadingTab> ReleaseShownTabs(this Workspace w)
    {
        var reloading = new List<ReloadingTab>();
        foreach (var tab in w.Tabs)
        {
            var location = tab.Location;
            if (location == null)
                continue;
            var position = w.FindPositionToKeep(tab);
            tab.Release(Strings.For(w.Locale).DocumentReloading);
            reloading.Add(new ReloadingTab(tab, location, tab.Version, position));
        }
        return reloading;
    }

    private static CodeViewPosition? FindPositionToKeep(this Workspace w, DocumentTab tab)
    {
        if (tab.Grid != null)
            return tab.Position;
        if (tab.Language != w.Language)
            return null;
        return tab.Position;
    }

    private static Reloaded ReopenFiles(this Workspace w, IReadOnlyList<string> paths, List<NodePath> expanded, List<NodePath> places, CancellationToken cancellationToken)
    {
        var opened = w.OpenFiles(paths, cancellationToken);
        var expandedNodes = w.LoadExpandedNodes(expanded, cancellationToken);
        var placeNodes = w.FindNodes(places, cancellationToken);
        return new Reloaded(opened.Errors, expandedNodes, placeNodes);
    }

    private static List<BrowseNode> LoadExpandedNodes(this Workspace w, List<NodePath> expanded, CancellationToken cancellationToken)
    {
        var nodes = new List<BrowseNode>();
        foreach (var path in expanded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = w.Assemblies.FindNode(path);
            if (node == null)
                continue;
            node.LoadChildren();
            nodes.Add(node);
        }
        return nodes;
    }

    private static BrowseNode?[] FindNodes(this Workspace w, List<NodePath> places, CancellationToken cancellationToken)
    {
        var found = new BrowseNode?[places.Count];
        for (var i = 0; i < found.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            found[i] = w.Assemblies.FindNode(places[i]);
        }
        return found;
    }

    private static void FinishReload(this Workspace w, Outcome<Reloaded> outcome, ReloadTicket ticket, int intent, List<ReloadingTab> tabs)
    {
        if (!w.Reload.Finish(ticket))
            return;
        var reloaded = outcome.Value;
        if (reloaded == null)
        {
            w.ReportError(Strings.For(w.Locale).ErrorReloadAssemblies, outcome.Error!.ToString());
            return;
        }
        w.ReportAll(reloaded.Errors);
        w.Tree.Expand(reloaded.Expanded);
        for (var i = 0; i < tabs.Count; i++)
            w.ReshowTab(tabs[i], reloaded.Places[i], intent);
        w.SaveSettings();
    }

    private static void ReshowTab(this Workspace w, ReloadingTab reloading, BrowseNode? node, int intent)
    {
        var tab = reloading.Tab;
        if (!w.Tabs.Contains(tab))
            return;
        if (tab.Version != reloading.Version)
            return;
        if (node == null)
        {
            w.ReleaseGoneTab(tab, reloading.Location);
            return;
        }
        tab.Show(node, w.Language, reloading.Position);
        if (tab == w.Tabs.Active && w.Intent.IsCurrent(intent))
            w.Tree.Reveal(node);
    }

    private static void ReleaseGoneTab(this Workspace w, DocumentTab tab, NodePath location)
    {
        var t = Strings.For(w.Locale);
        tab.Release(t.DocumentGone(location.Text));
        w.ReportError(t.ErrorFindAfterReload(location.Text), location.ToString());
    }

    /// <summary>Opens <paramref name="paths"/> in the background as a reload does, so that another
    /// reload or clearing the list stops it, and the settings are saved once it is done.</summary>
    internal static async void OpenAsReload(this Workspace w, List<string> paths)
    {
        var ticket = w.Reload.Start();
        var opened = await Background.Run(() => w.OpenFiles(paths, ticket.Token));
        w.FinishOpeningAsReload(opened, ticket);
    }

    private static void FinishOpeningAsReload(this Workspace w, Outcome<Opened> outcome, ReloadTicket ticket)
    {
        if (!w.Reload.Finish(ticket))
            return;
        var opened = outcome.Value;
        if (opened == null)
        {
            w.ReportError(Strings.For(w.Locale).ErrorOpenAssemblies, outcome.Error!.ToString());
            return;
        }
        w.ReportAll(opened.Errors);
        w.SaveSettings();
    }

    private static Opened OpenFiles(this Workspace w, IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        LoadedAssembly? last = null;
        var errors = new List<(string, string)>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                last = w.Assemblies.Open(path, cancellationToken);
            }
            catch (AssemblyLoadException e)
            {
                errors.Add(w.DescribeLoadFailure(path, e));
            }
        }
        return new Opened(last, errors);
    }

    private static (string Title, string Details) DescribeLoadFailure(this Workspace w, string path, AssemblyLoadException e)
    {
        var file = Path.GetFileName(path);
        var title = Strings.For(w.Locale).ErrorOpenFile(file);
        var details = Workspace.DescribeFailure(e.InnerException, e.Message);
        return (title, details);
    }

    private static void ReportAll(this Workspace w, List<(string Title, string Details)> errors)
    {
        foreach (var (title, details) in errors)
            w.ReportError(title, details);
    }

    private sealed record Opened(LoadedAssembly? Last, List<(string Title, string Details)> Errors);

    private sealed record Reloaded(List<(string Title, string Details)> Errors, List<BrowseNode> Expanded, BrowseNode?[] Places);

    private sealed record ReloadingTab(DocumentTab Tab, NodePath Location, int Version, CodeViewPosition? Position);
}
