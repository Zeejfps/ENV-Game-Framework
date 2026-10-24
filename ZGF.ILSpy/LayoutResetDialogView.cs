using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy;

/// <summary>Asks whether to put every panel back where it started before the layout is reset.</summary>
internal static class LayoutResetDialogView
{
    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var t = ui.State.Strings;
        var screen = ui.State;
        var open = screen.ConfirmingLayoutReset;
        using (var dialog = ui.Dialog("layout-reset", t.ViewResetLayout, ref open, closeButton: false))
        {
            if (dialog.IsOpen)
            {
                ui.Text(t.ViewResetLayoutQuestion, wrap: true);
                using (ui.DialogActions())
                {
                    if (ui.Button(t.ViewResetLayoutCancel, id: "reset-cancel", variant: ButtonVariant.Ghost))
                        FinishLayoutReset(screen, false);
                    if (ui.Button(t.ViewResetLayoutConfirm, id: "reset-confirm", variant: ButtonVariant.Primary))
                        FinishLayoutReset(screen, true);
                }
            }
        }
        if (!open && screen.ConfirmingLayoutReset)
            FinishLayoutReset(screen, false);
    }
}
