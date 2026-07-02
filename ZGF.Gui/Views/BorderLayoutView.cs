namespace ZGF.Gui.Views;

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

    /// <summary>Vertical spacing between the North, Center, and South regions.</summary>
    public float VGap
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>Horizontal spacing between the West, Center, and East regions.</summary>
    public float HGap
    {
        get;
        set => SetField(ref field, value);
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
            var height = North.MeasureHeight(position.Width);
            North.LeftConstraint = position.Left;
            North.BottomConstraint = position.Top - height;
            North.WidthConstraint = position.Width;
            North.HeightConstraint = height;
            North.LayoutSelf();
            centerAreaHeight -= height + VGap;
        }

        if (South != null)
        {
            var height = South.MeasureHeight(position.Width);
            South.LeftConstraint = position.Left;
            South.BottomConstraint = position.Bottom;
            South.WidthConstraint = position.Width;
            South.HeightConstraint = height;
            South.LayoutSelf();
            centerAreaHeight -= height + VGap;
            bottomOffset += height + VGap;
        }

        // Under RTL the leading (West) edge moves to the right and the trailing (East) edge to the
        // left; the vertical North/South edges are unaffected.
        var leftEdge = IsRtl ? East : West;
        var rightEdge = IsRtl ? West : East;

        if (leftEdge != null)
        {
            var width = leftEdge.MeasureWidth();
            leftEdge.LeftConstraint = position.Left;
            leftEdge.BottomConstraint = position.Bottom + bottomOffset;
            leftEdge.WidthConstraint = width;
            leftEdge.HeightConstraint = centerAreaHeight;
            leftEdge.LayoutSelf();
            centerAreaWidth -= width + HGap;
            leftOffset += width + HGap;
        }

        if (rightEdge != null)
        {
            var width = rightEdge.MeasureWidth();
            rightEdge.LeftConstraint = position.Right - width;
            rightEdge.BottomConstraint = position.Bottom + bottomOffset;
            rightEdge.WidthConstraint = width;
            rightEdge.HeightConstraint = centerAreaHeight;
            rightEdge.LayoutSelf();
            centerAreaWidth -= width + HGap;
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
