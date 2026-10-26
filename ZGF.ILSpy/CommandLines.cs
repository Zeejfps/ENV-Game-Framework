using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.Preferences;

namespace ZGF.ILSpy;

/// <summary>Doing what a command line asks: the one the app starts with, and those later runs hand the
/// window already open.</summary>
public static class CommandLines
{
    /// <summary>Decompiles to the language the command line names, and starts with the assembly list it
    /// names, emptied first when it asks for that.</summary>
    public static void FollowStartupCommandLine(AppState appState, CommandLine commandLine)
    {
        if (commandLine.Language != null)
            SetLanguage(appState, commandLine.Language.Value);
        if (commandLine.AssemblyList != null)
            StartWithAssemblyList(appState, commandLine.AssemblyList);
        if (commandLine.ClearList)
            EmptyActiveAssemblyList(appState);
    }

    /// <summary>Does what a command line a later run handed over asks of the open window: decompiles
    /// to its language, switches to its list, added when there is none, clears the list, then opens
    /// its files and shows what it names.</summary>
    public static void FollowHandedCommandLine(AppState appState, CommandLine commandLine)
    {
        if (commandLine.Language != null)
            SetLanguage(appState, commandLine.Language.Value);
        if (commandLine.AssemblyList != null)
            OpenAssemblyListNamed(appState, commandLine.AssemblyList);
        if (commandLine.ClearList)
            CloseAllAssemblies(appState);
        OpenCommandLineFiles(appState, commandLine);
    }

    /// <summary>Opens the files the command line names, then shows the namespace, type or member it
    /// asks to navigate to, or else the last file opened.</summary>
    public static void OpenCommandLineFiles(AppState appState, CommandLine commandLine)
    {
        if (commandLine.NavigateTo == null)
        {
            OpenAssemblies(appState, commandLine.AssemblyPaths);
            return;
        }
        OpenAndShowId(appState, commandLine.AssemblyPaths, commandLine.NavigateTo);
    }
}
