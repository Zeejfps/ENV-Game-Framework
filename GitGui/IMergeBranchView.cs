namespace GitGui;

public interface IMergeBranchView
{
    MergeStrategy Strategy { get; }
    bool MergeEnabled { set; }
    string? ErrorMessage { set; }
    MergePreviewState PreviewState { set; }
    event Action MergeRequested;
    void Close();
}

public readonly record struct MergeBranchRequest(
    Repo Repo,
    string SourceRef,
    string SourceDisplay,
    string TargetBranch);
