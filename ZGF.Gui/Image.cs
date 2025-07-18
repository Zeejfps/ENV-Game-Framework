namespace ZGF.Gui;

public sealed class Image : Component
{
    private string? _imageUri;
    public string? ImageUri
    {
        get => _imageUri;
        set => SetField(ref _imageUri, value);
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