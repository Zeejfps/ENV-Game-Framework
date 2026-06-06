using CoreAnimation;
using CoreGraphics;
using Foundation;
using Metal;
using UIKit;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Metal;
using ZGF.Rendering.Metal;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;

namespace ZGF.Gui.iOS.App;

// Stage 2 host: wires the shared ZGF.Gui.Metal canvas to a CAMetalLayer-backed view and
// drives it from a CADisplayLink. Mirrors the desktop setup in ZGF.Gui.Desktop's
// PlatformBackend.ResolveMetal, minus GLFW: same device/queue/fonts/canvas objects, same
// per-frame BeginFrame -> draw -> EndFrame loop via MetalSurfaceRenderer.
//
// DPI model matches the desktop Metal path exactly: the canvas works in *logical points*
// (its ortho projection spans the logical size) while the CAMetalLayer's drawableSize is in
// *pixels* (logical * scale). The projection bridges the two, and glyphs are baked at device
// pixels so text stays crisp on Retina.
public sealed class MetalViewController : UIViewController
{
    private MetalUiView _metalView = null!;
    private IMTLDevice _device = null!;
    private IMTLCommandQueue _queue = null!;

    private FreeTypeFontBackend _fonts = null!;
    private FontHandle _defaultFont;
    private MetalImageManager _imageManager = null!;
    private MetalSharedResources _shared = null!;
    private MetalSurfaceRenderer _surfaceRenderer = null!;
    private MetalRenderedCanvas? _canvas;
    private CADisplayLink? _displayLink;

    private byte[] _fontBytes = null!;
    private float _scale = 1f;

    public override void LoadView()
    {
        _metalView = new MetalUiView(UIScreen.MainScreen.Bounds);
        View = _metalView;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        _device = MTLDevice.SystemDefault ?? throw new InvalidOperationException("No Metal device available.");
        _queue = _device.CreateCommandQueue() ?? throw new InvalidOperationException("Could not create a Metal command queue.");
        _scale = (float)UIScreen.MainScreen.Scale;

        var layer = _metalView.MetalLayer;
        layer.Device = _device;
        layer.PixelFormat = MTLPixelFormat.BGRA8Unorm; // matches MetalSharedResources' pipeline color format
        layer.FramebufferOnly = true;
        layer.ContentsScale = _scale;

        // Inter is embedded in the platform-independent ZGF.Gui assembly (LogicalName
        // "Inter-Regular.ttf"); reuse it rather than shipping a copy in the app bundle.
        _fontBytes = AppUtilsAssets.LoadBytes(typeof(View).Assembly, "Inter-Regular.ttf");
        _fonts = new FreeTypeFontBackend();
        _defaultFont = _fonts.LoadFontFromMemory(_fontBytes, (int)MathF.Round(16 * _scale));

        var deviceHandle = (IntPtr)_device.Handle;
        var queueHandle = (IntPtr)_queue.Handle;
        var layerHandle = (IntPtr)layer.Handle;

        _imageManager = new MetalImageManager(deviceHandle);
        _shared = new MetalSharedResources(deviceHandle, queueHandle, _fonts, _imageManager);
        _surfaceRenderer = new MetalSurfaceRenderer(new IosMetalSurface(deviceHandle, queueHandle, layerHandle));
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        var bounds = _metalView.Bounds;
        var logicalW = (int)Math.Round(bounds.Width);
        var logicalH = (int)Math.Round(bounds.Height);
        if (logicalW <= 0 || logicalH <= 0)
            return;

        _scale = (float)(_metalView.Window?.Screen?.Scale ?? UIScreen.MainScreen.Scale);
        _metalView.MetalLayer.ContentsScale = _scale;
        _metalView.MetalLayer.DrawableSize = new CGSize(logicalW * _scale, logicalH * _scale);

        if (_canvas == null)
        {
            _canvas = new MetalRenderedCanvas(logicalW, logicalH, _fonts, _defaultFont, _shared, _scale);
            StartRenderLoop();
        }
        else if (_canvas.Width != logicalW || _canvas.Height != logicalH)
        {
            _canvas.Resize(logicalW, logicalH);
        }
    }

    private void StartRenderLoop()
    {
        _displayLink = CADisplayLink.Create(RenderFrame);
        _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
    }

    private void RenderFrame()
    {
        var canvas = _canvas;
        if (canvas == null)
            return;

        _surfaceRenderer.RenderFrame((encoder, commandBuffer) =>
        {
            canvas.BeginFrame();
            DrawScene(canvas);
            canvas.EndFrame(encoder, commandBuffer);
        });
    }

    // First-light scene: a rounded card with centered text. Replaced by a real View tree
    // (Context + MultiChildView) once the render path is confirmed on the simulator.
    private static void DrawScene(MetalRenderedCanvas canvas)
    {
        var w = canvas.Width;
        var h = canvas.Height;

        // Canvas is Y-up: y=0 is the bottom. Place the card near the top.
        var card = new RectF(24, h - 220, w - 48, 160);

        canvas.DrawRect(new DrawRectInputs
        {
            Position = card,
            Style = new RectStyle
            {
                BackgroundColor = 0xFF3478F6,
                BorderRadius = BorderRadiusStyle.All(20f),
            },
            ZIndex = 0,
        });

        canvas.DrawText(new DrawTextInputs
        {
            Position = card,
            Text = "Hello from ZGF.Gui\nrendering on iOS via Metal",
            Style = new TextStyle
            {
                TextColor = 0xFFFFFFFF,
                FontSize = 22f,
                HorizontalAlignment = TextAlignment.Center,
                VerticalAlignment = TextAlignment.Center,
            },
            ZIndex = 1,
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _displayLink?.Invalidate();
            _displayLink?.Dispose();
            _displayLink = null;
            (_canvas as IDisposable)?.Dispose();
            _shared?.Dispose();
            _imageManager?.Dispose();
        }
        base.Dispose(disposing);
    }
}
