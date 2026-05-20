namespace GitGui;

// Broadcast by BranchesViewModel when the user picks "Rename…" on a local branch row —
// DialogPresenter shows RenameBranchDialog so the user can type a new name and decide
// whether to force-overwrite an existing target.
public readonly record struct ShowRenameBranchDialogMessage(Repo Repo, string CurrentName);
