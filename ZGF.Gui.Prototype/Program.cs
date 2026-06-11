using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Prototype;
using ZGF.Gui.Prototype.Components;

var builder = GuiApp.CreateBuilder(new StartupConfig
{
    WindowWidth = 480,
    WindowHeight = 640,
    WindowTitle = "Component Prototype",
});

builder.Services.AddSingleton(_ =>
{
    var vm = new TodoViewModel();
    vm.AddTask();
    vm.AddTask();
    vm.AddTask();
    return vm;
});

using var app = builder
    .UseContent(new TodoScreen())
    .Build();

app.Run();
