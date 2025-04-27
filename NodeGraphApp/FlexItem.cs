namespace NodeGraphApp;

public delegate void BoundsChangedCallback(ScreenRect bounds);

public sealed class FlexItem
{
    private ScreenRect _bounds;

    public ScreenRect Bounds
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