using ZGF.Geometry;

namespace ZGF.Gui.Views;

/// <summary>
/// Draws an off-screen web view as a texture through the ordinary <see cref="ICanvas"/> image path, so
/// it composes with clipping, opacity, scale, z-order and scroll panes exactly like an
/// <see cref="ImageView"/>. Engine-agnostic: it creates its <see cref="IWebView"/> from an
/// <see cref="IWebViewFactory"/> and knows nothing about CEF. With no factory the view is null and a
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

    public WebViewView(ICanvas canvas, IWebViewFactory? factory)
    {
        _canvas = canvas;
        if (factory == null)
            return;
        var webView = factory.Create(new FrameSink(this));
        WebView = webView;
        Behaviors.Add(new LifetimeBehavior(this, webView));
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

    private void OnFrameUploaded(bool uploaded)
    {
        if (!uploaded)
            return;
        _uploaded = true;
        MarkVisualDirty();
    }

    private sealed class FrameSink(WebViewView view) : IWebViewFrameSink
    {
        public bool AcceptsSharedTextures(SharedTextureKind kind) => view._canvas.SupportsSharedTextures(kind);

        public void OnPixels(int widthPx, int heightPx, ReadOnlySpan<byte> rgbaTopDown) =>
            view.OnFrameUploaded(view._canvas.CreateOrUpdateRgbaImage(view._imageId, widthPx, heightPx, rgbaTopDown));

        public bool OnSharedTexture(in SharedTexture texture)
        {
            var uploaded = view._canvas.CreateOrUpdateSharedTextureImage(view._imageId, texture);
            view.OnFrameUploaded(uploaded);
            return uploaded;
        }
    }

    private sealed class LifetimeBehavior(WebViewView view, IWebView webView) : IViewBehavior
    {
        public void Attach(View _) { }

        public void Detach(View _)
        {
            webView.Dispose();
            view._canvas.RemoveImage(view._imageId);
        }
    }
}
