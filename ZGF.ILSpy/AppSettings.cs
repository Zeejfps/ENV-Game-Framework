using System.Text.Json;
using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What persists between runs, in the user's app-data folder, as read from its file by
/// <see cref="Load"/>: whatever the file held, every value here is one the app can use.</summary>
public sealed class AppSettings
{
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZGF.ILSpy", "settings.json");

    private const string ThemesFolderName = "themes";
    private const string IconThemesFolderName = "icon-themes";
    private const string UnreadableExtension = ".unreadable.json";

    /// <summary>Where <see cref="Save"/> writes; null keeps the settings in memory only.</summary>
    public string? FilePath
    {
        get;
        set
        {
            field = value;
            ThemesFolder = FindFolderBeside(value, ThemesFolderName);
            IconThemesFolder = FindFolderBeside(value, IconThemesFolderName);
        }
    }

    /// <summary>Where the user's themes are: a folder beside the settings file; null while the settings
    /// are kept in memory only.</summary>
    public string? ThemesFolder { get; private set; }

    /// <summary>Where the user's icon themes are: a folder beside the settings file; null while the
    /// settings are kept in memory only.</summary>
    public string? IconThemesFolder { get; private set; }

    /// <summary>The named assembly lists, the open one's as it was left.</summary>
    public List<AssemblyListRecord> AssemblyLists { get; set; } = new();

    /// <summary>The name of the list that was open; null opens the first.</summary>
    public string? ActiveAssemblyList { get; set; }

    /// <summary>The id of the chosen theme, the name of its file without the extension.</summary>
    public string Theme { get; set; } = ThemeCatalog.DarkId;

    /// <summary>The id of the chosen icon theme, the name of its file without the extension.</summary>
    public string IconTheme { get; set; } = IconThemeCatalog.ReferenceId;
    public DecompilationLanguage Language { get; set; } = DecompilationLanguage.CSharp;
    public DecompilerOptions Decompiler { get; set; } = DecompilerOptions.Default;

    /// <summary>The decompiler options panel's folded groups.</summary>
    public List<string>? FoldedOptionGroups { get; set; }

    /// <summary>How the documents draw code; null for the defaults.</summary>
    public DisplayOptions? Display { get; set; }

    /// <summary>The interface language; null follows the system's.</summary>
    public Locale? Locale { get; set; }
    public ApiVisibility ApiVisibility { get; set; } = ApiVisibility.AllButCompilerGenerated;
    public SearchKinds SearchKinds { get; set; } = SearchKinds.TypesAndMembers;
    /// <summary>Where the views and the open list's documents were docked; null for where they start out.</summary>
    public DockLayout? Layout { get; set; }

    public List<BookmarkRecord> Bookmarks { get; set; } = new();

    /// <summary>The bookmarks panel's column weights as last dragged; null for the defaults.</summary>
    public List<float>? BookmarkColumns { get; set; }

    /// <summary>The search results' column weights as last dragged; null for the defaults.</summary>
    public List<float>? SearchColumns { get; set; }

    /// <summary>The export dialog's table column weights as last dragged; null for the defaults.</summary>
    public List<float>? ExportColumns { get; set; }

    /// <summary>The metadata grids' flags filters, by kind of row and column.</summary>
    public Dictionary<string, SavedFilterState>? FlagsFilters { get; set; }

    /// <summary>The main window as it was left; null opens it at the default size, centered.</summary>
    public WindowBounds? Window { get; set; }

    /// <summary>The export dialog's window as it was left; null opens it at its default size, centered.</summary>
    public WindowBounds? ExportWindow { get; set; }

    /// <summary>The feed the "open from NuGet" dialog last searched; null searches the NuGet.Config's first.</summary>
    public SavedNuGetSearch? NuGet { get; set; }

    /// <summary>The "open from NuGet" dialog's window as it was left; null opens it at its default size, centered.</summary>
    public WindowBounds? NuGetWindow { get; set; }

    public const int MinWindowWidth = 480;
    public const int MinWindowHeight = 320;
    public const int MaxWindowSize = 16384;

