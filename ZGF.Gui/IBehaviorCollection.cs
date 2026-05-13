namespace ZGF.Gui;

public interface IBehaviorCollection : IEnumerable<IViewBehavior>
{
    int Count { get; }
    void Add(IViewBehavior behavior);
    bool Remove(IViewBehavior behavior);
}
