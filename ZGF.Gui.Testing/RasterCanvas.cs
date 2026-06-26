using System.Numerics;
using PngSharp.Api;
using ZGF.Fonts;
using ZGF.Geometry;

namespace ZGF.Gui.Testing;

/// <summary>
/// A GPU-free rasterizer: the same <see cref="RenderedCanvasBase"/> as the real OpenGL canvas, so
/// staging, sorting, clipping, the transform stack, and font shaping are byte-for-byte identical —
/// only the final pixel output differs. It composites the staged draw instances into an RGBA buffer
/// on the CPU and encodes it with <see cref="Png"/>, giving the test harness a real screenshot
/// without a window, a context, or a GPU.
/// <para>
/// Fidelity is "close enough to look at": fills are 1-sample (rects/glyphs hard-edged, shapes lightly
/// anti-aliased). Box shadows and images are not painted (a screenshot is for layout/colour/clipping
/// bugs, which the text snapshot can't express; the snapshot remains the precise source of truth).
/// </para>
/// </summary>
public sealed class RasterCanvas : RenderedCanvasBase
{
    private readonly uint _clearColor;
    private byte[] _rgba;
    private int _fbW, _fbH;

    // Latest materialized instance arrays handed to us by the base via the Upload* hooks. Draw calls
    // index into these by (InstanceStart, InstanceCount). The base reuses the array instances across
    // frames and only re-uploads what changed, so holding the references is safe.
    private RectInstance[] _rects = Array.Empty<RectInstance>();
    private GlyphInstance[] _glyphs = Array.Empty<GlyphInstance>();
    private ShapeInstance[] _shapes = Array.Empty<ShapeInstance>();
    private Vector4[] _clips = Array.Empty<Vector4>();
    private int _clipCount;

    /// <param name="clearColor">Packed ARGB the buffer is cleared to before painting (any area the UI
    /// doesn't cover). Defaults to opaque near-black.</param>
    public RasterCanvas(
        int width, int height,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        float dpiScale = 1f,
        uint clearColor = 0xFF1E1E1Eu)
        : base(width, height, fonts, defaultFont, dpiScale)
    {
        _clearColor = clearColor;
        AllocBuffer();
    }

    private void AllocBuffer()
    {
        _fbW = Math.Max(1, (int)MathF.Round(Width * DpiScale));
        _fbH = Math.Max(1, (int)MathF.Round(Height * DpiScale));
        _rgba = new byte[_fbW * _fbH * 4];
    }

    protected override void OnResize(int width, int height) => AllocBuffer();

    // No image manager headless: images aren't painted. Report a 1x1 size so the base DrawImage's
    // aspect-fit math stays finite (a zero size would divide to NaN).
    protected override Size GetImageSizeImpl(string imageId) => new() { Width = 1, Height = 1 };
    protected override uint GetImageTextureId(string imageId) => 0;

    protected override void UploadRectInstances(RectInstance[] data, int count) => _rects = data;
    protected override void UploadGlyphInstances(GlyphInstance[] data, int count) => _glyphs = data;
    protected override void UploadShapeInstances(ShapeInstance[] data, int count) => _shapes = data;
    protected override void UploadImageInstances(ImageInstance[] data, int count) { }
    protected override void UploadShadowInstances(ShadowInstance[] data, int count) { }

    protected override void UploadClips(List<Vector4> clips)
    {
        if (_clips.Length < clips.Count) _clips = new Vector4[clips.Count];
        for (var i = 0; i < clips.Count; i++) _clips[i] = clips[i];
        _clipCount = clips.Count;
    }

    // We read the atlas pixels directly in RasterGlyph, so there's nothing to upload.
    protected override void UpdateAtlasIfDirty() { }

    protected override void IssueDraws(IReadOnlyList<DrawCall> drawCalls)
    {
        ClearTo(_clearColor);

        for (var i = 0; i < drawCalls.Count; i++)
        {
            var call = drawCalls[i];
            var end = call.InstanceStart + call.InstanceCount;
            switch (call.Kind)
            {
                case DrawKind.Rect:
                    for (var k = call.InstanceStart; k < end; k++) RasterRect(_rects[k]);
                    break;
                case DrawKind.Glyph:
                    for (var k = call.InstanceStart; k < end; k++) RasterGlyph(_glyphs[k]);
                    break;
                case DrawKind.Shape:
                    for (var k = call.InstanceStart; k < end; k++) RasterShape(_shapes[k]);
                    break;
                // Image and Shadow are intentionally not painted (see class summary).
            }
        }
    }

