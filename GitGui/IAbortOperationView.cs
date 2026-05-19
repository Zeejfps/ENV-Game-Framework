namespace GitGui;

public interface IAbortOperationView
{
    bool AbortEnabled { set; }
    string? ErrorMessage { set; }

    event Action AbortRequested;

    void Close();
}
