namespace GitGui;

public interface IGitService
{
    CommitSnapshot Load(Repo repo, int cap);
    CommitDetails LoadDetails(Repo repo, string sha);
}
