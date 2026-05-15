namespace ZGF.Gui;

public interface IComponentCollection : IEnumerable<View>
{
    int Count { get; }
    View this[int index] { get; }
    void Add(View view);
    void Insert(int index, View view);
    void Move(View view, int newIndex);
    bool Remove(View view);
    bool Contains(View view);
    void Clear();
}
