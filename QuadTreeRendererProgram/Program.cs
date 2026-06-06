using SoftwareRendererOpenGlBackend;
using ZGF.Desktop;

var app = new QuadTreeRendererApp(new StartupConfig
{
    WindowWidth = 1280,
    WindowHeight = 720,
    WindowTitle = "Quad Tree Renderer"
});

app.Run();