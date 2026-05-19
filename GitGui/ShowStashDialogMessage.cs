namespace GitGui;

// Broadcast when the user clicks the "Stash" button in the actions toolbar —
// DialogPresenter shows StashDialog so the user can name the stash, choose whether
// to include untracked files, and whether to keep staged changes in the index.
public readonly record struct ShowStashDialogMessage(Repo Repo);
