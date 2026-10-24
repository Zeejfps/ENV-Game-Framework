using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.CodeDisplay;

namespace ZGF.ILSpy;

/// <summary>Asks whether to put every display option back to its default before they are reset.</summary>
internal static class DisplayOptionsResetDialogView
{
    public static void Render(Ui<UiState> ui)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        var open = screen.ConfirmingDisplayReset;
        using (var dialog = ui.Dialog("display-reset", t.DisplayOptionsReset, ref open, closeButton: false))
        {
            if (dialog.IsOpen)
            {
                ui.Text(t.DisplayOptionsResetQuestion, wrap: true);
                using (ui.DialogActions())
                {
                    if (ui.Button(t.DisplayOptionsResetCancel, id: "display-reset-cancel", variant: ButtonVariant.Ghost))
                        FinishDisplayOptionsReset(screen, false);
                    if (ui.Button(t.DisplayOptionsResetConfirm, id: "display-reset-confirm", variant: ButtonVariant.Primary))
                        FinishDisplayOptionsReset(screen, true);
                }
            }
        }
        if (!open && screen.ConfirmingDisplayReset)
            FinishDisplayOptionsReset(screen, false);
    }
}
