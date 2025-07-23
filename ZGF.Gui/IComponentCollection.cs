namespace ZGF.Gui;

public interface IComponentCollection : IEnumerable<View>
{
    int Count { get; }
    void Add(View view);
    bool Remove(View view);
    bool Contains(View view);
}