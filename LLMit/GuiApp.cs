using GLFW;
using ZGF.AppUtils;
using ZGF.Core;
using ZGF.Gui;
using ZGF.Gui.Tests;
using static GL46;

namespace LLMit;

public sealed class GuiApp : OpenGlApp
{
    private readonly GlfwInputSystem _inputSystem;
    private readonly OpenGlRenderedCanvas _canvas;
    private readonly View _gui;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly SizeCallback _windowSizeCallback;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly SizeCallback _framebufferSizeCallback;
    private readonly ContextMenuManager _contextMenuManager;

    public GuiApp(StartupConfig startupConfig, Context context, View content) : base(startupConfig)
    {
        var imageManager = new GlImageManager();
        var contextMenuPane = new View();
        _contextMenuManager = new ContextMenuManager(contextMenuPane);
        var fontFilePath = PathUtils.ResolveLocalPath("Assets/Fonts/Charcoal/Charcoal_p20.xml");
        var bitmapFont = BitmapFont.LoadFromFile(fontFilePath);
        _canvas = new OpenGlRenderedCanvas(
            startupConfig.WindowWidth,
            startupConfig.WindowHeight,
            bitmapFont,
            imageManager
        );

        _inputSystem = new GlfwInputSystem(WindowHandle, _canvas);

        context.Canvas = _canvas;

        context.AddService(_inputSystem.InputSystem);
        context.AddService(_contextMenuManager);
#if OSX
        context.AddService<IClipboard>(new OsxClipboard());
#elif WIN
        context.AddService<IClipboard>(new Win32Clipboard());
#else
        context.AddService<IClipboard>(new AppClipboard());
#endif

        _gui = new View
        {
            PreferredWidth = _canvas.Width,
            PreferredHeight = _canvas.Height,
            Context = context,
            Children =
            {
                content,
                contextMenuPane,
            }
        };

        _windowSizeCallback = HandleWindowSizeChanged;
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        Glfw.SetWindowSizeCallback(WindowHandle, _windowSizeCallback);
        Glfw.SetFramebufferSizeCallback(WindowHandle, _framebufferSizeCallback);

        glClearColor(0, 0, 0, 0);
    }

    protected override void OnUpdate()
    {
        Render();
        _inputSystem.Update();
        _contextMenuManager.Update();
    }

    protected override void DisposeManagedResources()
    {
    }

    protected override void DisposeUnmanagedResources()
    {
    }

    private void HandleWindowSizeChanged(GLFW.Window window, int width, int height)
    {
        _gui.PreferredWidth = width;
        _gui.PreferredHeight = height;
        _canvas.Resize(width, height);
        Render();
        Glfw.SwapBuffers(window);
    }

    private void HandleFramebufferSizeChanged(GLFW.Window window, int width, int height)
    {
        glViewport(0, 0, width, height);
    }

    private void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);

        _canvas.BeginFrame();
        _gui.LayoutSelf();
        _gui.DrawSelf();
        _canvas.EndFrame();
    }
}
