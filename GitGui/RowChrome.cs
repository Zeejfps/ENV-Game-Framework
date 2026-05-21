using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

// Visual bindings shared by RepoRow, WorktreeRow, and SubmoduleRow: the
// hover/active background fade and the missing/active/normal label colour selector.
// Each row still owns its icon + layout — only the colour decisions are centralised.
internal static class RowChrome
{
    public static void BindRowBackground(RectView background, IReadable<bool> isHovered, IRepoRegistry registry, Guid rowId)
    {
        background.BindBackgroundColor(() =>
        {
            var active = registry.Active.Value?.Id == rowId;
            return (isHovered.Value, active) switch
            {
                (_, true) => DialogPalette.RowActive,
                (true, false) => DialogPalette.RowHover,
                _ => DialogPalette.RowTransparent,
            };
        });
    }

    public static uint RowTextColor(IRepoRegistry registry, Repo row)
    {
        if (row.IsMissing) return DialogPalette.RowTextMissing;
        return registry.Active.Value?.Id == row.Id
            ? DialogPalette.RowTextActive
            : DialogPalette.RowText;
    }
}
