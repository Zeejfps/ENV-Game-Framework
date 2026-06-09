namespace ZGF.Gui.Views;

public sealed class ImageView : MultiChildView
{
    public uint TintColor { get; set; } = 0xFFFFFFFF;
    public float Rotation { get; set; }

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
            TintColor = TintColor,
            Rotation = Rotation,
        });
    }
}