    /// <summary><paramref name="bounds"/> with its size kept within reason, or null when it has none.
    /// A position off every monitor is the window system's to correct when the window opens.</summary>
    public static WindowBounds? Sanitize(WindowBounds? bounds) =>
        bounds is { Width: > 0, Height: > 0 } b
            ? b with
            {
                Width = Math.Clamp(b.Width, MinWindowWidth, MaxWindowSize),
                Height = Math.Clamp(b.Height, MinWindowHeight, MaxWindowSize),
            }
            : null;

    /// <summary>A dialog's <paramref name="bounds"/> with its size kept within reason, or null when it
    /// has none. A dialog keeps its own smallest size, so only the largest is enforced here.</summary>
    public static WindowBounds? SanitizeDialog(WindowBounds? bounds)
    {
        if (bounds == null)
            return null;
        var b = bounds.Value;
        if (b.Width <= 0 || b.Height <= 0)
            return null;
        return b with
        {
            Width = Math.Min(b.Width, MaxWindowSize),
            Height = Math.Min(b.Height, MaxWindowSize),
            Maximized = false,
        };
    }

    public void RememberWindow(WindowBounds bounds)
    {
        Window = Sanitize(bounds);
    }

    public StartupConfig CreateWindowConfig()
    {
        var config = new StartupConfig
        {
            WindowWidth = 1400,
            WindowHeight = 900,
            WindowTitle = "ILSpy",
        };
        if (Window == null)
            return config;
        return Window.Value.ApplyTo(config);
    }

    /// <summary>The settings in the file at <paramref name="path"/>, each value read on its own, so a
    /// value the file holds wrong leaves out only that one, for its default to stand in.</summary>
    public static SettingsLoad Load(string path)
    {
        var (settings, problem) = Read(path);
        settings.FilePath = MaySaveOver(problem) ? path : null;
        return new SettingsLoad(settings, problem);
    }

    private static bool MaySaveOver(SettingsProblem problem) => problem switch
    {
        SettingsProblem.NotRead notRead => notRead.Kept is not SettingsKept.NotSaved,
        SettingsProblem.NotJson notJson => notJson.Kept is not SettingsKept.NotSaved,
        SettingsProblem.ValuesLeftOut leftOut => leftOut.Kept is not SettingsKept.NotSaved,
        _ => true,
    };

