namespace GitGui;

// Broadcast by LocalChangesViewModel when the user clicks Discard in the Unstaged panel
// header — DialogPresenter shows DiscardChangesDialog so the user can confirm before the
// working-tree changes are thrown away.
public readonly record struct ShowDiscardChangesDialogMessage(Repo Repo, IReadOnlyList<string> Paths);
