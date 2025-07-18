namespace ZGF.Gui;

public sealed class ImageInfo
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required string ImageUri { get; init; }
}

public sealed class Image : Component
{
    private ImageInfo? _imageInfo;
    public ImageInfo? ImageUri
    {
        get => _imageInfo;
        set => SetField(ref _imageInfo, value);
    }

    public override float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
            return PreferredWidth.Value;

        if (ImageUri != null)
            return ImageUri.Width;

        return 0;
    }

    public override float MeasureHeight()
    {
        if (PreferredHeight.IsSet)
            return PreferredHeight.Value;

        if (ImageUri != null)
            return ImageUri.Height;

        return 0;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (_imageInfo == null)
            return;

        c.AddCommand(new DrawImageCommand
        {
            Position = Position,
            ImageInfo = _imageInfo,
            ZIndex = ZIndex,
        });
    }
}