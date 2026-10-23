using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.ILSpy;

/// <summary>Asks whether an export may write into a folder that already holds other files.</summary>
internal static class ExportConfirmDialogView
{
    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var request = w.ConfirmingExport;
        var open = request != null;
        using (var dialog = ui.Dialog("export-folder", t.ExportFolderNotEmptyTitle, ref open, closeButton: false))
        {
            if (dialog.IsOpen)
            {
                ui.Text(t.ExportFolderNotEmpty(request!.Folder), wrap: true);
                using (ui.DialogActions())
                {
                    if (ui.Button(t.ExportCancel, id: "export-cancel", variant: ButtonVariant.Ghost))
                        w.FinishExportConfirmation(false);
                    if (ui.Button(t.ExportConfirm, id: "export-confirm", variant: ButtonVariant.Primary))
                        w.FinishExportConfirmation(true);
                }
            }
        }
        if (!open && w.ConfirmingExport != null)
            w.FinishExportConfirmation(false);
    }
}
