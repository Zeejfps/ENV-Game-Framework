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

    /// <summary>Records the open assemblies as the open list's, unless they are being reopened, when
    /// the list still holds them all.</summary>
    internal static void RememberOpenAssemblies(this Workspace w)
    {
        if (w.IsReloading)
            return;
        w.Lists.RememberActive(w.Assemblies.FilePaths);
    }

    /// <summary>Opens the list named <paramref name="name"/> in place of the open one. The assemblies,
    /// documents, history and analyses of the one before go.</summary>
    public static void SwitchList(this Workspace w, string name)
    {
        w.RememberOpenAssemblies();
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
    }

    public static void OpenListsDialog(this Workspace w)
    {
        w.RememberOpenAssemblies();
        w.ListsDialog.Open(w.Lists);
    }

    /// <summary>Adds an empty list and opens it, to fill from the tree.</summary>
    public static void CreateList(this Workspace w)
    {
        var t = Strings.For(w.Locale);
        var name = w.Lists.Create(t.ListsNewName);
        w.ListsDialog.Pick(w.Lists, name);
        w.SwitchList(name);
    }

    /// <summary>Adds a copy of the list named <paramref name="name"/>, leaving the open one open.</summary>
    public static void DuplicateList(this Workspace w, string name)
    {
        var t = Strings.For(w.Locale);
        w.RememberOpenAssemblies();
        var copy = w.Lists.Duplicate(name, t.ListsCopyName(name));
        w.ListsDialog.Pick(w.Lists, copy);
    }

    public static void RenameList(this Workspace w, string name, string newName)
    {
        if (!w.Lists.Rename(name, newName))
            return;
        w.ListsDialog.Pick(w.Lists, newName);
    }

    /// <summary>Removes the list named <paramref name="name"/>. When it was open, the list that takes
    /// its place opens.</summary>
    public static void DeleteList(this Workspace w, string name)
    {
        var wasActive = string.Equals(name, w.Lists.Active, StringComparison.OrdinalIgnoreCase);
        w.RememberOpenAssemblies();
        w.Lists.Delete(name);
        w.ListsDialog.Pick(w.Lists, w.Lists.Active);
        if (wasActive)
            w.OpenActiveList();
    }
}
