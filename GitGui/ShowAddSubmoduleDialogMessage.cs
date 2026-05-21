namespace GitGui;

// Broadcast from a primary RepoRow's "Add submodule…" menu — DialogPresenter shows
// AddSubmoduleDialog so the user can enter URL, path, and an optional branch.
public readonly record struct ShowAddSubmoduleDialogMessage(Repo Primary);
