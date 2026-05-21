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

        _view.SetHeader(new GroupHeaderRow(_group));
        _view.BindRows(
            () => VisiblePrimaries(_group, _registry),
            primary => new RepoEntry(primary, _registry));
    }

    public void Dispose() { }

    // Group.RepoIds holds primary IDs only — worktrees and submodules nest under their
    // parent via RepoEntry. Collapsed groups still surface the active row's primary so
    // the user can see "where they are" when the rest of the group is hidden.
    private static IEnumerable<Repo> VisiblePrimaries(Group group, IRepoRegistry registry)
    {
        var reposById = registry.Repos.ToDictionary(r => r.Id);

        if (group.IsCollapsed)
        {
            var active = registry.Active.Value;
            if (active is null) yield break;
            var primaryId = active.ParentRepoId ?? active.Id;
            foreach (var repoId in group.RepoIds)
            {
                if (repoId == primaryId && reposById.TryGetValue(repoId, out var repo))
                    yield return repo;
            }
            yield break;
        }

        foreach (var repoId in group.RepoIds)
        {
            if (reposById.TryGetValue(repoId, out var repo) && repo.IsPrimary)
                yield return repo;
        }
    }
}
