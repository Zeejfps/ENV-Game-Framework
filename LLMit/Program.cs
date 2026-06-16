using LLMit.Components;
using LLMit.ViewModels;
using ZGF.Desktop;
using ZGF.Gui.Desktop;

var builder = GuiApp.CreateBuilder(new StartupConfig
{
    WindowTitle = "LLMit",
    WindowWidth = 1280,
    WindowHeight = 720,
});

builder.Services.AddSingleton(_ => new AppViewModel());

using var guiApp = builder.UseContent(new AppScreen()).Build();
guiApp.Run();
