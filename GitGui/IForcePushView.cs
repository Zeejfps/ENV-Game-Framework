namespace GitGui;

public interface IForcePushView
{
    bool ForcePushEnabled { set; }
    string? ErrorMessage { set; }

    event Action ForcePushRequested;

    void Close();
}
