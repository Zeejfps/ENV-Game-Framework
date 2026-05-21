namespace GitGui;

// Broadcast by CommitsPresenter when the user picks "Reset to this commit" and the working
// tree has staged or unstaged changes — DialogPresenter shows ResetCommitDialog so the
// user picks soft/mixed/hard explicitly. Counts are surfaced so the dialog can preview the
// blast radius without re-querying the repo.
public readonly record struct ShowResetCommitDialogMessage(
    Repo Repo,
    string Sha,
    string ShortSha,
    int StagedCount,
    int UnstagedCount);
