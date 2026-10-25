using System.ComponentModel;
using System.Diagnostics;

namespace ZGF.Desktop;

/// <summary>The user's default web browser.</summary>
public static class WebBrowser
{
    /// <summary>Opens <paramref name="url"/> in the default browser. Does nothing when the OS has none
    /// to start.</summary>
    public static void Open(string url)
    {
        var start = StartInfo(url);
        try
        {
            using var process = Process.Start(start);
        }
        catch (Win32Exception)
        {
        }
    }

    /// <summary>Windows opens a URL through the shell; macOS and Linux through <c>open</c> and
    /// <c>xdg-open</c>.</summary>
    private static ProcessStartInfo StartInfo(string url)
    {
        if (OperatingSystem.IsWindows())
            return new ProcessStartInfo(url) { UseShellExecute = true };
        var command = OperatingSystem.IsMacOS() ? "open" : "xdg-open";
        var start = new ProcessStartInfo(command) { UseShellExecute = false };
        start.ArgumentList.Add(url);
        return start;
    }
}
