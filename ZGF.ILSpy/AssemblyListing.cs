using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Switching between the named assembly lists, and creating, renaming and deleting them.</summary>
public static class AssemblyListing
{
    /// <summary>Opens the open list's assemblies as the app starts, as a reload, so that the list is
    /// saved whole until they are all open.</summary>
    public static void RestoreActiveList(AppState appState)
    {
        AssemblyLoading.OpenAsReload(appState, ListExistingAssemblies(appState));
    }

    /// <summary>The open list's assemblies whose files are still there.</summary>
    private static List<string> ListExistingAssemblies(AppState appState)
    {
        return appState.Lists.ActivePaths
            .Where(File.Exists)
            .ToList();
    }

    /// <summary>Records the open assemblies as the open list's, unless they are being reopened, when
    /// the list still holds them all.</summary>
    public static void RememberOpenAssemblies(AppState appState)
    {
        if (appState.IsReloading)
            return;
        appState.Lists.RememberActive(appState.Assemblies.FilePaths);
    }

    /// <summary>Records the open assemblies and the analyses as the open list's.</summary>
    internal static void RememberActiveList(AppState appState)
    {
        RememberOpenAssemblies(appState);
        var analyzed = appState.Analyzer.ListSavedAnalyses();
        appState.Lists.RememberActiveAnalyses(analyzed);
    }

    /// <summary>Opens the list named <paramref name="name"/> in place of the open one, with the
    /// analyses it had. The assemblies, documents, history and analyses of the one before go, kept
    /// with it for when it opens again.</summary>
    public static void SwitchList(AppState appState, string name)
    {
        RememberActiveList(appState);
        if (!appState.Lists.Activate(name))
            return;
        OpenActiveList(appState);
    }

    private static void OpenActiveList(AppState appState)
    {
        AssemblyLoading.ClearList(appState);
        Docking.CloseDocuments(appState);
        appState.History.Clear();
        AssemblyLoading.OpenAsReload(appState, ListExistingAssemblies(appState));
        appState.Analyzer.AddPending(appState.Lists.ActiveAnalyses);
        Analyzing.RestoreAnalyses(appState);
    }

    /// <summary>Adds an empty list, leaving the open one open, and returns its name.</summary>
    public static string CreateList(AppState appState)
    {
        var t = Strings.For(appState.Locale);
        return appState.Lists.Create(t.ListsNewName);
    }

    /// <summary>Adds a copy of the list named <paramref name="name"/>, leaving the open one open, and
    /// returns the copy's name.</summary>
    public static string DuplicateList(AppState appState, string name)
    {
        var t = Strings.For(appState.Locale);
        RememberActiveList(appState);
        return appState.Lists.Duplicate(name, t.ListsCopyName(name));
    }

    /// <summary>Removes the list named <paramref name="name"/>. When it was open, the list that takes
    /// its place opens.</summary>
    public static void DeleteList(AppState appState, string name)
    {
        var wasActive = string.Equals(name, appState.Lists.Active, StringComparison.OrdinalIgnoreCase);
        RememberActiveList(appState);
        appState.Lists.Delete(name);
        if (wasActive)
            OpenActiveList(appState);
    }
}
