using LLMit;
using ZGF.Core;

var appView = new AppView();
var guiApp = new GuiApp(new StartupConfig
{
    WindowTitle = "LLMit",
    WindowWidth = 1280,
    WindowHeight = 720,
}, appView);

guiApp.Run();