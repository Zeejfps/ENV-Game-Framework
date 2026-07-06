using System.Numerics;
using ZGF.Svg.Raster;

namespace ZGF.Svg;

/// <summary>
/// CPU rasterizer for parsed SVG documents. Reusable but not thread-safe: it owns
/// growable scratch buffers reused across calls, so steady-state rasterization is
/// allocation-free.
/// </summary>
public sealed class SvgRasterizer
{
    private readonly PathBuffer _path = new();
    private readonly CellRasterizer _cells = new();
    private readonly PixelBlender _blender = new();
    private readonly Stroker _stroker = new();
    private readonly Dasher _dasher = new();
    private float[] _dashScratch = new float[16];

    /// <summary>
    /// Rasterizes the document into <paramref name="rgbaDest"/>: tightly packed,
    /// top-down, straight (non-premultiplied) RGBA8, cleared to transparent first.
    /// The viewBox is mapped with the default preserveAspectRatio (xMidYMid meet):
    /// uniform scale, centered, letterboxed with transparency.
    /// </summary>
    public void Rasterize(SvgDocument document, Span<byte> rgbaDest, int widthPx, int heightPx, uint currentColorArgb = 0xFF000000)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthPx);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightPx);
        if (rgbaDest.Length != widthPx * heightPx * 4)
            throw new ArgumentException($"Destination must be exactly {widthPx * heightPx * 4} bytes (w*h*4), got {rgbaDest.Length}.", nameof(rgbaDest));

        _blender.Begin(widthPx, heightPx);

        var scene = document.Scene;
        var viewBox = document.ViewBox;
        if (scene.Commands.Length > 0 && viewBox.Width > 0f && viewBox.Height > 0f)
        {
            var scale = MathF.Min(widthPx / viewBox.Width, heightPx / viewBox.Height);
            var padX = (widthPx - viewBox.Width * scale) * 0.5f;
            var padY = (heightPx - viewBox.Height * scale) * 0.5f;
            var viewBoxToDevice =
                Matrix3x2.CreateTranslation(-viewBox.MinX, -viewBox.MinY) *
                Matrix3x2.CreateScale(scale) *
                Matrix3x2.CreateTranslation(padX, padY);

            _cells.Begin(widthPx, heightPx);

            foreach (ref readonly var cmd in scene.Commands.AsSpan())
            {
                var transform = cmd.Transform * viewBoxToDevice;
                var segments = scene.Segments.AsSpan(cmd.SegStart, cmd.SegCount);

                if (cmd.Fill.Kind != SvgPaintKind.None)
                {
                    _path.Clear();
                    PathFlattener.Flatten(segments, transform, _path);

                    // Fills implicitly close open subpaths, per SVG semantics.
                    for (var c = 0; c < _path.ContourCount; c++)
                    {
                        var contour = _path.GetContour(c, out _);
                        _cells.AddContour(contour, close: true);
                    }
                    _cells.Fill(cmd.FillRule, cmd.Fill.Resolve(currentColorArgb), _blender);
                }

                if (cmd.Stroke.Kind != SvgPaintKind.None && cmd.StrokeWidth > 0f)
                {
                    _path.Clear();
                    PathFlattener.Flatten(segments, transform, _path, PathFlattener.StrokeToleranceDevicePx);

                    // Stroke width scales with the (near-uniform) transform; for
                    // non-uniform transforms this uses the average scale factor.
                    var det = transform.M11 * transform.M22 - transform.M12 * transform.M21;
                    var strokeScale = MathF.Sqrt(MathF.Abs(det));
                    var halfWidth = cmd.StrokeWidth * strokeScale * 0.5f;

                    var dashes = ReadOnlySpan<float>.Empty;
                    var dashOffset = 0f;
                    if (cmd.DashCount > 0)
                    {
                        if (_dashScratch.Length < cmd.DashCount)
                            _dashScratch = new float[cmd.DashCount * 2];
                        for (var i = 0; i < cmd.DashCount; i++)
                            _dashScratch[i] = scene.DashValues[cmd.DashStart + i] * strokeScale;
                        dashes = _dashScratch.AsSpan(0, cmd.DashCount);
                        dashOffset = cmd.DashOffset * strokeScale;
                    }

                    for (var c = 0; c < _path.ContourCount; c++)
                    {
                        var contour = _path.GetContour(c, out var closed);
                        if (dashes.IsEmpty)
                            _stroker.StrokeContour(contour, closed, halfWidth, cmd.Cap, cmd.Join, cmd.MiterLimit, _cells);
                        else
                            _dasher.DashAndStroke(contour, closed, dashes, dashOffset, halfWidth, cmd.Cap, cmd.Join, cmd.MiterLimit, _stroker, _cells);
                    }
                    _cells.Fill(SvgFillRule.NonZero, cmd.Stroke.Resolve(currentColorArgb), _blender);
                }
            }
        }

        _blender.WriteTo(rgbaDest);
    }
}
