using System.Globalization;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Bookmarks;
using static ZGF.ILSpy.IconTheming;
using static ZGF.ILSpy.Settings;
using static ZGF.ILSpy.Theming;

namespace ZGF.ILSpy;

/// <summary>
/// The app's data, and which tab, list and history entry are current. What the user can do to it are
/// static functions grouped by feature, such as <see cref="Navigation"/> and <see cref="Docking"/>.
/// It never sees the screen; a feature that needs the view to act later commands
/// it through <see cref="ViewCommands"/>. Lives on the UI thread: background work (opening, decompiling,
/// resolving, searching) is awaited there through <see cref="Background"/>. A result is dropped when
/// the user asked for something else in the meantime.
/// </summary>
public sealed class AppState
{
    public AppSettings Settings { get; }
    public AssemblyList Assemblies { get; } = new();

    /// <summary>The named assembly lists, one of which <see cref="Assemblies"/> holds open.</summary>
    public SavedAssemblyLists Lists { get; } = new();

    public NavigationHistory History { get; } = new();
    public UserIntent Intent { get; } = new();
    public Reload Reload { get; } = new();
    public OpenRequests Opens { get; } = new();
    public LastSave LastSave { get; } = new();

    /// <summary>Whether writing the settings is failing, and when to try again.</summary>
    public SaveRetry SaveRetry { get; } = new();
    public BookmarkList Bookmarks { get; } = new();

    /// <summary>The symbols being analyzed and what the analyses found.</summary>
    public AnalyzerTree Analyzer { get; }

    public ErrorLog Errors { get; } = new();
    public DocumentTabs Tabs { get; } = new();

    /// <summary>The declarations and documentation shown when hovering types and members in code.</summary>
    public CodeTooltips Tooltips { get; }

    /// <summary>The themes the app can be drawn in, built-in and the user's.</summary>
    public ThemeCatalog Themes { get; } = new(BuiltInThemes.Files);

    /// <summary>Watches the user's themes folder while the app runs.</summary>
    public ThemeFolderWatch ThemesWatch { get; } = new();

    /// <summary>The icon themes the app can be drawn with, built-in and the user's.</summary>
    public IconThemeCatalog IconThemes { get; } = new(BuiltInIconThemes.Files);

    /// <summary>Watches the user's icon themes folder, and the fonts and pictures in it, while the app runs.</summary>
    public ThemeFolderWatch IconThemesWatch { get; } = new(IconThemeFiles, includesSubfolders: true);

    /// <summary>The id of the chosen theme, kept even while no theme has it.</summary>
    public string Theme;

    /// <summary>The id of the chosen icon theme, kept even while no icon theme has it.</summary>
    public string IconTheme;
    public ApiVisibility ApiVisibility;

    /// <summary>The interface language.</summary>
    public Locale Locale;

    public DecompilationLanguage Language { get; set; }

    /// <summary>How C# is decompiled.</summary>
    public DecompilerOptions DecompilerOptions { get; set; }

    /// <summary>The commands the features queue for the view when none is on the stack, for the screen to run
    /// in order before it draws next.</summary>
    public Queue<ViewCommand> ViewCommands { get; } = new();

    /// <summary>Counts each time the screen hands over a change to what the settings keep of it.</summary>
    public int ViewSettingsChanges;

    /// <summary>True while a reference or a history entry is being found in the background.</summary>
    public bool IsResolving => Intent.IsResolving;

    /// <summary>True while <see cref="ZGF.ILSpy.Assemblies.RefreshAssemblies"/> reopens the assemblies.</summary>
    public bool IsReloading => Reload.IsRunning;

    /// <summary>True while analyzer roots wait for their symbols to be found.</summary>
    public bool IsFindingAnalyses => Analyzer.IsFinding;

    /// <summary>The bookmark imports finding their places or waiting for the user to say how to take them.</summary>
    public BookmarkImports Imports { get; } = new();

    /// <summary>True while imported bookmarks' places are being found in the background.</summary>
    public bool IsImporting => Imports.IsFinding;

    /// <summary>The code, projects, solutions and PDBs being written in the background, one at a time.</summary>
    public ExportQueue Export { get; } = new();

    /// <summary>The reports of the exports that finished, oldest first, until they are taken off the
    /// exports panel.</summary>
    public List<ExportReport> FinishedExports { get; } = new();

    /// <summary>True while <see cref="Export"/> runs.</summary>
    public bool IsExporting => Export.IsRunning;

    public AppState(AppSettings settings)
    {
        Settings = settings;
        Tooltips = new CodeTooltips(Assemblies);
        Theme = settings.Theme;
        IconTheme = settings.IconTheme;
        Language = settings.Language;
        DecompilerOptions = settings.Decompiler;
        Locale = ChooseLocale(settings);
        ApiVisibility = settings.ApiVisibility;
        var first = AddTab();
        Tabs.MakeActive(first);
        Analyzer = new AnalyzerTree();
        Lists.Load(settings.AssemblyLists, settings.ActiveAssemblyList);
        var leftOutBookmarks = Bookmarks.Load(settings.Bookmarks);
        Analyzer.AddPending(Lists.ActiveAnalyses);
        LastSave.Record(CaptureSavedSources(this));
        ReportSettingsLoadError(settings);
        ReportLeftOutBookmarks(this, leftOutBookmarks);
        LoadThemes(this);
        LoadIconThemes(this);
    }

    private static Locale ChooseLocale(AppSettings settings)
    {
        if (settings.Locale != null)
            return settings.Locale.Value;
        return Locales.For(CultureInfo.CurrentUICulture);
    }

    private void ReportSettingsLoadError(AppSettings settings)
    {
        if (settings.LoadError == null)
            return;
        ReportError(Strings.For(Locale).ErrorReadSettings, settings.LoadError);
    }

    public void ReportError(string title, string? details = null) => Errors.Add(title, details);

    /// <summary>Queues <paramref name="command"/> for the view to run before it draws next.</summary>
    public void EnqueueCommand(ViewCommand command) => ViewCommands.Enqueue(command);

    /// <summary>True when <paramref name="node"/>'s assembly, or the package it was read out of, is
    /// open in the active list, loaded or failed.</summary>
    public bool IsListed(BrowseNode node) => Assemblies.Listed.Contains(node.Assembly.ListedItem);

    /// <summary>True when the assembly or package at <paramref name="filePath"/> is open in the active
    /// list and loaded.</summary>
    internal bool IsListed(string filePath)
    {
        var listed = Assemblies.Listed;
        for (var i = 0; i < listed.Count; i++)
        {
            if (listed[i].HasLoadError)
                continue;
            if (string.Equals(listed[i].FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    internal DocumentTab AddTab()
    {
        var tab = new DocumentTab();
        Tabs.Add(tab);
        return tab;
    }

    internal static string DescribeFailure(Exception? error, string fallback)
    {
        if (error == null)
            return fallback;
        return error.ToString();
    }
}
