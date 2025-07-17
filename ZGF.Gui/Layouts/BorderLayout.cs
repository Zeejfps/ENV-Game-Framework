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
        Console.WriteLine(position);
        
        var centerAreaWidth = position.Width;
        var centerAreaHeight = position.Height;

        var leftOffset = 0f;
        var bottomOffset = 0f;
        
        if (North != null)
        {
            var height = North.MeasureHeight();
            North.LeftConstraint = position.Left;
            North.BottomConstraint = position.Top - height;
            North.WidthConstraint = position.Width;
            North.LayoutSelf();
            centerAreaHeight -= height;
        }

        if (South != null)
        {
            var height = South.MeasureHeight();
            South.LeftConstraint = position.Left;
            South.BottomConstraint = position.Bottom;
            South.WidthConstraint = position.Width;
            South.LayoutSelf();
            centerAreaHeight -= height;
            bottomOffset += height;
        }

        if (West != null)
        {
            var width = West.MeasureWidth();
            West.LeftConstraint = position.Left;
            West.BottomConstraint = position.Bottom + bottomOffset;
            West.HeightConstraint = centerAreaHeight;
            West.LayoutSelf();
            centerAreaWidth -= width;
            leftOffset += width;
        }
        
        if (East != null)
        {
            var width = East.MeasureWidth();
            East.LeftConstraint = position.Right - width;
            East.BottomConstraint = position.Bottom + bottomOffset;
            East.HeightConstraint = centerAreaHeight;
            East.LayoutSelf();
            centerAreaWidth -= width;
        }

        if (Center != null)
        {
            Center.LeftConstraint = position.Left + leftOffset;
            Center.BottomConstraint = position.Bottom + bottomOffset;
            Center.WidthConstraint = centerAreaWidth;
            Center.HeightConstraint = centerAreaHeight;
            Center.LayoutSelf();
        }
    }
}