    /// <summary>The rendered framebuffer as top-down RGBA (PNG row order). Call after a
    /// BeginFrame/Draw/EndFrame pass.</summary>
    public byte[] ToRgbaTopDown()
    {
        var top = new byte[_rgba.Length];
        var rowBytes = _fbW * 4;
        for (var y = 0; y < _fbH; y++)
        {
            // Internal buffer is y-up (row 0 = bottom, matching the draw space); PNG is top-down.
            Array.Copy(_rgba, (_fbH - 1 - y) * rowBytes, top, y * rowBytes, rowBytes);
        }
        return top;
    }

    public void SavePng(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        Png.EncodeToFile(Png.CreateRgba(_fbW, _fbH, ToRgbaTopDown()), path);
    }

    // ---------- rasterization ----------

    private void ClearTo(uint argb)
    {
        var (a, r, g, b) = Unpack(argb);
        for (var i = 0; i < _rgba.Length; i += 4)
        {
            _rgba[i] = r; _rgba[i + 1] = g; _rgba[i + 2] = b; _rgba[i + 3] = a;
        }
    }

    private void RasterRect(in RectInstance rect)
    {
        var (clx0, cly0, clx1, cly1) = ClipBounds(rect.ClipIndex);
        float l = rect.Rect.X, b = rect.Rect.Y, r = rect.Rect.X + rect.Rect.Z, t = rect.Rect.Y + rect.Rect.W;

        var x0 = Math.Max((int)MathF.Floor(l), clx0);
        var y0 = Math.Max((int)MathF.Floor(b), cly0);
        var x1 = Math.Min((int)MathF.Ceiling(r), clx1);
        var y1 = Math.Min((int)MathF.Ceiling(t), cly1);

        // BorderSize = (top, right, bottom, left); BorderRadius = (tl, tr, br, bl).
        var bs = rect.BorderSize;
        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            float px = x + 0.5f, py = y + 0.5f;
            if (!InsideRounded(px, py, l, b, r, t, rect.BorderRadius)) continue;

            uint color = rect.BgColor;
            if (bs.X > 0 && py > t - bs.X) color = rect.BorderColorTop;
            else if (bs.Z > 0 && py < b + bs.Z) color = rect.BorderColorBottom;
            else if (bs.W > 0 && px < l + bs.W) color = rect.BorderColorLeft;
            else if (bs.Y > 0 && px > r - bs.Y) color = rect.BorderColorRight;

            var (ca, cr, cg, cb) = Unpack(color);
            Blend(x, y, cr, cg, cb, ca);
        }
    }

    private void RasterGlyph(in GlyphInstance g)
    {
        var (a, r, gg, b) = Unpack(g.Color);
        if (a == 0 || g.Rect.Z <= 0 || g.Rect.W <= 0) return;

        var atlas = FontBackend.AtlasPixels;
        int aw = FontBackend.AtlasWidth, ah = FontBackend.AtlasHeight;

        var (clx0, cly0, clx1, cly1) = ClipBounds(g.ClipIndex);
        var x0 = Math.Max((int)MathF.Floor(g.Rect.X), clx0);
        var y0 = Math.Max((int)MathF.Floor(g.Rect.Y), cly0);
        var x1 = Math.Min((int)MathF.Ceiling(g.Rect.X + g.Rect.Z), clx1);
        var y1 = Math.Min((int)MathF.Ceiling(g.Rect.Y + g.Rect.W), cly1);

        float u0 = g.AtlasUV.X, v0 = g.AtlasUV.Y, uw = g.AtlasUV.Z, vh = g.AtlasUV.W;
        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            var fx = (x + 0.5f - g.Rect.X) / g.Rect.Z;
            var fy = (y + 0.5f - g.Rect.Y) / g.Rect.W;
            if (fx < 0f || fx > 1f || fy < 0f || fy > 1f) continue;

            var ax = (int)((u0 + fx * uw) * aw);
            var ay = (int)((v0 + fy * vh) * ah);
            if (ax < 0 || ax >= aw || ay < 0 || ay >= ah) continue;

            int cov = atlas[ay * aw + ax];
            if (cov == 0) continue;
            Blend(x, y, r, gg, b, a * cov / 255);
        }
    }

    private void RasterShape(in ShapeInstance s)
    {
        var (a, r, g, b) = Unpack(s.Color);
        if (a == 0) return;

        var (clx0, cly0, clx1, cly1) = ClipBounds(s.ClipIndex);
        var x0 = Math.Max((int)MathF.Floor(s.OuterRect.X), clx0);
        var y0 = Math.Max((int)MathF.Floor(s.OuterRect.Y), cly0);
        var x1 = Math.Min((int)MathF.Ceiling(s.OuterRect.X + s.OuterRect.Z), clx1);
        var y1 = Math.Min((int)MathF.Ceiling(s.OuterRect.Y + s.OuterRect.W), cly1);

        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            var d = ShapeDistance(s, x + 0.5f, y + 0.5f);
            if (d > 0.5f) continue;
            int cov = d <= -0.5f ? 255 : (int)((0.5f - d) * 255f);
            Blend(x, y, r, g, b, a * cov / 255);
        }
    }

    // Signed distance to the shape's edge (<=0 inside). Mirrors the shape encodings documented on
    // RenderedCanvasBase.ShapeInstance. Dashing/gradient are ignored (visual-only embellishments).
    private static float ShapeDistance(in ShapeInstance s, float px, float py)
    {
        switch (s.ShapeType)
        {
            case 0: // filled circle
                return Dist(px, py, s.ShapeData.X, s.ShapeData.Y) - s.ShapeData.Z;
            case 1: // ring
                return MathF.Abs(Dist(px, py, s.ShapeData.X, s.ShapeData.Y) - s.ShapeData.Z) - s.HalfWidth;
            case 2: // line / capsule
                return DistToSegment(px, py, s.ShapeData.X, s.ShapeData.Y, s.ShapeData.Z, s.ShapeData.W) - s.HalfWidth;
            default: // quadratic bezier, sampled as a polyline
            {
                float x0 = s.ShapeData.X, y0 = s.ShapeData.Y, cx = s.ShapeData.Z, cy = s.ShapeData.W;
                float x2 = s.ShapeData2.X, y2 = s.ShapeData2.Y;
                var min = float.MaxValue;
                float prevX = x0, prevY = y0;
                const int steps = 16;
                for (var i = 1; i <= steps; i++)
                {
                    var tt = i / (float)steps;
                    var omt = 1f - tt;
                    var bx = omt * omt * x0 + 2f * omt * tt * cx + tt * tt * x2;
                    var by = omt * omt * y0 + 2f * omt * tt * cy + tt * tt * y2;
                    var d = DistToSegment(px, py, prevX, prevY, bx, by);
                    if (d < min) min = d;
                    prevX = bx; prevY = by;
                }
                return min - s.HalfWidth;
            }
        }
    }

    private (int X0, int Y0, int X1, int Y1) ClipBounds(uint clipIndex)
    {
        if (clipIndex >= (uint)_clipCount) return (0, 0, _fbW, _fbH);
        var c = _clips[clipIndex]; // (left, bottom, right, top)
        var x0 = Math.Max(0, (int)MathF.Floor(c.X));
        var y0 = Math.Max(0, (int)MathF.Floor(c.Y));
        var x1 = Math.Min(_fbW, (int)MathF.Ceiling(c.Z));
        var y1 = Math.Min(_fbH, (int)MathF.Ceiling(c.W));
        return (x0, y0, x1, y1);
    }

    private void Blend(int x, int y, byte sr, byte sg, byte sb, int srcA)
    {
        if (srcA <= 0 || (uint)x >= (uint)_fbW || (uint)y >= (uint)_fbH) return;
        if (srcA > 255) srcA = 255;
        var i = (y * _fbW + x) * 4;
        var inv = 255 - srcA;
        _rgba[i] = (byte)((sr * srcA + _rgba[i] * inv) / 255);
        _rgba[i + 1] = (byte)((sg * srcA + _rgba[i + 1] * inv) / 255);
        _rgba[i + 2] = (byte)((sb * srcA + _rgba[i + 2] * inv) / 255);
        _rgba[i + 3] = (byte)Math.Min(255, srcA + _rgba[i + 3] * inv / 255);
    }

    private static bool InsideRounded(float px, float py, float l, float b, float r, float t, Vector4 rad)
    {
        if (px < l || px >= r || py < b || py >= t) return false;
        if (rad.X > 0 && px < l + rad.X && py > t - rad.X) return Dist(px, py, l + rad.X, t - rad.X) <= rad.X;
        if (rad.Y > 0 && px > r - rad.Y && py > t - rad.Y) return Dist(px, py, r - rad.Y, t - rad.Y) <= rad.Y;
        if (rad.Z > 0 && px > r - rad.Z && py < b + rad.Z) return Dist(px, py, r - rad.Z, b + rad.Z) <= rad.Z;
        if (rad.W > 0 && px < l + rad.W && py < b + rad.W) return Dist(px, py, l + rad.W, b + rad.W) <= rad.W;
        return true;
    }

    private static float Dist(float ax, float ay, float bx, float by)
    {
        float dx = ax - bx, dy = ay - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float DistToSegment(float px, float py, float x0, float y0, float x1, float y1)
    {
        float dx = x1 - x0, dy = y1 - y0;
        var lenSq = dx * dx + dy * dy;
        var tt = lenSq <= 1e-6f ? 0f : Math.Clamp(((px - x0) * dx + (py - y0) * dy) / lenSq, 0f, 1f);
        return Dist(px, py, x0 + tt * dx, y0 + tt * dy);
    }

    private static (byte A, byte R, byte G, byte B) Unpack(uint argb) =>
        ((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
}
