using ZGF.Geometry;

namespace ZGF.Gui.Views;

/// <summary>
/// Draws an off-screen web view as a texture through the ordinary <see cref="ICanvas"/> image path, so
/// it composes with clipping, opacity, scale, z-order and scroll panes exactly like an
/// <see cref="ImageView"/>. Engine-agnostic: it owns an <see cref="IWebView"/> (or none) and knows
/// nothing about CEF. When no <see cref="IWebViewFactory"/> is registered the view is null and a
/// placeholder is drawn instead of crashing.
/// <para>Input forwarding and cursor/focus wiring live in the desktop <c>WebView</c> widget's
/// controller; this view is purely the renderer.</para>
/// </summary>
public sealed class WebViewView : View
{
    private static int _nextId;

    private readonly ICanvas _canvas;
    private readonly string _imageId = $"webview:{Interlocked.Increment(ref _nextId)}";

    private int _lastDeviceWidth;
    private int _lastDeviceHeight;
    private bool _uploaded;

    public WebViewView(ICanvas canvas, IWebView? webView)
    {
        _canvas = canvas;
        WebView = webView;
        if (webView != null)
            Behaviors.Add(new SurfaceBehavior(this, webView));
    }

    /// <summary>The underlying web view, or null when no engine is registered. The desktop widget's
    /// controller forwards input to it and reads <see cref="IWebView.CursorChanged"/> from it.</summary>
    public IWebView? WebView { get; }

    // A web view fills whatever box it is given (like a block element); it has no intrinsic content
    // size, so fall back to the parent-allotted constraint when no explicit size is set.
    protected override float MeasureWidthIntrinsic() =>
        Width.IsSet ? Width.Value : WidthConstraint;

    protected override float MeasureHeightIntrinsic(float availableWidth) =>
        Height.IsSet ? Height.Value : HeightConstraint;

    protected override void OnDrawSelf(ICanvas c)
    {
        if (WebView == null)
        {
            DrawPlaceholder(c);
            return;
        }

        // Keep the browser sized to the laid-out box in device pixels, so text stays crisp on HiDPI.
        var dpi = c.DpiScale;
        var deviceWidth = Math.Max(1, (int)MathF.Round(Position.Width * dpi));
        var deviceHeight = Math.Max(1, (int)MathF.Round(Position.Height * dpi));
        if (deviceWidth != _lastDeviceWidth || deviceHeight != _lastDeviceHeight)
        {
            _lastDeviceWidth = deviceWidth;
            _lastDeviceHeight = deviceHeight;
            WebView.Resize(deviceWidth, deviceHeight, dpi);
        }

        // CEF paints a top-down BGRA buffer we swizzle to RGBA; CreateOrUpdateRgbaImage flips it to the
        // GL/Metal bottom-up convention internally, so it draws right-side up at the logical Position.
        if (WebView.TryAcquireFrame(out var w, out var h, out var rgba) && w > 0 && h > 0)
        {
            c.CreateOrUpdateRgbaImage(_imageId, w, h, rgba);
            _uploaded = true;
        }

        if (!_uploaded)
        {
            DrawPlaceholder(c);
            return;
        }

        c.DrawImage(new DrawImageInputs
        {
            Position = Position,
            ImageId = _imageId,
            ZIndex = GetDrawZIndex(),
            TintColor = 0xFFFFFFFF,
            Rotation = 0f,
        });
    }

    private void DrawPlaceholder(ICanvas c)
    {
        c.DrawRect(new DrawRectInputs
        {
            Position = Position,
            ZIndex = GetDrawZIndex(),
            Style = new RectStyle
            {
                BackgroundColor = WebView == null ? 0xFF3A2030u : 0xFF202020u,
            },
        });
    }

    // Called by the behavior when the engine reports a fresh frame — schedule a repaint. A layout
    // invalidation here is cheap (the box size hasn't changed, so measures return cached values) and is
    // the seam a View subclass has for "please repaint me".
    private void NotifyFrameReady() => SetDirty();

    // Bridges the view's mounted lifetime to the web view: subscribes to frame notifications while
    // mounted, removes the uploaded texture and disposes the engine browser on unmount.
    private sealed class SurfaceBehavior(WebViewView view, IWebView webView) : IViewBehavior
    {
        public void Attach(View _) => webView.FrameReady += view.NotifyFrameReady;

        public void Detach(View _)
        {
            webView.FrameReady -= view.NotifyFrameReady;
            view._canvas.RemoveImage(view._imageId);
            webView.Dispose();
        }
    }
}
