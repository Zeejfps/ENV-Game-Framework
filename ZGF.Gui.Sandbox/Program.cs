using ZGF.Desktop;
using ZGF.Gui.Sandbox;

using var app = new App(new StartupConfig
{
    WindowWidth = 1280,
    WindowHeight = 720,
    WindowTitle = "Entity Builder",
    IsUndecorated = false,
});
app.Run();