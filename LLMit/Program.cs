using LLMit.Views;
using ZGF.Desktop;
using ZGF.Gui.Desktop;

var builder = GuiApp.CreateBuilder(new StartupConfig
{
    WindowTitle = "LLMit",
    WindowWidth = 1280,
    WindowHeight = 720,
});

using var guiApp = builder.UseContent(new AppView()).Build();
guiApp.Run();
