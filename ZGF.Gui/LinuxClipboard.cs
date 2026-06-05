using System.Diagnostics;
using System.Runtime.Versioning;

namespace ZGF.Gui;

[SupportedOSPlatform("linux")]
public sealed class LinuxClipboard : IClipboard
{
    private sealed record Backend(string CopyPath, string[] CopyArgs, string PastePath, string[] PasteArgs);

    private readonly Backend? _backend;
    private string? _inProcess;

    public LinuxClipboard()
    {
        _backend = Resolve();
        if (_backend == null)
            Console.WriteLine("[Clipboard] No wl-clipboard/xclip/xsel found on PATH; using in-process clipboard.");
    }

    public void SetText(string text)
    {
        _inProcess = text;
        if (_backend == null) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _backend.CopyPath,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var arg in _backend.CopyArgs) psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null) return;

            process.StandardInput.Write(text);
            process.StandardInput.Close();
            // wl-copy/xclip/xsel fork a daemon to serve the selection, so don't block on exit.
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Clipboard] copy via {_backend.CopyPath} failed: {e.Message}");
        }
    }

    public string? GetText()
    {
        if (_backend == null) return _inProcess;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _backend.PastePath,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var arg in _backend.PasteArgs) psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null) return _inProcess;

            var text = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? text : _inProcess;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Clipboard] paste via {_backend.PastePath} failed: {e.Message}");
            return _inProcess;
        }
    }

    private static Backend? Resolve()
    {
        var wayland = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
            || string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase);

        var wlCopy = FindOnPath("wl-copy");
        var wlPaste = FindOnPath("wl-paste");
        var xclip = FindOnPath("xclip");
        var xsel = FindOnPath("xsel");

        Backend? WlClipboard() => wlCopy != null && wlPaste != null
            ? new Backend(wlCopy, [], wlPaste, ["--no-newline"])
            : null;
        Backend? Xclip() => xclip != null
            ? new Backend(xclip, ["-selection", "clipboard"], xclip, ["-selection", "clipboard", "-o"])
            : null;
        Backend? Xsel() => xsel != null
            ? new Backend(xsel, ["--clipboard", "--input"], xsel, ["--clipboard", "--output"])
            : null;

        // Prefer the backend matching the active session, then fall back to the others.
        return wayland
            ? WlClipboard() ?? Xclip() ?? Xsel()
            : Xclip() ?? Xsel() ?? WlClipboard();
    }

    private static string? FindOnPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path)) return null;
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            if (dir.Length == 0) continue;
            var full = Path.Combine(dir, exe);
            if (File.Exists(full)) return full;
        }
        return null;
    }
}
