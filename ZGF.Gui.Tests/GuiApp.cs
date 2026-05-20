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
        _contextMenuManager = new ContextMenuManager(contextMenuPane);

        _inputSystem = new GlfwInputSystem(window.WindowHandle, canvas);
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
        var handle = _fontBackend.LoadFontFromFile(PathUtils.ResolveLocalPath(path), pixelSize);
        _canvas.RegisterFont(family, handle);
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
        // GL needs viewport adjusted; Metal's drawable size is updated by MetalApp.
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
