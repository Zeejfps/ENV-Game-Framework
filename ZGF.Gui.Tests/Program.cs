using ZGF.Desktop;
using ZGF.Gui.Tests;

var app = new App(new StartupConfig
{
    WindowWidth = 1280,
    WindowHeight = 720,
    WindowTitle = "Entity Builder",
    IsUndecorated = false,
});
app.Run();