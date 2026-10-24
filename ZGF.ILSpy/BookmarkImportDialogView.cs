using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Bookmarks;

namespace ZGF.ILSpy;

/// <summary>Asks whether imported bookmarks replace the ones there are or merge in beside them, one
/// import at a time.</summary>
internal static class BookmarkImportDialogView
{
    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var import = appState.Imports.Asking;
        if (import == null)
            return;
        Ask(ui, appState, import);
    }

    private static void Ask(Ui<UiState> ui, AppState appState, PendingImport import)
    {
        var t = ui.State.Strings;
        var open = true;
        using (var dialog = ui.Dialog("bookmark-import", t.BookmarksImportTitle, ref open, closeButton: false))
        {
            if (dialog.IsOpen)
            {
                ui.Text(t.BookmarksImportQuestion, wrap: true);
                using (ui.DialogActions())
                {
                    if (ui.Button(t.BookmarksImportCancel, id: "import-cancel", variant: ButtonVariant.Ghost))
                        FinishBookmarkImport(appState, import, null);
                    if (ui.Button(t.BookmarksImportReplace, id: "import-replace"))
                        FinishBookmarkImport(appState, import, BookmarkImportMode.Replace);
                    if (ui.Button(t.BookmarksImportMerge, id: "import-merge", variant: ButtonVariant.Primary))
                        FinishBookmarkImport(appState, import, BookmarkImportMode.Merge);
                }
            }
        }
        if (!open)
            FinishBookmarkImport(appState, import, null);
    }
}
