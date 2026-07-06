using ZGF.Svg;

namespace ZGF.Gui.Views;

/// <summary>
/// Draws an SVG document rasterized at its laid-out size in device pixels, so icons stay
/// crisp at any size and DPI. While the size is animating it keeps drawing the last
/// rasterization (GPU-scaled) and re-rasterizes once the size has been stable for
/// <see cref="SettleDelaySeconds"/> — unless the mismatch exceeds 2x, which re-rasterizes
/// immediately. A rasterization drawn inside a PushScale subtree is not compensated for
/// the scale and will appear soft.
/// </summary>
public sealed class SvgView : View
{
    private const float SettleDelaySeconds = 0.1f;

    private readonly SvgImageCache _cache;
    private readonly IFrameTicker _ticker;
    private readonly SvgDocument _document;
    private readonly Action<float> _tick;

    private string? _imageId;
    private int _rasterW;
    private int _rasterH;
    private uint _rasterColor;
    private int _pendingW;
    private int _pendingH;
    private float _settleSeconds;
    private bool _tickRegistered;

    public SvgView(SvgImageCache cache, IFrameTicker ticker, SvgDocument document)
    {
        _cache = cache;
        _ticker = ticker;
        _document = document;
        _tick = OnTick;
        Behaviors.Add(new TickCleanupBehavior());
    }

    private uint _currentColor = 0xFF000000;
    /// <summary>The SVG currentColor, baked into the rasterization.</summary>
    public uint CurrentColor
    {
        get => _currentColor;
        set => SetField(ref _currentColor, value);
    }

    public uint TintColor { get; set; } = 0xFFFFFFFF;
    public float Rotation { get; set; }

    protected override float MeasureWidthIntrinsic()
    {
        if (Width.IsSet)
            return Width.Value;
        if (_document.IntrinsicWidth > 0f)
            return _document.IntrinsicWidth;
        return WidthConstraint;
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        // Fixed intrinsic aspect — no reflow with availableWidth, mirroring ImageView.
        if (Height.IsSet)
            return Height.Value;
        if (_document.IntrinsicHeight > 0f)
            return _document.IntrinsicHeight;
        return HeightConstraint;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var (targetW, targetH) = ComputeTargetPixels(c);
        if (targetW <= 0 || targetH <= 0)
            return;

        var color = _currentColor;
        var immediate =
            _imageId == null ||
            color != _rasterColor ||
            targetW > _rasterW * 2 || targetH > _rasterH * 2 ||
            targetW * 2 < _rasterW || targetH * 2 < _rasterH;

        if (immediate)
        {
            RasterizeAt(c, targetW, targetH, color);
        }
        else if (targetW != _rasterW || targetH != _rasterH)
        {
            if (targetW != _pendingW || targetH != _pendingH)
            {
                _pendingW = targetW;
                _pendingH = targetH;
                _settleSeconds = 0f;
            }
            if (_settleSeconds >= SettleDelaySeconds)
                RasterizeAt(c, targetW, targetH, color);
            else
                EnsureTick();
        }
        else
        {
            StopTick();
        }

        c.DrawImage(new DrawImageInputs
        {
            Position = Position,
            ImageId = _imageId!,
            ZIndex = GetDrawZIndex(),
            TintColor = TintColor,
            Rotation = Rotation,
        });
    }

    /// <summary>
    /// The raster size in device pixels for the current layout rect, mirroring DrawImage's
    /// aspect-fit math (integer-truncated rect, centered fit) so the uploaded texture maps
    /// onto the drawn rect 1:1 at integer DPI scales.
    /// </summary>
    private (int W, int H) ComputeTargetPixels(ICanvas c)
    {
        var rectW = (int)Position.Width;
        var rectH = (int)Position.Height;
        if (rectW <= 0 || rectH <= 0 || _document.IntrinsicWidth <= 0f || _document.IntrinsicHeight <= 0f)
            return (0, 0);

        var aspect = _document.IntrinsicWidth / _document.IntrinsicHeight;
        float fitW, fitH;
        if (aspect > (float)rectW / rectH)
        {
            fitW = rectW;
            fitH = rectW / aspect;
        }
        else
        {
            fitH = rectH;
            fitW = rectH * aspect;
        }

        var dpi = c.DpiScale;
        return (
            Math.Max(1, (int)MathF.Round(fitW * dpi)),
            Math.Max(1, (int)MathF.Round(fitH * dpi)));
    }

    private void RasterizeAt(ICanvas c, int widthPx, int heightPx, uint color)
    {
        _imageId = _cache.Acquire(c, _document, widthPx, heightPx, color);
        _rasterW = widthPx;
        _rasterH = heightPx;
        _rasterColor = color;
        _pendingW = widthPx;
        _pendingH = heightPx;
        StopTick();
    }

    private void OnTick(float dtSeconds)
    {
        var before = _settleSeconds;
        _settleSeconds += dtSeconds;
        if (before < SettleDelaySeconds && _settleSeconds >= SettleDelaySeconds)
            SetDirty();
    }

    private void EnsureTick()
    {
        if (_tickRegistered)
            return;
        _tickRegistered = true;
        _settleSeconds = 0f;
        _ticker.Add(_tick);
    }

    private void StopTick()
    {
        if (!_tickRegistered)
            return;
        _tickRegistered = false;
        _ticker.Remove(_tick);
    }

    private sealed class TickCleanupBehavior : IViewBehavior
    {
        public void Attach(View view) { }
        public void Detach(View view) => ((SvgView)view).StopTick();
    }
}
