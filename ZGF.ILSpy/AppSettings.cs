using System.Text.Json;
using System.Text.Json.Serialization;
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

    public List<string> Assemblies { get; set; } = new();
    public bool DarkMode { get; set; } = true;
    public DecompilationLanguage Language { get; set; } = DecompilationLanguage.CSharp;
    public bool ShowNonPublic { get; set; } = true;
    public float TreeWidth { get; set; } = 340f;
    public float SearchHeight { get; set; } = 240f;

    public static AppSettings Load(string path)
    {
        AppSettings? settings = null;
        try
        {
            if (File.Exists(path))
                settings = JsonSerializer.Deserialize(File.ReadAllText(path), AppSettingsJson.Default.AppSettings);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            Console.Error.WriteLine($"[ILSpy] Ignoring unreadable settings: {e.Message}");
        }
        settings ??= new AppSettings();
        settings.FilePath = path;
        return settings;
    }

    public void Save()
    {
        if (FilePath == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, AppSettingsJson.Default.AppSettings));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"[ILSpy] Cannot save settings: {e.Message}");
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppSettingsJson : JsonSerializerContext;
