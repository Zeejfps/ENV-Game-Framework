using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Prototype;
using ZGF.Gui.Prototype.Components;

var vm = new TodoViewModel();
vm.AddTask();
vm.AddTask();
vm.AddTask();

var builder = GuiApp.CreateBuilder(new StartupConfig
{
    WindowWidth = 480,
    WindowHeight = 640,
    WindowTitle = "Component Prototype",
});

using var app = builder
    .UseContent(new TodoScreen(vm))
    .Build();

app.Run();
