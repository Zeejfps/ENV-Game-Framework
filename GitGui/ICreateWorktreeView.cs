namespace GitGui;

public interface ICreateWorktreeView
{
    string Path { get; }
    string StartPoint { get; }
    string NewBranchName { get; }
    bool Force { get; }
    bool CreateEnabled { set; }
    string? ErrorMessage { set; }
    event Action InputsChanged;
    event Action CreateRequested;
    void FocusPath();
    void Close();
}

public readonly record struct CreateWorktreeRequest(Repo Primary);
