namespace GitGui;

// Broadcast by BranchesViewModel when the user picks "Delete…" on a local branch row —
// DialogPresenter shows DeleteLocalBranchDialog to confirm and offer a force-delete
// checkbox for branches not fully merged into upstream/HEAD.
public readonly record struct ShowDeleteLocalBranchDialogMessage(Repo Repo, string BranchName);
