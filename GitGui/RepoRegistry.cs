namespace GitGui;

public sealed class RepoRegistry : IRepoRegistry
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly List<Repo> _repos;
    private readonly string _statePath;
    private readonly IMessageBus _bus;
    private Repo? _active;

    public RepoRegistry(RepoStateStore.State initial, string statePath, IMessageBus bus)
    {
        _repos = new List<Repo>(initial.Repos);
        _statePath = statePath;
        _bus = bus;
        if (initial.ActiveRepoId is { } id)
            _active = _repos.FirstOrDefault(r => r.Id == id);
    }

    public IReadOnlyList<Repo> Repos => _repos;
    public Repo? Active => _active;

    public void Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var normalized = Path.GetFullPath(path);
        if (!RepoStateStore.IsGitRepo(normalized))
            return;

        var existing = _repos.FirstOrDefault(r => string.Equals(r.Path, normalized, PathComparison));
        if (existing is not null)
        {
            SetActive(existing.Id);
            return;
        }

        var repo = new Repo(Guid.NewGuid(), normalized, Path.GetFileName(normalized));
        _repos.Add(repo);
        _active = repo;
        Save();
        _bus.Broadcast<ReposChangedMessage>();
        _bus.Broadcast(new ActiveRepoChangedMessage(repo.Id));
    }

    public void SetActive(Guid id)
    {
        var target = _repos.FirstOrDefault(r => r.Id == id);
        if (target is null) return;
        if (ReferenceEquals(_active, target)) return;
        _active = target;
        Save();
        _bus.Broadcast(new ActiveRepoChangedMessage(target.Id));
    }

    private void Save() =>
        RepoStateStore.Save(_statePath, _repos, _active?.Id);
}
