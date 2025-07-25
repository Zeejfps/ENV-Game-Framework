namespace ZGF.Gui;

public sealed class ImageView : View
{
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
            return Context.ImageManager.GetImageWidth(ImageId);

        return MinWidthConstraint;
    }

    public override float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight.Value;

        if (ImageId != null && Context != null)
            return Context.ImageManager.GetImageHeight(ImageId);

        return MaxHeightConstraint;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (_imageId == null)
            return;

        c.AddCommand(new DrawImageCommand
        {
            Position = Position,
            ImageId = _imageId,
            ZIndex = ZIndex,
        });
    }
}