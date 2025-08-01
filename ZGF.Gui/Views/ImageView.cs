namespace ZGF.Gui;

public sealed class ImageView : View
{
    private readonly ImageStyle _style = new();

    public StyleValue<uint> TintColor
    {
        get => _style.TintColor;
        set => SetField(ref _style.TintColor, value);
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

        return MinWidthConstraint;
    }

    public override float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight.Value;

        if (ImageId != null && Context != null)
            return Context.Canvas.GetImageHeight(ImageId);

        return MaxHeightConstraint;
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