namespace ZGF.Gui;

public sealed class ImageInfo
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string ImageUri { get; init; }
}

public sealed class Image : View
{
    private string? _imageUri;
    public string? ImageUri
    {
        get => _imageUri;
        set => SetField(ref _imageUri, value);
    }

    public override float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
            return PreferredWidth.Value;

        if (ImageUri != null && Context != null)
            return Context.ImageManager.GetImageWidth(ImageUri);

        return 0;
    }

    public override float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight.Value;

        if (ImageUri != null && Context != null)
            return Context.ImageManager.GetImageHeight(ImageUri);

        return 0;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (_imageUri == null)
            return;

        c.AddCommand(new DrawImageCommand
        {
            Position = Position,
            ImageUri = _imageUri,
            ZIndex = ZIndex,
        });
    }
}