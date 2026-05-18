namespace GitGui;

public interface ICheckoutBranchView
{
    string Name { get; }
    bool Track { get; }
    bool CheckoutEnabled { set; }
    event Action NameChanged;
    void FocusName(string initialText);
    void Close();
}