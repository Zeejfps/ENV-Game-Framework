namespace GitGui;

// Broadcast from either the primary RepoRow ("Update all submodules…") or a single
// submodule row ("Update submodule…"). If TargetSubmodule is null, the dialog targets
// every submodule under the primary; otherwise just that one.
public readonly record struct ShowUpdateSubmodulesDialogMessage(Repo Primary, Repo? TargetSubmodule);
