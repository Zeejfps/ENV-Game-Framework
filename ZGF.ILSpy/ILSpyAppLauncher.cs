using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.CommandLines;

namespace ZGF.ILSpy;

public static class ILSpyAppLauncher
{
    /// <summary>Hands <paramref name="commandLine"/> to the window already open with the same settings
    /// and leaves, unless it asks for a new window or none is open; else opens the app.</summary>
    public static void LaunchApp(CommandLine commandLine)
    {
        using var instance = AppInstance.Claim(commandLine.SettingsPath);
        if (ShouldHandOver(instance, commandLine) && instance.TryHandOver(commandLine))
            return;
        instance.Listen();
        RunApp(commandLine, instance);
    }

    private static bool ShouldHandOver(AppInstance instance, CommandLine commandLine)
    {
        if (instance.IsFirst)
            return false;
        return !commandLine.NewInstance;
    }

    private static void RunApp(CommandLine commandLine, AppInstance instance)
    {
        var (settings, problem) = AppSettings.Load(commandLine.SettingsPath);
        var appState = new AppState(settings, new BrowseSession());
        Settings.ReportSettingsProblem(appState, problem);
        FollowStartupCommandLine(appState, commandLine);
        var screen = IlSpyApp.CreateUiState(appState);
        var app = new IlSpyApp(appState, screen, commandLine, instance);
        var windowConfig = settings.CreateWindowConfig();
        app.Run(windowConfig, commandLine.RenderBackend);
    }
}