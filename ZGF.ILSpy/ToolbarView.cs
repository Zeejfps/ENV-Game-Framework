using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

internal static class ToolbarView
{
    private const string OpenShortcut = "Ctrl+O";
    private const string SearchShortcut = "Ctrl+Shift+F";

    private static Strings? _languagesFor;
    private static Segment<DecompilationLanguage>[] _languages = [];

    private static readonly (CSharpVersion Version, string Label)[] Versions =
    [
        (CSharpVersion.CSharp1, "C# 1.0 / VS .NET"),
        (CSharpVersion.CSharp2, "C# 2.0 / VS 2005"),
        (CSharpVersion.CSharp3, "C# 3.0 / VS 2008"),
        (CSharpVersion.CSharp4, "C# 4.0 / VS 2010"),
        (CSharpVersion.CSharp5, "C# 5.0 / VS 2012"),
        (CSharpVersion.CSharp6, "C# 6.0 / VS 2015"),
        (CSharpVersion.CSharp7, "C# 7.0 / VS 2017"),
        (CSharpVersion.CSharp7_1, "C# 7.1 / VS 2017.3"),
        (CSharpVersion.CSharp7_2, "C# 7.2 / VS 2017.4"),
        (CSharpVersion.CSharp7_3, "C# 7.3 / VS 2017.7"),
        (CSharpVersion.CSharp8, "C# 8.0 / VS 2019"),
        (CSharpVersion.CSharp9, "C# 9.0 / VS 2019.8"),
        (CSharpVersion.CSharp10, "C# 10.0 / VS 2022"),
        (CSharpVersion.CSharp11, "C# 11.0 / VS 2022.4"),
        (CSharpVersion.CSharp12, "C# 12.0 / VS 2022.8"),
        (CSharpVersion.CSharp13, "C# 13.0 / VS 2022.12"),
        (CSharpVersion.CSharp14, "C# 14.0 / VS 2026"),
    ];

    private static readonly string[] VersionLabels = Versions.Select(static v => v.Label).ToArray();

    public static void Render(Ui ui, Workspace w, Strings t)
    {
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
            {
                if (ui.IconButton("open", LucideIcons.FolderOpen, LucideIcons.Family, ui.Memo("open-tip", t, static s => s.ToolbarOpen(OpenShortcut)),
                        variant: ButtonVariant.Neutral))
                    MainMenuView.OpenFile(ui, w);
                if (ui.IconButton("search", LucideIcons.Search, LucideIcons.Family, ui.Memo("search-tip", t, static s => s.SearchFieldTooltip(SearchShortcut)),
                        variant: ButtonVariant.Neutral))
                    w.OpenSearch();
            }

            using (Group(ui))
            {
                using (var back = ui.SplitIconButton("back", LucideIcons.ArrowLeft, LucideIcons.Family,
                           ui.Memo("back-tip", t, static s => s.ToolbarBack("Alt+Left")), t.ToolbarBackHistory, w.History.CanGoBack, w.History.Revision))
                {
                    if (back.Clicked)
                        w.GoBack();
                    if (back.IsOpen)
                        HistoryItems(ui, w, -1);
                }
                using (var forward = ui.SplitIconButton("forward", LucideIcons.ArrowRight, LucideIcons.Family,
                           ui.Memo("forward-tip", t, static s => s.ToolbarForward("Alt+Right")), t.ToolbarForwardHistory, w.History.CanGoForward,
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
                    LanguageVersion(ui, w, t);
            }
        }
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

    private static void LanguageVersion(Ui ui, Workspace w, Strings t)
    {
        var current = w.DecompilerOptions.LanguageVersion;
        using var menu = ui.SelectButton("language-version", Label(current), ui.SelectButtonWidth(VersionLabels), t.ToolbarLanguageVersion);
        if (!menu.IsOpen) return;
        foreach (var (version, label) in Versions)
        {
            if (ui.MenuRadioItem(label, version == current))
                w.SetLanguageVersion(version);
        }
    }

    private static string Label(CSharpVersion version)
    {
        foreach (var (v, label) in Versions)
        {
            if (v == version) return label;
        }
        return version.ToString();
    }

    private static Ui.Scope Group(Ui ui) =>
        ui.Row(new Box { Width = Sizing.Fit(), Gap = ui.Theme.Gap, AlignY = Align.Center });

    private static void HistoryItems(Ui ui, Workspace w, int direction)
    {
        var entries = w.History.Entries;
        var palette = ui.Get<NodeIconPalette>() ?? NodeIconPalette.Dark;
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
