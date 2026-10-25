using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Assemblies;

namespace ZGF.ILSpy;

/// <summary>
/// The "open from NuGet" dialog: the feed's URL, typed or picked from the user's NuGet.Config, and a
/// query on top, with whether prereleases count; a page of the packages found below, the selected
/// one showing the version to open as a dropdown, with the pages to step through, then what the
/// selected one says of itself; at the bottom, Open, which downloads the package unless it is on
/// disk, then opens it and closes.
/// </summary>
internal static class NuGetPickerView
{
    private const string QueryId = "nuget-query";
    private const string ListId = "nuget-list";
    private const float DescriptionHeight = 64f;

    public static void Render(Ui<UiState> ui, AppState appState)
    {
        var picker = ui.State.NuGetPicker;
        OpenDownloaded(appState, picker);
        if (picker.IsOpen)
            UpdateSearch(ui, picker);
        var wasOpen = picker.IsOpen;
        Dialog(ui, picker);
        CloseIfDismissed(picker, wasOpen);
    }

    private static void OpenDownloaded(AppState appState, NuGetPicker picker)
    {
        var path = picker.TakeDownloaded();
        if (path == null)
            return;
        OpenAssembly(appState, path);
        picker.Close();
    }

    private static void UpdateSearch(Ui<UiState> ui, NuGetPicker picker)
    {
        var wait = picker.Update(ui.RealTime);
        if (wait > 0)
            ui.RequestUpdateIn(wait);
    }

    private static void Dialog(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        var card = new Box { Width = Sizing.Grow(0f, AppLayout.PickerWidth), Height = Sizing.Percent(AppLayout.PickerHeight) };
        using var dialog = ui.Dialog("nuget", t.NugetTitle, ref picker.IsOpen, card);
        if (!dialog.IsOpen)
            return;
        FeedRow(ui, picker);
        var open = QueryRow(ui, picker);
        open |= Packages(ui, picker);
        picker.FollowSelection();
        if (picker.TotalHits > 0)
            Pager(ui, picker);
        Details(ui, picker);
        open |= Footer(ui, picker);
        if (open)
            picker.Download();
    }

    /// <summary>Stops what the dialog started once its close button or Escape dismissed it, which
    /// only clears <see cref="NuGetPicker.IsOpen"/>.</summary>
    private static void CloseIfDismissed(NuGetPicker picker, bool wasOpen)
    {
        if (!wasOpen)
            return;
        if (picker.IsOpen)
            return;
        picker.Close();
    }

    /// <summary>The URL of the feed to search, and the feeds the user's NuGet.Config lists to pick from.</summary>
    private static void FeedRow(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        using (ui.Row(new Box { Gap = ui.Theme.GapTight, AlignY = Align.Center }))
        {
            ui.TextField("nuget-feed", ref picker.FeedUrl, t.NugetFeed, Sizing.Grow());
            KnownFeeds(ui, picker);
        }
    }

    private static void KnownFeeds(Ui<UiState> ui, NuGetPicker picker)
    {
        var sources = picker.Sources;
        using var menu = ui.DropdownButton("nuget-sources", ui.State.Strings.NugetKnownFeeds, enabled: sources.Count > 0, revision: sources.Count);
        if (!menu.IsOpen)
            return;
        foreach (var source in sources)
        {
            var current = source.Url == picker.FeedUrl;
            if (ui.MenuRadioItem(source.Name, current, id: source.Url))
                picker.PickFeed(source);
        }
    }

    /// <summary>The query and whether prereleases count. True when Enter was pressed in the query.</summary>
    private static bool QueryRow(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        using (ui.Row(new Box { Gap = ui.Theme.GapTight, AlignY = Align.Center }))
        {
            if (ui.HasFocus(ui.GetId(QueryId)))
                MoveSelectionFromQuery(ui, picker);
            if (picker.TakeQueryFocus())
                ui.Focus(ui.GetId(QueryId));
            var submitted = ui.TextField(QueryId, ref picker.Query, t.NugetQuery, Sizing.Grow(), ui.State.Icons.SearchField);
            ui.Checkbox(t.NugetPrerelease, ref picker.IncludePrerelease, id: "nuget-prerelease");
            return submitted;
        }
    }

    /// <summary>Walks the packages from the query, leaving Home and End to its text.</summary>
    private static void MoveSelectionFromQuery(Ui<UiState> ui, NuGetPicker picker)
    {
        var results = picker.Results;
        var page = ui.MeasureListPage(ui.GetId(ListId), RowHeight(ui));
        foreach (var stroke in ui.Input.Keys)
        {
            var movesCaret = stroke.Key is KeyboardKey.Home or KeyboardKey.End;
            if (!movesCaret)
                MoveSelection(picker, stroke, results, page);
        }
    }

    private static void MoveSelection(NuGetPicker picker, KeyStroke stroke, IReadOnlyList<FeedPackage> results, int page)
    {
        var index = picker.Selection.IndexIn(results);
        if (!UiWidgets.ApplyListKey(stroke, ref index, results.Count, page))
            return;
        picker.Selection.Select(results[index], results);
    }

    /// <summary>A package row, tall enough for the version dropdown of the selected one.</summary>
    private static float RowHeight(Ui<UiState> ui) => ui.Theme.ControlHeight + ui.Theme.GapTight;

