using LLMit;
using ZGF.Core;

var appView = new AppView();
var guiApp = new GuiApp(new StartupConfig
{
    WindowTitle = "LLMit",
    WindowWidth = 640,
    WindowHeight = 480,
}, appView);

guiApp.Run();