namespace GitGui;

public interface IGitService
{
    CommitSnapshot Load(Repo repo, int cap);
}
