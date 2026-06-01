using System.Runtime.InteropServices;
using GLFW;
using ZGF.AppUtils;
using ZGF.Core;
using ZGF.Fonts;
using ZGF.Observable;

namespace ZGF.Gui;

public sealed class GuiApp : IDisposable
{
    private readonly IApp _app;
    private readonly RenderedCanvasBase _mainCanvas;
    private readonly FreeTypeFontBackend _fontBackend;
    private readonly GlSharedResources? _glShared;
    private readonly MetalSharedResources? _metalShared;
    private readonly GlfwInputSystem _mainInput;
    private readonly MultiChildView _root;
    private readonly QueuedUiDispatcher _dispatcher;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly PopupWindowFactory _popupFactory;
    private readonly WindowCoordinates _coordinates;
    private readonly IWindowChrome _windowChrome;

    private GuiApp(
        IApp app,
        RenderedCanvasBase mainCanvas,
        FreeTypeFontBackend fontBackend,
        GlSharedResources? glShared,
        MetalSharedResources? metalShared,
        FontHandle defaultFont,
        Context context,
        View content)
    {
        _app = app;
        _mainCanvas = mainCanvas;
        _fontBackend = fontBackend;
        _glShared = glShared;
        _metalShared = metalShared;
        _mainInput = new GlfwInputSystem(app.MainWindow.WindowHandle, mainCanvas);
        _dispatcher = new QueuedUiDispatcher();

        var decorator = context.Get<IPopupNativeDecorator>() ?? new DefaultNoopDecorator();
        _windowChrome = context.Get<IWindowChrome>() ?? new NoopWindowChrome();
        _coordinates = new WindowCoordinates(app.MainWindow.WindowHandle, mainCanvas);
        _popupFactory = new PopupWindowFactory(
            app, fontBackend, defaultFont, glShared, metalShared, decorator, context,
            mainCanvasForFontRegistry: mainCanvas);

        _contextMenuManager = new ContextMenuManager(_popupFactory, _coordinates, _mainInput.InputSystem, measureContext: context);

        context.Canvas = mainCanvas;
        context.AddService(_mainInput.InputSystem);
        context.AddService(_contextMenuManager);
        context.AddService<IWindowCoordinates>(_coordinates);
        context.AddService<IPopupWindowFactory>(_popupFactory);
        context.AddService<IUiDispatcher>(_dispatcher);

        _root = new MultiChildView
        {
            Width = mainCanvas.Width,
            Height = mainCanvas.Height,
            Context = context,
            Children = { content },
        };

        PlatformBackend.PopulateMain = PopulateGui;

        app.OnTick += HandleTick;
        app.MainWindow.OnResize += HandleResize;
        app.MainWindow.OnFramebufferResize += HandleFramebufferResize;
        app.MainWindow.OnFocusChanged += HandleMainFocusChanged;
    }

    private void HandleMainFocusChanged(bool focused)
    {
        if (focused) return;
        // On macOS, clicking a popup NSWindow makes it key and main loses focus.
        // Treat focus loss as "close menus" only when the focus moved outside
        // our process — i.e., none of our windows are focused. Otherwise the
        // user is interacting with a menu popup we own.
        foreach (var w in _app.Windows)
        {
            if (Glfw.GetWindowAttribute((Window)w.WindowHandle, WindowAttribute.Focused))
                return;
        }
        _contextMenuManager.CloseAllImmediately();
    }

    public static GuiApp CreateDefault(StartupConfig config, Context context, View content)
    {
        var backend = PlatformBackend.Resolve(config);
        return new GuiApp(
            backend.App, backend.MainCanvas, backend.FontBackend,
            backend.GlShared, backend.MetalShared,
            backend.DefaultFont, context, content);
    }

    public void RegisterFont(string family, string path, int pixelSize)
    {
        var handle = _fontBackend.LoadFontFromFile(PathUtils.ResolveLocalPath(path), ScalePixelSize(pixelSize));
        _mainCanvas.RegisterFont(family, handle);
    }

    public void RegisterFont(string family, byte[] fontData, int pixelSize)
    {
        var handle = _fontBackend.LoadFontFromMemory(fontData, ScalePixelSize(pixelSize));
        _mainCanvas.RegisterFont(family, handle);
    }

    private int ScalePixelSize(int pixelSize)
    {
        var scaled = (int)MathF.Round(pixelSize * _mainCanvas.DpiScale);
        return scaled <= 0 ? pixelSize : scaled;
    }

    public void SetIcon(string rgbaPath)
    {
        var bytes = File.ReadAllBytes(PathUtils.ResolveLocalPath(rgbaPath));
        var count = BitConverter.ToInt32(bytes, 0);
        var images = new Image[count];
        var handles = new GCHandle[count];
        var offset = 4;
        try
        {
            for (var i = 0; i < count; i++)
            {
                var w = BitConverter.ToInt32(bytes, offset); offset += 4;
                var h = BitConverter.ToInt32(bytes, offset); offset += 4;
                var len = w * h * 4;
                var pixels = new byte[len];
                Buffer.BlockCopy(bytes, offset, pixels, 0, len);
                offset += len;
                handles[i] = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                images[i] = new Image(w, h, handles[i].AddrOfPinnedObject());
            }
            Glfw.SetWindowIcon(new Window(_app.MainWindow.WindowHandle), count, images);
        }
        finally
        {
            foreach (var h in handles)
                if (h.IsAllocated) h.Free();
        }
    }

    public event Action<int, int>? OnWindowResized;

    /// <summary>
    ///     Switches the main window's native title bar between dark and light
    ///     appearance. No-op on platforms without a registered <see cref="IWindowChrome"/>.
    /// </summary>
    public void SetTitleBarDark(bool dark) =>
        _windowChrome.SetTitleBarTheme(_app.MainWindow.WindowHandle, dark);

    public void Run() => _app.Run();

    private void HandleTick()
    {
        _dispatcher.Drain();
        _mainInput.Update();
        _popupFactory.UpdateActivePopupInput();
        _contextMenuManager.Update();
    }

    private void HandleResize(int width, int height)
    {
        _root.Width = width;
        _root.Height = height;
        _mainCanvas.Resize(width, height);
        _app.MainWindow.MakeContextCurrent();
        _app.MainWindow.RenderNow();
        OnWindowResized?.Invoke(width, height);
    }

    private void HandleFramebufferResize(int width, int height)
    {
        if (_app is OpenGlApp) GL46.glViewport(0, 0, width, height);
    }

    private void PopulateGui()
    {
        _root.LayoutSelf();
        _root.DrawSelf();
    }

    public void Dispose()
    {
        _app.OnTick -= HandleTick;
        _app.MainWindow.OnResize -= HandleResize;
        _app.MainWindow.OnFramebufferResize -= HandleFramebufferResize;
        _app.MainWindow.OnFocusChanged -= HandleMainFocusChanged;
        _popupFactory.Dispose();
        _glShared?.Dispose();
        _metalShared?.Dispose();
        _app.Dispose();
    }

    private sealed class DefaultNoopDecorator : IPopupNativeDecorator
    {
        public void DecoratePopup(IntPtr handle, bool mousePassThrough) { }
        public void BeginCapture(IntPtr handle, Action<ZGF.Geometry.PointI> cb) { }
        public void EndCapture(IntPtr handle) { }
        public void TransferCapture(IntPtr from, IntPtr to, Action<ZGF.Geometry.PointI> cb) { }
    }

    private sealed class NoopWindowChrome : IWindowChrome
    {
        public void SetTitleBarTheme(IntPtr handle, bool dark) { }
    }
}