    private static SettingsLoad Read(string path)
    {
        if (!File.Exists(path))
            return new SettingsLoad(new AppSettings(), new SettingsProblem.None());
        Dictionary<string, JsonElement>? fields;
        try
        {
            fields = JsonSerializer.Deserialize(File.ReadAllText(path), AppSettingsJson.Default.DictionaryStringJsonElement);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return new SettingsLoad(new AppSettings(), new SettingsProblem.NotJson(e.Message, Keep(path)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new SettingsLoad(new AppSettings(), new SettingsProblem.NotRead(e.Message, Keep(path)));
        }
        if (fields == null)
            return new SettingsLoad(new AppSettings(), new SettingsProblem.Empty());
        var file = new SettingsReader(fields);
        var settings = Parse(file);
        var layoutProblem = DropBrokenLayout(settings);
        if (file.LeftOut.Count == 0)
            return new SettingsLoad(settings, layoutProblem);
        if (layoutProblem is not SettingsProblem.None)
            file.LeaveOut(nameof(SettingsFile.Layout));
        return new SettingsLoad(settings, new SettingsProblem.ValuesLeftOut(file.LeftOut, Keep(path)));
    }

    /// <summary>Copies the settings file at <paramref name="path"/>, which could not be read whole,
    /// beside it, so that saving the settings does not lose what it holds.</summary>
    private static SettingsKept Keep(string path)
    {
        var copy = Path.ChangeExtension(path, UnreadableExtension);
        try
        {
            File.Copy(path, copy, overwrite: true);
            return new SettingsKept.CopiedTo(copy);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new SettingsKept.NotSaved();
        }
    }

    /// <summary>The settings <paramref name="file"/> holds, with what older files kept moved to where
    /// it is kept now, and windows sized within reason.</summary>
    private static AppSettings Parse(SettingsReader file)
    {
        var json = AppSettingsJson.Default;
        var lists = ReadAssemblyLists(file);
        var active = file.Read(nameof(SettingsFile.ActiveAssemblyList), json.String);
        MoveUnnamedListIntoDefault(lists, file.Read(SettingsFile.OldAssemblies, json.ListString));
        MoveAnalysesIntoActiveList(lists, active, file.Read(SettingsFile.OldAnalyzed, json.ListCodeAnchorJson));
        return new AppSettings
        {
            AssemblyLists = lists,
            ActiveAssemblyList = active,
            Theme = ThemeIdOf(file.Read(nameof(SettingsFile.Theme), json.String)),
            IconTheme = IconThemeIdOf(file.Read(nameof(SettingsFile.IconTheme), json.String)),
            Language = file.Read(nameof(SettingsFile.Language), json.NullableDecompilationLanguage) ?? DecompilationLanguage.CSharp,
            Decompiler = file.Read(nameof(SettingsFile.Decompiler), json.DecompilerOptions) ?? DecompilerOptions.Default,
            FoldedOptionGroups = NamesOf(file.Read(nameof(SettingsFile.FoldedOptionGroups), json.ListString)),
            Display = file.Read(nameof(SettingsFile.Display), json.DisplayOptions),
            Locale = file.Read(nameof(SettingsFile.Locale), json.NullableLocale),
            ApiVisibility = ApiVisibilityOf(
                file.Read(nameof(SettingsFile.ApiVisibility), json.NullableApiVisibility),
                file.Read(SettingsFile.OldShowNonPublic, json.NullableBoolean)),
            SearchKinds = file.Read(nameof(SettingsFile.SearchKinds), json.NullableSearchKinds) ?? SearchKinds.TypesAndMembers,
            Layout = file.Read(nameof(SettingsFile.Layout), json.DockLayout),
            Bookmarks = ReadBookmarks(file),
            BookmarkColumns = file.Read(nameof(SettingsFile.BookmarkColumns), json.ListSingle),
            SearchColumns = file.Read(nameof(SettingsFile.SearchColumns), json.ListSingle),
            ExportColumns = file.Read(nameof(SettingsFile.ExportColumns), json.ListSingle),
            FlagsFilters = file.Read(nameof(SettingsFile.FlagsFilters), json.DictionaryStringSavedFilterState),
            Window = Sanitize(file.Read(nameof(SettingsFile.Window), json.NullableWindowBounds)),
            ExportWindow = SanitizeDialog(file.Read(nameof(SettingsFile.ExportWindow), json.NullableWindowBounds)),
            NuGet = file.Read(nameof(SettingsFile.NuGet), json.NuGetSearchJson)?.Parse(),
            NuGetWindow = SanitizeDialog(file.Read(nameof(SettingsFile.NuGetWindow), json.NullableWindowBounds)),
        };
    }

    /// <summary>The lists the file holds, each read on its own, without nameless ones.</summary>
    private static List<AssemblyListRecord> ReadAssemblyLists(SettingsReader file)
    {
        const string field = nameof(SettingsFile.AssemblyLists);
        return file.ReadEach(field)
            .Select(l => file.Parse(l, AppSettingsJson.Default.AssemblyListJson, field)?.Parse(file, field))
            .OfType<AssemblyListRecord>()
            .ToList();
    }

    /// <summary>The bookmarks the file holds, each read on its own, without ones naming no assembly file.</summary>
    private static List<BookmarkRecord> ReadBookmarks(SettingsReader file)
    {
        const string field = nameof(SettingsFile.Bookmarks);
        return file.ReadEach(field)
            .Select(b => file.Parse(b, AppSettingsJson.Default.BookmarkJson, field)?.Parse())
            .OfType<BookmarkRecord>()
            .ToList();
    }

    private static List<string>? NamesOf(List<string?>? names)
    {
        return names?.OfType<string>().ToList();
    }

    /// <summary>Leaves out a saved layout the dock space cannot be given, for the panels to be where
    /// they start out.</summary>
    private static SettingsProblem DropBrokenLayout(AppSettings settings)
    {
        var layout = settings.Layout;
        if (layout == null)
            return new SettingsProblem.None();
        if (!layout.IsWhole())
        {
            settings.Layout = null;
            return new SettingsProblem.LayoutPartsMissing();
        }
        if (layout.RepeatsAnId())
        {
            settings.Layout = null;
            return new SettingsProblem.LayoutPartsRepeated();
        }
        return new SettingsProblem.None();
    }

    /// <summary>Makes the one list of a file from before lists had names the default list, unless
    /// the file has named lists too.</summary>
    private static void MoveUnnamedListIntoDefault(List<AssemblyListRecord> lists, List<string?>? unnamed)
    {
        if (unnamed == null)
            return;
        if (lists.Count > 0)
            return;
        lists.Add(new AssemblyListRecord(SavedAssemblyLists.DefaultName, SettingsJson.PathsOf(unnamed)));
    }

    /// <summary>Gives the analyses saved before each list kept its own to the open list, unless it has some.</summary>
    private static void MoveAnalysesIntoActiveList(List<AssemblyListRecord> lists, string? active, List<CodeAnchorJson?>? analyzed)
    {
        if (analyzed == null)
            return;
        if (lists.Count == 0)
            lists.Add(new AssemblyListRecord(SavedAssemblyLists.DefaultName, new List<string>()));
        var index = FindActiveListIndex(lists, active);
        var list = lists[index];
        var hasAnalyses = list.Analyzed != null && list.Analyzed.Count > 0;
        if (hasAnalyses)
            return;
        lists[index] = list with { Analyzed = SettingsJson.AnchorsOf(analyzed) };
    }

    /// <summary>Where the list <paramref name="active"/> names is, else the first.</summary>
    private static int FindActiveListIndex(List<AssemblyListRecord> lists, string? active)
    {
        if (active == null)
            return 0;
        var index = lists.FindIndex(l => string.Equals(l.Name.Trim(), active.Trim(), StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return 0;
        return index;
    }

    /// <summary>The visibility saved, or for a file from before it had three choices, what its
    /// non-public switch said.</summary>
    private static ApiVisibility ApiVisibilityOf(ApiVisibility? saved, bool? showNonPublic)
    {
        if (showNonPublic == false)
            return ApiVisibility.PublicOnly;
        return saved ?? ApiVisibility.AllButCompilerGenerated;
    }

    /// <summary>The id of the theme <paramref name="saved"/> names, as the name from before themes
    /// were files or as the id of its file; the default theme for none.</summary>
    private static string ThemeIdOf(string? saved)
    {
        if (string.IsNullOrWhiteSpace(saved))
            return ThemeCatalog.DarkId;
        return IdOfOldTheme(saved) ?? saved;
    }

    private static string? IdOfOldTheme(string name) => name switch
    {
        "Dark" => "dark",
        "Light" => "light",
        "GitHubDark" => "github-dark",
        "OneDarkPro" => "one-dark-pro",
        "Dracula" => "dracula",
        "Monokai" => "monokai",
        "CatppuccinMocha" => "catppuccin-mocha",
        "GitHubLight" => "github-light",
        "SolarizedLight" => "solarized-light",
        _ => null,
    };

    private static string IconThemeIdOf(string? saved)
    {
        if (string.IsNullOrWhiteSpace(saved))
            return IconThemeCatalog.ReferenceId;
        return saved;
    }

    private static string? FindFolderBeside(string? settingsFile, string name)
    {
        if (settingsFile == null)
            return null;
        var folder = Path.GetDirectoryName(settingsFile);
        if (folder == null)
            return null;
        return Path.Combine(folder, name);
    }

    /// <summary>Writes the settings to <see cref="FilePath"/>, replacing the file whole so a failed
    /// write never leaves it truncated.</summary>
    public SettingsWrite Save()
    {
        if (FilePath == null) return new SettingsWrite.Written();
        var temporary = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(SettingsFile.Of(this), AppSettingsJson.Default.SettingsFile));
            File.Move(temporary, FilePath, overwrite: true);
            return new SettingsWrite.Written();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new SettingsWrite.Failed($"{FilePath}{Environment.NewLine}{e.Message}");
        }
    }
}