    /// <summary>The packages found, or why there are none yet. True when one was activated.</summary>
    private static bool Packages(Ui<UiState> ui, NuGetPicker picker)
    {
        var theme = ui.Theme;
        using (ui.Panel(new Box { Height = Sizing.Grow(), Padding = theme.BorderWidth, Background = theme.Field, Clip = true }))
        {
            var placeholder = Placeholder(ui.State.Strings, picker);
            if (placeholder != null)
            {
                ui.Text(placeholder, new Box { Padding = new Spacing(theme.ShellPaddingX, theme.Gap) }, color: theme.TextMuted, wrap: true);
                return false;
            }
            return Rows(ui, picker);
        }
    }

    /// <summary>What the list says in place of packages: that the config is being read, that the feed
    /// typed is no URL, that it is searching, why the search failed, or that nothing matched; null
    /// when there are packages to show.</summary>
    private static string? Placeholder(Strings t, NuGetPicker picker)
    {
        if (picker.Results.Count > 0)
            return null;
        if (picker.IsReadingSetup)
            return t.NugetReadingConfig;
        if (!picker.HasFeedUrl)
            return t.NugetNoFeed;
        if (picker.IsSearching)
            return t.NugetSearching;
        if (picker.SearchError != null)
            return t.NugetSearchFailed;
        return t.NugetNoMatch;
    }

    private static bool Rows(Ui<UiState> ui, NuGetPicker picker)
    {
        var results = picker.Results;
        var listId = ui.GetId(ListId);
        picker.Selection.ShowIn(results);
        using var list = ui.SelectableList(listId, results.Count, picker.Selection.Rows, RowHeight(ui));
        for (var i = list.First; i < list.End; i++)
            Row(ui, picker, list, results[i], i);
        picker.Selection.TakeFrom(results);
        return list.Activated;
    }

    /// <summary>A package's icon, id and latest version; the selected one offers its versions in a
    /// dropdown in place of its latest.</summary>
    private static void Row(Ui<UiState> ui, NuGetPicker picker, ListScope list, FeedPackage package, int index)
    {
        var theme = ui.Theme;
        using (ui.Id(package.Id))
        using (var row = list.Row(index, ui.GetId("row"), new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            var nameColor = row.Selected ? theme.SelectionText : theme.Text;
            NodeIcons.Draw(ui, NodeKind.Package);
            using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
            {
                ui.Text(package.Id, new Box { Width = Sizing.Grow() }, color: nameColor, overflow: TextOverflow.Ellipsis);
                if (row.Selected)
                    VersionSelect(ui, picker, package);
                else
                    ui.Text(package.Version, color: theme.TextMuted, size: theme.FontSizeSmall);
            }
        }
    }

    private static void VersionSelect(Ui<UiState> ui, NuGetPicker picker, FeedPackage package)
    {
        picker.OfferVersionsOf(package);
        var width = ui.SelectButtonWidth(picker.Versions);
        ui.Select("version", ref picker.Version, picker.Versions, VersionLabel, Sizing.Fixed(width), ui.State.Strings.NugetVersion,
            enabled: !picker.IsDownloading);
    }

    private static string VersionLabel(string version) => version;

    /// <summary>How many packages matched, the page shown of how many, and the buttons that step through them.</summary>
    private static void Pager(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            var count = ui.Memo("nuget-count", (picker.TotalHits, t), static a => a.t.NugetCount(a.TotalHits));
            ui.Text(count, color: theme.TextMuted, size: theme.FontSizeSmall);
            ui.Spacer();
            var page = ui.Memo("nuget-page", (picker.Page, picker.PageCount, t), static a => a.t.NugetPage(a.Page + 1, a.PageCount));
            ui.Text(page, color: theme.TextMuted, size: theme.FontSizeSmall);
            if (ui.Button(t.NugetPreviousPage, enabled: picker.HasPreviousPage, id: "nuget-previous", variant: ButtonVariant.Ghost))
                picker.ShowPreviousPage();
            if (ui.Button(t.NugetNextPage, enabled: picker.HasNextPage, id: "nuget-next", variant: ButtonVariant.Ghost))
                picker.ShowNextPage();
        }
    }

    /// <summary>Who made the selected package, and what it says it is.</summary>
    private static void Details(Ui<UiState> ui, NuGetPicker picker)
    {
        var theme = ui.Theme;
        var package = picker.Selected;
        using (ui.Column(new Box { Height = DescriptionHeight, Gap = theme.GapTight, Clip = true }))
        {
            if (package == null)
                return;
            var authors = ui.Memo("nuget-authors", (package.Authors, ui.State.Strings), static a => a.Strings.NugetAuthors(a.Authors));
            ui.Text(authors, color: theme.TextMuted, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
            ui.Text(package.Description, wrap: true);
        }
    }

    /// <summary>What is being downloaded, Cancel and Open. True when Open was pressed.</summary>
    private static bool Footer(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            if (picker.IsDownloading)
                Downloading(ui, picker);
            ui.Spacer();
            if (ui.Button(t.NugetCancel, id: "nuget-cancel"))
                picker.Close();
            return ui.Button(t.NugetOpen, enabled: picker.CanOpen, id: "nuget-open", variant: ButtonVariant.Primary);
        }
    }

    private static void Downloading(Ui<UiState> ui, NuGetPicker picker)
    {
        var theme = ui.Theme;
        var text = ui.Memo("nuget-downloading", (picker.DownloadingName, ui.State.Strings), static a => a.Strings.NugetDownloading(a.DownloadingName));
        ui.Text(text, color: theme.AccentText, size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
    }
}
