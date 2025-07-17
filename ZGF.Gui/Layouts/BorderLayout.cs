using ZGF.Geometry;

namespace ZGF.Gui.Layouts;

public sealed class BorderLayout : Component
{
    private Component? _north;
    public Component? North
    {
        get => _north;
        set => SetComponent(ref _north, value);
    }

    private Component? _east;
    public Component? East
    {
        get => _east;
        set => SetComponent(ref _east, value);
    }

    private Component? _west;
    public Component? West
    {
        get => _west;
        set => SetComponent(ref _west, value);
    }

    private Component? _south;
    public Component? South
    {
        get => _south;
        set => SetComponent(ref _south, value);
    }

    private Component? _center;
    public Component? Center
    {
        get => _center;
        set => SetComponent(ref _center, value);
    }

    private void SetComponent(ref Component? component, Component? value)
    {
        if (component == value)
            return;
            
        var prevComponent = component;
        component = value;
            
        if (prevComponent != null)
        {
            Remove(prevComponent);
        }

        if (component != null)
        {
            Add(component);
        }
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        
        var centerAreaWidth = position.Width;
        var centerAreaHeight = position.Height;

        var leftOffset = 0f;
        var bottomOffset = 0f;
        
        if (North != null)
        {
            var height = North.MeasureHeight();
            North.Constraints = new RectF(position.Left, position.Top - height, position.Width, height);
            North.LayoutSelf();
            centerAreaHeight -= height;
        }

        if (South != null)
        {
            var height = South.MeasureHeight();
            South.Constraints = new RectF(
                position.Left,
                position.Bottom,
                position.Width,
                height);
            
            South.LayoutSelf();
            centerAreaHeight -= height;
            bottomOffset += height;
        }

        if (West != null)
        {
            var width = West.MeasureWidth();
            West.Constraints = new RectF
            {
                Left = position.Left,
                Bottom = position.Bottom + bottomOffset,
                Width = width,
                Height = centerAreaHeight,
            };
            West.LayoutSelf();
            centerAreaWidth -= width;
            leftOffset += width;
        }
        
        if (East != null)
        {
            var width = East.MeasureWidth();
            East.Constraints = new RectF
            {
                Left = position.Right - width,
                Bottom = position.Bottom + bottomOffset,
                Width = width,
                Height = centerAreaHeight,
            };
            East.LayoutSelf();
            centerAreaWidth -= width;
        }

        if (Center != null)
        {
            Center.Constraints = new RectF
            {
                Left = position.Left + leftOffset,
                Width = centerAreaWidth,
                Bottom = position.Bottom + bottomOffset,
                Height = centerAreaHeight
            };
            Center.LayoutSelf();
        }
    }
}