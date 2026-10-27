using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Analysis;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.SavedDocuments;

namespace ZGF.ILSpy;

/// <summary>Switching between the named assembly lists, and creating, renaming and deleting them.</summary>
public static class AssemblyLists
{
    /// <summary>Opens the open list's assemblies as the app starts, as a reload, so that the list is
    /// saved whole until they are all open, then shows what the tabs reopened from its documents showed.
    /// Files that are gone or fail to load stay listed as failed.</summary>
    public static void RestoreActiveAssemblyList(AppState appState)
    {
        appState.Assemblies.Pdbs.Use(appState.Lists.ActivePdbs);
        appState.Assemblies.TargetFrameworks.Use(appState.Lists.ActiveFrameworks);
        var paths = appState.Lists.ActivePaths.ToList();
        OpenAssembliesAsReload(appState, paths);
    }

    /// <summary>Opens the list named <paramref name="name"/> as the app starts, in place of the one the
    /// settings had open, with the analyses and documents it had. A list by that name is added when there is none.</summary>
    public static void StartWithAssemblyList(AppState appState, string name)
    {
        if (!appState.Lists.Contains(name))
            appState.Lists.Create(name);
        if (!appState.Lists.Activate(name))
            return;
        appState.Analyzer.Clear();
        appState.Analyzer.AddPending(appState.Lists.ActiveAnalyses);
        ReopenDocuments(appState);
    }

    /// <summary>Takes every assembly, and what was picked for it, off the open list before the app starts opening it.</summary>
    public static void EmptyActiveAssemblyList(AppState appState)
    {
        appState.Lists.RememberActive([]);
        appState.Lists.RememberActivePdbs(new Dictionary<string, string>());
        appState.Lists.RememberActiveFrameworks(new Dictionary<string, string>());
    }

    /// <summary>Records the open assemblies and what was picked for them as the open list's, unless
    /// they are being reopened, when the list still holds them all.</summary>
    public static void RememberOpenAssemblies(AppState appState)
    {
        if (appState.IsReloading)
            return;
        appState.Lists.RememberActive(appState.Assemblies.FilePaths);
        appState.Lists.RememberActivePdbs(appState.Assemblies.Pdbs.All);
        appState.Lists.RememberActiveFrameworks(appState.Assemblies.TargetFrameworks.All);
    }

    /// <summary>Records the open assemblies, the analyses and the documents as the open list's.</summary>
    internal static void RememberActiveAssemblyList(AppState appState)
    {
        RememberOpenAssemblies(appState);
        var analyzed = appState.Analyzer.ListSavedAnalyses();
        appState.Lists.RememberActiveAnalyses(analyzed);
        appState.Lists.RememberActiveDocuments(SaveDocuments(appState));
    }

    /// <summary>Opens the list named <paramref name="name"/> in place of the open one, with the
    /// analyses and documents it had. The assemblies, documents, history and analyses of the one before go, kept
    /// with it for when it opens again.</summary>
    public static void SwitchAssemblyList(AppState appState, string name)
    {
        RememberActiveAssemblyList(appState);
        if (!appState.Lists.Activate(name))
            return;
        OpenActiveList(appState);
    }

    /// <summary>Opens the list named <paramref name="name"/> in place of the open one, as
    /// <see cref="SwitchAssemblyList"/> does, adding an empty one by that name when there is none.</summary>
    public static void OpenAssemblyListNamed(AppState appState, string name)
    {
        if (!appState.Lists.Contains(name))
            appState.Lists.Create(name);
        SwitchAssemblyList(appState, name);
    }

    private static void OpenActiveList(AppState appState)
    {
        CloseAllAssemblies(appState);
        ReopenDocuments(appState);
        appState.History.Clear();
        RestoreActiveAssemblyList(appState);
        appState.Analyzer.AddPending(appState.Lists.ActiveAnalyses);
        RestoreAnalyses(appState);
    }

    /// <summary>Adds an empty list, leaving the open one open, and returns its name.</summary>
    public static string CreateAssemblyList(AppState appState)
    {
        var t = Strings.For(appState.Locale);
        return appState.Lists.Create(t.ListsNewName);
    }

    /// <summary>Adds a list of <paramref name="preset"/>'s assemblies under its name, leaving the open
    /// one open, and returns the name it got.</summary>
    public static string AddAssemblyListPreset(AppState appState, AssemblyListPreset preset)
    {
        return appState.Lists.Create(preset.Name, preset.Paths);
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
