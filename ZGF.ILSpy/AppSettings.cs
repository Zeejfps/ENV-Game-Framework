using System.Text.Json;
using System.Text.Json.Serialization;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;
using ZGF.ILSpy.Core.Diagrams;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What persists between runs, in the user's app-data folder.</summary>
public sealed class AppSettings
{
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZGF.ILSpy", "settings.json");

    private const string ThemesFolderName = "themes";
    private const string IconThemesFolderName = "icon-themes";
    private const string UnreadableExtension = ".unreadable.json";

    /// <summary>Where <see cref="Save"/> writes; null keeps the settings in memory only.</summary>
    [JsonIgnore]
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
    [JsonIgnore]
    public string? ThemesFolder { get; private set; }

    /// <summary>Where the user's icon themes are: a folder beside the settings file; null while the
    /// settings are kept in memory only.</summary>
    [JsonIgnore]
    public string? IconThemesFolder { get; private set; }

    /// <summary>The named assembly lists, the open one's as it was left.</summary>
    public List<AssemblyListRecord> AssemblyLists { get; set; } = new();

    /// <summary>The name of the list that was open; null opens the first.</summary>
    public string? ActiveAssemblyList { get; set; }

    /// <summary>The one list that settings files kept before lists had names. <see cref="Load"/>
    /// moves it into <see cref="AssemblyLists"/>, so it is never written.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Assemblies { get; set; }

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

    /// <summary>The one switch settings files kept before <see cref="ApiVisibility"/> had three
    /// choices. <see cref="Load"/> moves it into that, so it is never written.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShowNonPublic { get; set; }
    public SearchKinds SearchKinds { get; set; } = SearchKinds.TypesAndMembers;
    /// <summary>Where the views and the open list's documents were docked; null for where they start out.</summary>
    public DockLayout? Layout { get; set; }

    public List<BookmarkRecord> Bookmarks { get; set; } = new();

    /// <summary>The symbols analyzed before each list kept its own, by assembly file and metadata token.
    /// <see cref="Load"/> moves them into the open list, so they are never written.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CodeAnchor>? Analyzed { get; set; }

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

    public static SettingsLoad Load(string path)
    {
        var read = Read(path);
        var settings = read.Settings;
        var problem = read.Problem;
        if (problem is SettingsProblem.None)
            problem = DropBrokenLayout(settings);
        settings.AssemblyLists = CleanAssemblyLists(settings.AssemblyLists);
        settings.MoveUnnamedListIntoDefault();
        settings.MoveAnalysesIntoActiveList();
        settings.MoveShowNonPublicIntoApiVisibility();
        settings.RenameThemeToId();
        settings.DefaultMissingIconTheme();
        settings.Window = Sanitize(settings.Window);
        settings.ExportWindow = SanitizeDialog(settings.ExportWindow);
        settings.NuGetWindow = SanitizeDialog(settings.NuGetWindow);
        settings.FilePath = MaySaveOver(problem) ? path : null;
        return new SettingsLoad(settings, problem);
    }

    private static bool MaySaveOver(SettingsProblem problem) => problem switch
    {
        SettingsProblem.NotRead notRead => notRead.Kept is not SettingsKept.NotSaved,
        SettingsProblem.NotJson notJson => notJson.Kept is not SettingsKept.NotSaved,
        _ => true,
    };

