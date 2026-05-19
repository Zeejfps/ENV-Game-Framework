namespace GitGui;

// Broadcast when the user clicks the "Branch" button in the actions toolbar — DialogPresenter
// shows CreateBranchDialog so the user can name the new branch, pick a starting point, and
// optionally check it out.
public readonly record struct ShowCreateBranchDialogMessage(Repo Repo, string SuggestedStartPoint);
