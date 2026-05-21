namespace GitGui;

public interface IAbortOperationView
{
    bool AbortEnabled { set; }
    bool CancelEnabled { set; }
    string? ErrorMessage { set; }

    // Lets the presenter relabel the confirm button after a failed --abort that exposed
    // a `git X --quit` / sentinel-cleanup escape hatch — e.g. "Abort rebase" → "Force clear".
    string ConfirmButtonLabel { set; }

    // While the abort runs, show a spinner inside the confirm button so the user can
    // tell something is happening — git aborts on large rebases take a few seconds.
    bool IsBusy { set; }
    float BusyRotation { set; }

    event Action AbortRequested;

    void Close();
}
