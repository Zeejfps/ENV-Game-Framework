namespace GitGui;

public readonly record struct ShowPublishBranchDialogMessage(
    Repo Repo,
    string LocalBranch);
