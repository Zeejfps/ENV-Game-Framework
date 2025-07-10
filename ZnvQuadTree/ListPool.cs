namespace ZnvQuadTree;

internal sealed class ListPool<T>
{
    private List<T>[] _availableLists;
    private int _availableListCount;
    
    public ListPool(int initialSize)
    {
        _availableLists = new List<T>[initialSize];
        for (var i = 0; i < initialSize; i++)
        {
            _availableLists[i] = new List<T>();
        }
    }

    public PooledList Rent()
    {
        if (_availableListCount == 0)
        {
            Grow();
        }

        var list = _availableLists[--_availableListCount];
        return new PooledList(this, list);
    }

    private void Return(List<T> list)
    {
        if (_availableListCount >= _availableLists.Length)
            return;
        
        list.Clear();
        _availableLists[_availableListCount++] = list;
    }

    private void Grow()
    {
        var oldCapacity = Math.Max(1, _availableLists.Length);
        var newCapacity = 2 * oldCapacity;
        
        Array.Resize(ref _availableLists, newCapacity);
        
        var listsToAdd = Math.Min(oldCapacity, newCapacity - oldCapacity);
        for (var i = 0; i < listsToAdd; i++)
        {
            _availableLists[_availableListCount++] = new List<T>();
        }
    }
    
    public readonly struct PooledList : IDisposable
    {
        public readonly List<T> List;
        private readonly ListPool<T> _pool;

        internal PooledList(ListPool<T> pool, List<T> list)
        {
            _pool = pool;
            List = list;
        }

        public void Dispose()
        {
            _pool.Return(List);
        }
    }

    public static ListPool<T> Shared { get; } = new(5);
}