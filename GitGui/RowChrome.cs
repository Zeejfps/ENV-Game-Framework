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
        // Capture-tokens via a local State the BindToTheme behavior refreshes. The Derived
        // backing BindBackgroundColor auto-tracks tokens, isHovered, and registry.Active —
        // any of them changing repaints.
        var tokens = new State<ThemeTokens>(ThemePresets.Dark);
        background.BindToTheme(t => tokens.Value = t);
        background.BindBackgroundColor(() =>
        {
            var d = tokens.Value.Dialog;
            var active = registry.Active.Value?.Id == rowId;
            return (isHovered.Value, active) switch
            {
                (_, true) => d.RowActive,
                (true, false) => d.RowHover,
                _ => d.RowTransparent,
            };
        });
    }

    /// <summary>
    /// Binds <paramref name="label"/>'s text color to the row's missing / active / normal
    /// state. Auto-tracks the theme and the registry's active id so theme swaps and active-
    /// repo switches each refresh the label.
    /// </summary>
    public static void BindRowTextColor(TextView label, IRepoRegistry registry, Repo row)
    {
        var tokens = new State<ThemeTokens>(ThemePresets.Dark);
        label.BindToTheme(t => tokens.Value = t);
        label.BindTextColor(() =>
        {
            var d = tokens.Value.Dialog;
            if (row.IsMissing) return d.RowTextMissing;
            return registry.Active.Value?.Id == row.Id ? d.RowTextActive : d.RowText;
        });
    }
}
