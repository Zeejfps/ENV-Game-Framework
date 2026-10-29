using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What the "open from NuGet" dialog's list says while it has no packages to show, with what
/// to do about it: wait while the config is read or the search runs, type a feed URL, retry a failed
/// search, or clear a query nothing matched; and the note over the packages shown when another page
/// of them failed to load.</summary>
internal static class NuGetPlaceholderView
{
    public static void Render(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Column(new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.ShellPaddingX, theme.ShellPaddingX), Gap = theme.Gap }))
        {
            if (picker.IsReadingSetup)
            {
                Waiting(ui, t.NugetReadingConfig);
                return;
            }
            if (!picker.HasFeedUrl)
            {
                Note(ui, t.NugetNoFeed);
                return;
            }
            if (picker.IsSearching)
            {
                Waiting(ui, t.NugetSearching);
                return;
            }
            var error = picker.SearchError;
            if (error != null)
            {
                SearchFailed(ui, picker, error);
                return;
            }
            NothingFound(ui, picker);
        }
    }

    private static void Waiting(Ui<UiState> ui, string text)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.Spinner(theme.Field);
            ui.Text(text, color: theme.TextMuted, wrap: true);
        }
    }

    private static void Note(Ui<UiState> ui, string text)
    {
        ui.Text(text, color: ui.Theme.TextMuted, wrap: true);
    }

    /// <summary>That the search failed and why, and a button that runs it again.</summary>
    private static void SearchFailed(Ui<UiState> ui, NuGetPicker picker, string error)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        Warning(ui, t.NugetSearchFailed);
        ui.Text(error, color: theme.TextMuted, size: theme.FontSizeSmall, wrap: true);
        if (ui.Button(t.NugetRetry, id: "nuget-retry"))
            picker.Retry();
    }

    /// <summary>That nothing matched the query, with a button that clears it; or that the feed lists nothing.</summary>
    private static void NothingFound(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        var query = picker.ShownQuery;
        if (query.Length == 0)
        {
            Note(ui, t.NugetFeedEmpty);
            return;
        }
        var noMatch = ui.Memo("nuget-no-match", (query, t), static a => a.t.NugetNoMatch(a.query));
        Note(ui, noMatch);
        if (ui.Button(t.NugetClearSearch, id: "nuget-clear"))
            picker.ClearQuery();
    }

    /// <summary>Over the packages shown: that the page asked for in their place failed, and a button that asks again.</summary>
    public static void PageFailed(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Padding = new Spacing(theme.ShellPaddingX, theme.GapTight), Gap = theme.GapTight, AlignY = Align.Center }))
        {
            var text = ui.Memo("nuget-page-failed", (picker.RequestedPage, t), static a => a.t.NugetPageFailed(a.RequestedPage + 1));
            Warning(ui, text);
            ui.Spacer();
            if (ui.Button(t.NugetRetry, id: "nuget-retry-page", variant: ButtonVariant.Ghost))
                picker.Retry();
        }
    }

    internal static void Warning(Ui<UiState> ui, string text)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
        {
            ui.Icon(new UiIcon(ui.State.Icons.NugetWarning, theme.Warning));
            ui.Text(text, color: theme.Text, wrap: true);
        }
    }
}
