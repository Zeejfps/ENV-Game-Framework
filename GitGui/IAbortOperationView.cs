namespace GitGui;

public interface IAbortOperationView
{
    bool AbortEnabled { set; }
    string? ErrorMessage { set; }

    // Lets the presenter relabel the confirm button after a failed --abort that exposed
    // a `git X --quit` / sentinel-cleanup escape hatch — e.g. "Abort rebase" → "Force clear".
    string ConfirmButtonLabel { set; }

    event Action AbortRequested;

    void Close();
}
