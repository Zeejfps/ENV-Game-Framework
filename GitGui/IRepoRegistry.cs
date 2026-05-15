namespace GitGui;

public interface IRepoRegistry
{
    IReadOnlyList<Repo> Repos { get; }
    Repo? Active { get; }
    void Open(string path);
    void SetActive(Guid id);
}
