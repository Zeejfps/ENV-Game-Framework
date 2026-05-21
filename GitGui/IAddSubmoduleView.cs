namespace GitGui;

public interface IAddSubmoduleView
{
    string Url { get; }
    string Path { get; }
    string Branch { get; }
    bool Force { get; }
    bool AddEnabled { set; }
    string? ErrorMessage { set; }
    event Action InputsChanged;
    event Action AddRequested;
    void FocusUrl();
    void Close();
}

public readonly record struct AddSubmoduleViewRequest(Repo Primary);
