using ZGF.Observable;

namespace GitGui;

public sealed class RepoRegistry : IRepoRegistry
{
    private const string DefaultNewGroupName = "New Group";

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly string _statePath;
    private readonly Dictionary<Guid, BranchesUiState> _branchesUi;

    public RepoRegistry(RepoStateStore.State initial, string statePath)
    {
        _statePath = statePath;
        _branchesUi = new Dictionary<Guid, BranchesUiState>(initial.BranchesUi);

        Repos = new ObservableList<Repo>();
        foreach (var r in initial.Repos) Repos.Add(r);

        Groups = new ObservableList<Group>();
        foreach (var g in initial.Groups) Groups.Add(g);

        Active = new State<Repo?>(
            initial.ActiveRepoId is { } id
                ? Repos.FirstOrDefault(r => r.Id == id)
                : null);

        RenamingGroupId = new State<Guid?>(null);
    }

    public ObservableList<Repo> Repos { get; }
    public ObservableList<Group> Groups { get; }
    public State<Repo?> Active { get; }
    public State<Guid?> RenamingGroupId { get; }

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

    public Guid CreateGroup(string name)
    {
        var displayName = string.IsNullOrWhiteSpace(name) ? DefaultNewGroupName : name;
        var group = new Group(Guid.NewGuid(), displayName, IsCollapsed: false, RepoIds: new List<Guid>());
        Groups.Add(group);
        Save();
        return group.Id;
    }

    public void RenameGroup(Guid id, string newName)
    {
        var trimmed = (newName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        for (var i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Id != id) continue;
            if (Groups[i].Name == trimmed) return;
            Groups.Replace(i, Groups[i] with { Name = trimmed });
            Save();
            return;
        }
    }

    public void DeleteGroup(Guid id)
    {
        if (Groups.Count <= 1) return;

        var index = -1;
        for (var i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Id != id) continue;
            index = i;
            break;
        }
        if (index < 0) return;

        var orphans = Groups[index].RepoIds.ToList();
        Groups.RemoveAt(index);

        if (orphans.Count > 0)
        {
            var targetIndex = index < Groups.Count ? index : Groups.Count - 1;
            var target = Groups[targetIndex];
            Groups.Replace(targetIndex, target with { RepoIds = target.RepoIds.Concat(orphans).ToList() });
        }
        Save();
    }

    public void MoveRepo(Guid repoId, Guid targetGroupId, int insertIndex)
    {
        int sourceGroupIndex = -1;
        int sourceRepoIndex = -1;
        for (var i = 0; i < Groups.Count; i++)
        {
            var repoIdx = Groups[i].RepoIds.IndexOf(repoId);
            if (repoIdx < 0) continue;
            sourceGroupIndex = i;
            sourceRepoIndex = repoIdx;
            break;
        }
        if (sourceGroupIndex < 0) return;

        var targetGroupIndex = -1;
        for (var i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Id != targetGroupId) continue;
            targetGroupIndex = i;
            break;
        }
        if (targetGroupIndex < 0) return;

        if (sourceGroupIndex == targetGroupIndex)
        {
            var ids = Groups[sourceGroupIndex].RepoIds.ToList();
            ids.RemoveAt(sourceRepoIndex);
            var adjusted = insertIndex > sourceRepoIndex ? insertIndex - 1 : insertIndex;
            adjusted = Math.Clamp(adjusted, 0, ids.Count);
            ids.Insert(adjusted, repoId);
            if (ids.SequenceEqual(Groups[sourceGroupIndex].RepoIds)) return;
            Groups.Replace(sourceGroupIndex, Groups[sourceGroupIndex] with { RepoIds = ids });
            Save();
            return;
        }

        var sourceIds = Groups[sourceGroupIndex].RepoIds.ToList();
        sourceIds.RemoveAt(sourceRepoIndex);
        Groups.Replace(sourceGroupIndex, Groups[sourceGroupIndex] with { RepoIds = sourceIds });

        var targetIds = Groups[targetGroupIndex].RepoIds.ToList();
        var insertAt = Math.Clamp(insertIndex, 0, targetIds.Count);
        targetIds.Insert(insertAt, repoId);
        Groups.Replace(targetGroupIndex, Groups[targetGroupIndex] with { RepoIds = targetIds });

        Save();
    }

    public void MoveGroup(Guid groupId, int insertIndex)
    {
        var sourceIndex = -1;
        for (var i = 0; i < Groups.Count; i++)
        {
            if (Groups[i].Id != groupId) continue;
            sourceIndex = i;
            break;
        }
        if (sourceIndex < 0) return;

        var adjusted = insertIndex > sourceIndex ? insertIndex - 1 : insertIndex;
        adjusted = Math.Clamp(adjusted, 0, Groups.Count - 1);
        if (adjusted == sourceIndex) return;

        Groups.Move(sourceIndex, adjusted);
        Save();
    }

    public void RemoveRepo(Guid repoId)
    {
        var repoIndex = -1;
        for (var i = 0; i < Repos.Count; i++)
        {
            if (Repos[i].Id != repoId) continue;
            repoIndex = i;
            break;
        }
        if (repoIndex < 0) return;

        for (var i = 0; i < Groups.Count; i++)
        {
            var idx = Groups[i].RepoIds.IndexOf(repoId);
            if (idx < 0) continue;
            var ids = Groups[i].RepoIds.ToList();
            ids.RemoveAt(idx);
            Groups.Replace(i, Groups[i] with { RepoIds = ids });
            break;
        }

        Repos.RemoveAt(repoIndex);

        if (Active.Value?.Id == repoId)
        {
            Active.Value = Repos.Count > 0 ? Repos[0] : null;
        }

        Save();
    }

    public void BeginRenameGroup(Guid id)
    {
        RenamingGroupId.Value = id;
    }

    public void EndRenameGroup()
    {
        RenamingGroupId.Value = null;
    }

    public BranchesUiState GetBranchesUi(Guid repoId)
    {
        if (_branchesUi.TryGetValue(repoId, out var state))
            return state.Clone();
        return new BranchesUiState();
    }

    public void SetBranchesUi(Guid repoId, BranchesUiState state)
    {
        _branchesUi[repoId] = state.Clone();
        Save();
    }

    private void Save() =>
        RepoStateStore.Save(_statePath, Repos, Groups, Active.Value?.Id, _branchesUi);
}
