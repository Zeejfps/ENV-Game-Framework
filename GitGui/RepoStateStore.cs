using System.Text.Json;

namespace GitGui;

public static class RepoStateStore
{
    private const int CurrentSchemaVersion = 1;

    public sealed record State(List<Repo> Repos, Guid? ActiveRepoId);

    private sealed class FileShape
    {
        public int SchemaVersion { get; set; }
        public List<Repo> Repos { get; set; } = new();
        public Guid? ActiveRepoId { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static State Load(string path)
    {
        if (!File.Exists(path))
            return new State(new List<Repo>(), null);

        try
        {
            using var stream = File.OpenRead(path);
            var file = JsonSerializer.Deserialize<FileShape>(stream, Options);
            if (file is null)
                return new State(new List<Repo>(), null);

            var repos = file.Repos
                .Select(r => r with { IsMissing = !IsGitRepo(r.Path) })
                .ToList();
            return new State(repos, file.ActiveRepoId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load repo state from {path}: {ex.Message}");
            return new State(new List<Repo>(), null);
        }
    }

    public static void Save(string path, IReadOnlyList<Repo> repos, Guid? activeId)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var file = new FileShape
        {
            SchemaVersion = CurrentSchemaVersion,
            Repos = repos.ToList(),
            ActiveRepoId = activeId,
        };
        var json = JsonSerializer.Serialize(file, Options);
        File.WriteAllText(path, json);
    }

    public static bool IsGitRepo(string path) =>
        Directory.Exists(Path.Combine(path, ".git")) ||
        File.Exists(Path.Combine(path, ".git"));
}
