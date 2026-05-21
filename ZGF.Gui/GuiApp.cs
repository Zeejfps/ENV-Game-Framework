using System.Runtime.InteropServices;
using GLFW;
using ZGF.AppUtils;
using ZGF.Core;
using ZGF.Fonts;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace LLMit;

public sealed class GuiApp : IDisposable
{
    private readonly IWindowApp _window;
    private readonly RenderedCanvasBase _canvas;
    private readonly FreeTypeFontBackend _fontBackend;
    private readonly Action<Action> _renderFrame;
    private readonly GlfwInputSystem _inputSystem;
    private readonly MultiChildView _gui;
    private readonly QueuedUiDispatcher _dispatcher;
    private readonly ContextMenuManager _contextMenuManager;

    private GuiApp(
        IWindowApp window,
        RenderedCanvasBase canvas,
        FreeTypeFontBackend fontBackend,
        Action<Action> renderFrame,
        Context context,
        MultiChildView content)
    {
        _window = window;
        _canvas = canvas;
        _fontBackend = fontBackend;
        _renderFrame = renderFrame;

        var contextMenuPane = new MultiChildView();
        _inputSystem = new GlfwInputSystem(window.WindowHandle, canvas);
        _contextMenuManager = new ContextMenuManager(contextMenuPane, _inputSystem.InputSystem);
        _dispatcher = new QueuedUiDispatcher();

        context.Canvas = canvas;
        context.AddService(_inputSystem.InputSystem);
        context.AddService(_contextMenuManager);
        context.AddService<IUiDispatcher>(_dispatcher);
#if OSX
        context.AddService<IClipboard>(new OsxClipboard());
#elif WIN
        context.AddService<IClipboard>(new Win32Clipboard());
#else
        context.AddService<IClipboard>(new AppClipboard());
#endif

        _gui = new MultiChildView
        {
            PreferredWidth = canvas.Width,
            PreferredHeight = canvas.Height,
            Context = context,
            Children =
            {
                content,
                contextMenuPane,
            }
        };

        window.OnUpdate += HandleUpdate;
        window.OnResize += HandleResize;
        window.OnFramebufferResize += HandleFramebufferResize;
    }

    public static GuiApp CreateDefault(StartupConfig config, Context context, MultiChildView content)
    {
        var backend = PlatformBackend.Resolve(config);
        return new GuiApp(backend.Window, backend.Canvas, backend.FontBackend, backend.RenderFrame, context, content);
    }

    public void RegisterFont(string family, string path, int pixelSize)
    {
        // pixelSize is in logical points; bake at device pixels so the atlas
        // glyph is high-res on HiDPI (the canvas downscales it when drawing).
        var scaled = (int)MathF.Round(pixelSize * _canvas.DpiScale);
        if (scaled <= 0) scaled = pixelSize;
        var handle = _fontBackend.LoadFontFromFile(PathUtils.ResolveLocalPath(path), scaled);
        _canvas.RegisterFont(family, handle);
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
            Glfw.SetWindowIcon(new Window(_window.WindowHandle), count, images);
        }
        finally
        {
            foreach (var h in handles)
                if (h.IsAllocated) h.Free();
        }
    }

    public void Run() => _window.Run();

    private void HandleUpdate()
    {
        _dispatcher.Drain();
        Render();
        _inputSystem.Update();
        _contextMenuManager.Update();
    }

    private void HandleResize(int width, int height)
    {
        _gui.PreferredWidth = width;
        _gui.PreferredHeight = height;
        _canvas.Resize(width, height);
        Render();
        if (_window is OpenGlApp gl)
            gl.SwapBuffers();
    }

    private void HandleFramebufferResize(int width, int height)
    {
        // GL viewport is in framebuffer pixels; Metal's drawable is sized by MetalApp.
        if (_window is OpenGlApp)
            GL46.glViewport(0, 0, width, height);
    }

    private void Render()
    {
        _renderFrame(PopulateGui);
    }

    private void PopulateGui()
    {
        _gui.LayoutSelf();
        _gui.DrawSelf();
    }

    public void Dispose()
    {
        _window.OnUpdate -= HandleUpdate;
        _window.OnResize -= HandleResize;
        _window.OnFramebufferResize -= HandleFramebufferResize;
        _window.Dispose();
    }
}
