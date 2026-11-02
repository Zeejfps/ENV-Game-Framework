using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>What the assembly lists dialog is doing: closed; open on the lists; renaming one, with the name
/// typed so far; or asking whether to delete one.</summary>
public abstract class ListsDialogMode
{
    private ListsDialogMode()
    {
    }

    public sealed class Closed : ListsDialogMode
    {
    }

    public sealed class Browsing : ListsDialogMode
    {
    }

    public sealed class Renaming : ListsDialogMode
    {
        private bool _isFocusPending = true;

        public Renaming(string list)
        {
            List = list;
            NewName = list;
        }

        public string List { get; }

        public string NewName;

        /// <summary>True once: the name box should take focus with the whole name selected.</summary>
        public bool TakeFocus()
        {
            var focus = _isFocusPending;
            _isFocusPending = false;
            return focus;
        }
    }

    public sealed class ConfirmingDelete : ListsDialogMode
    {
        public ConfirmingDelete(string list)
        {
            List = list;
        }

        public string List { get; }
    }
}

/// <summary>What the assembly lists dialog shows: what it is doing, and the list picked in it.</summary>
public sealed class AssemblyListsDialog
{
    public ListsDialogMode Mode { get; private set; } = new ListsDialogMode.Closed();

    public bool IsOpen => Mode is not ListsDialogMode.Closed;

    /// <summary>The picked row, an index into <see cref="SavedAssemblyLists.Names"/>.</summary>
    public ListSelection Selection { get; } = new();

    /// <summary>Opens the dialog with the open list of <paramref name="lists"/> picked.</summary>
    public void Open(SavedAssemblyLists lists)
    {
        Mode = new ListsDialogMode.Browsing();
        Pick(lists, lists.Active);
    }

    /// <summary>Picks the list named <paramref name="name"/> and starts renaming it.</summary>
    public void PickToRename(SavedAssemblyLists lists, string name)
    {
        Pick(lists, name);
        StartRenaming(name);
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

    public bool IsAsking => Mode is ListsDialogMode.Renaming or ListsDialogMode.ConfirmingDelete;

    public void StartRenaming(string name) => Mode = new ListsDialogMode.Renaming(name);

    public void StartConfirmingDelete(string name) => Mode = new ListsDialogMode.ConfirmingDelete(name);

    /// <summary>Leaves the rename or deletion being asked about.</summary>
    public void StopAsking()
    {
        if (!IsAsking)
            return;
        Mode = new ListsDialogMode.Browsing();
    }

    public void Close() => Mode = new ListsDialogMode.Closed();
}
