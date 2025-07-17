using ZGF.Geometry;

namespace ZGF.Gui.Layouts;

public enum MainAxisAlignment
{
    Start,    // Pack items to the start (left)
    Center,       // Pack items in the center
    End,      // Pack items to the end (right)
    SpaceBetween, // Evenly distribute items, first at start, last at end
    SpaceAround,  // Evenly distribute items with half-size spaces at the ends
    SpaceEvenly   // Evenly distribute items with equal space all around
}

public enum CrossAxisAlignment
{
    Start, // Align to the top
    Center,    // Align to the vertical center
    End,   // Align to the bottom
    Stretch    // Stretch to fill the container's height
}

public struct FlexStyle
{
    public StyleValue<float> Grow { get; set; }
}

public sealed class FlexRow : Component
{
    private float _gap;
    public float Gap
    {
        get => _gap;
        set => SetField(ref _gap, value);
    }
    
    private CrossAxisAlignment _crossAxisAlignment;
    public CrossAxisAlignment CrossAxisAlignment
    {
        get => _crossAxisAlignment;
        set => SetField(ref _crossAxisAlignment, value);
    }

    private MainAxisAlignment _mainAxisAlignment;
    public MainAxisAlignment MainAxisAlignment
    {
        get => _mainAxisAlignment;
        set => SetField(ref _mainAxisAlignment, value);
    }
    
    private readonly Dictionary<Component, FlexStyle> _flexStyleByComponent = new();

    public FlexRow()
    {
        
    }

    public FlexRow(MainAxisAlignment mainAxisAlignment, CrossAxisAlignment crossAxisAlignment, int gap)
    {
        MainAxisAlignment = mainAxisAlignment;
        CrossAxisAlignment = crossAxisAlignment;
        Gap = gap;
    }
    
    public void Add(Component component, FlexStyle style)
    {
        var added = _flexStyleByComponent.TryAdd(component, style);
        if (!added)
            throw new Exception("Component already added");
        
        Add(component);
    }
    
    public void UpdateStyle(Component component, FlexStyle style)
    {
        _flexStyleByComponent[component] = style;
        SetDirty();
    }

    protected override void OnComponentRemoved(Component component)
    {
        _flexStyleByComponent.Remove(component);
        base.OnComponentRemoved(component);
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var children = Children;
        if (children.Count == 0)
        {
            return;
        }
        
        var totalChildrenInitialWidth = 0f;
        var totalFlexGrow = 0f;
        
        foreach (var child in children)
        {
            var style = _flexStyleByComponent.GetValueOrDefault(child);
            totalChildrenInitialWidth += child.MeasureWidth();
            totalFlexGrow += style.Grow;
        }
        
        var totalGap = Gap * (children.Count - 1);
        var totalContentWidth = totalChildrenInitialWidth + totalGap;
        var remainingSpace = position.Width - totalContentWidth;

        var leftOffset = 0f;
        var interItemSpacing = 0f;
        
        if (remainingSpace > 0)
        {
            switch (MainAxisAlignment)
            {
                case MainAxisAlignment.End:
                    leftOffset = remainingSpace;
                    break;
                case MainAxisAlignment.Center:
                    leftOffset = remainingSpace / 2f;
                    break;
                case MainAxisAlignment.SpaceBetween:
                    interItemSpacing = children.Count > 1 ? remainingSpace / (children.Count - 1) : 0;
                    break;
                case MainAxisAlignment.SpaceAround:
                    interItemSpacing = remainingSpace / children.Count;
                    leftOffset = interItemSpacing / 2f;
                    break;
                case MainAxisAlignment.SpaceEvenly:
                    interItemSpacing = remainingSpace / (children.Count + 1);
                    leftOffset = interItemSpacing;
                    break;
            }
        }
        
        var currentLeft = position.Left + leftOffset;
        
        foreach (var child in children)
        {
            var style = _flexStyleByComponent.GetValueOrDefault(child);
            var childSize = child.MeasureSelf();
            var childInitialWidth = childSize.Width;
            var childInitialHeight = childSize.Height;

            // Calculate final width based on FlexGrow
            var finalChildWidth = childInitialWidth;
            if (remainingSpace > 0 && style.Grow > 0 && totalFlexGrow > 0)
            {
                finalChildWidth += (style.Grow / totalFlexGrow) * remainingSpace;
            }
            
            var crossxisAlignment = CrossAxisAlignment;

            // Calculate final height and vertical position
            float finalChildHeight;
            float childBottom; // Y-coordinate of the child's bottom edge

            switch (crossxisAlignment)
            {
                case CrossAxisAlignment.Stretch:
                    finalChildHeight = position.Height;
                    childBottom = position.Bottom;
                    break;

                case CrossAxisAlignment.Start: // Align to Top
                    finalChildHeight = childInitialHeight;
                    childBottom = position.Top - finalChildHeight;
                    break;

                case CrossAxisAlignment.End: // Align to Bottom
                    finalChildHeight = childInitialHeight;
                    childBottom = position.Bottom;
                    break;

                case CrossAxisAlignment.Center:
                default:
                    finalChildHeight = childInitialHeight;
                    var verticalPadding = (position.Height - finalChildHeight) / 2f;
                    childBottom = position.Bottom - verticalPadding;
                    break;
            }

            child.LeftConstraint = currentLeft;
            child.BottomConstraint = childBottom;
            child.WidthConstraint = finalChildWidth;
            child.HeightConstraint = finalChildHeight;
            child.LayoutSelf();
            
            currentLeft += finalChildWidth + Gap + interItemSpacing;
        }
    }
}