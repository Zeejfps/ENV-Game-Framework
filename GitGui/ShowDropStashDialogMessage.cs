namespace GitGui;

// Broadcast after a successful `git stash apply` triggered from the branches panel —
// DialogPresenter shows DropStashDialog so the user can drop (the typical
// "stash pop" finish) or keep the stash around.
public readonly record struct ShowDropStashDialogMessage(Repo Repo, int Index, string Label, string Subject);
