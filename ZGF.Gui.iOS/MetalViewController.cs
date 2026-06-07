using System;
using CoreAnimation;
using Foundation;
using Metal;
using UIKit;
using ZGF.Fonts;
using ZGF.Gui.iOS.Espresso;
using ZGF.Gui.Metal;
using ZGF.Gui.Mobile.Input;
using ZGF.Rendering.Metal;
using AppUtilsAssets = ZGF.AppUtils.EmbeddedAssets;
using CGRect = CoreGraphics.CGRect;
using CGSize = CoreGraphics.CGSize;
using MTLPixelFormat = Metal.MTLPixelFormat;

namespace ZGF.Gui.iOS;

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
    private Context? _context;
    private MobileInputSystem? _mobileInput;
    private EspressoDialingScreen? _screen;
    private MultiChildView? _root;
    private CADisplayLink? _displayLink;

    private byte[] _fontBytes = null!;
    private float _scale = 1f;

    public override void LoadView()
    {
        // Sized to its window by the scene's root-view-controller assignment and tracked on
        // resize; ViewDidLayoutSubviews reads the real bounds before building the canvas.
        _metalView = new MetalUiView(CGRect.Empty)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
        };
        View = _metalView;
    }

    // Display scale without the deprecated UIScreen.MainScreen; the trait environment carries it.
    private float CurrentScale()
    {
        var scale = (float)TraitCollection.DisplayScale;
        return scale > 0f ? scale : 2f;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        _device = MTLDevice.SystemDefault ?? throw new InvalidOperationException("No Metal device available.");
        _queue = _device.CreateCommandQueue() ?? throw new InvalidOperationException("Could not create a Metal command queue.");
        _scale = CurrentScale();

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

        _scale = CurrentScale();
        _metalView.MetalLayer.ContentsScale = _scale;
        _metalView.MetalLayer.DrawableSize = new CGSize(logicalW * _scale, logicalH * _scale);

        if (_canvas == null)
        {
            _canvas = new MetalRenderedCanvas(logicalW, logicalH, _fonts, _defaultFont, _shared, _scale);
            _context = new Context { Canvas = _canvas };

            // Register the shared touch input system before building the tree so the view
            // behaviors (UsePointerController) can resolve it as they attach to the context.
            _mobileInput = new MobileInputSystem(_canvas);
            _context.AddService(_mobileInput);
            _metalView.Input = _mobileInput;

            // Keyboard/text-entry service: the Metal view is itself the keyboard's first responder.
            _context.AddService<ITextInputService>(_metalView);

            // Keyboard size sink: the view reports it, the framework's scroll container avoids it.
            var keyboardInsets = new KeyboardInsets();
            _context.AddService(keyboardInsets);
            _metalView.Insets = keyboardInsets;

            _screen = new EspressoDialingScreen(logicalW, logicalH, _context);
            _root = _screen.Root;
            StartRenderLoop();
        }
        else if (_canvas.Width != logicalW || _canvas.Height != logicalH)
        {
            _canvas.Resize(logicalW, logicalH);
            _screen?.Resize(logicalW, logicalH);
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
        var root = _root;
        if (canvas == null || root == null)
            return;

        _surfaceRenderer.RenderFrame((encoder, commandBuffer) =>
        {
            canvas.BeginFrame();
            root.LayoutSelf();
            root.DrawSelf();
            canvas.EndFrame(encoder, commandBuffer);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _displayLink?.Invalidate();
            _displayLink?.Dispose();
            _displayLink = null;
            _canvas?.Dispose();
            _shared?.Dispose();
            _imageManager?.Dispose();
        }
        base.Dispose(disposing);
    }
}
