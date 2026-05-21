namespace GitGui;

public interface IDeinitSubmoduleView
{
    bool Force { get; }
    bool DeinitEnabled { set; }
    string? ErrorMessage { set; }
    event Action DeinitRequested;
    void Close();
}

public readonly record struct DeinitSubmoduleViewRequest(Repo Primary, Repo Submodule);
