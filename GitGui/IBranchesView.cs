namespace GitGui;

public interface IBranchesView
{
    void SetRows(IReadOnlyList<BranchRow> rows);
    void SetSelection(BranchSelection? selection);
    void SetBusyBranch(string? fullPath);
    void SetLoadError(string? error);

    /// <summary>Single click on a row. <c>null</c> means click in the panel outside any row.</summary>
    event Action<BranchRow?> RowClicked;

    /// <summary>Double click on a row.</summary>
    event Action<BranchRow> RowActivated;
}
