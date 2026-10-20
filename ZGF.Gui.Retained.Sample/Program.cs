using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Retained.Sample;

var builder = GuiApp.CreateBuilder(new StartupConfig
{
    WindowWidth = 520,
    WindowHeight = 640,
    WindowTitle = "Retained Mode Comparison",
});

if (args.Contains("--gl"))
    builder.UseRenderBackend(GuiRenderBackendKind.OpenGl);

builder.Context.AddSingleton(_ =>
{
    var vm = new TodoViewModel();
    vm.Add("Sketch the immediate-mode API");
    vm.Add("Render through the canvas");
    vm.Add("Decide if this beats the retained tree");
    return vm;
});

using var app = builder.Build(new TodoScreen());
app.Run();
