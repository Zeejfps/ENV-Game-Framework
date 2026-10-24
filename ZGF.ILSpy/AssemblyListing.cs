using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Switching between the named assembly lists, and creating, renaming and deleting them.</summary>
public static class AssemblyListing
{
    /// <summary>The open list's assemblies whose files are still there.</summary>
    public static List<string> ListExistingAssemblies(this Workspace w)
    {
        return w.Lists.ActivePaths
            .Where(File.Exists)
            .ToList();
    }

    /// <summary>Opens the open list's assemblies as the app starts, as a reload, so that the list is
    /// saved whole until they are all open.</summary>
    public static void RestoreActiveList(this Workspace w)
    {
        w.OpenAsReload(w.ListExistingAssemblies());
    }

    /// <summary>Records the open assemblies as the open list's, unless they are being reopened, when
    /// the list still holds them all.</summary>
    public static void RememberOpenAssemblies(this Workspace w)
    {
        if (w.IsReloading)
            return;
        w.Lists.RememberActive(w.Assemblies.FilePaths);
    }

    /// <summary>Records the open assemblies and the analyses as the open list's.</summary>
    internal static void RememberActiveList(this Workspace w)
    {
        w.RememberOpenAssemblies();
        var analyzed = w.Analyzer.ListSavedAnalyses();
        w.Lists.RememberActiveAnalyses(analyzed);
    }

    /// <summary>Opens the list named <paramref name="name"/> in place of the open one, with the
    /// analyses it had. The assemblies, documents, history and analyses of the one before go, kept
    /// with it for when it opens again.</summary>
    public static void SwitchList(this Workspace w, string name)
    {
        w.RememberActiveList();
        if (!w.Lists.Activate(name))
            return;
        w.OpenActiveList();
    }

    private static void OpenActiveList(this Workspace w)
    {
        w.ClearList();
        w.CloseDocuments();
        w.History.Clear();
        w.OpenAsReload(w.ListExistingAssemblies());
        w.Analyzer.AddPending(w.Lists.ActiveAnalyses);
        w.RestoreAnalyses();
    }

    /// <summary>Adds an empty list, leaving the open one open, and returns its name.</summary>
    public static string CreateList(this Workspace w)
    {
        var t = Strings.For(w.Locale);
        return w.Lists.Create(t.ListsNewName);
    }

    /// <summary>Adds a copy of the list named <paramref name="name"/>, leaving the open one open, and
    /// returns the copy's name.</summary>
    public static string DuplicateList(this Workspace w, string name)
    {
        var t = Strings.For(w.Locale);
        w.RememberActiveList();
        return w.Lists.Duplicate(name, t.ListsCopyName(name));
    }

    /// <summary>Removes the list named <paramref name="name"/>. When it was open, the list that takes
    /// its place opens.</summary>
    public static void DeleteList(this Workspace w, string name)
    {
        var wasActive = string.Equals(name, w.Lists.Active, StringComparison.OrdinalIgnoreCase);
        w.RememberActiveList();
        w.Lists.Delete(name);
        if (wasActive)
            w.OpenActiveList();
    }
}
