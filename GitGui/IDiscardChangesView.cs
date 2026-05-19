namespace GitGui;

public interface IDiscardChangesView
{
    bool DiscardEnabled { set; }
    string? ErrorMessage { set; }

    event Action DiscardRequested;

    void Close();
}
