using ZGF.Gui.Immediate;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Preferences;

namespace ZGF.ILSpy;

public static class ILSpyAppLauncher
{
    public static void LaunchApp(CommandLine commandLine)
    {
        var settings = AppSettings.Load(commandLine.SettingsPath);
        var appState = new AppState(settings);
        FollowCommandLine(appState, commandLine);
        var screen = IlSpyApp.CreateUiState(appState);
        var app = new IlSpyApp(appState, screen, commandLine.AssemblyPaths);
        var windowConfig = settings.CreateWindowConfig();
        app.Run(windowConfig, commandLine.RenderBackend);
    }

    /// <summary>Decompiles to the language the command line names, and starts with the assembly list it
    /// names, emptied first when it asks for that.</summary>
    internal static void FollowCommandLine(AppState appState, CommandLine commandLine)
    {
        if (commandLine.Language != null)
            SetLanguage(appState, commandLine.Language.Value);
        if (commandLine.AssemblyList != null)
            StartWithAssemblyList(appState, commandLine.AssemblyList);
        if (commandLine.ClearList)
            EmptyActiveAssemblyList(appState);
    }
}