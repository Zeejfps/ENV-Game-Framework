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
            North.Constraints = new RectF(position.Left, position.Top - North.Constraints.Height, position.Width, North.Constraints.Height);
            North.LayoutSelf();
            centerAreaHeight -= North.Position.Height;
        }

        if (South != null)
        {
            South.Constraints = new RectF(
                position.Left,
                position.Bottom,
                position.Width,
                South.Constraints.Height);
            
            South.LayoutSelf();
            centerAreaHeight -= South.Position.Height;
            bottomOffset += South.Position.Height;
        }

        if (West != null)
        {
            West.Constraints = new RectF
            {
                Left = position.Left,
                Bottom = position.Bottom + bottomOffset,
                Width = West.Constraints.Width,
                Height = centerAreaHeight,
            };
            West.LayoutSelf();
            centerAreaWidth -= West.Position.Width;
            leftOffset += West.Position.Width;
        }
        
        if (East != null)
        {
            East.Constraints = new RectF
            {
                Left = position.Right - East.Constraints.Width,
                Bottom = position.Bottom + bottomOffset,
                Width = East.Constraints.Width,
                Height = centerAreaHeight,
            };
            East.LayoutSelf();
            centerAreaWidth -= East.Position.Width;
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