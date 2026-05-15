namespace ZGF.Gui;

public interface IComponentCollection : IEnumerable<MultiChildView>
{
    int Count { get; }
    void Add(MultiChildView view);
    bool Remove(MultiChildView view);
    bool Contains(MultiChildView view);
    void Clear();
}