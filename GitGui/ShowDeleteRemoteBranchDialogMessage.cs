namespace GitGui;

// Broadcast by BranchesViewModel when the user picks "Delete remote branch…" on a remote
// branch row — DialogPresenter shows DeleteRemoteBranchDialog. The dialog is explicit that
// this is a network operation that affects only the remote (local copies are untouched).
public readonly record struct ShowDeleteRemoteBranchDialogMessage(Repo Repo, string RemoteName, string BranchName);
