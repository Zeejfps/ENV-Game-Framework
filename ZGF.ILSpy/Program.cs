using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy;

var settings = AppSettings.Load(AppSettings.DefaultPath);
var workspace = new Workspace(settings);
workspace.Open(settings.Assemblies.Where(File.Exists));
workspace.Open(args.Where(static a => !a.StartsWith("--", StringComparison.Ordinal)));

new IlSpyApp(workspace).Run(
    new StartupConfig
    {
        WindowWidth = 1400,
        WindowHeight = 900,
        WindowTitle = "ILSpy",
    },
    args.Contains("--gl") ? RenderBackendKind.OpenGl : RenderBackendKind.Auto);

workspace.SaveSettings();
