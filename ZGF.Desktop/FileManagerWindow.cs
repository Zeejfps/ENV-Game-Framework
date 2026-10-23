using System.ComponentModel;
using System.Diagnostics;

namespace ZGF.Desktop;

/// <summary>A window of the OS file manager: Explorer, Finder, or what <c>xdg-open</c> picks.</summary>
public static class FileManagerWindow
{
    /// <summary>Opens a file manager window on the folder at <paramref name="path"/>. Does nothing when
    /// the OS has no file manager to start.</summary>
    public static void Open(string path)
    {
        var start = new ProcessStartInfo(Command()) { UseShellExecute = false };
        start.ArgumentList.Add(path);
        try
        {
            using var process = Process.Start(start);
        }
        catch (Win32Exception)
        {
        }
    }

    private static string Command()
    {
        if (OperatingSystem.IsWindows())
            return "explorer.exe";
        if (OperatingSystem.IsMacOS())
            return "open";
        return "xdg-open";
    }
}
