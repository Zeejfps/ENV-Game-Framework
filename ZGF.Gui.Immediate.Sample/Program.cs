using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Sample;

var state = new TodoState();
state.Add("Sketch the immediate-mode API");
state.Add("Render through the canvas");
state.Add("Decide if this beats the retained tree");

new TodoApp(state).Run(
    new StartupConfig
    {
        WindowWidth = 520,
        WindowHeight = 640,
        WindowTitle = "Immediate Mode Prototype",
    },
    args.Contains("--gl") ? RenderBackendKind.OpenGl : RenderBackendKind.Auto);
