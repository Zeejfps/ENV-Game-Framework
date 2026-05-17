namespace ZGF.Gui.Layouts;

public sealed class BorderLayoutView : View
{
    public View? North
    {
        get;
        set => SetView(ref field, value);
    }

    public View? East
    {
        get;
        set => SetView(ref field, value);
    }

    public View? West
    {
        get;
        set => SetView(ref field, value);
    }

    private View? _south;
    public View? South
    {
        get => _south;
        set => SetView(ref _south, value);
    }

    public View? Center
    {
        get;
        set => SetView(ref field, value);
    }

    private void SetView(ref View? view, View? value)
    {
        if (view == value)
            return;

        var prevComponent = view;
        view = value;

        if (prevComponent != null)
        {
            RemoveChildFromSelf(prevComponent);
        }

        if (view != null)
        {
            AddChildToSelf(view);
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
            // Also publish the measured height as a constraint. A descendant's content
            // change marks only IsChildrenDirty up the tree; without this set-and-compare,
            // North.IsSelfDirty stays false and OnLayoutSelf never runs to pick up the new
            // height — leaving North.Position.Height stale even though centerAreaHeight
            // (computed below) reflects the new height. The desync manifests as the Center
            // resizing correctly while the North region keeps drawing at its old size.
            North.MaxHeightConstraint = height;
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
            // See note on North above — same problem, same fix.
            South.MaxHeightConstraint = height;
            South.LayoutSelf();
            centerAreaHeight -= height;
            bottomOffset += height;
        }

        if (West != null)
        {
            var width = West.MeasureWidth();
            West.LeftConstraint = position.Left;
            West.BottomConstraint = position.Bottom + bottomOffset;
            // Symmetric to North/South height-publish above.
            West.MinWidthConstraint = width;
            West.MaxWidthConstraint = width;
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
            // Symmetric to North/South height-publish above.
            East.MinWidthConstraint = width;
            East.MaxWidthConstraint = width;
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
