using ZGF.Observable;

namespace GitGui;

public interface IRepoRegistry
{
    ObservableList<Repo> Repos { get; }
    ObservableList<Group> Groups { get; }
    State<Repo?> Active { get; }
    void Open(string path);
    void SetActive(Guid id);
    void ToggleGroupCollapsed(Guid groupId);
}
