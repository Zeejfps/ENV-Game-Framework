using SoftwareRendererModule;
using ZGF.Core;
using ZGF.Geometry;
using static GL46;

namespace ZGF.Gui.Tests;

public sealed class App : OpenGlApp
{
    private readonly BitmapCanvas _canvas;

    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        var framebufferWidth = startupConfig.WindowWidth / 2;
        var framebufferHeight = startupConfig.WindowHeight / 2;
        var bitmap = new Bitmap(framebufferWidth, framebufferHeight);
        _canvas = new BitmapCanvas(bitmap);
        glClearColor(0f, 0f, 0f, 0f);
        
        var header = new Header
        {
            Constraints = new RectF
            {
                Height = 20f
            },
        };
        
        var gui = new BorderLayout
        {
            // Center = new TextButton("Hello World!"),
            North = header,
            Constraints = new RectF(0, 0, framebufferWidth, framebufferHeight)
        };
        gui.ApplyStyleSheet(new StyleSheet());

        Gui = gui;
    }

    private Component Gui { get; }

    protected override void OnUpdate()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        _canvas.BeginFrame();
        Gui.LayoutSelf();
        Gui.DrawSelf(_canvas);
        _canvas.EndFrame();
        EventSystem.Instance.Update();
    }

    protected override void DisposeManagedResources()
    {
    }

    protected override void DisposeUnmanagedResources()
    {
    }
}