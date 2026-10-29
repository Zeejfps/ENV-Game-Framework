using System.Diagnostics;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Decompilation;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.SavedDocuments;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy;

/// <summary>Opening, closing and reloading the assemblies in the list.</summary>
public static class Assemblies
{
    /// <summary>Opens the files in the background, then shows the last one opened.</summary>
    public static void OpenAssemblies(AppState appState, IEnumerable<string> paths)
    {
        var files = paths.ToArray();
        if (files.Length == 0)
            return;
        OpenInBackground(appState, files);
    }

    private static async void OpenInBackground(AppState appState, string[] files)
    {
        var ticket = appState.Intent.Begin();
        var token = appState.Opens.Token;
        var opened = await Background.Run(() => OpenFiles(appState, files, token));
        ShowOpened(appState, opened, ticket);
    }

    private static void ShowOpened(AppState appState, Outcome<LoadedAssembly?> outcome, int ticket)
    {
        switch (outcome)
        {
            case Outcome<LoadedAssembly?>.Ok ok when ok.Value != null:
                if (!appState.Intent.IsStale(ticket))
                    ShowNode(appState, ok.Value.Root);
                break;
            case Outcome<LoadedAssembly?>.Ok:
                break;
            case Outcome<LoadedAssembly?>.Failed failed:
                ReportOpenFailure(appState, failed.Error);
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>Reports why opening failed, unless the opening was cancelled.</summary>
    internal static void ReportOpenFailure(AppState appState, Exception error)
    {
        if (error is OperationCanceledException)
            return;
        appState.ReportError(new AppProblem.OpenFailed(error));
    }

    public static void OpenAssembly(AppState appState, string path) => OpenAssemblies(appState, [path]);

    /// <summary>Opens the files among <paramref name="paths"/>, dropped on the window, as
    /// <see cref="OpenAssemblies"/> does; folders are left out.</summary>
    public static void OpenDroppedFiles(AppState appState, IReadOnlyList<string> paths)
    {
        var files = paths
            .Where(File.Exists)
            .ToList();
        OpenAssemblies(appState, files);
    }

    /// <summary>Opens, in the background, the assemblies <paramref name="assembly"/> references that
    /// can be found.</summary>
    public static async void OpenDependencies(AppState appState, LoadedModule assembly)
    {
        var token = appState.Opens.Token;
        var opened = await Background.Run(() => appState.Assemblies.ListReferences(assembly, token));
        if (opened is Outcome<List<LoadedModule>>.Failed failed)
            ReportOpenFailure(appState, failed.Error);
    }

    /// <summary>Checks in the background that the PDB at <paramref name="pdbPath"/> can be read for
    /// <paramref name="assembly"/>, then has the assembly read it in place of the one it finds itself,
    /// kept with the list, and reopens the assembly to show it. Reports why when it cannot be read.</summary>
    public static async void UsePdb(AppState appState, LoadedModule assembly, string pdbPath)
    {
        var checking = await Background.Run(() => assembly.CheckPdb(pdbPath));
        var check = checking switch
        {
            Outcome<PdbCheck>.Ok ok => ok.Value,
            Outcome<PdbCheck>.Failed => PdbCheck.Unreadable,
            _ => throw new UnreachableException(),
        };
        if (check != PdbCheck.Matches)
        {
            ReportUnreadablePdb(appState, assembly, pdbPath, check);
            return;
        }
        if (!appState.IsListed(assembly.Root))
            return;
        appState.Assemblies.Pdbs.Pick(assembly.FilePath, pdbPath);
        ReopenAssembly(appState, assembly);
    }

    private static void ReportUnreadablePdb(AppState appState, LoadedAssembly assembly, string pdbPath, PdbCheck check)
    {
        appState.ReportError(new AppProblem.PdbUnusable(pdbPath, assembly.Name, check));
    }

    /// <summary>Resolves <paramref name="assembly"/>'s references against the framework
    /// <paramref name="framework"/> names, such as <c>.NETCoreApp,Version=v8.0</c>, or, when it is
    /// null, against the one the assembly was built for; kept with the list, and the assembly reopened
    /// to show it. Does nothing when that is the framework already in use.</summary>
    public static void SetTargetFramework(AppState appState, LoadedModule assembly, string? framework)
    {
        if (!appState.IsListed(assembly.Root))
            return;
        if (IsInUse(assembly, framework))
            return;
        PickTargetFramework(appState.Assemblies.TargetFrameworks, assembly, framework);
        ReopenAssembly(appState, assembly);
    }

    /// <summary>Whether <paramref name="framework"/>, null for the one the assembly was built for, is
    /// what its references are resolved against now.</summary>
    private static bool IsInUse(LoadedModule assembly, string? framework)
    {
        if (!assembly.IsTargetFrameworkPicked)
            return framework == null;
        return assembly.TargetFramework == framework;
    }

    private static void PickTargetFramework(AssemblyPicks frameworks, LoadedAssembly assembly, string? framework)
    {
        if (framework == null)
        {
            frameworks.Forget(assembly.FilePath);
            return;
        }
        frameworks.Pick(assembly.FilePath, framework);
    }

    /// <summary>Reads the item of the list holding <paramref name="assembly"/> again in the background,
    /// its nodes loaded as far as they were, then lists it where the item is and shows each tab's place
    /// in it again. The other assemblies stay as they are.</summary>
    private static async void ReopenAssembly(AppState appState, LoadedAssembly assembly)
    {
        var item = assembly.ListedItem;
        var ticket = appState.Reopens.Start(item.FilePath);
        var tabs = ListTabsShowing(appState, item);
        var places = tabs.ConvertAll(t => t.Location);
        var reopened = await Background.Run(() => ReadAgain(appState, item, places));
        FinishReopen(appState, item, reopened, ticket, tabs);
    }

    private static List<ReloadingTab> ListTabsShowing(AppState appState, LoadedAssembly item)
    {
        var showing = new List<ReloadingTab>();
        foreach (var tab in appState.Tabs)
        {
            var location = tab.Location;
            if (location == null)
                continue;
            if (!IsPathOf(item, location))
                continue;
            var position = FindPositionToKeep(appState, tab);
            showing.Add(new ReloadingTab(tab, location, tab.Version, position));
        }
        return showing;
    }

    private static bool IsPathOf(LoadedAssembly item, NodePath path)
    {
        return string.Equals(path.FilePath, item.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    private static Reopened ReadAgain(AppState appState, LoadedAssembly item, List<NodePath> places)
    {
        var fresh = appState.Assemblies.ReadAgain(item);
        LoadAsLoaded(item.Root, fresh.Root);
        var placeNodes = places
            .Select(fresh.FindNode)
            .ToArray();
        return new Reopened(fresh, placeNodes);
    }

    /// <summary>Loads the children of each node under <paramref name="fresh"/> whose counterpart under
    /// <paramref name="old"/> has them loaded, so that the tree can show the fresh nodes as it showed
    /// the old ones.</summary>
    private static void LoadAsLoaded(BrowseNode old, BrowseNode fresh)
    {
        if (!old.TryGetChildren(out var children))
            return;
        fresh.LoadChildren();
        foreach (var child in children)
        {
            var counterpart = fresh.FindLoadedChildLike(child);
            if (counterpart != null)
                LoadAsLoaded(child, counterpart);
        }
    }

    private static void FinishReopen(AppState appState, LoadedAssembly item, Outcome<Reopened> outcome, int ticket, List<ReloadingTab> tabs)
    {
        if (!appState.Reopens.Finish(item.FilePath, ticket))
            return;
        SaveSettings(appState);
        switch (outcome)
        {
            case Outcome<Reopened>.Ok ok:
                ShowReopened(appState, item, ok.Value, tabs);
                break;
            case Outcome<Reopened>.Failed failed:
                appState.ReportError(new AppProblem.AssemblyNotReloaded(item.Name, failed.Error));
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void ShowReopened(AppState appState, LoadedAssembly item, Reopened reopened, List<ReloadingTab> tabs)
    {
        if (!appState.Assemblies.Replace(item, reopened.Item))
            return;
        for (var i = 0; i < tabs.Count; i++)
            ShowAgain(appState, tabs[i], reopened.Places[i]);
    }

    public static void CloseAssembly(AppState appState, LoadedAssembly assembly) => appState.Assemblies.Close(assembly);

    /// <summary>Takes the files that failed to load off the list.</summary>
    public static void CloseFailedAssemblies(AppState appState)
    {
        var failed = appState.Assemblies.Listed
            .OfType<FailedAssembly>()
            .ToList();
        foreach (var assembly in failed)
            appState.Assemblies.Close(assembly);
    }

    /// <summary>Whether any file on the list failed to load.</summary>
    public static bool AnyFailedToLoad(AppState appState)
    {
        return appState.Assemblies.Listed.Any(static a => a is FailedAssembly);
    }

    /// <summary>Orders the list by name, and assemblies of one name by version.</summary>
    public static void SortAssemblies(AppState appState) => appState.Assemblies.Sort();

    /// <summary>Closes every assembly of the open list, stopping its reload; tabs still waiting for it to
    /// reopen their places show what is left of them.</summary>
    public static void CloseAllAssemblies(AppState appState)
    {
        appState.Intent.Begin();
        appState.Opens.CancelAll();
        appState.Reload.Stop();
        ClearAssemblies(appState);
        appState.Assemblies.Pdbs.ForgetAll();
        appState.Assemblies.TargetFrameworks.ForgetAll();
        ShowReopenedDocuments(appState);
    }

    private static void ClearAssemblies(AppState appState)
    {
        appState.Assemblies.Clear();
        appState.Analyzer.Clear();
    }

    /// <summary>Reloads every assembly of the open list from disk in the background, then commands the view
    /// to expand the tree at <paramref name="expanded"/>, the paths of the nodes it had expanded, and
    /// shows each tab's place again, decompiled from the fresh files.</summary>
    public static async void RefreshAssemblies(AppState appState, List<NodePath> expanded)
    {
        RememberOpenAssemblies(appState);
        var intent = appState.Intent.Begin();
        var ticket = appState.Reload.Start();

        var paths = appState.Lists.ActivePaths.ToList();
        var tabs = ReleaseShownTabs(appState);
        var places = tabs.ConvertAll(t => t.Location);
        ClearAssemblies(appState);

        var reloaded = await Background.Run(() => ReopenFiles(appState, paths, expanded, places, ticket.Token));
        FinishReload(appState, reloaded, ticket, intent, tabs);
    }

    private static List<ReloadingTab> ReleaseShownTabs(AppState appState)
    {
        var reloading = new List<ReloadingTab>();
        foreach (var tab in appState.Tabs)
        {
            var location = tab.Location;
            if (location == null)
                continue;
            var position = FindPositionToKeep(appState, tab);
            tab.ShowReloading();
            reloading.Add(new ReloadingTab(tab, location, tab.Version, position));
        }
        return reloading;
    }

    private static CodeViewPosition? FindPositionToKeep(AppState appState, DocumentTab tab)
    {
        if (tab.Grid != null)
            return tab.Position;
        if (tab.Language != appState.Language)
            return null;
        return tab.Position;
    }

    private static Reloaded ReopenFiles(AppState appState, IReadOnlyList<string> paths, List<NodePath> expanded, List<NodePath> places, CancellationToken cancellationToken)
    {
        OpenFiles(appState, paths, cancellationToken);
        var expandedNodes = LoadExpandedNodes(appState, expanded, cancellationToken);
        var placeNodes = FindNodes(appState, places, cancellationToken);
        return new Reloaded(expandedNodes, placeNodes);
    }

    private static List<BrowseNode> LoadExpandedNodes(AppState appState, List<NodePath> expanded, CancellationToken cancellationToken)
    {
        var nodes = new List<BrowseNode>();
        foreach (var path in expanded)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = appState.Assemblies.FindNode(path);
            if (node == null)
                continue;
            node.LoadChildren();
            nodes.Add(node);
        }
        return nodes;
    }

    private static BrowseNode?[] FindNodes(AppState appState, List<NodePath> places, CancellationToken cancellationToken)
    {
        var found = new BrowseNode?[places.Count];
        for (var i = 0; i < found.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            found[i] = appState.Assemblies.FindNode(places[i]);
        }
        return found;
    }

    private static void FinishReload(AppState appState, Outcome<Reloaded> outcome, ReloadTicket ticket, int intent, List<ReloadingTab> tabs)
    {
        if (!appState.Reload.Finish(ticket))
            return;
        switch (outcome)
        {
            case Outcome<Reloaded>.Ok ok:
                ShowReloaded(appState, ok.Value, intent, tabs);
                break;
            case Outcome<Reloaded>.Failed failed:
                ShowReopenedDocuments(appState);
                appState.ReportError(new AppProblem.AssembliesNotReloaded(failed.Error));
                break;
            default:
                throw new UnreachableException();
        }
    }

    private static void ShowReloaded(AppState appState, Reloaded reloaded, int intent, List<ReloadingTab> tabs)
    {
        appState.EnqueueCommand(new ViewCommand.ExpandNodes(reloaded.Expanded));
        for (var i = 0; i < tabs.Count; i++)
            ReshowTab(appState, tabs[i], reloaded.Places[i], intent);
        ShowReopenedDocuments(appState);
        SaveSettings(appState);
    }

    private static void ReshowTab(AppState appState, ReloadingTab reloading, BrowseNode? node, int intent)
    {
        if (!ShowAgain(appState, reloading, node))
            return;
        if (reloading.Tab == appState.Tabs.Active && appState.Intent.IsCurrent(intent))
            appState.EnqueueCommand(new ViewCommand.RevealNode(node!));
    }

    /// <summary>Shows <paramref name="node"/>, the tab's place found again, in the tab, or that its place
    /// is gone, unless the tab was closed or shows something else now. True when it shows the node.</summary>
    private static bool ShowAgain(AppState appState, ReloadingTab reloading, BrowseNode? node)
    {
        var tab = reloading.Tab;
        if (!appState.Tabs.Contains(tab))
            return false;
        if (tab.Version != reloading.Version)
            return false;
        if (node == null)
        {
            ReleaseGoneTab(appState, tab, reloading.Location);
            return false;
        }
        LoadIntoTab(appState, tab, node, new Arrival(reloading.Position));
        return true;
    }

    private static void ReleaseGoneTab(AppState appState, DocumentTab tab, NodePath location)
    {
        tab.ShowGone(location);
        appState.ReportError(new AppProblem.GoneAfterReload(location));
    }

    /// <summary>Opens <paramref name="paths"/> in the background as a reload does, so that another
    /// reload or clearing the list stops it, and the settings are saved once it is done.</summary>
    internal static async void OpenAssembliesAsReload(AppState appState, List<string> paths)
    {
        var ticket = appState.Reload.Start();
        var opened = await Background.Run(() => OpenFiles(appState, paths, ticket.Token));
        FinishOpeningAsReload(appState, opened, ticket);
    }

    private static void FinishOpeningAsReload(AppState appState, Outcome<LoadedAssembly?> outcome, ReloadTicket ticket)
    {
        if (!appState.Reload.Finish(ticket))
            return;
        ShowReopenedDocuments(appState);
        switch (outcome)
        {
            case Outcome<LoadedAssembly?>.Ok:
                SaveSettings(appState);
                break;
            case Outcome<LoadedAssembly?>.Failed failed:
                appState.ReportError(new AppProblem.OpenFailed(failed.Error));
                break;
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>Lists the files, those that fail to load as failed, and returns the last.</summary>
    private static LoadedAssembly? OpenFiles(AppState appState, IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        LoadedAssembly? last = null;
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            last = appState.Assemblies.Open(path, cancellationToken);
        }
        return last;
    }

    private sealed record Reloaded(List<BrowseNode> Expanded, BrowseNode?[] Places);

    private sealed record Reopened(LoadedAssembly Item, BrowseNode?[] Places);

    private sealed record ReloadingTab(DocumentTab Tab, NodePath Location, int Version, CodeViewPosition? Position);
}
