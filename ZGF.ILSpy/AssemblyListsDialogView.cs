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

    public static void Render(Ui<UiState> ui, SavedAssemblyLists lists)
    {
        var t = ui.State.Strings;
        var dialog = ui.State.ListsDialog;
        if (!dialog.IsOpen)
            return;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.ListsDialogWidth) };
        var open = true;
        using (var scope = ui.Dialog("assembly-lists", t.ListsTitle, ref open, card, closeOnEscape: !dialog.IsAsking))
        {
            if (!scope.IsOpen)
                return;
            Toolbar(ui, lists);
            Rows(ui, lists);
            RenameProblem(ui, lists);
            ContextMenu(ui, lists);
            Footer(ui);
        }
        if (!open)
            dialog.Close();
    }

    /// <summary>New list on the left; what can be done to the picked list on the right.</summary>
    private static void Toolbar(Ui<UiState> ui, SavedAssemblyLists lists)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var picked = dialog.Picked(lists);
        var any = picked != null;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
        {
            if (ui.Button(t.ListsNew, id: "new", icon: new UiIcon(ui.State.Icons.ListsNew)))
                ui.State.Later((dialog, name: t.ListsNewName), static (a, s) => Create(s.dialog, a, s.name));
            ui.Spacer();
            if (ui.IconButton("duplicate", ui.State.Icons.ListsDuplicate, t.ListsDuplicate, any) && picked != null)
                ui.State.Later((dialog, name: picked, copyName: t.ListsCopyName(picked)), static (a, s) => Duplicate(s.dialog, a, s.name, s.copyName));
            if (ui.IconButton("rename", ui.State.Icons.ListsRename, t.ListsRename, any) && picked != null)
                dialog.StartRenaming(picked);
            if (ui.IconButton("delete", ui.State.Icons.ListsDelete, t.ListsDelete, any) && picked != null)
                dialog.StartConfirmingDelete(picked);
        }
    }

    /// <summary>A row per list. Enter activates the picked one; a double-click renames it, as F2 does,
    /// and Delete asks to delete it. Picking another row keeps the name being typed, if it may be kept.</summary>
    private static void Rows(Ui<UiState> ui, SavedAssemblyLists lists)
    {
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var names = lists.Names;
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
                doubleClicked |= Row(ui, lists, list, listId, i, names[i]);
            if (list.Focused)
                HandleKeys(ui, lists);
            if (list.Activated)
                ActOnRow(ui, names[list.Index], doubleClicked);
            if (list.ContextMenuRequested)
                ui.OpenContextMenu(MenuId, list.ContextMenuAt);
        }
        if (dialog.Selection.Index != picked)
            LeavePickedRow(ui);
    }

    /// <summary>A check mark when the list is the active one, its name, or the box renaming it, how
    /// many assemblies it holds, and its Activate button, disabled on the active list. True when the
    /// row was double-clicked.</summary>
    private static bool Row(Ui<UiState> ui, SavedAssemblyLists lists, ListScope list, int listId, int index, string name)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var active = name == lists.Active;
        using (ui.Id(name))
        using (var row = list.Row(index, ui.GetId("row"), new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            ActiveMark(ui, active);
            if (dialog.Mode is ListsDialogMode.Renaming renaming && renaming.List == name)
                NameBox(ui, renaming, listId);
            else
                NameText(ui, name, row.Selected);
            var count = lists.PathsOf(name).Count;
            ui.Text(t.ListsCount(count), color: theme.TextMuted, size: theme.FontSizeSmall);
            if (ui.Button(t.ListsActivate, enabled: !active, id: "activate", variant: ButtonVariant.Ghost))
                ui.State.Later(name, static (a, n) => SwitchAssemblyList(a, n));
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
    private static void NameBox(Ui<UiState> ui, ListsDialogMode.Renaming renaming, int listId)
    {
        var begin = renaming.TakeFocus();
        var result = ui.InlineEdit(NameId, ref renaming.NewName, begin, Sizing.Grow());
        if (result == InlineEditResult.Editing)
            return;
        var stillHere = ui.HasFocus(ui.GetId(NameId));
        FinishNameBox(ui, result);
        if (stillHere || result == InlineEditResult.Cancelled)
            ui.Focus(listId, takesText: false);
    }

    private static void FinishNameBox(Ui<UiState> ui, InlineEditResult result)
    {
        if (result == InlineEditResult.Committed)
        {
            FinishRenaming(ui.State);
            return;
        }
        ui.State.ListsDialog.StopAsking();
    }

    private static void HandleKeys(Ui<UiState> ui, SavedAssemblyLists lists)
    {
        var dialog = ui.State.ListsDialog;
        if (dialog.IsAsking)
            return;
        var picked = dialog.Picked(lists);
        if (picked == null)
            return;
        if (ui.Input.TakeKey(KeyboardKey.F2))
            dialog.StartRenaming(picked);
        if (ui.IsPressed(Shortcuts.Delete))
            dialog.StartConfirmingDelete(picked);
    }

    /// <summary>A double-click renames the row; Enter activates it.</summary>
    private static void ActOnRow(Ui<UiState> ui, string name, bool doubleClicked)
    {
        if (doubleClicked)
        {
            ui.State.ListsDialog.StartRenaming(name);
            return;
        }
        ui.State.Later(name, static (a, n) => SwitchAssemblyList(a, n));
    }

    /// <summary>Keeps the name being typed when another row is picked, and drops a deletion being asked about.</summary>
    private static void LeavePickedRow(Ui<UiState> ui)
    {
        var dialog = ui.State.ListsDialog;
        if (dialog.Mode is ListsDialogMode.Renaming)
        {
            FinishRenaming(ui.State);
            return;
        }
        dialog.StopAsking();
    }

    /// <summary>Under the list, why the name typed for the list being renamed cannot be kept.</summary>
    private static void RenameProblem(Ui<UiState> ui, SavedAssemblyLists lists)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        if (dialog.Mode is not ListsDialogMode.Renaming renaming)
            return;
        var newName = renaming.NewName;
        if (lists.CanRename(renaming.List, newName))
            return;
        ui.Text(DescribeRenameProblem(t, newName), color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    private static string DescribeRenameProblem(Strings t, string newName)
    {
        if (newName.Trim().Length == 0)
            return t.ListsNameEmpty;
        return t.ListsNameTaken;
    }

    private static void ContextMenu(Ui<UiState> ui, SavedAssemblyLists lists)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var picked = ui.State.ListsDialog.Picked(lists);
        if (picked == null)
            return;
        MenuItems(ui, lists, picked);
    }

    private static void MenuItems(Ui<UiState> ui, SavedAssemblyLists lists, string picked)
    {
        var t = ui.State.Strings;
        var dialog = ui.State.ListsDialog;
        var active = picked == lists.Active;
        if (ui.MenuItem(t.ListsActivate, enabled: !active, id: "activate"))
            ui.State.Later(picked, static (a, n) => SwitchAssemblyList(a, n));
        if (ui.MenuItem(t.ListsRename, Shortcuts.Rename, id: "rename"))
            dialog.StartRenaming(picked);
        if (ui.MenuItem(t.ListsDuplicate, id: "duplicate"))
            ui.State.Later((dialog, name: picked, copyName: t.ListsCopyName(picked)), static (a, s) => Duplicate(s.dialog, a, s.name, s.copyName));
        ui.MenuSeparator();
        if (ui.MenuItem(t.ListsDelete, Shortcuts.Delete, id: "delete"))
            dialog.StartConfirmingDelete(picked);
    }

    /// <summary>Add preconfigured on the left, Close on the right.</summary>
    private static void Footer(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var dialog = ui.State.ListsDialog;
        using (ui.DialogActions())
        {
            PresetMenu(ui);
            ui.Spacer();
            if (ui.Button(t.ListsClose, id: "close"))
                dialog.Close();
        }
    }

    /// <summary>The frameworks installed here, each with how many assemblies it holds; picking one adds
    /// a list of them, picked to be named. The frameworks are looked for the first time the menu opens.</summary>
    private static void PresetMenu(Ui<UiState> ui)
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
                ui.State.Later((dialog: ui.State.ListsDialog, preset), static (a, s) => AddPreset(s.dialog, a, s.preset));
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
    private static void Create(AssemblyListsDialog dialog, AppState appState, string newName)
    {
        var name = CreateAssemblyList(appState, newName);
        dialog.PickToRename(appState.Lists, name);
    }

    /// <summary>Adds a copy of the list named <paramref name="name"/>, picked to be named, leaving the
    /// open one open.</summary>
    private static void Duplicate(AssemblyListsDialog dialog, AppState appState, string name, string copyName)
    {
        var copy = DuplicateAssemblyList(appState, name, copyName);
        dialog.PickToRename(appState.Lists, copy);
    }

    /// <summary>Gives the list being renamed the name typed, when it may have it, and stops renaming either way.</summary>
    private static void FinishRenaming(UiState screen)
    {
        var dialog = screen.ListsDialog;
        if (dialog.Mode is not ListsDialogMode.Renaming renaming)
            return;
        dialog.StopAsking();
        screen.Later((list: renaming.List, newName: renaming.NewName), static (a, s) => a.Lists.Rename(s.list, s.newName));
    }
}
