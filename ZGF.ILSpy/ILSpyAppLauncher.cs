using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

public static class ILSpyAppLauncher
{
    public static void LaunchApp(CommandLine commandLine)
    {
        var settings = AppSettings.Load(commandLine.SettingsPath);
        var appState = new AppState(settings);
        var screen = IlSpyApp.CreateUiState(appState);
        var app = new IlSpyApp(appState, screen, commandLine.AssemblyPaths);
        var windowConfig = settings.CreateWindowConfig();
        app.Run(windowConfig, commandLine.RenderBackend);
    }
}