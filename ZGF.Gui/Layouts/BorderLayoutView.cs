namespace ZGF.Gui.Layouts;

public sealed class BorderLayoutView : View
{
    private View? _north;
    public View? North
    {
        get => _north;
        set => SetComponent(ref _north, value);
    }

    private View? _east;
    public View? East
    {
        get => _east;
        set => SetComponent(ref _east, value);
    }

    private View? _west;
    public View? West
    {
        get => _west;
        set => SetComponent(ref _west, value);
    }

    private View? _south;
    public View? South
    {
        get => _south;
        set => SetComponent(ref _south, value);
    }

    private View? _center;
    public View? Center
    {
        get => _center;
        set => SetComponent(ref _center, value);
    }

    private void SetComponent(ref View? component, View? value)
    {
        if (component == value)
            return;
            
        var prevComponent = component;
        component = value;
            
        if (prevComponent != null)
        {
            RemoveChildFromSelf(prevComponent);
        }

        if (component != null)
        {
            AddChildToSelf(component);
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
            North.LeftConstraint = position.Left;
            North.BottomConstraint = position.Top - height;
            North.MinWidthConstraint = position.Width;
            North.MaxWidthConstraint = position.Width;
            North.LayoutSelf();
            centerAreaHeight -= height;
        }

        if (South != null)
        {
            var height = South.MeasureHeight();
            South.LeftConstraint = position.Left;
            South.BottomConstraint = position.Bottom;
            South.MinWidthConstraint = position.Width;
            South.MaxWidthConstraint = position.Width;
            South.LayoutSelf();
            centerAreaHeight -= height;
            bottomOffset += height;
        }

        if (West != null)
        {
            var width = West.MeasureWidth();
            West.LeftConstraint = position.Left;
            West.BottomConstraint = position.Bottom + bottomOffset;
            West.MaxHeightConstraint = centerAreaHeight;
            West.LayoutSelf();
            centerAreaWidth -= width;
            leftOffset += width;
        }
        
        if (East != null)
        {
            var width = East.MeasureWidth();
            East.LeftConstraint = position.Right - width;
            East.BottomConstraint = position.Bottom + bottomOffset;
            East.MaxHeightConstraint = centerAreaHeight;
            East.LayoutSelf();
            centerAreaWidth -= width;
        }

        if (Center != null)
        {
            Center.LeftConstraint = position.Left + leftOffset;
            Center.BottomConstraint = position.Bottom + bottomOffset;
            Center.MinWidthConstraint = centerAreaWidth;
            Center.MaxWidthConstraint = centerAreaWidth;
            Center.MaxHeightConstraint = centerAreaHeight;
            Center.LayoutSelf();
        }
    }
}