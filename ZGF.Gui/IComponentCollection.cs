namespace ZGF.Gui;

public interface IComponentCollection : IEnumerable<Component>
{
    int Count { get; }
    void Add(Component component);
    bool Remove(Component component);
}