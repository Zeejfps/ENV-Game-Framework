using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Platforms.Linux;

[SupportedOSPlatform("linux")]
public sealed class LinuxFilePicker : IFilePicker
{
    private readonly Context _context;
    private readonly string? _zenity;
    private readonly string? _kdialog;
    private int _pickerOpen;

    public LinuxFilePicker(Context context)
    {
        _context = context;
        _zenity = FindOnPath("zenity");
        _kdialog = FindOnPath("kdialog");

        if (_zenity == null && _kdialog == null)
            Console.WriteLine("[FilePicker] No zenity/kdialog found on PATH; folder picker is unavailable.");
    }

    public void PickFolder(string title, Action<string> onPicked) => Pick(title, folder: true, null, onPicked);

    public void PickFile(string title, string? initialDirectory, Action<string> onPicked) =>
        Pick(title, folder: false, initialDirectory, onPicked);

    private void Pick(string title, bool folder, string? initialDirectory, Action<string> onPicked)
    {
        var hasStart = !string.IsNullOrEmpty(initialDirectory);
        // zenity treats a --filename ending in '/' as a starting directory; kdialog takes a start
        // path positionally.
        var kdialogStart = hasStart ? initialDirectory! : ".";

        string tool;
        string[] args;
        if (_zenity != null)
        {
            tool = _zenity;
            List<string> zenityArgs = ["--file-selection", $"--title={title}"];
            if (folder) zenityArgs.Add("--directory");
            if (hasStart) zenityArgs.Add($"--filename={initialDirectory!.TrimEnd('/')}/");
            args = [.. zenityArgs];
        }
        else if (_kdialog != null)
        {
            tool = _kdialog;
            args = folder
                ? ["--getexistingdirectory", kdialogStart, "--title", title]
                : ["--getopenfilename", kdialogStart, "--title", title];
        }
        else
        {
            Console.WriteLine($"[FilePicker] No native picker available. Title: {title}");
            return;
        }

        // The picker stays open until it exits — ignore Browse clicks made while one is up.
        if (Interlocked.CompareExchange(ref _pickerOpen, 1, 0) != 0) return;

        // The picker is a separate process the user can leave open indefinitely. Waiting for it
        // on the UI thread stalls the event loop, so the window manager marks the app
        // unresponsive and offers to kill it. Wait on a worker and post the result back.
        var dispatcher = _context.Require<IUiDispatcher>();
        Task.Run(() =>
        {
            try
            {
                var path = RunPicker(tool, args);
                if (!string.IsNullOrEmpty(path))
                    dispatcher.Post(() => onPicked(path));
            }
            finally
            {
                Interlocked.Exchange(ref _pickerOpen, 0);
            }
        });
    }

    private static string? RunPicker(string tool, string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = tool,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            foreach (var arg in args) psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null) return null;

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Cancel returns a non-zero exit code with empty output for both zenity and kdialog.
            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(stderr))
                    Console.WriteLine($"[FilePicker] {Path.GetFileName(tool)} exited {process.ExitCode}: {stderr.Trim()}");
                return null;
            }

            var path = stdout.Trim();
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[FilePicker] {Path.GetFileName(tool)} failed: {e.Message}");
            return null;
        }
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
