namespace GitGui;

// Broadcast from a WorktreeRow's "Remove worktree…" menu — DialogPresenter shows the
// confirm dialog. Primary + worktree are split so the presenter can shell out against
// the primary's working dir but speak about the worktree by name to the user.
public readonly record struct ShowRemoveWorktreeDialogMessage(Repo Primary, Repo Worktree);
