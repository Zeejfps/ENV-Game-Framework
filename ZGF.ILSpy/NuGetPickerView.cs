using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Assemblies;

namespace ZGF.ILSpy;

/// <summary>
/// The "open from NuGet" dialog: the feed and a query on top, with whether prereleases count; the
/// packages found below, then what the selected one says of itself; at the bottom, the version to
/// open and Open, which downloads the package unless it is on disk, then opens it and closes.
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
        var open = QueryRow(ui, picker);
        open |= Packages(ui, picker);
        picker.FollowSelection();
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

    /// <summary>The feed, when there is more than one, the query and whether prereleases count. True
    /// when Enter was pressed in the query.</summary>
    private static bool QueryRow(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        using (ui.Row(new Box { Gap = ui.Theme.GapTight, AlignY = Align.Center }))
        {
            if (picker.Feeds.Length > 1)
                FeedSelect(ui, picker);
            var listId = ui.GetId(ListId);
            if (!ui.HasFocus(listId))
                MoveSelectionFromQuery(ui, picker, listId);
            if (picker.TakeQueryFocus())
                ui.Focus(ui.GetId(QueryId));
            var submitted = ui.TextField(QueryId, ref picker.Query, t.NugetQuery, Sizing.Grow(), ui.State.Icons.SearchField);
            ui.Checkbox(t.NugetPrerelease, ref picker.IncludePrerelease, id: "nuget-prerelease");
            return submitted;
        }
    }

    private static void FeedSelect(Ui<UiState> ui, NuGetPicker picker)
    {
        var width = ui.SelectButtonWidth(picker.FeedNames);
        var feed = picker.Feed!;
        if (ui.Select("nuget-feed", ref feed, picker.Feeds, FeedName, Sizing.Fixed(width), ui.State.Strings.NugetFeed))
            picker.Feed = feed;
    }

    private static string FeedName(NuGetFeed feed) => feed.Source.Name;

    /// <summary>Walks the packages from the query, leaving Home and End to its text.</summary>
    private static void MoveSelectionFromQuery(Ui<UiState> ui, NuGetPicker picker, int listId)
    {
        var results = picker.Results;
        var page = ui.MeasureListPage(listId, ui.Theme.RowHeight);
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

    /// <summary>What the list says in place of packages: that it is searching, why the search failed,
    /// or that nothing matched; null when there are packages to show.</summary>
    private static string? Placeholder(Strings t, NuGetPicker picker)
    {
        if (picker.Results.Count > 0)
            return null;
        if (picker.Feed == null)
            return t.NugetReadingConfig;
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
        using var list = ui.SelectableList(listId, results.Count, picker.Selection.Rows);
        for (var i = list.First; i < list.End; i++)
            Row(ui, list, results[i], i);
        picker.Selection.TakeFrom(results);
        return list.Activated;
    }

    private static void Row(Ui<UiState> ui, ListScope list, FeedPackage package, int index)
    {
        var theme = ui.Theme;
        using (ui.Id(package.Id))
        using (var row = list.Row(index, ui.GetId("row"), new Box { Gap = theme.GapTight }))
        {
            var nameColor = row.Selected ? theme.SelectionText : theme.Text;
            NodeIcons.Draw(ui, NodeKind.Package);
            using (ui.Row(new Box { Gap = theme.Gap, AlignY = Align.Center }))
            {
                ui.Text(package.Id, new Box { Width = Sizing.Grow() }, color: nameColor, overflow: TextOverflow.Ellipsis);
                ui.Text(package.Version, color: theme.TextMuted, size: theme.FontSizeSmall);
            }
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

    /// <summary>What is being downloaded, the version to open, Cancel and Open. True when Open was pressed.</summary>
    private static bool Footer(Ui<UiState> ui, NuGetPicker picker)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box { Gap = theme.GapTight, AlignY = Align.Center }))
        {
            if (picker.IsDownloading)
                Downloading(ui, picker);
            ui.Spacer();
            if (picker.Selected != null)
                VersionSelect(ui, picker);
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

    private static void VersionSelect(Ui<UiState> ui, NuGetPicker picker)
    {
        var width = ui.SelectButtonWidth(picker.Versions);
        ui.Select("nuget-version", ref picker.Version, picker.Versions, VersionLabel, Sizing.Fixed(width), ui.State.Strings.NugetVersion,
            enabled: !picker.IsDownloading);
    }

    private static string VersionLabel(string version) => version;
}
