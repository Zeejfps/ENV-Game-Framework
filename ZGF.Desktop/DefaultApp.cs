using System.ComponentModel;
using System.Diagnostics;

namespace ZGF.Desktop;

/// <summary>The app the OS opens a kind of file with, such as the user's editor for <c>.json</c>.</summary>
public static class DefaultApp
{
    /// <summary>Opens the file at <paramref name="path"/> in the app the OS opens its kind of file with.
    /// Does nothing when the OS has none to start.</summary>
    public static void OpenFile(string path)
    {
        var start = StartInfo(path);
        try
        {
            using var process = Process.Start(start);
        }
        catch (Win32Exception)
        {
        }
    }

    /// <summary>Windows opens a file through the shell; macOS and Linux through <c>open</c> and
    /// <c>xdg-open</c>.</summary>
    private static ProcessStartInfo StartInfo(string path)
    {
        if (OperatingSystem.IsWindows())
            return new ProcessStartInfo(path) { UseShellExecute = true };
        var command = OperatingSystem.IsMacOS() ? "open" : "xdg-open";
        var start = new ProcessStartInfo(command) { UseShellExecute = false };
        start.ArgumentList.Add(path);
        return start;
    }
}
