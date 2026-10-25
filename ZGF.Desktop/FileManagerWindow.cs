using System.ComponentModel;
using System.Diagnostics;

namespace ZGF.Desktop;

/// <summary>A window of the OS file manager: Explorer, Finder, or what <c>xdg-open</c> picks.</summary>
public static class FileManagerWindow
{
    private const int FileManagerReplyMilliseconds = 2000;

    /// <summary>Opens a file manager window on the folder at <paramref name="path"/>. Does nothing when
    /// the OS has no file manager to start.</summary>
    public static void Open(string path)
    {
        var start = new ProcessStartInfo(Command()) { UseShellExecute = false };
        start.ArgumentList.Add(path);
        TryStart(start);
    }

    private static string Command()
    {
        if (OperatingSystem.IsWindows())
            return "explorer.exe";
        if (OperatingSystem.IsMacOS())
            return "open";
        return "xdg-open";
    }

    /// <summary>Opens a file manager window on the folder holding the file at <paramref name="path"/>
    /// with the file selected, or on the folder alone where the file manager cannot select it. Opens
    /// the folder when the file is gone, and does nothing when that is gone too.</summary>
    public static void Reveal(string path)
    {
        if (!File.Exists(path))
        {
            OpenFolderOf(path);
            return;
        }
        if (OperatingSystem.IsWindows())
        {
            SelectInExplorer(path);
            return;
        }
        if (OperatingSystem.IsMacOS())
        {
            SelectInFinder(path);
            return;
        }
        Task.Run(() => SelectOnLinux(path));
    }

    private static void OpenFolderOf(string path)
    {
        var folder = Path.GetDirectoryName(path);
        if (folder == null)
            return;
        if (!Directory.Exists(folder))
            return;
        Open(folder);
    }

    /// <summary>Explorer takes the file as part of its one <c>/select,</c> argument, quoted as a whole path.</summary>
    private static void SelectInExplorer(string path)
    {
        var start = new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = false };
        TryStart(start);
    }

    private static void SelectInFinder(string path)
    {
        var start = new ProcessStartInfo("open") { UseShellExecute = false };
        start.ArgumentList.Add("-R");
        start.ArgumentList.Add(path);
        TryStart(start);
    }

    /// <summary>Asks the desktop's file manager over D-Bus to show the file selected, and opens its
    /// folder instead when none answers.</summary>
    private static void SelectOnLinux(string path)
    {
        if (AskFileManagerToShow(path))
            return;
        OpenFolderOf(path);
    }

    private static bool AskFileManagerToShow(string path)
    {
        var start = new ProcessStartInfo("dbus-send") { UseShellExecute = false };
        start.ArgumentList.Add("--session");
        start.ArgumentList.Add("--print-reply");
        start.ArgumentList.Add("--dest=org.freedesktop.FileManager1");
        start.ArgumentList.Add("/org/freedesktop/FileManager1");
        start.ArgumentList.Add("org.freedesktop.FileManager1.ShowItems");
        start.ArgumentList.Add("array:string:" + new Uri(path).AbsoluteUri);
        start.ArgumentList.Add("string:");
        try
        {
            using var process = Process.Start(start);
            if (process == null)
                return false;
            if (!process.WaitForExit(FileManagerReplyMilliseconds))
                return false;
            return process.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static void TryStart(ProcessStartInfo start)
    {
        try
        {
            using var process = Process.Start(start);
        }
        catch (Win32Exception)
        {
        }
    }
}
