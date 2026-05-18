namespace GitGui;

public interface IActionsToolbarView
{
    bool PushEnabled { set; }
    bool PullEnabled { set; }
    bool PushBusy { set; }
    bool PullBusy { set; }
    float PushRotation { set; }
    float PullRotation { set; }
    string? Error { set; }
    event Action PushRequested;
    event Action PullRequested;
}
