namespace ZGF.Gui;

public class RectView : MultiChildView
{
    public StyleValue<uint> BackgroundColor
    {
        // Return the cascade-resolved value (what's actually drawn), matching the other
        // getters on this view. The setter still writes to _localStyle (imperative
        // override slot) and re-cascades.
        get => new(_resolvedStyle.BackgroundColor, true);
        set
        {
            if (Equals(_localStyle.BackgroundColor, value)) return;
            _localStyle.BackgroundColor = value;
            MarkLocalStyleDirty();
        }
    }

    public PaddingStyle Padding
    {
        get => _resolvedStyle.Padding;
        set
        {
            if (Equals(_localStyle.Padding, value)) return;
            _localStyle.Padding = value;
            MarkLocalStyleDirty();
        }
    }

    public BorderColorStyle BorderColor
    {
        get => _resolvedStyle.BorderColor;
        set
        {
            if (Equals(_localStyle.BorderColor, value)) return;
            _localStyle.BorderColor = value;
            MarkLocalStyleDirty();
        }
    }

    public BorderSizeStyle BorderSize
    {
        get => _resolvedStyle.BorderSize;
        set
        {
            if (Equals(_localStyle.BorderSize, value)) return;
            _localStyle.BorderSize = value;
            MarkLocalStyleDirty();
        }
    }

    public BorderRadiusStyle BorderRadius
    {
        get => _resolvedStyle.BorderRadius;
        set
        {
            if (Equals(_localStyle.BorderRadius, value)) return;
            _localStyle.BorderRadius = value;
            MarkLocalStyleDirty();
        }
    }

    public BoxShadowStyle BoxShadow
    {
        get => _resolvedStyle.BoxShadow;
        set
        {
            if (Equals(_localStyle.BoxShadow, value)) return;
            _localStyle.BoxShadow = value;
            MarkLocalStyleDirty();
        }
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var z = GetDrawZIndex();

        if (_resolvedStyle.BoxShadow.IsActive)
        {
            c.DrawBoxShadow(new DrawBoxShadowInputs
            {
                Position = Position,
                BorderRadius = _resolvedStyle.BorderRadius,
                Shadow = _resolvedStyle.BoxShadow,
                ZIndex = z,
            });
        }

        c.DrawRect(new DrawRectInputs
        {
            Position = Position,
            Style = BuildDrawStyle(),
            ZIndex = z,
        });
    }

    private RectStyle BuildDrawStyle() => new()
    {
        BackgroundColor = new StyleValue<uint>(_resolvedStyle.BackgroundColor, true),
        Padding = _resolvedStyle.Padding,
        BorderColor = _resolvedStyle.BorderColor,
        BorderSize = _resolvedStyle.BorderSize,
        BorderRadius = _resolvedStyle.BorderRadius,
        BoxShadow = _resolvedStyle.BoxShadow,
    };

    public override float MeasureWidth()
    {
        var width = base.MeasureWidth();
        var padding = _resolvedStyle.Padding;
        var borderSize = _resolvedStyle.BorderSize;
        width += padding.Left + padding.Right + borderSize.Left + borderSize.Right;
        return width;
    }

    public override float MeasureHeight(float availableWidth)
    {
        var padding = _resolvedStyle.Padding;
        var borderSize = _resolvedStyle.BorderSize;
        var horizontalChrome = padding.Left + padding.Right + borderSize.Left + borderSize.Right;
        var childAvailableWidth = availableWidth > 0f ? availableWidth - horizontalChrome : availableWidth;
        var height = base.MeasureHeight(childAvailableWidth);
        height += padding.Top + padding.Bottom + borderSize.Top + borderSize.Bottom;
        return height;
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var padding = _resolvedStyle.Padding;
        var border = _resolvedStyle.BorderSize;

        var left = position.Left + padding.Left + border.Left;
        var right = position.Right - padding.Right - border.Right;
        var top = position.Top - padding.Top - border.Top;
        var bottom = position.Bottom + padding.Bottom + border.Bottom;

        foreach (var child in Children)
        {
            child.LeftConstraint = left;
            child.BottomConstraint = bottom;
            child.WidthConstraint = right - left;
            child.HeightConstraint = top - bottom;
            child.LayoutSelf();
        }
    }
}
