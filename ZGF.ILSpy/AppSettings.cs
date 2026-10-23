using System.Text.Json;
using System.Text.Json.Serialization;
using ZGF.Desktop;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What persists between runs, in the user's app-data folder.</summary>
public sealed class AppSettings
{
    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZGF.ILSpy", "settings.json");

    /// <summary>Where <see cref="Save"/> writes; null keeps the settings in memory only.</summary>
    [JsonIgnore]
    public string? FilePath { get; set; }

    /// <summary>Why the file could not be read, when <see cref="Load"/> fell back to defaults.</summary>
    [JsonIgnore]
    public string? LoadError { get; private set; }

    /// <summary>The named assembly lists, the open one's as it was left.</summary>
    public List<AssemblyListRecord> AssemblyLists { get; set; } = new();

    /// <summary>The name of the list that was open; null opens the first.</summary>
    public string? ActiveAssemblyList { get; set; }

    /// <summary>The one list that settings files kept before lists had names. <see cref="Load"/>
    /// moves it into <see cref="AssemblyLists"/>, so it is never written.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Assemblies { get; set; }
    public AppThemeId Theme { get; set; } = AppThemeId.Dark;
    public DecompilationLanguage Language { get; set; } = DecompilationLanguage.CSharp;
    public DecompilerOptions Decompiler { get; set; } = DecompilerOptions.Default;

    /// <summary>The decompiler options panel's folded groups.</summary>
    public List<string>? FoldedOptionGroups { get; set; }

    /// <summary>The interface language; null follows the system's.</summary>
    public Locale? Locale { get; set; }
    public bool ShowNonPublic { get; set; } = true;
    public SearchKinds SearchKinds { get; set; } = SearchKinds.TypesAndMembers;
    public DockLayout? Layout { get; set; }

    public List<BookmarkRecord> Bookmarks { get; set; } = new();

    /// <summary>The symbols the analyzer was analyzing, by assembly file and metadata token.</summary>
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

    public static AppSettings Load(string path)
    {
        AppSettings? settings = null;
        string? error = null;
        try
        {
            if (File.Exists(path))
                settings = JsonSerializer.Deserialize(File.ReadAllText(path), AppSettingsJson.Default.AppSettings);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            error = $"{path}{Environment.NewLine}{e.Message}";
        }
        settings ??= new AppSettings();
        settings.AssemblyLists = CleanAssemblyLists(settings.AssemblyLists);
        settings.MoveUnnamedListIntoDefault();
        settings.Window = Sanitize(settings.Window);
        settings.ExportWindow = SanitizeDialog(settings.ExportWindow);
        settings.FilePath = path;
        settings.LoadError = error;
        return settings;
    }

    /// <summary><paramref name="lists"/> without what a hand-edited file can leave in them: missing
    /// lists, names or assemblies, and blank paths.</summary>
    private static List<AssemblyListRecord> CleanAssemblyLists(List<AssemblyListRecord>? lists)
    {
        if (lists == null)
            return new List<AssemblyListRecord>();
        return lists
            .Where(static l => l != null && l.Name != null)
            .Select(static l => new AssemblyListRecord(l.Name, CleanPaths(l.Assemblies)))
            .ToList();
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

    /// <summary>Writes the settings to <see cref="FilePath"/>, replacing the file whole so a failed
    /// write never leaves it truncated. Returns why it failed, or null.</summary>
    public string? Save()
    {
        if (FilePath == null) return null;
        var temporary = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, AppSettingsJson.Default.AppSettings));
            File.Move(temporary, FilePath, overwrite: true);
            return null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return $"{FilePath}{Environment.NewLine}{e.Message}";
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(BookmarkFile))]
internal sealed partial class AppSettingsJson : JsonSerializerContext;
