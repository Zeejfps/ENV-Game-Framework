using ZGF.Observable;

namespace GitGui;

internal sealed class RepoBarViewModel : IDisposable
{
    private readonly IRepoRegistry _registry;

    public ObservableList<Group> Groups => _registry.Groups;
    public Command NewGroup { get; }

    public RepoBarViewModel(IRepoRegistry registry)
    {
        _registry = registry;
        NewGroup = new Command(DoNewGroup);
    }

    private void DoNewGroup()
    {
        var id = _registry.CreateGroup("New Group");
        _registry.BeginRenameGroup(id);
    }

    public void Dispose() { }
}
