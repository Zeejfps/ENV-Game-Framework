namespace GitGui;

// Broadcast by BranchesView when the user double-clicks a remote branch that has no
// matching local branch — OverlayView shows CheckoutBranchDialog in response so the user
// can pick a local name and decide whether to set up tracking.
public readonly record struct ShowCheckoutDialogMessage(
    Repo Repo,
    string RemoteName,
    string RemoteBranchName,
    string SuggestedLocalName);
