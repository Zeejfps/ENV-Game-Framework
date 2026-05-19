namespace GitGui;

public interface ICreateBranchView
{
    string Name { get; }
    string StartPoint { get; }
    bool Checkout { get; }
    bool CreateEnabled { set; }
    string? ErrorMessage { set; }
    event Action NameChanged;
    event Action CreateRequested;
    void FocusName();
    void Close();
}

public readonly record struct CreateBranchRequest(Repo Repo, string SuggestedStartPoint);
