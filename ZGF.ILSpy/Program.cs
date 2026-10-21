using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy;

var settings = AppSettings.Load(SettingsPath(args) ?? AppSettings.DefaultPath);
var workspace = new Workspace(settings);
string[] startup = [..settings.Assemblies.Where(File.Exists), ..args.Where(static a => !a.StartsWith("--", StringComparison.Ordinal))];

var config = new StartupConfig
{
    WindowWidth = 1400,
    WindowHeight = 900,
    WindowTitle = "ILSpy",
};
if (settings.Window is { } bounds)
    config = bounds.ApplyTo(config);

var app = new IlSpyApp(workspace);
app.Post(() => workspace.Open(startup));
app.Run(config, args.Contains("--gl") ? RenderBackendKind.OpenGl : RenderBackendKind.Auto);

settings.Window = AppSettings.Sanitize(app.WindowBounds);
workspace.SaveSettings();

static string? SettingsPath(string[] args)
{
    const string option = "--settings=";
    var arg = args.FirstOrDefault(static a => a.StartsWith(option, StringComparison.Ordinal));
    return arg?[option.Length..];
}
