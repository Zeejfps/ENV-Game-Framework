namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    /// <summary>Creates the image <paramref name="imageId"/> from straight-alpha RGBA8 pixels, rows
    /// top-down, or replaces its pixels and size, for <see cref="Image"/> to draw. Returns false
    /// where the canvas cannot hold images.</summary>
    public bool SetImage(string imageId, int width, int height, ReadOnlySpan<byte> rgba) =>
        _canvas.CreateOrUpdateRgbaImage(imageId, width, height, rgba);

    /// <summary>Whether <see cref="SetSharedTextureImage"/> can import textures of <paramref name="kind"/>.</summary>
    public bool SupportsSharedTextures(SharedTextureKind kind) => _canvas.SupportsSharedTextures(kind);

    /// <summary>Creates or replaces the image <paramref name="imageId"/> from a GPU texture another API
    /// hands over, copied before this returns; false when it could not be imported.</summary>
    public bool SetSharedTextureImage(string imageId, in SharedTexture texture) =>
        _canvas.CreateOrUpdateSharedTextureImage(imageId, texture);

    /// <summary>How many device pixels one UI point covers: the monitor's scale times <see cref="Scale"/>.</summary>
    public float PixelScale => _canvas.DpiScale;

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
