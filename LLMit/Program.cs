using LLMit;
using LLMit.Views;
using ZGF.Desktop;
using ZGF.Gui;
using ZGF.Gui.Desktop;

var context = new Context();
var appView = new AppView();
var guiApp = GuiApp.CreateDefault(new StartupConfig
{
    WindowTitle = "LLMit",
    WindowWidth = 1280,
    WindowHeight = 720,
}, context, appView);

guiApp.Run();