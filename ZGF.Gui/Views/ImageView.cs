namespace ZGF.Gui.Views;

public sealed class ImageView : MultiChildView
{
    private readonly ICanvas _canvas;

    public ImageView(ICanvas canvas)
    {
        _canvas = canvas;
    }

    public uint TintColor { get; set; } = 0xFFFFFFFF;
    public float Rotation { get; set; }

    private string? _imageId;
    public string? ImageId
    {
        get => _imageId;
        set => SetField(ref _imageId, value);
    }

    protected override float MeasureWidthIntrinsic()
    {
        if (Width.IsSet)
            return Width.Value;

        if (ImageId != null)
            return _canvas.GetImageWidth(ImageId);

        return WidthConstraint;
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        // Images have an intrinsic size and don't reflow — ignore availableWidth.
        if (Height.IsSet)
            return Height.Value;

        if (ImageId != null)
            return _canvas.GetImageHeight(ImageId);

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
            ZIndex = GetDrawZIndex(),
            TintColor = TintColor,
            Rotation = Rotation,
        });
    }
}