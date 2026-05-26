namespace ZGF.Gui;

public interface IStyleModifierCollection : IEnumerable<string>
{
    int Count { get; }
    bool Contains(string modifier);
    void Add(string modifier);
    bool Remove(string modifier);
}
