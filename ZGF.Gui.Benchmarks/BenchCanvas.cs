using System.Numerics;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;

namespace ZGF.Gui.Benchmarks;

/// <summary>
/// A concrete <see cref="RenderedCanvasBase"/> whose GPU upload/draw hooks are
/// no-ops (but optionally capture the uploaded buffers and draw calls), so
/// benchmarks and correctness probes can exercise the base class's per-frame CPU
/// pipeline without a real graphics device.
/// </summary>
public sealed class BenchCanvas : RenderedCanvasBase
{
    public BenchCanvas(int width, int height, FreeTypeFontBackend fonts, FontHandle defaultFont, float dpiScale = 1f)
        : base(width, height, fonts, defaultFont, dpiScale)
    {
    }

    public bool Capture { get; set; }
    public readonly List<RectInstance> CapturedRects = new();
    public readonly List<GlyphInstance> CapturedGlyphs = new();
    public readonly List<ImageInstance> CapturedImages = new();
    public readonly List<ShadowInstance> CapturedShadows = new();
    public readonly List<ShapeInstance> CapturedShapes = new();
    public readonly List<Vector4> CapturedClips = new();
    public readonly List<DrawCall> CapturedDrawCalls = new();
    public int LastDrawCallCount { get; private set; }

    protected override void UploadRectInstances(RectInstance[] data, int count)
    {
        if (!Capture) return;
        CapturedRects.Clear();
        for (var i = 0; i < count; i++) CapturedRects.Add(data[i]);
    }

    protected override void UploadGlyphInstances(GlyphInstance[] data, int count)
    {
        if (!Capture) return;
        CapturedGlyphs.Clear();
        for (var i = 0; i < count; i++) CapturedGlyphs.Add(data[i]);
    }

    protected override void UploadImageInstances(ImageInstance[] data, int count)
    {
        if (!Capture) return;
        CapturedImages.Clear();
        for (var i = 0; i < count; i++) CapturedImages.Add(data[i]);
    }

    protected override void UploadShadowInstances(ShadowInstance[] data, int count)
    {
        if (!Capture) return;
        CapturedShadows.Clear();
        for (var i = 0; i < count; i++) CapturedShadows.Add(data[i]);
    }

    protected override void UploadShapeInstances(ShapeInstance[] data, int count)
    {
        if (!Capture) return;
        CapturedShapes.Clear();
        for (var i = 0; i < count; i++) CapturedShapes.Add(data[i]);
    }

    protected override void UploadClips(ReadOnlySpan<Vector4> clips, int count)
    {
        if (!Capture) return;
        CapturedClips.Clear();
        CapturedClips.AddRange(clips[..count]);
    }

    protected override void UpdateAtlasIfDirty() { }

    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls)
    {
        LastDrawCallCount = drawCalls.Count;
        if (!Capture) return;
        CapturedDrawCalls.Clear();
        foreach (var dc in drawCalls) CapturedDrawCalls.Add(dc);
    }

    protected override void OnResize(int width, int height) { }

    private readonly Dictionary<string, Size> _imageSizes = new();

    public override bool CreateOrUpdateRgbaImage(string imageId, int widthPx, int heightPx, ReadOnlySpan<byte> rgbaTopDown)
    {
        _imageSizes[imageId] = new Size { Width = widthPx, Height = heightPx };
        return true;
    }

    public override bool HasImage(string imageId) => _imageSizes.ContainsKey(imageId);

    public override void RemoveImage(string imageId) => _imageSizes.Remove(imageId);

    protected override bool TryGetImage(string imageId, out uint textureId, out Size size)
    {
        textureId = 0;
        return _imageSizes.TryGetValue(imageId, out size);
    }
}
