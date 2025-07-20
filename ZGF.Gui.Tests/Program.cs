using ZGF.Core;
using ZGF.Gui.Tests;

var app = new App(new StartupConfig
{
    WindowWidth = 1280,
    WindowHeight = 640,
    WindowTitle = "Test",
    IsUndecorated = false,
});
app.Run();