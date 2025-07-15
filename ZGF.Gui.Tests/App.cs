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
        Gui = new Container();
        var bitmap = new Bitmap(startupConfig.WindowWidth, startupConfig.WindowHeight);
        _canvas = new BitmapCanvas(bitmap);
        glClearColor(0f, 0f, 0f, 0f);

        // var columnLayout = new ColumnLayout();
        // columnLayout.Add(new TextButton("Button one"));
        // columnLayout.Add(new TextButton("Button two"));
        // columnLayout.Add(new TextButton("Button three"));

        var header = new Rect
        {
            Position = new RectF(0f, 0f, 0f, 50f),
        };

        var footer = new Rect
        {
            Position = new RectF(0f, 0f, 0f, 20f),
        };

        var layout = new BorderLayout();
        layout.Center = new Rect();
        layout.North = header;
        layout.South = footer;

        Gui.Position = new RectF(0, 0, startupConfig.WindowWidth, startupConfig.WindowHeight);
        Gui.Layout = layout;
        Gui.ApplyStyle(new StyleSheet());
    }

    private Container Gui { get; }

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