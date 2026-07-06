using ZGF.Desktop;
using ZGF.Gui;
using ZGF.Gui.Desktop;
using ZGF.Gui.Prototype;
using ZGF.Gui.Desktop.Components.Controls;

var svgDemo = args.Contains("svg");
var shotIndex = Array.IndexOf(args, "--shot");
var shotPath = shotIndex >= 0 && shotIndex + 1 < args.Length ? args[shotIndex + 1] : null;

var builder = GuiApp.CreateBuilder(new StartupConfig
{
    WindowWidth = svgDemo ? 620 : 480,
    WindowHeight = 640,
    WindowTitle = svgDemo ? "SVG Demo" : "Component Prototype",
});

if (args.Contains("--gl"))
    builder.UseRenderBackend(GuiRenderBackendKind.OpenGl);

builder.Services.AddSingleton(_ =>
{
    var vm = new TodoViewModel();
    vm.AddTask();
    vm.AddTask();
    vm.AddTask();
    return vm;
});

GuiApp? appRef = null;
if (shotPath != null)
{
    builder.UseStartup(ctx =>
    {
        var ticker = ctx.Require<IFrameTicker>();
        var elapsed = 0f;
        Action<float> tick = null!;
        tick = dt =>
        {
            elapsed += dt;
            if (elapsed < 1.5f)
                return;
            ticker.Remove(tick);
            appRef!.CaptureScreenshot(shotPath, () => Environment.Exit(0));
        };
        ticker.Add(tick);
    });
}

using var app = builder
    .UseContent(svgDemo ? new SvgDemoScreen() : new TodoScreen())
    .Build();
appRef = app;

app.Run();
