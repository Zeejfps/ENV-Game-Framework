namespace GitGui;

public readonly record struct ShowRebaseBranchDialogMessage(
    Repo Repo,
    string SourceBranch,
    string TargetRef,
    string TargetDisplay);
