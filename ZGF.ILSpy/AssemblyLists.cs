using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Docking;

namespace ZGF.ILSpy;

/// <summary>Switching between the named assembly lists, and creating, renaming and deleting them.</summary>
public static class AssemblyLists
{
    /// <summary>Opens the open list's assemblies as the app starts, as a reload, so that the list is
    /// saved whole until they are all open.</summary>
    public static void RestoreActiveAssemblyList(AppState appState)
    {
        var existingAssemblies = ListExistingAssemblies(appState);
        OpenAssembliesAsReload(appState, existingAssemblies);
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
    internal static void RememberActiveAssemblyList(AppState appState)
    {
        RememberOpenAssemblies(appState);
        var analyzed = appState.Analyzer.ListSavedAnalyses();
        appState.Lists.RememberActiveAnalyses(analyzed);
    }

    /// <summary>Opens the list named <paramref name="name"/> in place of the open one, with the
    /// analyses it had. The assemblies, documents, history and analyses of the one before go, kept
    /// with it for when it opens again.</summary>
    public static void SwitchAssemblyList(AppState appState, string name)
    {
        RememberActiveAssemblyList(appState);
        if (!appState.Lists.Activate(name))
            return;
        OpenActiveList(appState);
    }

    private static void OpenActiveList(AppState appState)
    {
        CloseAllAssemblies(appState);
        CloseDocuments(appState);
        appState.History.Clear();
        OpenAssembliesAsReload(appState, ListExistingAssemblies(appState));
        appState.Analyzer.AddPending(appState.Lists.ActiveAnalyses);
        RestoreAnalyses(appState);
    }

    /// <summary>Adds an empty list, leaving the open one open, and returns its name.</summary>
    public static string CreateAssemblyList(AppState appState)
    {
        var t = Strings.For(appState.Locale);
        return appState.Lists.Create(t.ListsNewName);
    }

    /// <summary>Adds a copy of the list named <paramref name="name"/>, leaving the open one open, and
    /// returns the copy's name.</summary>
    public static string DuplicateAssemblyList(AppState appState, string name)
    {
        var t = Strings.For(appState.Locale);
        RememberActiveAssemblyList(appState);
        return appState.Lists.Duplicate(name, t.ListsCopyName(name));
    }

    /// <summary>Removes the list named <paramref name="name"/>. When it was open, the list that takes
    /// its place opens.</summary>
    public static void DeleteAssemblyList(AppState appState, string name)
    {
        var wasActive = string.Equals(name, appState.Lists.Active, StringComparison.OrdinalIgnoreCase);
        RememberActiveAssemblyList(appState);
        appState.Lists.Delete(name);
        if (wasActive)
            OpenActiveList(appState);
    }
}
