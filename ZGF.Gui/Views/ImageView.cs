namespace ZGF.Gui;

public sealed class ImageView : MultiChildView
{
    private readonly ImageStyle _style = new();

    public StyleValue<uint> TintColor
    {
        get => _style.TintColor;
        set => SetField(ref _style.TintColor, value);
    }

    public StyleValue<float> Rotation
    {
        get => _style.Rotation;
        set => SetField(ref _style.Rotation, value);
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

    public override float MeasureHeight()
    {
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
            Style = _style
        });
    }
}