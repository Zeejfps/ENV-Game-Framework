namespace GitGui;

public readonly record struct ShowMergeBranchDialogMessage(
    Repo Repo,
    string SourceRef,
    string SourceDisplay,
    string TargetBranch);