    private static SettingsLoad Read(string path)
    {
        if (!File.Exists(path))
            return new SettingsLoad(new AppSettings(), new SettingsProblem.None());
        try
        {
            var settings = JsonSerializer.Deserialize(File.ReadAllText(path), AppSettingsJson.Default.AppSettings);
            if (settings == null)
                return new SettingsLoad(new AppSettings(), new SettingsProblem.Empty());
            return new SettingsLoad(settings, new SettingsProblem.None());
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return new SettingsLoad(new AppSettings(), new SettingsProblem.NotJson(e.Message, Keep(path)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new SettingsLoad(new AppSettings(), new SettingsProblem.NotRead(e.Message, Keep(path)));
        }
    }

    /// <summary>Copies the settings file at <paramref name="path"/>, which could not be read, beside
    /// it, so that saving the settings does not lose what it holds.</summary>
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

    /// <summary><paramref name="lists"/> without what a hand-edited file can leave in them: missing
    /// lists, names or assemblies, blank paths, picked PDBs among them, documents missing what they show,
    /// docked as a view, or docked as a panel another document was.</summary>
    private static List<AssemblyListRecord> CleanAssemblyLists(List<AssemblyListRecord>? lists)
    {
        if (lists == null)
            return new List<AssemblyListRecord>();
        return lists
            .Where(static l => l != null && l.Name != null)
            .Select(static l => new AssemblyListRecord(l.Name, CleanPaths(l.Assemblies), CleanAnalyses(l.Analyzed), CleanPicks(l.Pdbs), CleanPicks(l.Frameworks), CleanDocuments(l.Documents)))
            .ToList();
    }

    private static DocumentsRecord? CleanDocuments(DocumentsRecord? documents)
    {
        if (documents == null)
            return null;
        if (documents.Tabs == null)
            return null;
        var tabs = documents.Tabs
            .Where(IsWhole)
            .Where(static t => !DockPanels.IsView(t.Panel))
            .DistinctBy(static t => t.Panel)
            .ToList();
        return documents with { Tabs = tabs };
    }

    private static bool IsWhole(DocumentRecord? document) => document switch
    {
        NodeDocumentRecord node => IsWholeNode(node),
        DiagramDocumentRecord diagram => IsWholeDiagram(diagram),
        ComparisonDocumentRecord comparison => IsWholeComparison(comparison),
        _ => false,
    };

    private static bool IsWholeNode(NodeDocumentRecord node)
    {
        if (node.Panel == null)
            return false;
        if (node.FilePath == null)
            return false;
        return node.Place != null;
    }

    private static bool IsWholeDiagram(DiagramDocumentRecord diagram)
    {
        if (diagram.Panel == null)
            return false;
        return diagram.Types != null;
    }

    private static bool IsWholeComparison(ComparisonDocumentRecord comparison)
    {
        if (comparison.Panel == null)
            return false;
        if (comparison.LeftPath == null)
            return false;
        return comparison.RightPath != null;
    }

    private static Dictionary<string, string>? CleanPicks(Dictionary<string, string>? picks)
    {
        if (picks == null)
            return null;
        return picks
            .Where(static p => !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(static p => p.Key, static p => p.Value);
    }

    private static List<string> CleanPaths(List<string>? paths)
    {
        if (paths == null)
            return new List<string>();
        return paths
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    private void MoveUnnamedListIntoDefault()
    {
        if (Assemblies == null)
            return;
        var paths = CleanPaths(Assemblies);
        Assemblies = null;
        if (AssemblyLists.Count > 0)
            return;
        AssemblyLists.Add(new AssemblyListRecord(SavedAssemblyLists.DefaultName, paths));
    }

    private void MoveShowNonPublicIntoApiVisibility()
    {
        if (ShowNonPublic == null)
            return;
        if (ShowNonPublic == false)
            ApiVisibility = ApiVisibility.PublicOnly;
        ShowNonPublic = null;
    }

    /// <summary>Turns the theme's name from before themes were files into the id of its file, and a
    /// missing theme into the default one.</summary>
    private void RenameThemeToId()
    {
        if (string.IsNullOrWhiteSpace(Theme))
        {
            Theme = ThemeCatalog.DarkId;
            return;
        }
        var id = IdOfOldTheme(Theme);
        if (id != null)
            Theme = id;
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

    private void DefaultMissingIconTheme()
    {
        if (string.IsNullOrWhiteSpace(IconTheme))
            IconTheme = IconThemeCatalog.ReferenceId;
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

    private static List<CodeAnchor> CleanAnalyses(List<CodeAnchor>? analyzed)
    {
        if (analyzed == null)
            return new List<CodeAnchor>();
        return analyzed;
    }

    /// <summary>Gives the analyses saved before each list kept its own to the open list, unless it has some.</summary>
    private void MoveAnalysesIntoActiveList()
    {
        if (Analyzed == null)
            return;
        var analyzed = Analyzed;
        Analyzed = null;
        if (AssemblyLists.Count == 0)
            AssemblyLists.Add(new AssemblyListRecord(SavedAssemblyLists.DefaultName, new List<string>()));
        var index = FindActiveListIndex();
        var active = AssemblyLists[index];
        var hasAnalyses = active.Analyzed != null && active.Analyzed.Count > 0;
        if (hasAnalyses)
            return;
        AssemblyLists[index] = active with { Analyzed = analyzed };
    }

    /// <summary>Where the list <see cref="ActiveAssemblyList"/> names is, else the first.</summary>
    private int FindActiveListIndex()
    {
        var index = AssemblyLists.FindIndex(IsActiveList);
        if (index < 0)
            return 0;
        return index;
    }

    private bool IsActiveList(AssemblyListRecord list)
    {
        if (ActiveAssemblyList == null)
            return false;
        return string.Equals(list.Name.Trim(), ActiveAssemblyList.Trim(), StringComparison.OrdinalIgnoreCase);
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
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, AppSettingsJson.Default.AppSettings));
            File.Move(temporary, FilePath, overwrite: true);
            return new SettingsWrite.Written();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new SettingsWrite.Failed($"{FilePath}{Environment.NewLine}{e.Message}");
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, Converters =
[
    typeof(EnumByName<ApiScope>), typeof(EnumByName<ApiVisibility>), typeof(EnumByName<BookmarkKind>),
    typeof(EnumByName<CodeAnchorKind>), typeof(EnumByName<CSharpVersion>), typeof(EnumByName<DecompilationLanguage>),
    typeof(EnumByName<Direction>), typeof(EnumByName<Locale>), typeof(EnumByName<MatchMode>),
    typeof(EnumByName<MemberSection>), typeof(EnumByName<NodeKind>), typeof(EnumByName<SearchKinds>),
    typeof(EnumByName<TokenFormat>), typeof(EnumByName<TriState>),
])]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(BookmarkFile))]
internal sealed partial class AppSettingsJson : JsonSerializerContext;
