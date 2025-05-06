namespace NodeGraphApp;

public class Column
{
    private RectF _bounds;
    public RectF Bounds
    {
        get => _bounds;
        set => SetField(ref _bounds, value);
    }
    
    private Padding _padding;
    public Padding Padding
    {
        get => _padding;
        set => SetField(ref _padding, value);
    }
    
    private float _itemGap;
    public float ItemGap
    {
        get => _itemGap;
        set => SetField(ref _itemGap, value);
    }
    
    public int ItemCount => _items.Count;
    
    private bool _isDirty;
    private readonly List<ColumnItem> _items = new();

    public void AddItem(ColumnItem item)
    {
        _items.Add(item);
        item.BoundsChanged += OnItemBoundsChanged;
        SetDirty();
    }

    public void RemoveItem(ColumnItem item)
    {
        var removed = _items.Remove(item);
        if (removed)
        {
            item.BoundsChanged -= OnItemBoundsChanged;
            SetDirty();
        }
    }

    private void OnItemBoundsChanged(RectF bounds)
    {
        SetDirty();
    }

    private void SetDirty()
    {
        _isDirty = true;
    }
    
    public void InsertItem(ColumnItem item, int index)
    {
        _items.Insert(index, item);
        item.BoundsChanged += OnItemBoundsChanged;
        SetDirty();
    }

    public void RemoveItemAt(int index)
    {
        var item = _items[index];
        item.BoundsChanged -= OnItemBoundsChanged;
        _items.RemoveAt(index);
        SetDirty();
    }
    
    public void Update()
    {
        var items = _items;
        foreach (var item in items)
            item.Update();
        
        if (!_isDirty)
            return;
        
        var bounds = Bounds;
        var itemGap = ItemGap;
        var padding = Padding;
        var itemCount = _items.Count;
        
        var totalHeight = itemGap * (itemCount - 1) + padding.Top + padding.Bottom;
        foreach (var item in items)
        {
            totalHeight += item.Bounds.Height;
        }
        bounds = bounds with
        {
            Height = totalHeight
        };
        
        var top = bounds.Top - padding.Top;
        var left = bounds.Left + padding.Left;
        var width = bounds.Width - padding.Left - padding.Right;
        foreach (var item in items)
        {
            var height = item.Bounds.Height;
            item.Bounds = RectF.FromLeftTopWidthHeight(left, top, width, height);
            top -= height + itemGap;
        }

        _bounds = bounds;
        _isDirty = false;
    }

    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        SetDirty();
    }
}

public abstract class ColumnItem
{
    private RectF _bounds;
    public RectF Bounds
    {
        get => _bounds;
        set
        {
            if (_bounds == value)
                return;
            _bounds = value;
            OnBoundsChanged();
        }
    }
    
    public event Action<RectF>? BoundsChanged;

    public void Update()
    {
        OnUpdate();
    }
    
    protected abstract void OnUpdate();

    protected virtual void OnBoundsChanged()
    {
        BoundsChanged?.Invoke(_bounds);
    }
}