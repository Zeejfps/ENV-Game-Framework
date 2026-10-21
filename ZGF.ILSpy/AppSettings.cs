using System.Text.Json;
using System.Text.Json.Serialization;
using ZGF.Desktop;
using ZGF.ILSpy.Core;

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

    public List<string> Assemblies { get; set; } = new();
    public bool DarkMode { get; set; } = true;
    public DecompilationLanguage Language { get; set; } = DecompilationLanguage.CSharp;
    public bool ShowNonPublic { get; set; } = true;
    public float TreeWidth { get; set; } = 340f;
    public float SearchHeight { get; set; } = 240f;

    /// <summary>The main window as it was left; null opens it at the default size, centered.</summary>
    public WindowBounds? Window { get; set; }

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
        settings.Assemblies ??= new List<string>();
        settings.Assemblies.RemoveAll(static p => string.IsNullOrWhiteSpace(p));
        settings.Window = Sanitize(settings.Window);
        settings.FilePath = path;
        settings.LoadError = error;
        return settings;
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
internal sealed partial class AppSettingsJson : JsonSerializerContext;
