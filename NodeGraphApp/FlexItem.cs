namespace NodeGraphApp;

public delegate void BoundsChangedCallback(RectF bounds);

public sealed class FlexItem
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
            BoundsChanged?.Invoke(_bounds);
        }
    }
    
    public BoundsChangedCallback? BoundsChanged { get; set; }
    public float FlexGrow { get; init; }
    public float BaseHeight { get; init; }
}