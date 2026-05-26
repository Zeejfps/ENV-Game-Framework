namespace ZGF.Gui;

public interface IStyleClassCollection : IEnumerable<string>
{
    int Count { get; }
    bool Contains(string styleClass);
    void Add(string styleClass);
    bool Remove(string styleClass);
}
