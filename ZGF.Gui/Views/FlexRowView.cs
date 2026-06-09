namespace ZGF.Gui.Views;

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

/// <summary>Horizontal <see cref="FlexView"/>. Cross-axis defaults to Start; opt children into growth with <see cref="FlexItem"/>.</summary>
public sealed class FlexRowView : FlexView
{
    public FlexRowView()
    {
        Axis = Axis.Horizontal;
    }
}
