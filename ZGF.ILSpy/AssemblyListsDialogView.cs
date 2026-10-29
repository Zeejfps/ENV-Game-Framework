using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.AssemblyLists;

namespace ZGF.ILSpy;

/// <summary>
/// The named assembly lists, the active one marked with a check, each with how many assemblies it
/// holds and a button that activates it. Above them, New list and, for the picked one, Duplicate,
/// Rename and Delete; the same are in a row's context menu. A list is renamed in place, by
/// double-clicking it or pressing F2: Enter or clicking away keeps the name, Escape drops it. Delete
/// is confirmed in a dialog of its own, <see cref="AssemblyListDeleteDialogView"/>. Below them, a menu
/// adds a list of the assemblies of a framework installed here.
/// </summary>
internal static class AssemblyListsDialogView
{
    private const string ListId = "lists";
    private const string NameId = "list-name";
    private const string MenuId = "lists-menu";
    private const string PresetMenuId = "presets";

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var dialog = ui.State.ListsDialog;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.ListsDialogWidth) };
        using (var scope = ui.Dialog("assembly-lists", t.ListsTitle, ref dialog.IsOpen, card, closeOnEscape: !dialog.IsAsking))
        {
            if (!scope.IsOpen)
                return;
            Toolbar(ui, appState);
            Rows(ui, appState);
            RenameProblem(ui, appState);
            ContextMenu(ui, appState);
            Footer(ui, appState);
        }
        if (!dialog.IsOpen)
            dialog.Close();
    }

    /// <summary>New list on the left; what can be done to the picked list on the right.</summary>
    private static void Toolbar(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var picked = dialog.Picked(appState.Lists);
        var any = picked != null;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
        {
            if (ui.Button(t.ListsNew, id: "new", icon: new UiIcon(ui.State.Icons.ListsNew)))
                Create(dialog, appState, t);
            ui.Spacer();
            if (ui.IconButton("duplicate", ui.State.Icons.ListsDuplicate, t.ListsDuplicate, any))
                Duplicate(dialog, appState, t, picked!);
            if (ui.IconButton("rename", ui.State.Icons.ListsRename, t.ListsRename, any))
                dialog.StartRenaming(picked!);
            if (ui.IconButton("delete", ui.State.Icons.ListsDelete, t.ListsDelete, any))
                dialog.StartConfirmingDelete(picked!);
        }
    }

    /// <summary>A row per list. Enter activates the picked one; a double-click renames it, as F2 does,
    /// and Delete asks to delete it. Picking another row keeps the name being typed, if it may be kept.</summary>
    private static void Rows(Ui<UiState> ui, AppState appState)
    {
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var names = appState.Lists.Names;
        var picked = dialog.Selection.Index;
        var rowHeight = theme.ControlHeight + theme.GapTight;
        var frame = new Box
        {
            Height = AppLayout.ListsDialogListHeight,
            Padding = theme.BorderWidth,
            Background = theme.Field,
            BorderColor = theme.Border,
            BorderWidth = theme.BorderWidth,
            Radius = theme.RadiusSmall,
            Clip = true,
        };
        using (ui.Panel(frame))
        {
            var listId = ui.GetId(ListId);
            using var list = ui.SelectableList(listId, names.Count, dialog.Selection, rowHeight);
            var doubleClicked = false;
            for (var i = list.First; i < list.End; i++)
                doubleClicked |= Row(ui, appState, list, listId, i, names[i]);
            if (list.Focused)
                HandleKeys(ui, appState);
            if (list.Activated)
                ActOnRow(ui, appState, names[list.Index], doubleClicked);
            if (list.ContextMenuRequested)
                ui.OpenContextMenu(MenuId, list.ContextMenuAt);
        }
        if (dialog.Selection.Index != picked)
            LeavePickedRow(ui, appState);
    }

    /// <summary>A check mark when the list is the active one, its name, or the box renaming it, how
    /// many assemblies it holds, and its Activate button, disabled on the active list. True when the
    /// row was double-clicked.</summary>
    private static bool Row(Ui<UiState> ui, AppState appState, ListScope list, int listId, int index, string name)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var active = name == appState.Lists.Active;
        using (ui.Id(name))
        using (var row = list.Row(index, ui.GetId("row"), new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            ActiveMark(ui, active);
            if (dialog.Renaming == name)
                NameBox(ui, appState, listId);
            else
                NameText(ui, name, row.Selected);
            var count = appState.Lists.PathsOf(name).Count;
            ui.Text(t.ListsCount(count), color: theme.TextMuted, size: theme.FontSizeSmall);
            if (ui.Button(t.ListsActivate, enabled: !active, id: "activate", variant: ButtonVariant.Ghost))
                SwitchAssemblyList(appState, name);
            return row.Interaction.DoubleClicked;
        }
    }

    /// <summary>The column that marks the active list with a check, empty on the others.</summary>
    private static void ActiveMark(Ui<UiState> ui, bool active)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = theme.IconSize, Height = Sizing.Grow() }))
        {
            if (active)
                ui.Icon(new UiIcon(ui.State.Icons.ListsActive, theme.Accent));
        }
    }

    private static void NameText(Ui<UiState> ui, string name, bool selected)
    {
        var theme = ui.Theme;
        var color = selected ? theme.SelectionText : theme.Text;
        ui.Text(name, new Box { Width = Sizing.Grow() }, color: color, overflow: TextOverflow.Ellipsis);
    }

    /// <summary>The name being typed for the list being renamed, kept on Enter or once focus moves
    /// elsewhere, dropped on Escape; the list takes the focus back unless it moved elsewhere.</summary>
    private static void NameBox(Ui<UiState> ui, AppState appState, int listId)
    {
        var dialog = ui.State.ListsDialog;
        var begin = dialog.TakeNameFocus();
        var result = ui.InlineEdit(NameId, ref dialog.NewName, begin, Sizing.Grow());
        if (result == InlineEditResult.Editing)
            return;
        var stillHere = ui.HasFocus(ui.GetId(NameId));
        FinishNameBox(ui, appState, result);
        if (stillHere || result == InlineEditResult.Cancelled)
            ui.Focus(listId, takesText: false);
    }

    private static void FinishNameBox(Ui<UiState> ui, AppState appState, InlineEditResult result)
    {
        var dialog = ui.State.ListsDialog;
        if (result == InlineEditResult.Committed)
        {
            FinishRenaming(dialog, appState.Lists);
            return;
        }
        dialog.StopAsking();
    }

    private static void HandleKeys(Ui<UiState> ui, AppState appState)
    {
        var dialog = ui.State.ListsDialog;
        if (dialog.IsAsking)
            return;
        var picked = dialog.Picked(appState.Lists);
        if (picked == null)
            return;
        if (ui.Input.TakeKey(KeyboardKey.F2))
            dialog.StartRenaming(picked);
        if (ui.IsPressed(Shortcuts.Delete))
            dialog.StartConfirmingDelete(picked);
    }

    /// <summary>A double-click renames the row; Enter activates it.</summary>
    private static void ActOnRow(Ui<UiState> ui, AppState appState, string name, bool doubleClicked)
    {
        if (doubleClicked)
        {
            ui.State.ListsDialog.StartRenaming(name);
            return;
        }
        SwitchAssemblyList(appState, name);
    }

    /// <summary>Keeps the name being typed when another row is picked, and drops a deletion being asked about.</summary>
    private static void LeavePickedRow(Ui<UiState> ui, AppState appState)
    {
        var dialog = ui.State.ListsDialog;
        if (dialog.Renaming != null)
        {
            FinishRenaming(dialog, appState.Lists);
            return;
        }
        dialog.StopAsking();
    }

    /// <summary>Under the list, why the name typed for the list being renamed cannot be kept.</summary>
    private static void RenameProblem(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var name = dialog.Renaming;
        if (name == null)
            return;
        var newName = dialog.NewName;
        if (appState.Lists.CanRename(name, newName))
            return;
        ui.Text(DescribeRenameProblem(t, newName), color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    private static string DescribeRenameProblem(Strings t, string newName)
    {
        if (newName.Trim().Length == 0)
            return t.ListsNameEmpty;
        return t.ListsNameTaken;
    }

    private static void ContextMenu(Ui<UiState> ui, AppState appState)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var picked = ui.State.ListsDialog.Picked(appState.Lists);
        if (picked == null)
            return;
        MenuItems(ui, appState, picked);
    }

    private static void MenuItems(Ui<UiState> ui, AppState appState, string picked)
    {
        var t = ui.State.Strings;
        var dialog = ui.State.ListsDialog;
        var active = picked == appState.Lists.Active;
        if (ui.MenuItem(t.ListsActivate, enabled: !active, id: "activate"))
            SwitchAssemblyList(appState, picked);
        if (ui.MenuItem(t.ListsRename, Shortcuts.Rename, id: "rename"))
            dialog.StartRenaming(picked);
        if (ui.MenuItem(t.ListsDuplicate, id: "duplicate"))
            Duplicate(dialog, appState, t, picked);
        ui.MenuSeparator();
        if (ui.MenuItem(t.ListsDelete, Shortcuts.Delete, id: "delete"))
            dialog.StartConfirmingDelete(picked);
    }

    /// <summary>Add preconfigured on the left, Close on the right.</summary>
    private static void Footer(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var dialog = ui.State.ListsDialog;
        using (ui.DialogActions())
        {
            PresetMenu(ui, appState);
            ui.Spacer();
            if (ui.Button(t.ListsClose, id: "close"))
                dialog.Close();
        }
    }

    /// <summary>The frameworks installed here, each with how many assemblies it holds; picking one adds
    /// a list of them, picked to be named. The frameworks are looked for the first time the menu opens.</summary>
    private static void PresetMenu(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        using var menu = ui.SelectButton(PresetMenuId, t.ListsAddPreset);
        if (!menu.IsOpen)
            return;
        var presets = AssemblyListPresets.Installed;
        if (presets.Count == 0)
            ui.MenuItem(t.ListsNoPresets, enabled: false);
        foreach (var preset in presets)
        {
            var count = t.ListsCount(preset.Paths.Count);
            if (ui.MenuItem(preset.Name, detail: count))
                AddPreset(ui.State.ListsDialog, appState, preset);
        }
    }

    /// <summary>Adds a list of <paramref name="preset"/>'s assemblies, picked to be named, leaving the
    /// open one open.</summary>
    private static void AddPreset(AssemblyListsDialog dialog, AppState appState, AssemblyListPreset preset)
    {
        var name = AddAssemblyListPreset(appState, preset);
        dialog.PickToRename(appState.Lists, name);
    }

    /// <summary>Opens the dialog with the open list picked, its assemblies counted as they are now.</summary>
    internal static void Open(AssemblyListsDialog dialog, AppState appState)
    {
        RememberOpenAssemblies(appState);
        dialog.Open(appState.Lists);
    }

    /// <summary>Adds an empty list, picked to be named, leaving the open one open.</summary>
    private static void Create(AssemblyListsDialog dialog, AppState appState, Strings t)
    {
        var name = CreateAssemblyList(appState, t.ListsNewName);
        dialog.PickToRename(appState.Lists, name);
    }

    /// <summary>Adds a copy of the list named <paramref name="name"/>, picked to be named, leaving the
    /// open one open.</summary>
    private static void Duplicate(AssemblyListsDialog dialog, AppState appState, Strings t, string name)
    {
        var copy = DuplicateAssemblyList(appState, name, t.ListsCopyName(name));
        dialog.PickToRename(appState.Lists, copy);
    }

    /// <summary>Gives the list being renamed the name typed, when it may have it, and stops renaming either way.</summary>
    private static void FinishRenaming(AssemblyListsDialog dialog, SavedAssemblyLists lists)
    {
        var name = dialog.Renaming;
        if (name == null)
            return;
        var newName = dialog.NewName;
        dialog.StopAsking();
        lists.Rename(name, newName);
    }
}
