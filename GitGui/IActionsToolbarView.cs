namespace GitGui;

public interface IActionsToolbarView
{
    bool PushEnabled { set; }
    bool PullEnabled { set; }
    bool FetchEnabled { set; }
    bool PushBusy { set; }
    bool PullBusy { set; }
    bool FetchBusy { set; }
    float PushRotation { set; }
    float PullRotation { set; }
    float FetchRotation { set; }
    bool RepoActionsEnabled { set; }
    bool StashEnabled { set; }
    // Counts shown as small colored chips next to the Push/Pull labels. Null/0 hides.
    // Push uses the ahead colour, Pull uses the behind colour — same vocabulary as
    // BranchesView so the visual language stays consistent across the two surfaces.
    int? PushBadge { set; }
    int? PullBadge { set; }
    // Current branch name to display in the toolbar's status chip. Null hides the chip
    // entirely; non-null + IsDetached=true makes it render "(detached HEAD)" instead.
    string? CurrentBranch { set; }
    bool CurrentBranchDetached { set; }
    string? Error { set; }
    event Action PushRequested;
    event Action PullRequested;
    event Action FetchRequested;
    event Action OpenInFolderRequested;
    event Action OpenInTerminalRequested;
    event Action BranchRequested;
    event Action StashRequested;
}
