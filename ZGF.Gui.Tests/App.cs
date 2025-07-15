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
        var bitmap = new Bitmap(startupConfig.WindowWidth, startupConfig.WindowHeight);
        _canvas = new BitmapCanvas(bitmap);
        glClearColor(0f, 0f, 0f, 0f);

        // var columnLayout = new ColumnLayout();
        // columnLayout.Add(new TextButton("Button one"));
        // columnLayout.Add(new TextButton("Button two"));
        // columnLayout.Add(new TextButton("Button three"));

        var header = new Header
        {
            Constraints = new RectF
            {
                Height = 20f
            },
        };

        var footer = new Rect
        {
            Constraints = new RectF
            {
                Height = 20f,
            },
        };

        // var center = new ColumnLayout();
        // center.Add(new TextButton("Hello World!"));
        // center.Add(new TextButton("Hello World!"));
        // center.Add(new TextButton("Hello World!"));

        var gui = new BorderLayout
        {
            Center = new TextButton("Hello World!"),
            North = header,
            South = footer,
            Constraints = new RectF(0, 0, startupConfig.WindowWidth, startupConfig.WindowHeight)
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