using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy;

var settings = AppSettings.Load(SettingsPath(args) ?? AppSettings.DefaultPath);
var workspace = new Workspace(settings);
string[] startup = [..settings.Assemblies.Where(File.Exists), ..args.Where(static a => !a.StartsWith("--", StringComparison.Ordinal))];

new IlSpyApp(workspace, startup).Run(
    new StartupConfig
    {
        WindowWidth = 1400,
        WindowHeight = 900,
        WindowTitle = "ILSpy",
    },
    args.Contains("--gl") ? RenderBackendKind.OpenGl : RenderBackendKind.Auto);

workspace.SaveSettings();

static string? SettingsPath(string[] args)
{
    const string option = "--settings=";
    var arg = args.FirstOrDefault(static a => a.StartsWith(option, StringComparison.Ordinal));
    return arg?[option.Length..];
}
