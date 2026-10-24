using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>What the assembly lists dialog shows: whether it is open, the list picked in it, and
/// the list being renamed, with the name typed so far, or whose deletion is being confirmed.</summary>
public sealed class AssemblyListsDialog
{
    private bool _focusName;

    public bool IsOpen;

    /// <summary>The picked row, an index into <see cref="SavedAssemblyLists.Names"/>.</summary>
    public ListSelection Selection { get; } = new();

    /// <summary>The list being renamed, or null.</summary>
    public string? Renaming { get; private set; }

    public string NewName = "";

    /// <summary>The list whose deletion is being confirmed, or null.</summary>
    public string? Deleting { get; private set; }

    /// <summary>Opens the dialog with the open list picked, its assemblies counted as they are now.</summary>
    public void Open(AppState appState)
    {
        AssemblyListing.RememberOpenAssemblies(appState);
        IsOpen = true;
        Pick(appState.Lists, appState.Lists.Active);
    }

    /// <summary>Adds an empty list, picked to be named, leaving the open one open.</summary>
    public void Create(AppState appState)
    {
        var name = AssemblyListing.CreateList(appState);
        PickToRename(appState.Lists, name);
    }

    /// <summary>Adds a copy of the list named <paramref name="name"/>, picked to be named, leaving the
    /// open one open.</summary>
    public void Duplicate(AppState appState, string name)
    {
        var copy = AssemblyListing.DuplicateList(appState, name);
        PickToRename(appState.Lists, copy);
    }

    private void PickToRename(SavedAssemblyLists lists, string name)
    {
        Pick(lists, name);
        StartRenaming(name);
    }

    /// <summary>Removes the list named <paramref name="name"/> and picks the open list, which takes its
    /// place when it was open.</summary>
    public void Delete(AppState appState, string name)
    {
        AssemblyListing.DeleteList(appState, name);
        Pick(appState.Lists, appState.Lists.Active);
    }

    /// <summary>Picks the list named <paramref name="name"/>, leaving any rename or deletion.</summary>
    public void Pick(SavedAssemblyLists lists, string name)
    {
        var names = lists.Names;
        var index = FindIndex(names, name);
        Selection.Select(index, names.Count);
        StopAsking();
    }

    private static int FindIndex(IReadOnlyList<string> names, string name)
    {
        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], name.Trim(), StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    /// <summary>The name of the picked list, or null.</summary>
    public string? Picked(SavedAssemblyLists lists)
    {
        var names = lists.Names;
        var index = Selection.Index;
        if (index < 0)
            return null;
        if (index >= names.Count)
            return null;
        return names[index];
    }

    public bool IsAsking => Renaming != null || Deleting != null;

    public void StartRenaming(string name)
    {
        StopAsking();
        Renaming = name;
        NewName = name;
        _focusName = true;
    }

    /// <summary>Gives the list being renamed the name typed, when it may have it, and stops renaming either way.</summary>
    public void FinishRenaming(SavedAssemblyLists lists)
    {
        var name = Renaming;
        if (name == null)
            return;
        var newName = NewName;
        StopAsking();
        lists.Rename(name, newName);
    }

    /// <summary>True once after <see cref="StartRenaming"/>: the name box should take focus with the
    /// whole name selected.</summary>
    public bool TakeNameFocus()
    {
        var focus = _focusName;
        _focusName = false;
        return focus;
    }

    public void StartConfirmingDelete(string name)
    {
        StopAsking();
        Deleting = name;
    }

    /// <summary>Leaves the rename or deletion being asked about.</summary>
    public void StopAsking()
    {
        Renaming = null;
        Deleting = null;
        _focusName = false;
    }

    public void Close()
    {
        IsOpen = false;
        StopAsking();
    }
}
