using System.Numerics;
using ZGF.Fonts;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

/// <summary>
/// A concrete <see cref="RenderedCanvasBase"/> whose GPU hooks are no-ops but which keeps the
/// instances the base staged, so a test can assert what the real canvas would have drawn — same
/// staging, sorting, transform and font code, no device.
/// </summary>
internal sealed class CaptureCanvas : RenderedCanvasBase
{
    public CaptureCanvas(FreeTypeFontBackend fonts, FontHandle defaultFont, float dpiScale = 1f)
        : base(800, 600, fonts, defaultFont, dpiScale)
    {
    }

    public List<RectInstance> Rects { get; } = new();
    public List<GlyphInstance> Glyphs { get; } = new();
    public List<ImageInstance> Images { get; } = new();
    public List<ShadowInstance> Shadows { get; } = new();
    public List<ShapeInstance> Shapes { get; } = new();

    /// <summary>The clip rects of the last frame as (left, bottom, right, top), slot 0 the whole canvas.</summary>
    public List<Vector4> Clips { get; } = new();

    /// <summary>The size an image is drawn as if it had, for <see cref="ICanvas.DrawImage"/>; images not
    /// named here have none.</summary>
    public Dictionary<string, Size> ImageSizes { get; } = new();

    /// <summary>
    /// Runs one frame and returns with the instance lists holding what would be on screen. The base
    /// skips re-uploading a kind whose staged content is unchanged, so these keep the last uploaded
    /// contents rather than emptying — which is what is still drawn.
    /// </summary>
    public void Frame(Action<ICanvas> draw)
    {
        BeginFrame();
        draw(this);
        EndFrame();
    }

    protected override void UploadRectInstances(RectInstance[] data, int count) => Keep(Rects, data, count);
    protected override void UploadGlyphInstances(GlyphInstance[] data, int count) => Keep(Glyphs, data, count);
    protected override void UploadImageInstances(ImageInstance[] data, int count) => Keep(Images, data, count);
    protected override void UploadShadowInstances(ShadowInstance[] data, int count) => Keep(Shadows, data, count);
    protected override void UploadShapeInstances(ShapeInstance[] data, int count) => Keep(Shapes, data, count);

    protected override void UploadClips(List<Vector4> clips)
    {
        Clips.Clear();
        Clips.AddRange(clips);
    }

    private static void Keep<T>(List<T> kept, T[] data, int count)
    {
        kept.Clear();
        for (var i = 0; i < count; i++) kept.Add(data[i]);
    }

    protected override void UpdateAtlasIfDirty() { }
    /// <summary>The draw calls of the last frame, in the order the GPU would run them.</summary>
    public List<DrawCall> DrawCalls { get; } = new();

    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls)
    {
        DrawCalls.Clear();
        DrawCalls.AddRange(drawCalls);
    }
    protected override void OnResize(int width, int height) { }
    protected override Size GetImageSizeImpl(string imageId) => ImageSizes.TryGetValue(imageId, out var size) ? size : default;
    protected override uint GetImageTextureId(string imageId) => 0;
}
