using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

public static class ILSpyAppLauncher
{
    public static void LaunchApp(CommandLine commandLine)
    {
        var settings = AppSettings.Load(commandLine.SettingsPath);
        var appState = new AppState(settings);
        var screen = new UiState(appState);
        var app = new IlSpyApp(appState, screen, commandLine.AssemblyPaths);
        var windowConfig = settings.CreateWindowConfig();
        ImmediateGuiAppHost.Run(app, windowConfig, commandLine.RenderBackend);
    }
}