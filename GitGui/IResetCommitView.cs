namespace GitGui;

public interface IResetCommitView
{
    bool ButtonsEnabled { set; }
    string? ErrorMessage { set; }

    event Action<ResetMode> ResetRequested;

    void Close();
}
