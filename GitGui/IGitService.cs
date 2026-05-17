namespace GitGui;

public interface IGitService
{
    CommitSnapshot Load(Repo repo, int cap);
    CommitDetails LoadDetails(Repo repo, string sha);
    LocalChangesSnapshot GetLocalChanges(Repo repo);
    void Stage(Repo repo, IReadOnlyList<string> paths);
    void Unstage(Repo repo, IReadOnlyList<string> paths);
}
