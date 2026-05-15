using ZGF.Observable;

namespace GitGui;

public sealed class RepoRegistry : IRepoRegistry
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _statePath;

    public RepoRegistry(RepoStateStore.State initial, string statePath)
    {
        _statePath = statePath;

        Repos = new ObservableList<Repo>();
        foreach (var r in initial.Repos) Repos.Add(r);

        Groups = new ObservableList<Group>();
        foreach (var g in initial.Groups) Groups.Add(g);

        Active = new State<Repo?>(
            initial.ActiveRepoId is { } id
                ? Repos.FirstOrDefault(r => r.Id == id)
                : null);
    }

    public ObservableList<Repo> Repos { get; }
    public ObservableList<Group> Groups { get; }
    public State<Repo?> Active { get; }

    public void Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var normalized = Path.GetFullPath(path);
        if (!RepoStateStore.IsGitRepo(normalized))
            return;

        var existing = Repos.FirstOrDefault(r => string.Equals(r.Path, normalized, PathComparison));
        if (existing is not null)
        {
            SetActive(existing.Id);
            return;
        }

        var repo = new Repo(Guid.NewGuid(), normalized, Path.GetFileName(normalized));
        Repos.Add(repo);

        var first = Groups[0];
        Groups.Replace(0, first with { RepoIds = first.RepoIds.Append(repo.Id).ToList() });

        Active.Value = repo;
        Save();
    }

    public void SetActive(Guid id)
    {
        var target = Repos.FirstOrDefault(r => r.Id == id);
        if (target is null) return;
        if (ReferenceEquals(Active.Value, target)) return;
        Active.Value = target;
        Save();
    }

    public void ToggleGroupCollapsed(Guid groupId)
    {
        for (var i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Id != groupId) continue;
            Groups.Replace(i, Groups[i] with { IsCollapsed = !Groups[i].IsCollapsed });
            Save();
            return;
        }
    }

    private void Save() =>
        RepoStateStore.Save(_statePath, Repos, Groups, Active.Value?.Id);
}
