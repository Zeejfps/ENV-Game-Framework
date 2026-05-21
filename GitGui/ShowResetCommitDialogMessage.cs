namespace GitGui;

// Broadcast by CommitsPresenter when the user picks "Reset … to here" and the working
// tree has staged or unstaged changes — DialogPresenter shows ResetCommitDialog so the
// user picks soft/mixed/hard explicitly. BranchName is null when HEAD is detached.
public readonly record struct ShowResetCommitDialogMessage(
    Repo Repo,
    string Sha,
    string ShortSha,
    string Summary,
    string? BranchName,
    int StagedCount,
    int UnstagedCount);
