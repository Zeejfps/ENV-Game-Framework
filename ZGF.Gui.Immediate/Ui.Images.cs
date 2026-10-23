namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    /// <summary>Creates the image <paramref name="imageId"/> from straight-alpha RGBA8 pixels, rows
    /// top-down, or replaces its pixels and size, for <see cref="Image"/> to draw. Returns false
    /// where the canvas cannot hold images.</summary>
    public bool SetImage(string imageId, int width, int height, ReadOnlySpan<byte> rgba) =>
        _canvas.CreateOrUpdateRgbaImage(imageId, width, height, rgba);

    public bool HasImage(string imageId) => _canvas.HasImage(imageId);

    /// <summary>Frees the image <paramref name="imageId"/>; drawing it afterwards shows nothing.</summary>
    public void RemoveImage(string imageId) => _canvas.RemoveImage(imageId);

    /// <summary>Records an element drawing the image <paramref name="imageId"/> fitted into it,
    /// keeping its aspect: <paramref name="width"/> by <paramref name="height"/> unless
    /// <paramref name="box"/> sizes it.</summary>
    public void Image(string imageId, float width, float height, Box box = default)
    {
        using (Box(box with { Width = box.Width ?? width, Height = box.Height ?? height }))
            Paint(imageId, static (painter, id) => painter.Image(painter.Rect, id));
    }
}
