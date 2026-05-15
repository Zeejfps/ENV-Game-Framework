namespace GitGui;

public interface IRepoRegistry
{
    IReadOnlyList<Repo> Repos { get; }
    IReadOnlyList<Group> Groups { get; }
    Repo? Active { get; }
    void Open(string path);
    void SetActive(Guid id);
    void ToggleGroupCollapsed(Guid groupId);
}
