using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>Asks, over the assembly lists dialog, whether to delete the list picked there, saying
/// which list opens in its place when it is the active one.</summary>
internal static class AssemblyListDeleteDialogView
{
    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        var dialog = ui.State.ListsDialog;
        var name = dialog.Deleting;
        var open = name != null;
        using (var scope = ui.Dialog("assembly-list-delete", t.ListsDeleteTitle, ref open, closeButton: false))
        {
            if (scope.IsOpen)
            {
                ui.Text(t.ListsDeleteQuestion(name!), wrap: true);
                if (name == appState.Lists.Active)
                    ui.Text(t.ListsDeleteReplacedBy(appState.Lists.ReplacementFor(name!)), color: theme.TextMuted, wrap: true);
                using (ui.DialogActions())
                {
                    if (ui.Button(t.ListsCancel, id: "delete-cancel", variant: ButtonVariant.Ghost))
                        dialog.StopAsking();
                    if (ui.Button(t.ListsDeleteConfirm, id: "delete-confirm", variant: ButtonVariant.Primary))
                        dialog.Delete(appState, name!);
                }
            }
        }
        if (!open && dialog.Deleting != null)
            dialog.StopAsking();
    }
}
