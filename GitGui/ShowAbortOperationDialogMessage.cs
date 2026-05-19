namespace GitGui;

// Broadcast by the operation-state banner when the user clicks Abort. DialogPresenter
// shows AbortOperationDialog so the user can confirm before the in-progress op is rolled
// back. Carries the current state so the dialog can tailor its wording and the presenter
// can route to the right `git ... --abort` command.
public readonly record struct ShowAbortOperationDialogMessage(Repo Repo, RepoOperationState State);
