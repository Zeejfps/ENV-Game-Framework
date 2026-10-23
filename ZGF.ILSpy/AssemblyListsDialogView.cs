using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>
/// The named assembly lists, the open one marked, each with how many assemblies it holds. The
/// picked one can be duplicated, renamed in a box under the list, deleted once that is confirmed
/// there, or switched to, as Enter or a double-click on it does. A new list opens straight away, to
/// fill from the tree.
/// </summary>
internal static class AssemblyListsDialogView
{
    private const string ListId = "lists";
    private const string NameId = "list-name";

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var dialog = w.ListsDialog;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.ListsDialogWidth) };
        using (var scope = ui.Dialog("assembly-lists", t.ListsTitle, ref dialog.IsOpen, card))
        {
            if (!scope.IsOpen)
                return;
            Rows(ui, w);
            Question(ui, w);
            Actions(ui, w);
        }
        if (!dialog.IsOpen)
            dialog.Close();
    }

    private static void Rows(Ui<UiState> ui, Workspace w)
    {
        var theme = ui.Theme;
        var lists = w.Lists;
        var dialog = w.ListsDialog;
        var names = lists.Names;
        var picked = dialog.Selection.Index;
        var frame = new Box { Height = AppLayout.ListsDialogListHeight, Padding = theme.BorderWidth, Background = theme.Field, Clip = true };
        using (ui.Panel(frame))
        {
            using var list = ui.SelectableList(ui.GetId(ListId), names.Count, dialog.Selection);
            for (var i = list.First; i < list.End; i++)
                Row(ui, w, list, i, names[i]);
            if (list.Activated)
                w.SwitchList(names[list.Index]);
        }
        if (dialog.Selection.Index != picked)
            dialog.StopAsking();
    }

    /// <summary>A list's name, marked when it is the open one, and how many assemblies it holds.</summary>
    private static void Row(Ui<UiState> ui, Workspace w, ListScope list, int index, string name)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var lists = w.Lists;
        var active = name == lists.Active;
        using (ui.Id(name))
        using (var row = list.Row(index, ui.GetId("row"), new Box { Gap = theme.Gap, AlignY = Align.Center }))
        {
            var color = row.Selected ? theme.SelectionText : theme.Text;
            ui.Text(name, new Box { Width = Sizing.Grow() }, color: color, overflow: TextOverflow.Ellipsis);
            if (active)
                ui.Text(t.ListsOpen, color: theme.TextMuted, size: theme.FontSizeSmall);
            var count = lists.PathsOf(name).Count;
            ui.Text(t.ListsCount(count), color: theme.TextMuted, size: theme.FontSizeSmall);
        }
    }

    /// <summary>The new name of the list being renamed, or whether to delete the one asked about.</summary>
    private static void Question(Ui<UiState> ui, Workspace w)
    {
        var dialog = w.ListsDialog;
        if (dialog.Renaming != null)
            RenameBox(ui, w, dialog.Renaming);
        if (dialog.Deleting != null)
            DeleteQuestion(ui, w, dialog.Deleting);
    }

    private static void RenameBox(Ui<UiState> ui, Workspace w, string name)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = w.ListsDialog;
        if (dialog.TakeNameFocus())
            ui.Focus(ui.GetId(NameId));
        var valid = w.Lists.CanRename(name, dialog.NewName);
        using (ui.Row(new Box { Gap = theme.GapTight }))
        {
            var entered = ui.TextField(NameId, ref dialog.NewName, t.ListsNewName, Sizing.Grow());
            var saved = ui.Button(t.ListsSaveName, enabled: valid, id: "rename-save", variant: ButtonVariant.Primary);
            if (ui.Button(t.ListsCancel, id: "rename-cancel", variant: ButtonVariant.Ghost))
                dialog.StopAsking();
            var confirmed = entered || saved;
            if (valid && confirmed)
                w.RenameList(name, dialog.NewName);
        }
        if (!valid)
            ui.Text(RenameProblem(t, dialog.NewName), color: theme.Danger, size: theme.FontSizeSmall, wrap: true);
    }

    private static string RenameProblem(Strings t, string newName)
    {
        if (newName.Trim().Length == 0)
            return t.ListsNameEmpty;
        return t.ListsNameTaken;
    }

    private static void DeleteQuestion(Ui<UiState> ui, Workspace w, string name)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = w.ListsDialog;
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.Text(t.ListsDeleteQuestion(name), new Box { Width = Sizing.Grow() }, wrap: true);
            if (ui.Button(t.ListsCancel, id: "delete-cancel", variant: ButtonVariant.Ghost))
                dialog.StopAsking();
            if (ui.Button(t.ListsDelete, id: "delete-confirm", variant: ButtonVariant.Primary))
                w.DeleteList(name);
        }
    }

    /// <summary>What can be done to the picked list on the left, and switching to it on the right.</summary>
    private static void Actions(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = w.ListsDialog;
        var picked = dialog.Picked(w.Lists);
        var canAct = picked != null && !dialog.IsAsking;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Center }))
        {
            if (ui.Button(t.ListsNew, enabled: !dialog.IsAsking, id: "new"))
                w.CreateList();
            if (ui.Button(t.ListsDuplicate, enabled: canAct, id: "duplicate"))
                w.DuplicateList(picked!);
            if (ui.Button(t.ListsRename, enabled: canAct, id: "rename"))
                dialog.StartRenaming(picked!);
            if (ui.Button(t.ListsDelete, enabled: canAct, id: "delete"))
                dialog.StartConfirmingDelete(picked!);
            ui.Spacer();
            if (ui.Button(t.ListsClose, id: "close", variant: ButtonVariant.Ghost))
                dialog.Close();
            var canSwitch = canAct && picked != w.Lists.Active;
            if (ui.Button(t.ListsSwitch, enabled: canSwitch, id: "switch", variant: ButtonVariant.Primary))
                w.SwitchList(picked!);
        }
    }
}
