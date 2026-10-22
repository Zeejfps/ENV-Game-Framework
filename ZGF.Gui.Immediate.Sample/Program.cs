using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Sample;

var state = new TodoState { DarkMode = !args.Contains("--light") };
state.Add("Sketch the immediate-mode API");
state.Add("Render through the canvas");
state.Add("Decide if this beats the retained tree");

var app = new TodoApp(state);
ImmediateGuiAppHost.Run(
    app,
    new StartupConfig
    {
        WindowWidth = 520,
        WindowHeight = 640,
        WindowTitle = "Immediate Mode Prototype",
    },
    args.Contains("--gl") ? RenderBackendKind.OpenGl : RenderBackendKind.Auto);
