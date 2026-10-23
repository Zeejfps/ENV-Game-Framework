using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class ToolbarView
{
    private const string ListItemPrefix = "list:";

    private static Strings? _languagesFor;
    private static Segment<DecompilationLanguage>[] _languages = [];

    public static void Render(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.Row(new Box
        {
            Height = theme.ToolbarHeight,
            Padding = new Spacing(theme.ShellPaddingX, theme.ToolbarPaddingY),
            Gap = theme.GroupGap,
            AlignY = Align.Center,
            Background = theme.Surface,
        }))
        {
            using (Group(ui))
                ListPicker(ui, w);

            using (Group(ui))
            {
                if (ui.IconButton("open", LucideIcons.FolderOpen, LucideIcons.Family, ui.Memo("open-tip", t, static s => s.ToolbarOpen(Shortcuts.Open)),
                        variant: ButtonVariant.Neutral))
                    MainMenuView.OpenFile(ui, w);
                if (ui.IconButton("search", LucideIcons.Search, LucideIcons.Family, ui.Memo("search-tip", t, static s => s.SearchFieldTooltip(Shortcuts.Search)),
                        variant: ButtonVariant.Neutral))
                    w.OpenSearch();
            }

            using (Group(ui))
            {
                using (var back = ui.SplitIconButton("back", LucideIcons.ArrowLeft, LucideIcons.Family,
                           ui.Memo("back-tip", t, static s => s.ToolbarBack(Shortcuts.Back)), t.ToolbarBackHistory, w.History.CanGoBack, w.History.Revision))
                {
                    if (back.Clicked)
                        w.GoBack();
                    if (back.IsOpen)
                        HistoryItems(ui, w, -1);
                }
                using (var forward = ui.SplitIconButton("forward", LucideIcons.ArrowRight, LucideIcons.Family,
                           ui.Memo("forward-tip", t, static s => s.ToolbarForward(Shortcuts.Forward)), t.ToolbarForwardHistory, w.History.CanGoForward,
                           w.History.Revision))
                {
                    if (forward.Clicked)
                        w.GoForward();
                    if (forward.IsOpen)
                        HistoryItems(ui, w, 1);
                }
            }

            using (Group(ui))
            {
                var language = w.Language;
                if (ui.SegmentedControl("language", ref language, Languages(t)))
                    w.SetLanguage(language);
                if (language == DecompilationLanguage.CSharp)
                {
                    LanguageVersion(ui, w);
                    if (ui.IconButton("decompiler-options", LucideIcons.Wrench, LucideIcons.Family, t.PanelDecompilerOptions, variant: ButtonVariant.Neutral))
                        w.OpenDecompilerOptions();
                }
            }
        }
    }

    /// <summary>The open assembly list's name, opening a menu of every list to switch to, and of the
    /// dialog that manages them. As wide as the longest name, within limits.</summary>
    private static void ListPicker(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var lists = w.Lists;
        var width = ListPickerWidth(ui, lists);
        using var menu = ui.SelectButton("list", lists.Active, width, t.ListsPick, revision: lists.Count);
        if (!menu.IsOpen)
            return;
        foreach (var name in lists.Names)
            ListItem(ui, w, name);
        ui.MenuSeparator();
        if (ui.MenuItem(t.ListsManage, id: "manage"))
            w.OpenListsDialog();
    }

    /// <summary>Measured again only when the lists or the theme changed, so a frame allocates nothing.</summary>
    private static float ListPickerWidth(Ui<UiState> ui, SavedAssemblyLists lists)
    {
        var measured = ui.GetState<MeasuredWidth>(ui.GetId("list-width"));
        var unchanged = lists.Revision == measured.Revision && ReferenceEquals(ui.Theme, measured.Theme);
        if (unchanged)
            return measured.Width;
        measured.Revision = lists.Revision;
        measured.Theme = ui.Theme;
        var names = lists.Names.ToArray();
        var widest = ui.SelectButtonWidth(names);
        measured.Width = Math.Clamp(widest, AppLayout.ListPickerMinWidth, AppLayout.ListPickerMaxWidth);
        return measured.Width;
    }

    /// <summary>The width the list picker was last measured at, for which lists and theme.</summary>
    private sealed class MeasuredWidth
    {
        public int Revision = -1;
        public UiTheme? Theme;
        public float Width;
    }

    private static void ListItem(Ui<UiState> ui, Workspace w, string name)
    {
        var active = name == w.Lists.Active;
        if (!ui.MenuRadioItem(name, active, id: ListItemPrefix + name))
            return;
        w.SwitchList(name);
    }

    private static Segment<DecompilationLanguage>[] Languages(Strings t)
    {
        if (ReferenceEquals(t, _languagesFor)) return _languages;
        _languagesFor = t;
        _languages =
        [
            new(DecompilationLanguage.CSharp, "C#", t.ToolbarDecompileCsharp),
            new(DecompilationLanguage.IL, "IL", t.ToolbarDisassembleIl),
        ];
        return _languages;
    }

    private static void LanguageVersion(Ui<UiState> ui, Workspace w)
    {
        var t = ui.State.Strings;
        var current = w.DecompilerOptions.LanguageVersion;
        using var menu = ui.SelectButton("language-version", CSharpVersions.Label(current), ui.SelectButtonWidth(CSharpVersions.Labels),
            t.ToolbarLanguageVersion);
        if (!menu.IsOpen) return;
        foreach (var (version, label) in CSharpVersions.All)
        {
            if (ui.MenuRadioItem(label, version == current))
                w.SetLanguageVersion(version);
        }
    }

    private static Ui.Scope Group(Ui<UiState> ui) =>
        ui.Row(new Box { Width = Sizing.Fit(), Gap = ui.Theme.Gap, AlignY = Align.Center });

    private static void HistoryItems(Ui<UiState> ui, Workspace w, int direction)
    {
        var entries = w.History.Entries;
        var palette = ui.State.NodeIconPalette;
        for (var i = w.History.CurrentIndex + direction; i >= 0 && i < entries.Count; i += direction)
        {
            using (ui.Id(i))
            {
                var entry = entries[i];
                if (ui.MenuItem(entry.Place.Text, id: "entry", icon: NodeIcons.Icon(entry.Place.Kind, entry.Accessibility, palette)))
                {
                    w.GoTo(i);
                    return;
                }
            }
        }
    }
}
