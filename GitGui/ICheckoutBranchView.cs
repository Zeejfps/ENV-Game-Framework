namespace GitGui;

public interface ICheckoutBranchView
{
    string Name { get; }
    bool Track { get; }
    bool CheckoutEnabled { set; }
    event Action NameChanged;
    event Action CheckoutRequested;
    void FocusName(string initialText);
    void Close();
}