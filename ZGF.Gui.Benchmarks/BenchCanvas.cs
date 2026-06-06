using System.Numerics;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// A concrete <see cref="RenderedCanvasBase"/> whose GPU upload/draw hooks are
/// no-ops, so benchmarks exercise the base class's per-frame CPU pipeline
/// (stage -> sort -> materialize -> dirty-check -> build draw calls) without a
/// real graphics device.
/// </summary>
public sealed class BenchCanvas : RenderedCanvasBase
{
    public BenchCanvas(int width, int height, FreeTypeFontBackend fonts, FontHandle defaultFont, float dpiScale = 1f)
        : base(width, height, fonts, defaultFont, dpiScale)
    {
    }

    protected override void UploadRectInstances(RectInstance[] data, int count) { }
    protected override void UploadGlyphInstances(GlyphInstance[] data, int count) { }
    protected override void UploadImageInstances(ImageInstance[] data, int count) { }
    protected override void UploadShadowInstances(ShadowInstance[] data, int count) { }
    protected override void UploadClips(List<Vector4> clips) { }
    protected override void UpdateAtlasIfDirty() { }
    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls) { }
    protected override void OnResize(int width, int height) { }
    protected override Size GetImageSizeImpl(string imageId) => default;
    protected override uint GetImageTextureId(string imageId) => 0;
}
