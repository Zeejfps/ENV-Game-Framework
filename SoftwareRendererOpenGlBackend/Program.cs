using SoftwareRendererOpenGlBackend;
using ZGF.GlfwUtils;

using var app = new QuadTreeRendererApp(new StartupConfig
{
    WindowWidth = 640,
    WindowHeight = 480,
    WindowTitle = "Quad Tree Renderer"
});

app.Run();