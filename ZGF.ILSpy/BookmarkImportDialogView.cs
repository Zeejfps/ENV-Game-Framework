using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Bookmarks;

namespace ZGF.ILSpy;

/// <summary>Asks whether imported bookmarks replace the ones there are or merge in beside them, one
/// import at a time.</summary>
internal static class BookmarkImportDialogView
{
    public static void Render(Ui<UiState> ui, BookmarkImports imports)
    {
        var import = imports.Asking;
        if (import == null)
            return;
        Ask(ui, import);
    }

    private static void Ask(Ui<UiState> ui, PendingImport import)
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
                        ui.State.Later(import, static (a, i) => FinishBookmarkImport(a, i, null));
                    if (ui.Button(t.BookmarksImportReplace, id: "import-replace"))
                        ui.State.Later(import, static (a, i) => FinishBookmarkImport(a, i, BookmarkImportMode.Replace));
                    if (ui.Button(t.BookmarksImportMerge, id: "import-merge", variant: ButtonVariant.Primary))
                        ui.State.Later(import, static (a, i) => FinishBookmarkImport(a, i, BookmarkImportMode.Merge));
                }
            }
        }
        if (!open)
            ui.State.Later(import, static (a, i) => FinishBookmarkImport(a, i, null));
    }
}
