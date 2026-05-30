namespace ZGF.Gui;

public sealed class ImageView : MultiChildView
{
    private uint _tintColor = 0xFFFFFFFF;
    public uint TintColor
    {
        get => _tintColor;
        set => SetField(ref _tintColor, value);
    }

    private float _rotation;
    public float Rotation
    {
        get => _rotation;
        set => SetField(ref _rotation, value);
    }

    private string? _imageId;
    public string? ImageId
    {
        get => _imageId;
        set => SetField(ref _imageId, value);
    }

    public override float MeasureWidth()
    {
        if (Width.IsSet)
            return Width.Value;

        if (ImageId != null && Context != null)
            return Context.Canvas.GetImageWidth(ImageId);

        return WidthConstraint;
    }

    public override float MeasureHeight(float availableWidth)
    {
        // Images have an intrinsic size and don't reflow — ignore availableWidth.
        if (Height.IsSet)
            return Height.Value;

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
            TintColor = _tintColor,
            Rotation = _rotation,
        });
    }
}