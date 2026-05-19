namespace GitGui;

internal sealed class RepoBarPresenter : IDisposable
{
    private readonly IRepoBarView _view;
    private readonly IRepoRegistry _registry;

    public RepoBarPresenter(IRepoBarView view, IRepoRegistry registry)
    {
        _view = view;
        _registry = registry;

        _view.NewGroupRequested += OnNewGroupRequested;
        _view.BindGroups(_registry.Groups, group => new GroupSection(group, _registry));
    }

    public void Dispose()
    {
        _view.NewGroupRequested -= OnNewGroupRequested;
    }

    private void OnNewGroupRequested()
    {
        var id = _registry.CreateGroup("New Group");
        _registry.BeginRenameGroup(id);
    }
}
