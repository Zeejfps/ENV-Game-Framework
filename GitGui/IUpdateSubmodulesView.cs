namespace GitGui;

public interface IUpdateSubmodulesView
{
    bool Init { get; }
    bool Recursive { get; }
    SubmoduleUpdateMode Mode { get; }
    bool UpdateEnabled { set; }
    string? ErrorMessage { set; }
    event Action UpdateRequested;
    void Close();
}

// TargetSubmodule == null means "update every submodule under the parent".
public readonly record struct UpdateSubmodulesViewRequest(Repo Primary, Repo? TargetSubmodule);
