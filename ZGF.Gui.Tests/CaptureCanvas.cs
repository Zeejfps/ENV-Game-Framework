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

    /// <summary>
    /// Runs one frame and returns with <see cref="Rects"/> and <see cref="Glyphs"/> holding what
    /// would be on screen. The base skips re-uploading a kind whose staged content is unchanged, so
    /// these keep the last uploaded contents rather than emptying — which is what is still drawn.
    /// </summary>
    public void Frame(Action<ICanvas> draw)
    {
        BeginFrame();
        draw(this);
        EndFrame();
    }

    protected override void UploadRectInstances(RectInstance[] data, int count)
    {
        Rects.Clear();
        for (var i = 0; i < count; i++) Rects.Add(data[i]);
    }

    protected override void UploadGlyphInstances(GlyphInstance[] data, int count)
    {
        Glyphs.Clear();
        for (var i = 0; i < count; i++) Glyphs.Add(data[i]);
    }

    protected override void UploadImageInstances(ImageInstance[] data, int count) { }
    protected override void UploadShadowInstances(ShadowInstance[] data, int count) { }
    protected override void UploadShapeInstances(ShapeInstance[] data, int count) { }
    protected override void UploadClips(List<Vector4> clips) { }
    protected override void UpdateAtlasIfDirty() { }
    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls) { }
    protected override void OnResize(int width, int height) { }
    protected override Size GetImageSizeImpl(string imageId) => default;
    protected override uint GetImageTextureId(string imageId) => 0;
}
