using System.Text.Json;

namespace GitGui;

public static class RepoStateStore
{
    private const int CurrentSchemaVersion = 2;
    private const string DefaultGroupName = "Repositories";

    public sealed record State(List<Repo> Repos, List<Group> Groups, Guid? ActiveRepoId);

    private sealed class FileShape
    {
        public int SchemaVersion { get; set; }
        public List<Repo> Repos { get; set; } = new();
        public List<Group>? Groups { get; set; }
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
            return EmptyState();

        try
        {
            using var stream = File.OpenRead(path);
            var file = JsonSerializer.Deserialize<FileShape>(stream, Options);
            if (file is null)
                return EmptyState();

            var repos = file.Repos
                .Select(r => r with { IsMissing = !IsGitRepo(r.Path) })
                .ToList();

            var groups = file.Groups;
            if (groups is null || groups.Count == 0)
            {
                groups =
                [
                    new Group(Guid.NewGuid(), DefaultGroupName, IsCollapsed: false,
                        RepoIds: repos.Select(r => r.Id).ToList())

                ];
            }
            else
            {
                groups = ReconcileGroups(groups, repos);
            }

            return new State(repos, groups, file.ActiveRepoId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load repo state from {path}: {ex.Message}");
            return EmptyState();
        }
    }

    public static void Save(string path, IReadOnlyList<Repo> repos, IReadOnlyList<Group> groups, Guid? activeId)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var file = new FileShape
        {
            SchemaVersion = CurrentSchemaVersion,
            Repos = repos.ToList(),
            Groups = groups.ToList(),
            ActiveRepoId = activeId,
        };
        var json = JsonSerializer.Serialize(file, Options);
        File.WriteAllText(path, json);
    }

    public static bool IsGitRepo(string path) =>
        Directory.Exists(Path.Combine(path, ".git")) ||
        File.Exists(Path.Combine(path, ".git"));

    private static State EmptyState()
    {
        var defaultGroup = new Group(Guid.NewGuid(), DefaultGroupName, IsCollapsed: false, RepoIds: new List<Guid>());
        return new State(new List<Repo>(), new List<Group> { defaultGroup }, null);
    }

    private static List<Group> ReconcileGroups(List<Group> groups, List<Repo> repos)
    {
        var knownRepoIds = repos.Select(r => r.Id).ToHashSet();
        var assigned = groups.SelectMany(g => g.RepoIds).Where(knownRepoIds.Contains).ToHashSet();
        var orphans = repos.Select(r => r.Id).Where(id => !assigned.Contains(id)).ToList();

        var cleaned = groups
            .Select(g => g with { RepoIds = g.RepoIds.Where(knownRepoIds.Contains).ToList() })
            .ToList();

        if (orphans.Count > 0)
        {
            var first = cleaned[0];
            cleaned[0] = first with { RepoIds = first.RepoIds.Concat(orphans).ToList() };
        }
        return cleaned;
    }
}
