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
        GuiContent = new Container();
        var bitmap = new Bitmap(startupConfig.WindowWidth, startupConfig.WindowHeight);
        _canvas = new BitmapCanvas(bitmap);
        glClearColor(0f, 0f, 0f, 0f);

        var columnLayout = new ColumnLayout();
        columnLayout.Add(new Button());
        columnLayout.Add(new Button());
        columnLayout.Add(new Button());

        GuiContent.Position = new RectF(0, 0, 640, 480);
        GuiContent.Layout = columnLayout;
        GuiContent.ApplyStyle(new StyleSheet());
    }

    private Container GuiContent { get; }

    protected override void OnUpdate()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        _canvas.BeginFrame();
        GuiContent.LayoutSelf();
        GuiContent.DrawSelf(_canvas);
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