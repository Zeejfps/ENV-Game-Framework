namespace GitGui;

// Broadcast from a submodule row's "Deinit submodule…" menu — DialogPresenter shows
// DeinitSubmoduleDialog so the user can confirm and optionally force a deinit with a
// dirty working tree.
public readonly record struct ShowDeinitSubmoduleDialogMessage(Repo Primary, Repo Submodule);
