namespace GitGui;

// Broadcast by ActionsToolbarPresenter when the user clicks Push but the local branch has
// diverged from its upstream (Ahead > 0 AND Behind > 0). DialogPresenter shows
// ForcePushDialog so the user can confirm overwriting the remote. Carries the ahead/behind
// counts so the dialog can describe the situation precisely.
public readonly record struct ShowForcePushDialogMessage(Repo Repo, string BranchName, int Ahead, int Behind);
