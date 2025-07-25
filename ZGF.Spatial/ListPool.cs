﻿namespace ZnvQuadTree;

internal sealed class ListPool<T>
{
    private List<T>[] _availableLists;
    private int _availableListCount;
    private object _lock = new();
    
    public ListPool(int initialSize)
    {
        _availableListCount = initialSize;
        _availableLists = new List<T>[initialSize];
        for (var i = 0; i < initialSize; i++)
        {
            _availableLists[i] = new List<T>();
        }
    }

    public PooledList Rent()
    {
        lock (_lock)
        {
            if (_availableListCount == 0)
            {
                Grow();
            }

            var list = _availableLists[--_availableListCount];
            return new PooledList(this, list);
        }
    }

    private void Return(List<T> list)
    {
        lock (_lock)
        {
            if (_availableListCount >= _availableLists.Length)
                return;
        
            list.Clear();
            _availableLists[_availableListCount++] = list;
        }
    }

    private void Grow()
    {
        var oldCapacity = _availableLists.Length;
        var newCapacity = 2 * Math.Max(1, oldCapacity);
        
        Array.Resize(ref _availableLists, newCapacity);
        
        var listsToAdd = Math.Min(oldCapacity, newCapacity - oldCapacity);
        for (var i = 0; i < listsToAdd; i++)
        {
            _availableLists[_availableListCount++] = new List<T>();
        }
    }
    
    public ref struct PooledList : IDisposable
    {
        public readonly List<T> List; 
        private readonly ListPool<T> _pool;
        private bool _isDisposed;

        internal PooledList(ListPool<T> pool, List<T> list)
        {
            _pool = pool;
            List = list;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            
            _isDisposed = true;
            _pool.Return(List);
        }
    }

    public static ListPool<T> Shared { get; } = new(5);
}