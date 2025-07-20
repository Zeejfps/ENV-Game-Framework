using ZGF.Core;
using ZGF.Gui.Tests;

var app = new App(new StartupConfig
{
    WindowWidth = 720,
    WindowHeight = 640,
    WindowTitle = "Test",
    IsUndecorated = true,
});
app.Run();