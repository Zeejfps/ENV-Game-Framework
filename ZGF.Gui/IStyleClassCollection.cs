namespace ZGF.Gui;

public interface IStyleClassCollection : IEnumerable<string>
{
    int Count { get; }
    void Add(string styleClass);
    bool Remove(string styleClass);
}