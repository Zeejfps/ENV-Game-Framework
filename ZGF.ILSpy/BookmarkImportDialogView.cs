using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Asks whether imported bookmarks replace the ones there are or merge in beside them.</summary>
internal static class BookmarkImportDialogView
{
    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var open = w.PendingImport != null;
        using (var dialog = ui.Dialog("bookmark-import", t.BookmarksImportTitle, ref open, closeButton: false))
        {
            if (dialog.IsOpen)
            {
                ui.Text(t.BookmarksImportQuestion, wrap: true);
                using (ui.DialogActions())
                {
                    if (ui.Button(t.BookmarksImportCancel, id: "import-cancel", variant: ButtonVariant.Ghost))
                        w.FinishImport(null);
                    if (ui.Button(t.BookmarksImportReplace, id: "import-replace"))
                        w.FinishImport(BookmarkImportMode.Replace);
                    if (ui.Button(t.BookmarksImportMerge, id: "import-merge", variant: ButtonVariant.Primary))
                        w.FinishImport(BookmarkImportMode.Merge);
                }
            }
        }
        if (!open && w.PendingImport != null)
            w.FinishImport(null);
    }
}
