namespace GitGui;

public interface IStashView
{
    string Message { get; }
    bool IncludeUntracked { get; }
    bool KeepStaged { get; }
    bool StashEnabled { set; }
    string? ErrorMessage { set; }
    event Action MessageChanged;
    event Action StashRequested;
    void FocusMessage();
    void Close();
}

public readonly record struct StashRequest(Repo Repo);
