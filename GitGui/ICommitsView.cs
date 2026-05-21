namespace GitGui;

public abstract record CommitsViewModel
{
    public sealed record NoRepo : CommitsViewModel;
    public sealed record Loading : CommitsViewModel;
    public sealed record Error(string Message) : CommitsViewModel;
    public sealed record Loaded(CommitSnapshot Snapshot) : CommitsViewModel;
}

public interface ICommitsView
{
    /// <summary>Push the load/render state.</summary>
    void SetViewModel(CommitsViewModel vm);

    /// <summary>Push the current selection; <c>null</c> clears it.</summary>
    void SetSelectedSha(string? sha);

    /// <summary>The view's current selection, or <c>null</c>. Survives presenter
    /// disposal so a freshly-mounted presenter can re-broadcast it.</summary>
    string? SelectedSha { get; }

    /// <summary>Raised when the user clicks a row. Payload is the row's commit SHA.</summary>
    event Action<string> CommitClicked;

    /// <summary>Raised when the user picks "Checkout commit" from a row's context menu.</summary>
    event Action<string> CheckoutCommitRequested;
}
