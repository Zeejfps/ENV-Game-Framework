using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>
/// The named assembly lists, the active one marked with a check, each with how many assemblies it
/// holds and a button that activates it. Above them, New list and, for the picked one, Duplicate,
/// Rename and Delete; the same are in a row's context menu. A list is renamed in place, by
/// double-clicking it or pressing F2: Enter or clicking away keeps the name, Escape drops it. Delete
/// is confirmed in a dialog of its own, <see cref="AssemblyListDeleteDialogView"/>.
/// </summary>
internal static class AssemblyListsDialogView
{
    private const string ListId = "lists";
    private const string NameId = "list-name";
    private const string MenuId = "lists-menu";

    private static readonly UiIcon NewIcon = new(LucideIcons.Plus, LucideIcons.Family);

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var dialog = w.ListsDialog;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.ListsDialogWidth) };
        using (var scope = ui.Dialog("assembly-lists", t.ListsTitle, ref dialog.IsOpen, card, closeOnEscape: !dialog.IsAsking))
        {
            if (!scope.IsOpen)
                return;
            Toolbar(ui, w);
            Rows(ui, w);
            RenameProblem(ui, w);
            ContextMenu(ui, w);
            Footer(ui, dialog);
        }
        if (!dialog.IsOpen)
            dialog.Close();
    }

    /// <summary>New list on the left; what can be done to the picked list on the right.</summary>
    private static void Toolbar(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var picked = w.ListsDialog.Picked(w.Lists);
        var any = picked != null;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
        {
            if (ui.Button(t.ListsNew, id: "new", icon: NewIcon))
                w.CreateList();
            ui.Spacer();
            if (ui.IconButton("duplicate", LucideIcons.Copy, LucideIcons.Family, t.ListsDuplicate, any))
                w.DuplicateList(picked!);
            if (ui.IconButton("rename", LucideIcons.Pencil, LucideIcons.Family, t.ListsRename, any))
                w.ListsDialog.StartRenaming(picked!);
            if (ui.IconButton("delete", LucideIcons.Trash2, LucideIcons.Family, t.ListsDelete, any))
                w.ListsDialog.StartConfirmingDelete(picked!);
        }
    }

    /// <summary>A row per list. Enter activates the picked one; a double-click renames it, as F2 does,
    /// and Delete asks to delete it. Picking another row keeps the name being typed, if it may be kept.</summary>
    private static void Rows(Ui<UiState> ui, Workspace w)
    {
        var theme = ui.Theme;
        var dialog = w.ListsDialog;
        var names = w.Lists.Names;
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
                doubleClicked |= Row(ui, w, list, listId, i, names[i]);
            if (list.Focused)
                HandleKeys(ui, w);
            if (list.Activated)
                ActOnRow(w, names[list.Index], doubleClicked);
            if (list.ContextMenuRequested)
                ui.OpenContextMenu(MenuId, list.ContextMenuAt);
        }
        if (dialog.Selection.Index != picked)
            LeavePickedRow(w);
    }

    /// <summary>A check mark when the list is the active one, its name, or the box renaming it, how
    /// many assemblies it holds, and its Activate button, disabled on the active list. True when the
    /// row was double-clicked.</summary>
    private static bool Row(Ui<UiState> ui, Workspace w, ListScope list, int listId, int index, string name)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = w.ListsDialog;
        var active = name == w.Lists.Active;
        using (ui.Id(name))
        using (var row = list.Row(index, ui.GetId("row"), new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            ActiveMark(ui, active);
            if (dialog.Renaming == name)
                NameBox(ui, w, listId);
            else
                NameText(ui, name, row.Selected);
            var count = w.Lists.PathsOf(name).Count;
            ui.Text(t.ListsCount(count), color: theme.TextMuted, size: theme.FontSizeSmall);
            if (ui.Button(t.ListsActivate, enabled: !active, id: "activate", variant: ButtonVariant.Ghost))
                w.SwitchList(name);
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
                ui.Icon(ActiveIcon(theme));
        }
    }

    private static UiIcon ActiveIcon(UiTheme theme) => new(LucideIcons.Check, LucideIcons.Family, theme.Accent);

    private static void NameText(Ui<UiState> ui, string name, bool selected)
    {
        var theme = ui.Theme;
        var color = selected ? theme.SelectionText : theme.Text;
        ui.Text(name, new Box { Width = Sizing.Grow() }, color: color, overflow: TextOverflow.Ellipsis);
    }

    /// <summary>The name being typed for the list being renamed, kept on Enter or once focus moves
    /// elsewhere, dropped on Escape; the list takes the focus back unless it moved elsewhere.</summary>
    private static void NameBox(Ui<UiState> ui, Workspace w, int listId)
    {
        var dialog = w.ListsDialog;
        var begin = dialog.TakeNameFocus();
        var result = ui.InlineEdit(NameId, ref dialog.NewName, begin, Sizing.Grow());
        if (result == InlineEditResult.Editing)
            return;
        var stillHere = ui.HasFocus(ui.GetId(NameId));
        FinishNameBox(w, result);
        if (stillHere || result == InlineEditResult.Cancelled)
            ui.Focus(listId, takesText: false);
    }

    private static void FinishNameBox(Workspace w, InlineEditResult result)
    {
        if (result == InlineEditResult.Committed)
        {
            w.FinishRenaming();
            return;
        }
        w.ListsDialog.StopAsking();
    }

    private static void HandleKeys(Ui<UiState> ui, Workspace w)
    {
        var dialog = w.ListsDialog;
        if (dialog.IsAsking)
            return;
        var picked = dialog.Picked(w.Lists);
        if (picked == null)
            return;
        if (ui.Input.TakeKey(KeyboardKey.F2))
            dialog.StartRenaming(picked);
        if (Shortcuts.DeletePressed(ui))
            dialog.StartConfirmingDelete(picked);
    }

    /// <summary>A double-click renames the row; Enter activates it.</summary>
    private static void ActOnRow(Workspace w, string name, bool doubleClicked)
    {
        if (doubleClicked)
        {
            w.ListsDialog.StartRenaming(name);
            return;
        }
        w.SwitchList(name);
    }

    /// <summary>Keeps the name being typed when another row is picked, and drops a deletion being asked about.</summary>
    private static void LeavePickedRow(Workspace w)
    {
        var dialog = w.ListsDialog;
        if (dialog.Renaming != null)
        {
            w.FinishRenaming();
            return;
        }
        dialog.StopAsking();
    }

    /// <summary>Under the list, why the name typed for the list being renamed cannot be kept.</summary>
    private static void RenameProblem(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var name = w.ListsDialog.Renaming;
        if (name == null)
            return;
        var newName = w.ListsDialog.NewName;
        if (w.Lists.CanRename(name, newName))
            return;
        ui.Text(DescribeRenameProblem(t, newName), color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    private static string DescribeRenameProblem(Strings t, string newName)
    {
        if (newName.Trim().Length == 0)
            return t.ListsNameEmpty;
        return t.ListsNameTaken;
    }

    private static void ContextMenu(Ui<UiState> ui, Workspace w)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var picked = w.ListsDialog.Picked(w.Lists);
        if (picked == null)
            return;
        MenuItems(ui, w, picked);
    }

    private static void MenuItems(Ui<UiState> ui, Workspace w, string picked)
    {
        var t = ui.State.Strings;
        var dialog = w.ListsDialog;
        var active = picked == w.Lists.Active;
        if (ui.MenuItem(t.ListsActivate, enabled: !active, id: "activate"))
            w.SwitchList(picked);
        if (ui.MenuItem(t.ListsRename, "F2", id: "rename"))
            dialog.StartRenaming(picked);
        if (ui.MenuItem(t.ListsDuplicate, id: "duplicate"))
            w.DuplicateList(picked);
        ui.MenuSeparator();
        if (ui.MenuItem(t.ListsDelete, "Delete", id: "delete"))
            dialog.StartConfirmingDelete(picked);
    }

    private static void Footer(Ui<UiState> ui, AssemblyListsDialog dialog)
    {
        var t = ui.State.Strings;
        using (ui.DialogActions())
        {
            if (ui.Button(t.ListsClose, id: "close"))
                dialog.Close();
        }
    }
}
