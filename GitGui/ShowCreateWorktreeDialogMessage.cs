namespace GitGui;

// Broadcast from a primary RepoRow's "New worktree…" menu — DialogPresenter shows the
// CreateWorktreeDialog so the user can pick branch/ref + filesystem path.
public readonly record struct ShowCreateWorktreeDialogMessage(Repo Primary);
