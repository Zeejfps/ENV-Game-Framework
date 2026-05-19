namespace GitGui;

internal sealed class GroupSectionPresenter : IDisposable
{
    private readonly IGroupSectionView _view;
    private readonly Group _group;
    private readonly IRepoRegistry _registry;

    public GroupSectionPresenter(IGroupSectionView view, Group group, IRepoRegistry registry)
    {
        _view = view;
        _group = group;
        _registry = registry;

        _view.SetHeader(new GroupHeaderRow(_group, _registry));
        _view.BindRows(
            () => VisibleRepos(_group, _registry),
            repo => new RepoRow(repo, _registry));
    }

    public void Dispose() { }

    // Collapsed groups still surface the active repo so the user can see "where they are"
    // when the rest of the group is hidden.
    private static IEnumerable<Repo> VisibleRepos(Group group, IRepoRegistry registry)
    {
        var reposById = registry.Repos.ToDictionary(r => r.Id);

        if (group.IsCollapsed)
        {
            var activeId = registry.Active.Value?.Id;
            foreach (var repoId in group.RepoIds)
            {
                if (reposById.TryGetValue(repoId, out var repo) && repo.Id == activeId)
                    yield return repo;
            }
            yield break;
        }

        foreach (var repoId in group.RepoIds)
        {
            if (reposById.TryGetValue(repoId, out var repo))
                yield return repo;
        }
    }
}
