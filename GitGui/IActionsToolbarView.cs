namespace GitGui;

public interface IActionsToolbarView
{
    bool PushEnabled { set; }
    bool PullEnabled { set; }
    bool PushBusy { set; }
    bool PullBusy { set; }
    float PushRotation { set; }
    float PullRotation { set; }
    bool RepoActionsEnabled { set; }
    string? Error { set; }
    event Action PushRequested;
    event Action PullRequested;
    event Action OpenInFolderRequested;
    event Action OpenInTerminalRequested;
}
