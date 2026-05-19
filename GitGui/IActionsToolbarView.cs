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
    string? Error { set; }
    event Action PushRequested;
    event Action PullRequested;
    event Action FetchRequested;
    event Action OpenInFolderRequested;
    event Action OpenInTerminalRequested;
    event Action BranchRequested;
}
