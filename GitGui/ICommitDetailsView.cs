namespace GitGui;

public interface ICommitDetailsView
{
    void ShowPlaceholder(string text);
    void ShowDetails(CommitDetails details);
}
