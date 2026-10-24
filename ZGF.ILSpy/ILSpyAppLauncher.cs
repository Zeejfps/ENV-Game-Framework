using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

public static class ILSpyAppLauncher
{
    public static void LaunchApp(CommandLine commandLine)
    {
        var settings = AppSettings.Load(commandLine.SettingsPath);
        var workspace = new Workspace(settings);
        var screen = new UiState(workspace);
        var app = new IlSpyApp(workspace, screen, commandLine.AssemblyPaths);
        var windowConfig = settings.CreateWindowConfig();
        ImmediateGuiAppHost.Run(app, windowConfig, commandLine.RenderBackend);
    }
}