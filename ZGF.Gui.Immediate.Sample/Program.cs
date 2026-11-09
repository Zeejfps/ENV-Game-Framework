using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Sample;

var launch = AutomationOptions.Parse(args);
if (launch is AutomationLaunch.Malformed malformed)
{
    Console.Error.WriteLine(malformed.Reason);
    return 2;
}
var automation = launch is AutomationLaunch.Automated automated ? automated.Options : null;

var state = new TodoState { DarkMode = !args.Contains("--light") };
state.Add("Sketch the immediate-mode API");
state.Add("Render through the canvas");
state.Add("Decide if this beats the retained tree");

var app = new TodoApp(state);
var host = new ImmediateGuiAppHost<DefaultUiState>(
    app,
    new StartupConfig
    {
        WindowWidth = 520,
        WindowHeight = 640,
        WindowTitle = "Immediate Mode Prototype",
    },
    args.Contains("--gl") ? RenderBackendKind.OpenGl : RenderBackendKind.Auto,
    automation);
host.Run();
return 0;
