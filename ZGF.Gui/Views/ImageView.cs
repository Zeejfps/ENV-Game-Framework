namespace ZGF.Gui;

public sealed class ImageView : MultiChildView
{
    public StyleValue<uint> TintColor
    {
        get => _localStyle.TintColor;
        set
        {
            if (Equals(_localStyle.TintColor, value)) return;
            _localStyle.TintColor = value;
            MarkLocalStyleDirty();
        }
    }

    public StyleValue<float> Rotation
    {
        get => _localStyle.Rotation;
        set
        {
            if (Equals(_localStyle.Rotation, value)) return;
            _localStyle.Rotation = value;
            MarkLocalStyleDirty();
        }
    }

    private string? _imageId;
    public string? ImageId
    {
        get => _imageId;
        set => SetField(ref _imageId, value);
    }

    public override float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
            return PreferredWidth.Value;

        if (ImageId != null && Context != null)
            return Context.Canvas.GetImageWidth(ImageId);

        return WidthConstraint;
    }

    public override float MeasureHeight(float availableWidth)
    {
        // Images have an intrinsic size and don't reflow — ignore availableWidth.
        if (PreferredHeight.IsSet)
            return PreferredHeight.Value;

        if (ImageId != null && Context != null)
            return Context.Canvas.GetImageHeight(ImageId);

        return HeightConstraint;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (_imageId == null)
            return;

        c.DrawImage(new DrawImageInputs
        {
            Position = Position,
            ImageId = _imageId,
            ZIndex = ZIndex,
            Style = BuildDrawStyle(),
        });
    }

    private ImageStyle BuildDrawStyle() => new()
    {
        TintColor = new StyleValue<uint>(_resolvedStyle.TintColor, true),
        Rotation = new StyleValue<float>(_resolvedStyle.Rotation, true),
    };
}
